using System.Security.Cryptography;
using Catalogo.Application;
using Catalogo.Application.ImportacionProgramas;
using Catalogo.Domain;
using Xunit;

namespace Catalogo.Tests;

public class ImportacionProgramasServiceTests
{
    private static readonly byte[] Pdf = "%PDF-1.7 sintetico"u8.ToArray();

    private sealed class Extractor : IExtractorTextoPdf
    {
        public Dictionary<string, ExtraccionPdf> Por { get; } = [];
        public int Llamadas { get; private set; }
        public ExtraccionPdf Extraer(byte[] contenido, CancellationToken ct)
        {
            Llamadas++;
            var clave = System.Text.Encoding.ASCII.GetString(contenido);
            return Por.TryGetValue(clave, out var r) ? r : new(EstadoExtraccion.Texto, PlanSintetico.Completo().Palabras);
        }
    }

    private sealed class Repo : IAsignaturaRepository, ICarreraRepository, IImportacionProgramasRepository
    {
        public Carrera Carrera { get; } = new("Carrera de prueba", "FISEI", true);
        public List<Asignatura> Asignaturas { get; } = [];
        public List<UnidadCurricular> Unidades { get; } = [];
        public List<ProgramaAnalitico> Programas { get; } = [];
        public Task<Asignatura?> BuscarAsync(Guid id, CancellationToken ct) => Task.FromResult(Asignaturas.SingleOrDefault(a => a.Id == id));
        public Task<IReadOnlyList<Asignatura>> ListarPorCarreraAsync(Guid carreraId, CancellationToken ct) => Task.FromResult<IReadOnlyList<Asignatura>>(Asignaturas);
        public Task<bool> ExisteCodigoAsync(Guid carreraId, string codigo, CancellationToken ct) => Task.FromResult(Asignaturas.Any(a => a.CarreraId == carreraId && a.CodigoNormalizado == codigo));
        public Task CrearAsync(Asignatura asignatura, CancellationToken ct) { Asignaturas.Add(asignatura); return Task.CompletedTask; }
        Task<Carrera?> ICarreraRepository.BuscarAsync(Guid id, CancellationToken ct) => Task.FromResult(id == Carrera.Id ? Carrera : null);
        public Task<IReadOnlyList<Carrera>> ListarAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<Carrera>>([Carrera]);
        public Task<bool> ExisteNombreAsync(string nombre, string facultad, Guid? excluirId, CancellationToken ct) => Task.FromResult(false);
        public Task<bool> TieneAsignaturasAsync(Guid id, CancellationToken ct) => Task.FromResult(false);
        public Task GuardarAsync(Carrera carrera, bool nueva, CancellationToken ct) => Task.CompletedTask;
        public Task EliminarAsync(Carrera carrera, CancellationToken ct) => Task.CompletedTask;
        public Task GuardarAsync(Asignatura asignatura, IReadOnlyList<UnidadCurricular> unidades, ProgramaAnalitico programa, CancellationToken ct)
        {
            Asignaturas.Add(asignatura); Unidades.AddRange(unidades); Programas.Add(programa); return Task.CompletedTask;
        }
        public Task<ProgramaAnalitico?> ObtenerProgramaAsync(Guid asignaturaId, CancellationToken ct) => Task.FromResult(Programas.SingleOrDefault(p => p.AsignaturaId == asignaturaId));
    }

    private sealed class Reloj : TimeProvider { public override DateTimeOffset GetUtcNow() => new(2026, 11, 4, 15, 0, 0, TimeSpan.Zero); }

    private static (ImportacionProgramasService, Repo, Extractor) Crear()
    {
        var repo = new Repo(); var extractor = new Extractor();
        return (new ImportacionProgramasService(extractor, repo, repo, repo, new Reloj()), repo, extractor);
    }

    private static ProgramaConfirmado Confirmado(Guid carrera, string codigo = "SINT-01-02") => new(carrera, codigo, "Asignatura sintetica", 1, 3, 144,
    [
        new(1, "Unidad sintetica uno", 72, "Resultado sintetico uno.", [new("Tema uno", ["Contenido A", "Contenido B"]), new("Tema dos", ["Contenido C"])]),
        new(2, "Unidad sintetica dos", 72, "Resultado sintetico dos.", [new("Tema tres", ["Contenido D"])])
    ]);

    [Fact]
    public async Task Analiza_cada_archivo_por_separado_sin_escribir_en_el_catalogo()
    {
        var (service, repo, extractor) = Crear();
        extractor.Por["%PDF-sin-texto"] = new(EstadoExtraccion.SinTexto, []);
        extractor.Por["%PDF-roto"] = new(EstadoExtraccion.Invalido, []);
        extractor.Por["%PDF-otro"] = new(EstadoExtraccion.Texto, new PlanSintetico().Linea(1, 30, 700, "Factura sintetica").Palabras);
        var lote = await service.AnalizarAsync(repo.Carrera.Id,
        [
            new("valido.pdf", Pdf), new("escaneado.pdf", "%PDF-sin-texto"u8.ToArray()), new("roto.pdf", "%PDF-roto"u8.ToArray()),
            new("otro.pdf", "%PDF-otro"u8.ToArray()), new("falso.pdf", "MZ no es pdf"u8.ToArray())
        ], default);
        Assert.Equal(["Borrador", "SinTexto", "Invalido", "Invalido", "Invalido"], lote.Archivos.Select(a => a.Estado));
        Assert.Equal("SINT-01-02", lote.Archivos[0].Borrador!.Codigo);
        Assert.All(lote.Archivos.Skip(1), a => Assert.Null(a.Borrador));
        Assert.Contains("formato", lote.Archivos[3].Mensaje, StringComparison.Ordinal);
        Assert.Equal(4, extractor.Llamadas);
        Assert.Empty(repo.Asignaturas);
    }

    [Fact]
    public async Task Rechaza_archivos_grandes_y_lotes_fuera_de_limite()
    {
        var (service, repo, extractor) = Crear();
        var grande = new byte[ImportacionProgramasService.MaxBytesArchivo + 1]; "%PDF-"u8.CopyTo(grande);
        var lote = await service.AnalizarAsync(repo.Carrera.Id, [new("grande.pdf", grande), new("valido.pdf", Pdf)], default);
        Assert.Equal(["Invalido", "Borrador"], lote.Archivos.Select(a => a.Estado));
        Assert.Equal(1, extractor.Llamadas);
        await Assert.ThrowsAsync<CatalogoException>(() => service.AnalizarAsync(repo.Carrera.Id, [], default));
        var muchos = Enumerable.Range(0, ImportacionProgramasService.MaxArchivos + 1).Select(i => new ArchivoPrograma($"{i}.pdf", Pdf)).ToArray();
        Assert.Equal("validacion", (await Assert.ThrowsAsync<CatalogoException>(() => service.AnalizarAsync(repo.Carrera.Id, muchos, default))).Codigo);
    }

    [Fact]
    public async Task Acepta_en_un_lote_la_malla_completa_de_cincuenta_programas()
    {
        var (service, repo, _) = Crear();
        var malla = Enumerable.Range(0, 50).Select(i => new ArchivoPrograma($"{i}.pdf", Pdf)).ToArray();
        var lote = await service.AnalizarAsync(repo.Carrera.Id, malla, default);
        Assert.Equal(50, lote.Archivos.Count);
        Assert.All(lote.Archivos, a => Assert.Equal("Borrador", a.Estado));
    }

    [Fact]
    public async Task Carrera_inexistente_es_404_y_codigo_existente_se_advierte()
    {
        var (service, repo, _) = Crear();
        Assert.Equal("no_encontrada", (await Assert.ThrowsAsync<CatalogoException>(() => service.AnalizarAsync(Guid.NewGuid(), [new("a.pdf", Pdf)], default))).Codigo);
        repo.Asignaturas.Add(new Asignatura(repo.Carrera.Id, "SINT-01-02", "Ya registrada", 1, 3, 144));
        var lote = await service.AnalizarAsync(repo.Carrera.Id, [new("a.pdf", Pdf)], default);
        Assert.Contains(lote.Archivos[0].Borrador!.Advertencias, a => a.Contains("ya existe", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Confirmar_guarda_asignatura_unidades_tematicas_y_el_pdf_con_su_huella()
    {
        var (service, repo, _) = Crear();
        var r = await service.ConfirmarAsync(Confirmado(repo.Carrera.Id), new("plan.pdf", Pdf), "admin-sintetico", default);
        var a = Assert.Single(repo.Asignaturas);
        Assert.Equal(r.Id, a.Id);
        Assert.Equal("SINT-01-02", a.Codigo);
        Assert.Equal([1, 2], repo.Unidades.Select(u => u.Numero));
        Assert.Equal(["Tema uno", "Tema dos"], repo.Unidades[0].Tematicas.Select(t => t.Nombre));
        Assert.Equal(3, repo.Unidades[0].Contenidos.Count);
        var p = Assert.Single(repo.Programas);
        Assert.Equal(a.Id, p.AsignaturaId);
        Assert.Equal("plan.pdf", p.NombreArchivo);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(Pdf)), p.Sha256);
        Assert.Equal(Pdf, p.Contenido);
        Assert.Equal("admin-sintetico", p.CargadoPor);
        Assert.Equal(new DateTimeOffset(2026, 11, 4, 15, 0, 0, TimeSpan.Zero), p.FechaCarga);
        Assert.Same(p, await service.ObtenerProgramaAsync(a.Id, default));
    }

    [Fact]
    public async Task Confirmar_no_sobrescribe_codigo_existente()
    {
        var (service, repo, _) = Crear();
        repo.Asignaturas.Add(new Asignatura(repo.Carrera.Id, "SINT-01-02", "Ya registrada", 1, 3, 144));
        var error = await Assert.ThrowsAsync<CatalogoException>(() => service.ConfirmarAsync(Confirmado(repo.Carrera.Id, " sint-01-02 "), new("plan.pdf", Pdf), "admin", default));
        Assert.Equal("duplicado", error.Codigo);
        Assert.Single(repo.Asignaturas);
        Assert.Empty(repo.Programas);
    }

    [Fact]
    public async Task Confirmar_aplica_las_validaciones_de_HU05_y_HU06_sin_guardar_nada()
    {
        var (service, repo, _) = Crear();
        var c = Confirmado(repo.Carrera.Id);
        foreach (var invalido in new[]
        {
            c with { Unidades = [null!] },
            c with { Nivel = 0 },
            c with { Unidades = [] },
            c with { Unidades = [c.Unidades[0], c.Unidades[0]] },
            c with { Unidades = [c.Unidades[0] with { ResultadoAprendizaje = " " }] },
            c with { Unidades = [c.Unidades[0] with { Tematicas = [new("Tema uno", [])] }] }
        })
            Assert.Equal("validacion", (await Assert.ThrowsAsync<CatalogoException>(() => service.ConfirmarAsync(invalido, new("plan.pdf", Pdf), "admin", default))).Codigo);
        Assert.Equal("no_encontrada", (await Assert.ThrowsAsync<CatalogoException>(() => service.ConfirmarAsync(c with { CarreraId = Guid.NewGuid() }, new("plan.pdf", Pdf), "admin", default))).Codigo);
        Assert.Empty(repo.Asignaturas);
        Assert.Empty(repo.Programas);
    }

    [Fact]
    public async Task Confirmar_exige_un_pdf_legible()
    {
        var (service, repo, extractor) = Crear();
        extractor.Por["%PDF-roto"] = new(EstadoExtraccion.Invalido, []);
        foreach (var archivo in new ArchivoPrograma[] { new("falso.pdf", "texto"u8.ToArray()), new("roto.pdf", "%PDF-roto"u8.ToArray()) })
            Assert.Equal("validacion", (await Assert.ThrowsAsync<CatalogoException>(() => service.ConfirmarAsync(Confirmado(repo.Carrera.Id), archivo, "admin", default))).Codigo);
        Assert.Empty(repo.Asignaturas);
    }
}
