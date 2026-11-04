using Homologacion.Application.AnalisisExterno;
using Homologacion.Application.AnalisisInterno;
using Homologacion.Application.Equivalencias;
using Homologacion.Application.RevisionDocente;
using Homologacion.Domain.Equivalencias;
using Homologacion.Domain.RevisionDocente;
using Xunit;

namespace Homologacion.Application.Tests;

public class ReglaRevisionDocenteTests
{
    [Fact]
    public void De_acuerdo_toma_la_sugerencia_completa_del_agente()
    {
        var r = ReglaRevisionDocente.Resolver("DeAcuerdo", null, "  Coincido.  ", "Completo", "No homologable");
        Assert.Equal(("DeAcuerdo", "NoHomologable", "Coincido."), r);
    }

    [Theory]
    [InlineData("Incompleto", null)]
    [InlineData("Fallido", null)]
    [InlineData("SinAnalisis", null)]
    [InlineData("Completo", null)]
    public void De_acuerdo_sin_sugerencia_completa_se_rechaza(string estado, string? sugerencia) =>
        Assert.Throws<RevisionDocenteInvalidaException>(() => ReglaRevisionDocente.Resolver("DeAcuerdo", null, null, estado, sugerencia));

    [Fact]
    public void De_acuerdo_no_admite_decision_distinta_del_cliente() =>
        Assert.Throws<RevisionDocenteInvalidaException>(() => ReglaRevisionDocente.Resolver("DeAcuerdo", "NoHomologable", null, "Completo", "Homologable"));

    [Theory]
    [InlineData("Completo", "Homologable")]
    [InlineData("Incompleto", null)]
    [InlineData("SinAnalisis", null)]
    public void Ajuste_requiere_decision_y_observacion_con_o_sin_agente(string estado, string? sugerencia)
    {
        Assert.Equal(("Ajuste", "NoHomologable", "Contenidos insuficientes."),
            ReglaRevisionDocente.Resolver("Ajuste", "NoHomologable", "Contenidos insuficientes.", estado, sugerencia));
        Assert.Throws<RevisionDocenteInvalidaException>(() => ReglaRevisionDocente.Resolver("Ajuste", "NoHomologable", "  ", estado, sugerencia));
        Assert.Throws<RevisionDocenteInvalidaException>(() => ReglaRevisionDocente.Resolver("Ajuste", null, "Observación", estado, sugerencia));
        Assert.Throws<RevisionDocenteInvalidaException>(() => ReglaRevisionDocente.Resolver("Ajuste", "Quizas", "Observación", estado, sugerencia));
    }

    [Fact]
    public void Postura_desconocida_y_observacion_larga_se_rechazan()
    {
        Assert.Throws<RevisionDocenteInvalidaException>(() => ReglaRevisionDocente.Resolver("Aprobar", "Homologable", "x", "Completo", "Homologable"));
        Assert.Throws<RevisionDocenteInvalidaException>(() => ReglaRevisionDocente.Resolver("Ajuste", "Homologable", new string('x', 2001), "Completo", "Homologable"));
    }
}

public class RegistrarRevisionDocenteTests
{
    private static readonly Guid Solicitud = Guid.Parse("60000000-0000-0000-0000-000000000001");
    private static readonly Guid Aprobada = Guid.Parse("60000000-0000-0000-0000-000000000002");
    private static readonly Guid Receptora = Guid.Parse("60000000-0000-0000-0000-000000000003");
    private static readonly Guid Carrera = Guid.Parse("60000000-0000-0000-0000-000000000004");
    private static readonly Guid Ana = Guid.Parse("60000000-0000-0000-0000-0000000000a1");
    private static readonly Guid Luis = Guid.Parse("60000000-0000-0000-0000-0000000000b2");

    private sealed class Fuentes : ISolicitudesAnalisisInterno, ICatalogoAnalisisInterno
    {
        public string Tipo { get; set; } = "Interno";
        public HashSet<Guid> Asignados { get; } = [Ana, Luis];
        public Guid DocenteActual { get; set; } = Ana;
        public Task<SolicitudParaAnalisis?> ObtenerSolicitudAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(id == Solicitud && Asignados.Contains(DocenteActual)
                ? new SolicitudParaAnalisis(Solicitud, Tipo, Tipo == "Interno" ? Guid.NewGuid() : null, Carrera,
                    [new(Aprobada, Tipo == "Interno" ? Guid.NewGuid() : null, 3, 144, "Aprobada", new DateOnly(2025, 1, 1))])
                : null);
        public Task<AsignaturaParaAnalisis?> ObtenerAsignaturaAsync(Guid id, CancellationToken ct) => Task.FromResult(id == Receptora
            ? new AsignaturaParaAnalisis(Receptora, Carrera, "FISEI", true, 3, 144, "plan", "h", "c", true, [], [])
            : id == Guid.Empty ? null : new AsignaturaParaAnalisis(id, Guid.NewGuid(), "FISEI", true, 3, 144, "plan", "h", "c", true, [], []));
        public Task<RevisionParaAnalisis> ObtenerRevisionAsync(Guid origen, Guid receptora, CancellationToken ct) => throw new InvalidOperationException();
    }

    private sealed class Analisis : IResultadosAnalisisInterno, IResultadosAnalisisExterno
    {
        public List<AnalisisGuardado> Datos { get; } = [];
        public Task GuardarAsync(AnalisisGuardado resultado, CancellationToken ct) { Datos.Add(resultado); return Task.CompletedTask; }
        public Task<IReadOnlyList<AnalisisGuardado>> ListarAsync(Guid solicitudId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<AnalisisGuardado>>(Datos.Where(d => d.Analisis.SolicitudId == solicitudId).ToArray());
        public void Agregar(string estado, string? sugerencia) => Datos.Add(new(DateTimeOffset.UnixEpoch, new(Solicitud, Aprobada, Receptora, estado, "m",
            sugerencia is null ? null : new ResumenAnalisisInterno(100, 100, 100, sugerencia, "j", []),
            new([], null, null, null, null, 80, EstadoEquivalencia.InformacionIncompleta, "", [], [], []), null, true, [])));
    }

    private sealed class Almacen : IRevisionesDocentes
    {
        public bool Fallar { get; set; }
        public List<RevisionDocenteRegistrada> Datos { get; } = [];
        public Task GuardarAsync(RevisionDocenteRegistrada r, CancellationToken ct)
        {
            if (Fallar) throw new AlmacenAnalisisNoDisponibleException();
            Datos.RemoveAll(x => x.SolicitudId == r.SolicitudId && x.AsignaturaAprobadaId == r.AsignaturaAprobadaId
                && x.AsignaturaReceptoraId == r.AsignaturaReceptoraId && x.DocenteId == r.DocenteId);
            Datos.Add(r); return Task.CompletedTask;
        }
        public Task<IReadOnlyList<RevisionDocenteRegistrada>> ListarAsync(Guid solicitudId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<RevisionDocenteRegistrada>>(Datos.Where(x => x.SolicitudId == solicitudId).ToArray());
    }

    private sealed class Reloj : TimeProvider { public override DateTimeOffset GetUtcNow() => new(2026, 11, 4, 18, 0, 0, TimeSpan.Zero); }

    private static (RegistrarRevisionDocente, Fuentes, Analisis, Analisis, Almacen) Caso()
    {
        var f = new Fuentes(); var internos = new Analisis(); var externos = new Analisis(); var a = new Almacen();
        return (new RegistrarRevisionDocente(f, f, internos, externos, a, new Reloj()), f, internos, externos, a);
    }

    private static RevisionDocenteInput Input(string postura = "Ajuste", string? decision = "NoHomologable", string? observacion = "Faltan contenidos.") =>
        new(Aprobada, Receptora, postura, decision, observacion);

    [Fact]
    public async Task De_acuerdo_guarda_la_sugerencia_y_la_fotografia_del_agente()
    {
        var (caso, _, internos, _, almacen) = Caso();
        internos.Agregar("Completo", "Homologable");
        var r = await caso.RegistrarAsync(Solicitud, Ana, "docente.ana", Input("DeAcuerdo", null, null), default);
        Assert.Equal(("DeAcuerdo", "Homologable", "Completo", "Homologable"), (r.Postura, r.Decision, r.EstadoAgente, r.SugerenciaAgente));
        Assert.Equal((Ana, "docente.ana", new DateTimeOffset(2026, 11, 4, 18, 0, 0, TimeSpan.Zero)), (r.DocenteId, r.Docente, r.Fecha));
        Assert.Single(almacen.Datos);
    }

    [Fact]
    public async Task Externo_usa_el_analisis_externo_y_sin_completo_exige_ajuste()
    {
        var (caso, fuentes, internos, externos, _) = Caso();
        fuentes.Tipo = "Externo";
        internos.Agregar("Completo", "Homologable"); // No debe usarse para una solicitud externa.
        externos.Agregar("Incompleto", null);
        await Assert.ThrowsAsync<DatosAnalisisSolicitudException>(() => caso.RegistrarAsync(Solicitud, Ana, "ana", Input("DeAcuerdo", null, null), default));
        var r = await caso.RegistrarAsync(Solicitud, Ana, "ana", Input(), default);
        Assert.Equal(("Ajuste", "NoHomologable", "Incompleto"), (r.Postura, r.Decision, r.EstadoAgente));
    }

    [Fact]
    public async Task Sin_analisis_del_agente_se_registra_la_decision_propia()
    {
        var (caso, _, _, _, _) = Caso();
        var r = await caso.RegistrarAsync(Solicitud, Ana, "ana", Input(decision: "Homologable"), default);
        Assert.Equal(("SinAnalisis", (string?)null, "Homologable"), (r.EstadoAgente, r.SugerenciaAgente, r.Decision));
    }

    [Fact]
    public async Task Cada_docente_tiene_su_registro_y_corregir_reemplaza_el_propio()
    {
        var (caso, fuentes, _, _, almacen) = Caso();
        await caso.RegistrarAsync(Solicitud, Ana, "ana", Input(decision: "Homologable"), default);
        await caso.RegistrarAsync(Solicitud, Ana, "ana", Input(decision: "NoHomologable", observacion: "Corrijo."), default);
        fuentes.DocenteActual = Luis;
        await caso.RegistrarAsync(Solicitud, Luis, "luis", Input(decision: "Homologable", observacion: "Discrepo."), default);
        var lista = await caso.ListarAsync(Solicitud, default);
        Assert.Equal(2, lista.Count);
        Assert.Equal("NoHomologable", lista.Single(x => x.DocenteId == Ana).Decision);
        Assert.Equal("Corrijo.", lista.Single(x => x.DocenteId == Ana).Observacion);
        Assert.Equal(2, almacen.Datos.Count);
    }

    [Fact]
    public async Task Docente_no_asignado_no_registra_ni_consulta()
    {
        var (caso, fuentes, _, _, almacen) = Caso();
        fuentes.DocenteActual = Guid.NewGuid();
        await Assert.ThrowsAsync<SolicitudAnalisisNoEncontradaException>(() => caso.RegistrarAsync(Solicitud, fuentes.DocenteActual, "otro", Input(), default));
        await Assert.ThrowsAsync<SolicitudAnalisisNoEncontradaException>(() => caso.ListarAsync(Solicitud, default));
        Assert.Empty(almacen.Datos);
    }

    [Fact]
    public async Task Pareja_ajena_a_la_solicitud_se_rechaza()
    {
        var (caso, _, _, _, almacen) = Caso();
        await Assert.ThrowsAsync<DatosAnalisisSolicitudException>(() => caso.RegistrarAsync(Solicitud, Ana, "ana", Input() with { AprobadaId = Guid.NewGuid() }, default));
        await Assert.ThrowsAsync<DatosAnalisisSolicitudException>(() => caso.RegistrarAsync(Solicitud, Ana, "ana", Input() with { ReceptoraId = Guid.NewGuid() }, default));
        await Assert.ThrowsAsync<DatosAnalisisSolicitudException>(() => caso.RegistrarAsync(Solicitud, Ana, "ana", Input() with { ReceptoraId = Guid.Empty }, default));
        Assert.Empty(almacen.Datos);
    }

    [Fact]
    public async Task Reglas_invalidas_y_fallo_de_almacen_no_guardan()
    {
        var (caso, _, _, _, almacen) = Caso();
        await Assert.ThrowsAsync<DatosAnalisisSolicitudException>(() => caso.RegistrarAsync(Solicitud, Ana, "ana", Input(observacion: " "), default));
        almacen.Fallar = true;
        await Assert.ThrowsAsync<AlmacenAnalisisNoDisponibleException>(() => caso.RegistrarAsync(Solicitud, Ana, "ana", Input(), default));
        Assert.Empty(almacen.Datos);
    }
}
