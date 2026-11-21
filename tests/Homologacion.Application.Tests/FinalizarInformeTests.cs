using Homologacion.Application.AnalisisInterno;
using Homologacion.Application.Informe;
using Homologacion.Domain.Auditoria;
using Homologacion.Domain.Finalizacion;
using Xunit;

namespace Homologacion.Application.Tests;

/// <summary>HU-33 / Issue #64: el coordinador revisa el informe remitido, decide las parejas «Sin acuerdo» y finaliza la solicitud.</summary>
public class FinalizarInformeTests
{
    private static readonly Guid Solicitud = Guid.Parse("93300000-0000-0000-0000-000000000001");
    private static readonly Guid Ana = Guid.Parse("93300000-0000-0000-0000-0000000000d1");
    private static readonly Guid Luis = Guid.Parse("93300000-0000-0000-0000-0000000000d2");
    private static readonly Guid Coordinadora = Guid.Parse("93300000-0000-0000-0000-0000000000c1");
    private static readonly Guid AprobadaA = Guid.Parse("93300000-0000-0000-0000-00000000000a");
    private static readonly Guid AprobadaB = Guid.Parse("93300000-0000-0000-0000-00000000000b");
    private static readonly Guid ReceptoraA = Guid.Parse("93300000-0000-0000-0000-0000000000ea");
    private static readonly Guid ReceptoraB = Guid.Parse("93300000-0000-0000-0000-0000000000eb");
    private static readonly DateTimeOffset Ahora = new(2026, 11, 23, 15, 0, 0, TimeSpan.Zero);

    /// <summary>Simula la transacción de ConCierreAsync: si la acción falla, se descartan eventos y decisiones de esa acción.</summary>
    private sealed class Fuentes : IInformesHomologacion, IAuditoriaInformes, IDecisionesCoordinador, IEstadoSolicitudes
    {
        public InformeHomologacion? Informe { get; set; }
        public List<EventoInforme> Eventos { get; } = [];
        public List<DecisionCoordinador> Decisiones { get; } = [];
        public Exception? FalloEstado { get; set; }
        public int MarcasFinalizada { get; private set; }
        public int Cierres { get; private set; }

        public Task<InformeHomologacion?> ObtenerAsync(Guid solicitudId, CancellationToken ct) => Task.FromResult(solicitudId == Solicitud ? Informe : null);
        public Task GuardarAsync(InformeHomologacion informe, CancellationToken ct) => throw new InvalidOperationException();
        public async Task<T> ConCierreAsync<T>(Guid solicitudId, Func<Task<T>> accion, CancellationToken ct)
        {
            Cierres++;
            var (eventos, decisiones) = (Eventos.ToArray(), Decisiones.ToArray());
            try { return await accion(); }
            catch
            {
                Eventos.Clear(); Eventos.AddRange(eventos);
                Decisiones.Clear(); Decisiones.AddRange(decisiones);
                throw;
            }
        }
        public Task RegistrarAsync(EventoInforme evento, CancellationToken ct)
        {
            if (Eventos.Any(e => e.SolicitudId == evento.SolicitudId && e.Tipo == evento.Tipo)) throw new EventoInformeInvalidoException("Repetido.");
            Eventos.Add(evento); return Task.CompletedTask;
        }
        Task<IReadOnlyList<EventoInforme>> IAuditoriaInformes.ListarAsync(Guid solicitudId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<EventoInforme>>(Eventos.Where(e => e.SolicitudId == solicitudId).ToArray());
        public Task GuardarAsync(DecisionCoordinador decision, CancellationToken ct)
        {
            Decisiones.RemoveAll(d => d.SolicitudId == decision.SolicitudId && d.AsignaturaAprobadaId == decision.AsignaturaAprobadaId
                && d.AsignaturaReceptoraId == decision.AsignaturaReceptoraId);
            Decisiones.Add(decision); return Task.CompletedTask;
        }
        Task<IReadOnlyList<DecisionCoordinador>> IDecisionesCoordinador.ListarAsync(Guid solicitudId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<DecisionCoordinador>>(Decisiones.Where(d => d.SolicitudId == solicitudId).ToArray());
        public Task MarcarRemitidaAsync(Guid solicitudId, CancellationToken ct) => throw new InvalidOperationException();
        public Task MarcarFinalizadaAsync(Guid solicitudId, CancellationToken ct)
        {
            if (FalloEstado is not null) throw FalloEstado;
            MarcasFinalizada++; return Task.CompletedTask;
        }
    }

    private sealed class Reloj : TimeProvider { public override DateTimeOffset GetUtcNow() => Ahora; }

    private static DocenteEnInforme Docente(Guid id, string decision) =>
        new(id, id == Ana ? "docente.ana" : "docente.luis", "Ajuste", decision, "Observación sintética.", Ahora.AddDays(-2));

    private static ParejaEnInforme Pareja(Guid aprobada, Guid receptora, string codigo, string final) =>
        new(aprobada, receptora, codigo, "Receptora " + codigo, new("Completo", "Homologable", 85, 90, 100),
            [Docente(Ana, "Homologable"), Docente(Luis, final == "SinAcuerdo" ? "NoHomologable" : "Homologable")], final);

    private static InformeHomologacion Informe() =>
        new(Solicitud, "Interno", Ahora.AddDays(-1), Ana, "docente.ana",
            [Pareja(AprobadaA, ReceptoraA, "R-01", "SinAcuerdo"), Pareja(AprobadaB, ReceptoraB, "R-02", "Convalidar")], [], 1, 0, 1, []);

    private static (RevisionCoordinadorInforme, Fuentes) Caso(bool remitido = true)
    {
        var f = new Fuentes { Informe = Informe() };
        f.Eventos.Add(HistorialInforme.Crear(Solicitud, TipoEventoInforme.Generado, Ahora.AddDays(-1), Ana, "docente.ana", "Docente"));
        if (remitido) f.Eventos.Add(HistorialInforme.Crear(Solicitud, TipoEventoInforme.Remitido, Ahora.AddHours(-20), Luis, "docente.luis", "Docente"));
        return (new RevisionCoordinadorInforme(f, f, f, f, new Reloj()), f);
    }

    private static DecisionCoordinadorInput Decidir(string? resultado = "NoConvalidar", string? justificacion = "No cubre los resultados de aprendizaje de la receptora.",
        Guid? aprobada = null, Guid? receptora = null) => new(aprobada ?? AprobadaA, receptora ?? ReceptoraA, resultado, justificacion);

    [Fact]
    public async Task CA1_el_coordinador_ve_el_informe_remitido_con_la_remision_y_lo_que_falta_decidir()
    {
        var (caso, _) = Caso();
        var revision = await caso.ConsultarAsync(Solicitud, default);
        Assert.Equal(2, revision.Informe.Parejas.Count);
        Assert.All(revision.Informe.Parejas, p => Assert.Equal(2, p.Docentes.Count));
        Assert.Equal((Ahora.AddHours(-20), "docente.luis"), (revision.RemitidoEl, revision.RemitidoPor));
        Assert.Empty(revision.Decisiones);
        Assert.Equal(1, revision.DecisionesPendientes);
        Assert.Null(revision.Finalizacion);
    }

    [Fact]
    public async Task CA1_sin_informe_remitido_no_hay_revision()
    {
        var (caso, f) = Caso(remitido: false);
        await Assert.ThrowsAsync<InformeNoRemitidoException>(() => caso.ConsultarAsync(Solicitud, default));
        f.Informe = null;
        await Assert.ThrowsAsync<InformeNoRemitidoException>(() => caso.ConsultarAsync(Solicitud, default));
    }

    [Fact]
    public async Task CA2_registra_la_decision_motivada_con_autor_y_fecha_y_puede_corregirla_antes_de_finalizar()
    {
        var (caso, f) = Caso();
        var decision = await caso.DecidirAsync(Solicitud, Decidir(), Coordinadora, "coordinadora.eva", default);
        Assert.Equal((AprobadaA, ReceptoraA, "NoConvalidar", Coordinadora, "coordinadora.eva", Ahora),
            (decision.AsignaturaAprobadaId, decision.AsignaturaReceptoraId, decision.Resultado, decision.CoordinadorId, decision.Coordinador, decision.Fecha));
        await caso.DecidirAsync(Solicitud, Decidir("Convalidar", "Revisado el sílabo: cubre los contenidos."), Coordinadora, "coordinadora.eva", default);
        var registrada = Assert.Single(f.Decisiones);
        Assert.Equal(("Convalidar", "Revisado el sílabo: cubre los contenidos."), (registrada.Resultado, registrada.Justificacion));
        Assert.Equal(0, (await caso.ConsultarAsync(Solicitud, default)).DecisionesPendientes);
        Assert.Equal(2, f.Cierres);
    }

    [Fact]
    public async Task CA2_no_decide_parejas_con_acuerdo_ni_parejas_ajenas_al_informe()
    {
        var (caso, f) = Caso();
        await Assert.ThrowsAsync<DecisionCoordinadorInvalidaException>(() =>
            caso.DecidirAsync(Solicitud, Decidir(aprobada: AprobadaB, receptora: ReceptoraB), Coordinadora, "coordinadora.eva", default));
        await Assert.ThrowsAsync<DatosAnalisisSolicitudException>(() =>
            caso.DecidirAsync(Solicitud, Decidir(aprobada: AprobadaB, receptora: ReceptoraA), Coordinadora, "coordinadora.eva", default));
        await Assert.ThrowsAsync<DecisionCoordinadorInvalidaException>(() =>
            caso.DecidirAsync(Solicitud, Decidir(justificacion: " "), Coordinadora, "coordinadora.eva", default));
        Assert.Empty(f.Decisiones);
    }

    [Fact]
    public async Task CA2_no_finaliza_mientras_falte_una_decision()
    {
        var (caso, f) = Caso();
        var error = await Assert.ThrowsAsync<DatosAnalisisSolicitudException>(() => caso.FinalizarAsync(Solicitud, Coordinadora, "coordinadora.eva", default));
        Assert.Contains("R-01", error.Message);
        Assert.DoesNotContain(f.Eventos, e => e.Tipo == TipoEventoInforme.Finalizado);
        Assert.Equal(0, f.MarcasFinalizada);
    }

    [Fact]
    public async Task CA3_finaliza_con_fecha_y_coordinador_y_despues_no_admite_cambios()
    {
        var (caso, f) = Caso();
        await caso.DecidirAsync(Solicitud, Decidir(), Coordinadora, "coordinadora.eva", default);
        var fin = await caso.FinalizarAsync(Solicitud, Coordinadora, "coordinadora.eva", default);
        Assert.Equal((Solicitud, Ahora, Coordinadora, "coordinadora.eva"), (fin.SolicitudId, fin.Fecha, fin.FinalizadoPorId, fin.FinalizadoPor));
        var evento = Assert.Single(f.Eventos, e => e.Tipo == TipoEventoInforme.Finalizado);
        Assert.Equal((Ahora, Coordinadora, "coordinadora.eva", "Coordinador"), (evento.Fecha, evento.UsuarioId, evento.Usuario, evento.Rol));
        Assert.Equal(1, f.MarcasFinalizada);
        Assert.Equal(fin, (await caso.ConsultarAsync(Solicitud, default)).Finalizacion);

        await Assert.ThrowsAsync<SolicitudYaFinalizadaException>(() => caso.FinalizarAsync(Solicitud, Coordinadora, "coordinadora.eva", default));
        await Assert.ThrowsAsync<SolicitudYaFinalizadaException>(() =>
            caso.DecidirAsync(Solicitud, Decidir("Convalidar"), Coordinadora, "coordinadora.eva", default));
        Assert.Equal("NoConvalidar", Assert.Single(f.Decisiones).Resultado);
        Assert.Equal(1, f.MarcasFinalizada);
    }

    [Fact]
    public async Task CA4_sin_informe_remitido_no_se_finaliza_ni_se_decide()
    {
        var (caso, f) = Caso(remitido: false);
        await Assert.ThrowsAsync<DatosAnalisisSolicitudException>(() => caso.FinalizarAsync(Solicitud, Coordinadora, "coordinadora.eva", default));
        await Assert.ThrowsAsync<DatosAnalisisSolicitudException>(() => caso.DecidirAsync(Solicitud, Decidir(), Coordinadora, "coordinadora.eva", default));
        f.Informe = null;
        await Assert.ThrowsAsync<DatosAnalisisSolicitudException>(() => caso.FinalizarAsync(Solicitud, Coordinadora, "coordinadora.eva", default));
        Assert.Empty(f.Decisiones);
        Assert.Equal(0, f.MarcasFinalizada);
    }

    [Fact]
    public async Task Sin_identidad_del_coordinador_no_se_registra_nada()
    {
        var (caso, f) = Caso();
        await Assert.ThrowsAsync<SolicitudAnalisisNoEncontradaException>(() => caso.DecidirAsync(Solicitud, Decidir(), Guid.Empty, "x", default));
        await Assert.ThrowsAsync<SolicitudAnalisisNoEncontradaException>(() => caso.FinalizarAsync(Solicitud, Guid.Empty, "x", default));
        Assert.Empty(f.Decisiones);
        Assert.Equal(0, f.MarcasFinalizada);
    }

    [Fact]
    public async Task Si_Solicitudes_no_confirma_el_estado_no_queda_finalizacion_y_se_puede_reintentar()
    {
        var (caso, f) = Caso();
        await caso.DecidirAsync(Solicitud, Decidir(), Coordinadora, "coordinadora.eva", default);
        f.FalloEstado = new FuenteAnalisisNoDisponibleException();
        await Assert.ThrowsAsync<FuenteAnalisisNoDisponibleException>(() => caso.FinalizarAsync(Solicitud, Coordinadora, "coordinadora.eva", default));
        Assert.DoesNotContain(f.Eventos, e => e.Tipo == TipoEventoInforme.Finalizado);
        f.FalloEstado = new EstadoSolicitudNoPermiteFinalizarException("La solicitud en estado «Registrada» no puede finalizarse.");
        await Assert.ThrowsAsync<EstadoSolicitudNoPermiteFinalizarException>(() => caso.FinalizarAsync(Solicitud, Coordinadora, "coordinadora.eva", default));
        Assert.DoesNotContain(f.Eventos, e => e.Tipo == TipoEventoInforme.Finalizado);
        f.FalloEstado = null;
        await caso.FinalizarAsync(Solicitud, Coordinadora, "coordinadora.eva", default);
        Assert.Single(f.Eventos, e => e.Tipo == TipoEventoInforme.Finalizado);
    }

    [Fact]
    public async Task Un_informe_sin_parejas_sin_acuerdo_se_finaliza_sin_decisiones()
    {
        var (caso, f) = Caso();
        f.Informe = Informe() with { Parejas = [Pareja(AprobadaB, ReceptoraB, "R-02", "Convalidar")], SinAcuerdo = 0 };
        await caso.FinalizarAsync(Solicitud, Coordinadora, "coordinadora.eva", default);
        Assert.Equal(1, f.MarcasFinalizada);
    }
}
