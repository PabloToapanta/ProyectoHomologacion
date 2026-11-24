using Homologacion.Application.AnalisisInterno;
using Homologacion.Application.Configuracion;
using Homologacion.Application.Equivalencias;
using Homologacion.Domain.Configuracion;
using Homologacion.Domain.Equivalencias;

namespace Homologacion.Application.ConsultaHomologables;

public sealed record CarreraConsulta(Guid Id, string Nombre, string Facultad, bool Vigencia);
public sealed record MallaConsulta(Guid Id, string Nombre, bool Vigente);
public sealed record AsignaturaConsultaDto(Guid Id, string Codigo, string Nombre, int Nivel)
{
    public MallaConsulta? Malla { get; init; }
}
public sealed record AsignaturaConsultaCatalogo(AsignaturaConsultaDto Asignatura, AsignaturaParaAnalisis Contexto);
public sealed record ParConsultaCatalogo(Guid OrigenId, Guid ReceptoraId, RevisionParaAnalisis Revision);
public sealed record ContextoConsultaCatalogo(CarreraConsulta Origen, CarreraConsulta Receptora,
    IReadOnlyList<AsignaturaConsultaCatalogo> Asignaturas, IReadOnlyList<ParConsultaCatalogo> Pares);

/// <summary>HU-09: lectura del catálogo con el JWT del usuario y la clave interna; null si alguna carrera no existe.</summary>
public interface ICatalogoConsultaHomologables
{
    Task<IReadOnlyList<CarreraConsulta>> CarrerasAsync(CancellationToken ct);
    Task<ContextoConsultaCatalogo?> ContextoAsync(Guid origen, Guid receptora, CancellationToken ct);
}

public sealed record ParHomologable(AsignaturaConsultaDto Origen, AsignaturaConsultaDto Receptora, string Estado,
    decimal? PorcentajeAcademico, decimal? PorcentajeHoras, decimal? PorcentajeCreditos, decimal UmbralPct,
    string Justificacion, IReadOnlyList<string> InformacionFaltante);
public sealed record ResultadoConsultaHomologables(CarreraConsulta Origen, CarreraConsulta Receptora, Guid? ConfiguracionVersionId,
    IReadOnlyList<ParHomologable> Pares, IReadOnlyList<string> Advertencias);

public sealed class CarreraConsultaNoEncontradaException() : Exception("No se encontró una de las carreras.");

/// <summary>
/// HU-09: aplica el motor determinista (HU-14) a cada par con correspondencias revisadas entre dos carreras FISEI.
/// CA3: no crea ni modifica correspondencias, no guarda resultados y no llama al agente.
/// </summary>
public sealed class ConsultarHomologables(ICatalogoConsultaHomologables catalogo, EvaluarEquivalencia motor,
    IParametrosHomologacionProvider? parametrosProvider = null)
{
    public const string FaltaRevisionVigente = "correspondencias revisadas desactualizadas o programas incompletos: requiere nueva revisión del par";
    private const string Fisei = "FISEI";

    public Task<IReadOnlyList<CarreraConsulta>> CarrerasAsync(CancellationToken ct) => catalogo.CarrerasAsync(ct);

    public async Task<ResultadoConsultaHomologables> EjecutarAsync(Guid origenId, Guid receptoraId, CancellationToken ct)
    {
        if (origenId == Guid.Empty || receptoraId == Guid.Empty) throw new DatosAnalisisSolicitudException("Seleccione ambas carreras.");
        if (origenId == receptoraId) throw new DatosAnalisisSolicitudException("Seleccione dos carreras distintas.");
        var contexto = await catalogo.ContextoAsync(origenId, receptoraId, ct) ?? throw new CarreraConsultaNoEncontradaException();
        if (contexto.Origen?.Id != origenId || contexto.Receptora?.Id != receptoraId || contexto.Asignaturas is null || contexto.Pares is null
            || contexto.Asignaturas.Any(a => a?.Asignatura is null || a.Contexto is null) || contexto.Pares.Any(p => p?.Revision is null))
            throw new FuenteAnalisisNoDisponibleException();
        if (!EsFisei(contexto.Origen) || !EsFisei(contexto.Receptora))
            throw new DatosAnalisisSolicitudException("La consulta solo compara carreras de la FISEI.");
        var parametros = parametrosProvider is null ? ParametrosHomologacion.Predeterminados() : await parametrosProvider.ObtenerVigentesAsync(ct);
        var porId = contexto.Asignaturas.ToDictionary(a => a.Asignatura.Id);
        var pares = new List<ParHomologable>();
        foreach (var par in contexto.Pares)
        {
            if (!porId.TryGetValue(par.OrigenId, out var origen) || !porId.TryGetValue(par.ReceptoraId, out var receptora)
                || origen.Contexto.CarreraId != origenId || receptora.Contexto.CarreraId != receptoraId)
                throw new FuenteAnalisisNoDisponibleException();
            pares.Add(Evaluar(origen, receptora, par.Revision, parametros));
        }
        var ordenados = pares.OrderBy(p => p.Origen.Nivel).ThenBy(p => p.Origen.Codigo, StringComparer.Ordinal)
            .ThenBy(p => p.Receptora.Nivel).ThenBy(p => p.Receptora.Codigo, StringComparer.Ordinal).ToArray();
        return new(contexto.Origen, contexto.Receptora, parametros.VersionId, ordenados,
        [
            "Consulta informativa del motor determinista sobre correspondencias revisadas del catálogo. No es una homologación ni una aprobación oficial.",
            "Solo se listan pares con revisión registrada; los pares sin revisión no se infieren. Se usan horas y créditos del catálogo, no los de un estudiante."
        ]);
    }

    private ParHomologable Evaluar(AsignaturaConsultaCatalogo origen, AsignaturaConsultaCatalogo receptora, RevisionParaAnalisis revision, ParametrosHomologacion parametros)
    {
        if (!revision.Registrada || !revision.Vigente || revision.Contenidos is null || revision.Resultados is null)
            return Incompleto(origen, receptora, parametros, [FaltaRevisionVigente]);
        var datos = new SolicitudEquivalencia([new(AnalizarSolicitudInterna.Datos(origen.Contexto), true)], AnalizarSolicitudInterna.Datos(receptora.Contexto),
            AnalizarSolicitudInterna.Vinculos(origen.Contexto.Id, revision.Contenidos), AnalizarSolicitudInterna.Vinculos(origen.Contexto.Id, revision.Resultados));
        EvaluacionEquivalencia e;
        try { e = motor.Ejecutar(datos, parametros); }
        catch (DatosEvaluacionInvalidosException error) { return Incompleto(origen, receptora, parametros, [error.Message]); }
        return new(origen.Asignatura, receptora.Asignatura, e.Estado.ToString(), e.CompatibilidadAcademicaPct, e.CompatibilidadHorariaPct,
            e.CompatibilidadCreditosPct, e.UmbralPct, e.Justificacion, e.InformacionFaltante);
    }

    private static ParHomologable Incompleto(AsignaturaConsultaCatalogo o, AsignaturaConsultaCatalogo r, ParametrosHomologacion p, IReadOnlyList<string> faltantes) =>
        new(o.Asignatura, r.Asignatura, nameof(EstadoEquivalencia.InformacionIncompleta), null, null, null, p.UmbralPct,
            "Información incompleta: " + string.Join("; ", faltantes) + ".", faltantes);

    private static bool EsFisei(CarreraConsulta c) => string.Equals(c.Facultad?.Trim(), Fisei, StringComparison.OrdinalIgnoreCase);
}
