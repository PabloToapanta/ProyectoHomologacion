using Homologacion.Application.AnalisisInterno;
using Homologacion.Application.Informe;
using Homologacion.Domain.Auditoria;
using Xunit;

namespace Homologacion.Application.Tests;

/// <summary>HU-32 / Issue #63: el docente asignado remite el informe generado al coordinador una sola vez.</summary>
public class RemitirInformeTests
{
    private static readonly Guid Solicitud = Guid.Parse("93200000-0000-0000-0000-000000000001");
    private static readonly Guid Ana = Guid.Parse("93200000-0000-0000-0000-0000000000d1");
    private static readonly Guid Luis = Guid.Parse("93200000-0000-0000-0000-0000000000d2");
    private static readonly DateTimeOffset Ahora = new(2026, 11, 21, 15, 0, 0, TimeSpan.Zero);

    /// <summary>Simula la transacción de ConCierreAsync: si la acción falla, los eventos insertados se descartan.</summary>
    private sealed class Fuentes : ISolicitudesAnalisisInterno, IInformesHomologacion, IAuditoriaInformes, IEstadoSolicitudes
    {
        public bool Asignada { get; set; } = true;
        public InformeHomologacion? Informe { get; set; }
        public List<EventoInforme> Eventos { get; } = [];
        public Exception? FalloEstado { get; set; }
        public int MarcasRemitida { get; private set; }

        public Task<SolicitudParaAnalisis?> ObtenerSolicitudAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(id == Solicitud && Asignada ? new SolicitudParaAnalisis(Solicitud, "Interno", Guid.NewGuid(), Guid.NewGuid(), []) : null);
        public Task<InformeHomologacion?> ObtenerAsync(Guid solicitudId, CancellationToken ct) => Task.FromResult(solicitudId == Solicitud ? Informe : null);
        public Task GuardarAsync(InformeHomologacion informe, CancellationToken ct) => throw new InvalidOperationException();
        public async Task<T> ConCierreAsync<T>(Guid solicitudId, Func<Task<T>> accion, CancellationToken ct)
        {
            var antes = Eventos.ToArray();
            try { return await accion(); }
            catch { Eventos.Clear(); Eventos.AddRange(antes); throw; }
        }
        public Task RegistrarAsync(EventoInforme evento, CancellationToken ct) { Eventos.Add(evento); return Task.CompletedTask; }
        public Task<IReadOnlyList<EventoInforme>> ListarAsync(Guid solicitudId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<EventoInforme>>(Eventos.Where(e => e.SolicitudId == solicitudId).ToArray());
        public Task MarcarRemitidaAsync(Guid solicitudId, CancellationToken ct)
        {
            if (FalloEstado is not null) throw FalloEstado;
            MarcasRemitida++;
            return Task.CompletedTask;
        }
        public Task MarcarFinalizadaAsync(Guid solicitudId, CancellationToken ct) => throw new InvalidOperationException();
    }

    private sealed class Reloj : TimeProvider { public override DateTimeOffset GetUtcNow() => Ahora; }

    private static DocenteEnInforme Docente(Guid id, string decision) =>
        new(id, id == Ana ? "docente.ana" : "docente.luis", "Ajuste", decision, null, Ahora.AddHours(-3));

    private static InformeHomologacion Informe(params ParejaEnInforme[] parejas) =>
        new(Solicitud, "Interno", Ahora.AddHours(-1), Ana, "docente.ana", parejas, [], 0, 0, 0, []);

    private static ParejaEnInforme Pareja(params DocenteEnInforme[] docentes) =>
        new(Guid.NewGuid(), Guid.NewGuid(), "R-01", "Receptora", new("SinAnalisis", null, null, null, null), docentes, "SinAcuerdo");

    private static (RemitirInformeHomologacion, Fuentes) Caso(bool conGenerado = true)
    {
        var f = new Fuentes { Informe = Informe(Pareja(Docente(Ana, "Homologable"), Docente(Luis, "NoHomologable"))) };
        if (conGenerado) f.Eventos.Add(HistorialInforme.Crear(Solicitud, TipoEventoInforme.Generado, Ahora.AddHours(-1), Ana, "docente.ana", "Docente"));
        return (new RemitirInformeHomologacion(f, f, f, f, new Reloj()), f);
    }

    [Fact]
    public async Task CA1_remite_y_registra_fecha_y_docente()
    {
        var (caso, f) = Caso();
        var remision = await caso.RemitirAsync(Solicitud, Luis, "docente.luis", default);
        Assert.Equal((Solicitud, Ahora, Luis, "docente.luis"), (remision.SolicitudId, remision.Fecha, remision.RemitidoPorId, remision.RemitidoPor));
        var evento = f.Eventos.Single(e => e.Tipo == TipoEventoInforme.Remitido);
        Assert.Equal((Ahora, Luis, "docente.luis", "Docente"), (evento.Fecha, evento.UsuarioId, evento.Usuario, evento.Rol));
        // CA4: Solicitudes pasa la solicitud a «Remitida».
        Assert.Equal(1, f.MarcasRemitida);
    }

    [Fact]
    public async Task CA2_un_segundo_intento_no_cambia_la_remision()
    {
        var (caso, f) = Caso();
        await caso.RemitirAsync(Solicitud, Ana, "docente.ana", default);
        var registrada = f.Eventos.Single(e => e.Tipo == TipoEventoInforme.Remitido);
        await Assert.ThrowsAsync<InformeYaRemitidoException>(() => caso.RemitirAsync(Solicitud, Luis, "docente.luis", default));
        Assert.Same(registrada, f.Eventos.Single(e => e.Tipo == TipoEventoInforme.Remitido));
        Assert.Equal(1, f.MarcasRemitida);
    }

    [Fact]
    public async Task CA3_sin_informe_no_se_remite()
    {
        var (caso, f) = Caso(conGenerado: false);
        f.Informe = null;
        var error = await Assert.ThrowsAsync<DatosAnalisisSolicitudException>(() => caso.RemitirAsync(Solicitud, Ana, "docente.ana", default));
        Assert.Contains("informe", error.Message);
        Assert.Empty(f.Eventos);
        Assert.Equal(0, f.MarcasRemitida);
    }

    [Fact]
    public async Task Una_pareja_sin_el_analisis_de_ambos_docentes_no_se_remite()
    {
        var (caso, f) = Caso();
        f.Informe = Informe(Pareja(Docente(Ana, "Homologable")));
        await Assert.ThrowsAsync<DatosAnalisisSolicitudException>(() => caso.RemitirAsync(Solicitud, Ana, "docente.ana", default));
        Assert.DoesNotContain(f.Eventos, e => e.Tipo == TipoEventoInforme.Remitido);
        Assert.Equal(0, f.MarcasRemitida);
    }

    [Fact]
    public async Task CA5_docente_no_asignado_o_sin_identidad_no_remite()
    {
        var (caso, f) = Caso();
        await Assert.ThrowsAsync<SolicitudAnalisisNoEncontradaException>(() => caso.RemitirAsync(Solicitud, Guid.Empty, "x", default));
        f.Asignada = false;
        await Assert.ThrowsAsync<SolicitudAnalisisNoEncontradaException>(() => caso.RemitirAsync(Solicitud, Ana, "docente.ana", default));
        Assert.DoesNotContain(f.Eventos, e => e.Tipo == TipoEventoInforme.Remitido);
        Assert.Equal(0, f.MarcasRemitida);
    }

    [Fact]
    public async Task Si_Solicitudes_no_confirma_el_estado_no_queda_remision_y_se_puede_reintentar()
    {
        var (caso, f) = Caso();
        f.FalloEstado = new FuenteAnalisisNoDisponibleException();
        await Assert.ThrowsAsync<FuenteAnalisisNoDisponibleException>(() => caso.RemitirAsync(Solicitud, Ana, "docente.ana", default));
        Assert.DoesNotContain(f.Eventos, e => e.Tipo == TipoEventoInforme.Remitido);
        f.FalloEstado = null;
        await caso.RemitirAsync(Solicitud, Ana, "docente.ana", default);
        Assert.Single(f.Eventos, e => e.Tipo == TipoEventoInforme.Remitido);
    }

    [Fact]
    public async Task Si_el_estado_de_la_solicitud_no_lo_permite_no_queda_remision()
    {
        var (caso, f) = Caso();
        f.FalloEstado = new EstadoSolicitudNoPermiteRemitirException("La solicitud en estado «Registrada» no puede remitirse.");
        await Assert.ThrowsAsync<EstadoSolicitudNoPermiteRemitirException>(() => caso.RemitirAsync(Solicitud, Ana, "docente.ana", default));
        Assert.DoesNotContain(f.Eventos, e => e.Tipo == TipoEventoInforme.Remitido);
    }

    [Fact]
    public async Task Un_informe_anterior_a_HU25_sin_evento_generado_tambien_se_remite()
    {
        // No se inventa el evento «Generado» que falta; el informe guardado acredita la generación.
        var (caso, f) = Caso(conGenerado: false);
        await caso.RemitirAsync(Solicitud, Ana, "docente.ana", default);
        Assert.Equal([TipoEventoInforme.Remitido], f.Eventos.Select(e => e.Tipo));
    }
}
