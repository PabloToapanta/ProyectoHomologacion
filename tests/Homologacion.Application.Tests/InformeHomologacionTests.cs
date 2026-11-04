using Homologacion.Application.AnalisisExterno;
using Homologacion.Application.AnalisisInterno;
using Homologacion.Application.Informe;
using Homologacion.Application.RevisionDocente;
using Homologacion.Domain.Equivalencias;
using Xunit;

namespace Homologacion.Application.Tests;

public class InformeHomologacionTests
{
    private static readonly Guid Solicitud = Guid.Parse("90000000-0000-0000-0000-000000000001");
    private static readonly Guid A1 = Guid.Parse("90000000-0000-0000-0000-0000000000a1");
    private static readonly Guid A2 = Guid.Parse("90000000-0000-0000-0000-0000000000a2");
    private static readonly Guid A3 = Guid.Parse("90000000-0000-0000-0000-0000000000a3");
    private static readonly Guid R1 = Guid.Parse("90000000-0000-0000-0000-0000000000b1");
    private static readonly Guid R2 = Guid.Parse("90000000-0000-0000-0000-0000000000b2");
    private static readonly Guid Carrera = Guid.Parse("90000000-0000-0000-0000-000000000004");
    private static readonly Guid Ana = Guid.Parse("90000000-0000-0000-0000-0000000000d1");
    private static readonly Guid Luis = Guid.Parse("90000000-0000-0000-0000-0000000000d2");

    private sealed class Fuentes : ISolicitudesAnalisisInterno, IReceptorasAnalisisInterno, IResultadosAnalisisInterno, IResultadosAnalisisExterno, IRevisionesDocentes, IInformesHomologacion
    {
        public bool Asignada { get; set; } = true;
        public string Tipo { get; set; } = "Interno";
        public List<AnalisisGuardado> Agente { get; } = [];
        public List<RevisionDocenteRegistrada> Revisiones { get; } = [];
        public InformeHomologacion? Informe { get; set; }
        public bool AlmacenCaido { get; set; }
        /// <summary>Pausa la escritura indicada dentro del cierre para fijar el orden de dos peticiones simultáneas.</summary>
        public TaskCompletionSource? PausaRevision { get; set; }
        public TaskCompletionSource? PausaInforme { get; set; }
        public TaskCompletionSource EnEscritura { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly SemaphoreSlim cierre = new(1, 1);
        public async Task<T> ConCierreAsync<T>(Guid solicitudId, Func<Task<T>> accion, CancellationToken ct)
        {
            await cierre.WaitAsync(ct);
            try { return await accion(); } finally { cierre.Release(); }
        }
        public Task<SolicitudParaAnalisis?> ObtenerSolicitudAsync(Guid id, CancellationToken ct) => Task.FromResult(id == Solicitud && Asignada
            ? new SolicitudParaAnalisis(Solicitud, Tipo, null, Carrera, new[] { A1, A2, A3 }.Select(a =>new AprobadaParaAnalisis(a, null, 3, 144, "Aprobada", new DateOnly(2025, 1, 1))).ToArray())
            : null);
        public Task<IReadOnlyList<ReceptoraAnalisis>> ListarAsync(Guid carreraId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<ReceptoraAnalisis>>(carreraId == Carrera ? [new(R1, "R-01", "Receptora uno", 1), new(R2, "R-02", "Receptora dos", 2)] : []);
        public Task GuardarAsync(AnalisisGuardado resultado, CancellationToken ct) => Task.CompletedTask;
        Task<IReadOnlyList<AnalisisGuardado>> IResultadosAnalisisInterno.ListarAsync(Guid solicitudId, CancellationToken ct) => Task.FromResult<IReadOnlyList<AnalisisGuardado>>(Tipo == "Interno" ? Agente : []);
        Task<IReadOnlyList<AnalisisGuardado>> IResultadosAnalisisExterno.ListarAsync(Guid solicitudId, CancellationToken ct) => Task.FromResult<IReadOnlyList<AnalisisGuardado>>(Tipo == "Externo" ? Agente : []);
        public async Task GuardarAsync(RevisionDocenteRegistrada r, CancellationToken ct)
        {
            if (PausaRevision is not null) { EnEscritura.TrySetResult(); await PausaRevision.Task; }
            Revisiones.RemoveAll(x => (x.AsignaturaAprobadaId, x.AsignaturaReceptoraId, x.DocenteId) == (r.AsignaturaAprobadaId, r.AsignaturaReceptoraId, r.DocenteId));
            Revisiones.Add(r);
        }
        Task<IReadOnlyList<RevisionDocenteRegistrada>> IRevisionesDocentes.ListarAsync(Guid solicitudId, CancellationToken ct) => Task.FromResult<IReadOnlyList<RevisionDocenteRegistrada>>(Revisiones);
        public Task<InformeHomologacion?> ObtenerAsync(Guid solicitudId, CancellationToken ct) => Task.FromResult(Informe);
        public async Task GuardarAsync(InformeHomologacion informe, CancellationToken ct)
        {
            if (PausaInforme is not null) { EnEscritura.TrySetResult(); await PausaInforme.Task; }
            if (AlmacenCaido) throw new AlmacenAnalisisNoDisponibleException();
            if (Informe is not null) throw new InformeYaGeneradoException();
            Informe = informe;
        }
        public void Revision(Guid docente, Guid aprobada, Guid receptora, string decision, string postura = "Ajuste") =>
            Revisiones.Add(new(Solicitud, aprobada, receptora, docente, docente == Ana ? "docente.ana" : "docente.luis", postura, decision, "Observación sintética.",
                "Completo", "Homologable", new DateTimeOffset(2026, 11, 4, 12, 0, 0, TimeSpan.Zero)));
        public void AnalisisAgente(Guid aprobada, Guid receptora) => Agente.Add(new(DateTimeOffset.UnixEpoch, new(Solicitud, aprobada, receptora, "Completo", "m",
            new ResumenAnalisisInterno(90, 85, 100, "Homologable", "j", []), new([], null, 90, 85, 100, 80, EstadoEquivalencia.Homologable, "", [], [], []), null, true, [])));
    }

    private sealed class Reloj : TimeProvider { public override DateTimeOffset GetUtcNow() => new(2026, 11, 4, 20, 0, 0, TimeSpan.Zero); }
    private static (GenerarInformeHomologacion, Fuentes) Caso() { var f = new Fuentes(); return (new GenerarInformeHomologacion(f, f, f, f, f, f, new Reloj()), f); }

    [Fact]
    public async Task Genera_con_agente_docentes_y_sugerencia_final_por_pareja()
    {
        var (caso, f) = Caso();
        f.AnalisisAgente(A1, R1);
        f.Revision(Ana, A1, R1, "Homologable", "DeAcuerdo"); f.Revision(Luis, A1, R1, "Homologable");
        f.Revision(Ana, A2, R2, "NoHomologable"); f.Revision(Luis, A2, R2, "NoHomologable");
        var i = await caso.GenerarAsync(Solicitud, Ana, "docente.ana", default);
        Assert.Equal((Ana, "docente.ana", new DateTimeOffset(2026, 11, 4, 20, 0, 0, TimeSpan.Zero)), (i.GeneradoPorId, i.GeneradoPor, i.Fecha));
        var p1 = i.Parejas.Single(p => p.AsignaturaAprobadaId == A1);
        Assert.Equal(("Convalidar", "R-01", "Receptora uno"), (p1.SugerenciaFinal, p1.ReceptoraCodigo, p1.ReceptoraNombre));
        Assert.Equal(("Completo", "Homologable", 90m, 85m, 100m), (p1.Agente.Estado, p1.Agente.Sugerencia, p1.Agente.PorcentajeAcademico, p1.Agente.PorcentajeHoras, p1.Agente.PorcentajeCreditos));
        Assert.Equal(["docente.ana", "docente.luis"], p1.Docentes.Select(d => d.Docente));
        var p2 = i.Parejas.Single(p => p.AsignaturaAprobadaId == A2);
        Assert.Equal(("NoConvalidar", "SinAnalisis"), (p2.SugerenciaFinal, p2.Agente.Estado));
        Assert.Equal([A3], i.AprobadasSinAnalisis);
        Assert.Equal((1, 1, 0), (i.Convalidar, i.NoConvalidar, i.SinAcuerdo));
        Assert.Same(i, f.Informe);
    }

    [Fact]
    public async Task Decisiones_distintas_quedan_sin_acuerdo_con_ambas_visibles()
    {
        var (caso, f) = Caso();
        f.Revision(Ana, A1, R1, "Homologable"); f.Revision(Luis, A1, R1, "NoHomologable");
        var i = await caso.GenerarAsync(Solicitud, Luis, "docente.luis", default);
        var p = Assert.Single(i.Parejas);
        Assert.Equal("SinAcuerdo", p.SugerenciaFinal);
        Assert.Equal(["Homologable", "NoHomologable"], p.Docentes.Select(d => d.Decision));
        Assert.Equal(1, i.SinAcuerdo);
        Assert.Contains(i.Advertencias, a => a.Contains("coordinador", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Sin_parejas_o_con_un_docente_faltante_no_se_genera()
    {
        var (caso, f) = Caso();
        await Assert.ThrowsAsync<DatosAnalisisSolicitudException>(() => caso.GenerarAsync(Solicitud, Ana, "ana", default));
        f.Revision(Ana, A1, R1, "Homologable");
        var error = await Assert.ThrowsAsync<DatosAnalisisSolicitudException>(() => caso.GenerarAsync(Solicitud, Ana, "ana", default));
        Assert.Contains("R-01", error.Message, StringComparison.Ordinal);
        Assert.Null(f.Informe);
    }

    [Fact]
    public async Task El_informe_es_unico_y_se_consulta_solo_asignado()
    {
        var (caso, f) = Caso();
        f.Revision(Ana, A1, R1, "Homologable"); f.Revision(Luis, A1, R1, "Homologable");
        var i = await caso.GenerarAsync(Solicitud, Ana, "ana", default);
        await Assert.ThrowsAsync<InformeYaGeneradoException>(() => caso.GenerarAsync(Solicitud, Luis, "luis", default));
        Assert.Same(i, await caso.ConsultarAsync(Solicitud, default));
        f.Asignada = false;
        await Assert.ThrowsAsync<SolicitudAnalisisNoEncontradaException>(() => caso.ConsultarAsync(Solicitud, default));
        await Assert.ThrowsAsync<SolicitudAnalisisNoEncontradaException>(() => caso.GenerarAsync(Solicitud, Ana, "ana", default));
    }

    [Fact]
    public async Task Externo_usa_el_analisis_externo_y_fallo_de_almacen_no_confirma()
    {
        var (caso, f) = Caso();
        f.Tipo = "Externo"; f.AnalisisAgente(A1, R1);
        f.Revision(Ana, A1, R1, "Homologable"); f.Revision(Luis, A1, R1, "Homologable");
        f.AlmacenCaido = true;
        await Assert.ThrowsAsync<AlmacenAnalisisNoDisponibleException>(() => caso.GenerarAsync(Solicitud, Ana, "ana", default));
        Assert.Null(f.Informe);
        f.AlmacenCaido = false;
        var i = await caso.GenerarAsync(Solicitud, Ana, "ana", default);
        Assert.Equal(("Externo", "Completo"), (i.TipoCambio, i.Parejas[0].Agente.Estado));
    }

    [Fact]
    public async Task Con_informe_generado_no_se_corrigen_los_analisis_docentes()
    {
        var f = new Fuentes();
        f.Informe = new(Solicitud, "Interno", DateTimeOffset.UnixEpoch, Ana, "ana", [], [], 0, 0, 0, []);
        var registrar = new RegistrarRevisionDocente(f, new CatalogoFijo(), f, f, f, new Reloj(), f);
        await Assert.ThrowsAsync<InformeYaGeneradoException>(() => registrar.RegistrarAsync(Solicitud, Ana, "ana",
            new(A1, R1, "Ajuste", "Homologable", "Cambio tardío."), default));
    }

    [Fact]
    public async Task Una_correccion_en_curso_se_incluye_en_el_informe_que_se_genera_a_la_vez()
    {
        var (caso, f) = Caso();
        f.Revision(Ana, A1, R1, "Homologable"); f.Revision(Luis, A1, R1, "Homologable");
        var registrar = new RegistrarRevisionDocente(f, new CatalogoFijo(), f, f, f, new Reloj(), f);
        f.PausaRevision = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var correccion = Task.Run(() => registrar.RegistrarAsync(Solicitud, Ana, "docente.ana", new(A1, R1, "Ajuste", "NoHomologable", "Corrección."), default));
        await f.EnEscritura.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var generacion = Task.Run(() => caso.GenerarAsync(Solicitud, Luis, "docente.luis", default));
        await Task.Delay(200);
        Assert.False(generacion.IsCompleted, "El informe no debe leer los análisis mientras se guarda una corrección.");
        f.PausaRevision.SetResult();
        await correccion;
        var informe = await generacion.WaitAsync(TimeSpan.FromSeconds(5));
        var pareja = Assert.Single(informe.Parejas);
        Assert.Equal("NoHomologable", pareja.Docentes.Single(d => d.DocenteId == Ana).Decision);
        Assert.Equal("SinAcuerdo", pareja.SugerenciaFinal);
    }

    [Fact]
    public async Task Una_correccion_que_llega_mientras_se_guarda_el_informe_recibe_conflicto()
    {
        var (caso, f) = Caso();
        f.Revision(Ana, A1, R1, "Homologable"); f.Revision(Luis, A1, R1, "Homologable");
        var registrar = new RegistrarRevisionDocente(f, new CatalogoFijo(), f, f, f, new Reloj(), f);
        f.PausaInforme = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var generacion = Task.Run(() => caso.GenerarAsync(Solicitud, Luis, "docente.luis", default));
        await f.EnEscritura.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var correccion = Task.Run(() => registrar.RegistrarAsync(Solicitud, Ana, "docente.ana", new(A1, R1, "Ajuste", "NoHomologable", "Tarde."), default));
        await Task.Delay(200);
        f.PausaInforme.SetResult();
        var informe = await generacion.WaitAsync(TimeSpan.FromSeconds(5));
        await Assert.ThrowsAsync<InformeYaGeneradoException>(() => correccion.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Equal("Homologable", f.Revisiones.Single(r => r.DocenteId == Ana).Decision);
        Assert.Equal("Convalidar", informe.Parejas[0].SugerenciaFinal);
    }

    private sealed class CatalogoFijo : ICatalogoAnalisisInterno
    {
        public Task<AsignaturaParaAnalisis?> ObtenerAsignaturaAsync(Guid id, CancellationToken ct) =>
            Task.FromResult<AsignaturaParaAnalisis?>(new(id, Carrera, "FISEI", true, 3, 144, "plan", "h", "c", true, [], []));
        public Task<RevisionParaAnalisis> ObtenerRevisionAsync(Guid origen, Guid receptora, CancellationToken ct) => throw new InvalidOperationException();
    }
}
