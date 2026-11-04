using Catalogo.Domain;

namespace Catalogo.Application.ImportacionProgramas;

public enum EstadoExtraccion { Texto, SinTexto, Invalido }
public sealed record ExtraccionPdf(EstadoExtraccion Estado, IReadOnlyList<PalabraPdf> Palabras);
/// <summary>Puerto de extracción de texto con posición; la implementación técnica vive en Infrastructure.</summary>
public interface IExtractorTextoPdf { ExtraccionPdf Extraer(byte[] contenido, CancellationToken ct); }

public sealed record ArchivoPrograma(string Nombre, byte[] Contenido);
/// <summary>Estado: Borrador, SinTexto o Invalido. Solo «Borrador» trae datos y nunca se guarda sin confirmación.</summary>
public sealed record ResultadoAnalisisArchivo(string Archivo, string Estado, string Mensaje, ProgramaBorrador? Borrador);
public sealed record ResultadoAnalisisLote(Guid CarreraId, IReadOnlyList<ResultadoAnalisisArchivo> Archivos);
public sealed record UnidadConfirmada(int Numero, string Nombre, int Horas, string ResultadoAprendizaje, IReadOnlyList<TematicaDatos> Tematicas);
/// <summary>Borrador revisado y corregido por el Administrador.</summary>
public sealed record ProgramaConfirmado(Guid CarreraId, string Codigo, string Nombre, int Nivel, decimal Creditos, int Horas, IReadOnlyList<UnidadConfirmada> Unidades);

public interface IImportacionProgramasRepository
{
    /// <summary>Guarda asignatura, unidades y PDF en una sola transacción.</summary>
    Task GuardarAsync(Asignatura asignatura, IReadOnlyList<UnidadCurricular> unidades, ProgramaAnalitico programa, CancellationToken ct);
    Task<ProgramaAnalitico?> ObtenerProgramaAsync(Guid asignaturaId, CancellationToken ct);
}

/// <summary>HU-08 / Issue #50: carga masiva de programas analíticos FISEI con revisión humana antes de guardar.</summary>
public sealed class ImportacionProgramasService(IExtractorTextoPdf extractor, ICarreraRepository carreras,
    IAsignaturaRepository asignaturas, IImportacionProgramasRepository importaciones, TimeProvider reloj)
{
    public const int MaxArchivos = 20;
    public const int MaxBytesArchivo = ProgramaAnalitico.MaxBytes;
    public const int MaxUnidades = 50;

    public async Task<ResultadoAnalisisLote> AnalizarAsync(Guid carreraId, IReadOnlyList<ArchivoPrograma> archivos, CancellationToken ct)
    {
        if (archivos is null || archivos.Count is 0 or > MaxArchivos)
            throw new CatalogoException("validacion", $"Selecciona entre 1 y {MaxArchivos} archivos PDF.");
        await VerificarCarrera(carreraId, ct);
        var resultados = new List<ResultadoAnalisisArchivo>();
        foreach (var archivo in archivos)
        {
            ct.ThrowIfCancellationRequested();
            var nombre = string.IsNullOrWhiteSpace(archivo.Nombre) ? "archivo" : Path.GetFileName(archivo.Nombre.Replace('\\', '/'));
            var problema = ProblemaArchivo(archivo.Contenido);
            if (problema is not null) { resultados.Add(new(nombre, "Invalido", problema, null)); continue; }
            var extraccion = extractor.Extraer(archivo.Contenido, ct);
            if (extraccion.Estado == EstadoExtraccion.Invalido)
            { resultados.Add(new(nombre, "Invalido", "El PDF está dañado, protegido o excede los límites de procesamiento.", null)); continue; }
            if (extraccion.Estado == EstadoExtraccion.SinTexto)
            { resultados.Add(new(nombre, "SinTexto", "El PDF no contiene texto seleccionable; no se aplica OCR.", null)); continue; }
            var borrador = AnalizadorPlanAnaliticoFisei.Analizar(extraccion.Palabras);
            if (borrador is null)
            { resultados.Add(new(nombre, "Invalido", "El documento no sigue el formato del plan analítico FISEI.", null)); continue; }
            if (borrador.Codigo is not null && await asignaturas.ExisteCodigoAsync(carreraId, Normalizar(borrador.Codigo), ct))
                borrador = borrador with { Advertencias = [.. borrador.Advertencias, "El código ya existe en la carrera: la confirmación será rechazada sin sobrescribir."] };
            resultados.Add(new(nombre, "Borrador", borrador.Faltantes.Count == 0
                ? "Borrador listo para revisar." : "Borrador con datos no encontrados: complételos antes de confirmar.", borrador));
        }
        return new(carreraId, resultados);
    }

    public async Task<AsignaturaDto> ConfirmarAsync(ProgramaConfirmado programa, ArchivoPrograma archivo, string usuario, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(programa);
        var problema = ProblemaArchivo(archivo?.Contenido);
        if (problema is not null || extractor.Extraer(archivo!.Contenido, ct).Estado != EstadoExtraccion.Texto)
            throw new CatalogoException("validacion", problema ?? "El PDF del programa analítico no es legible.");
        // Las mismas reglas de HU-05 y HU-06; todo se valida antes de guardar.
        var asignatura = new Asignatura(programa.CarreraId, programa.Codigo, programa.Nombre, programa.Nivel, programa.Creditos, programa.Horas);
        if (programa.Unidades is null || programa.Unidades.Count is 0 or > MaxUnidades)
            throw new CatalogoException("validacion", $"Registra entre 1 y {MaxUnidades} unidades.");
        if (programa.Unidades.Select(u => u.Numero).Distinct().Count() != programa.Unidades.Count)
            throw new CatalogoException("validacion", "No repitas números de unidad.");
        var unidades = programa.Unidades.Select(u =>
        {
            if (u?.Tematicas is null || u.Tematicas.Any(t => t?.Contenidos is null)) throw new CatalogoException("validacion", "Temáticas incompletas.");
            var unidad = new UnidadCurricular(asignatura.Id, u.Numero, u.Nombre, u.Horas, u.ResultadoAprendizaje, u.Tematicas.SelectMany(t => t.Contenidos).ToList());
            unidad.Clasificar(u.Tematicas);
            return unidad;
        }).ToList();
        await VerificarCarrera(programa.CarreraId, ct);
        if (await asignaturas.ExisteCodigoAsync(programa.CarreraId, asignatura.CodigoNormalizado, ct))
            throw new CatalogoException("duplicado", "Ya existe una asignatura con ese código en la carrera.");
        var pdf = new ProgramaAnalitico(asignatura.Id, archivo.Nombre, archivo.Contenido, reloj.GetUtcNow(), usuario);
        await importaciones.GuardarAsync(asignatura, unidades, pdf, ct);
        return AsignaturaDto.From(asignatura);
    }

    public Task<ProgramaAnalitico?> ObtenerProgramaAsync(Guid asignaturaId, CancellationToken ct) => importaciones.ObtenerProgramaAsync(asignaturaId, ct);

    /// <summary>El servidor no confía en el nombre ni en el tipo declarado: comprueba tamaño y firma del contenido.</summary>
    private static string? ProblemaArchivo(byte[]? contenido)
    {
        if (contenido is null || contenido.Length == 0) return "El archivo está vacío.";
        if (contenido.Length > MaxBytesArchivo) return $"El archivo supera {MaxBytesArchivo / (1024 * 1024)} MB.";
        return contenido.AsSpan().StartsWith("%PDF-"u8) ? null : "El archivo no es un PDF.";
    }

    private static string Normalizar(string codigo) =>
        string.Join(" ", codigo.Normalize(System.Text.NormalizationForm.FormKC).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant();

    private async Task VerificarCarrera(Guid id, CancellationToken ct)
    {
        if (await carreras.BuscarAsync(id, ct) is null) throw new CatalogoException("no_encontrada", "No se encontró la carrera.");
    }
}
