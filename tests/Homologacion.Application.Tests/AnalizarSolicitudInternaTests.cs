using Homologacion.Application.AnalisisInterno;
using Homologacion.Application.Equivalencias;
using Xunit;

namespace Homologacion.Application.Tests;

internal sealed class EscenarioInterno : ISolicitudesAnalisisInterno, ICatalogoAnalisisInterno, IAgenteAnalisisInterno
{
    public static readonly Guid SolicitudId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    public static readonly Guid AprobadaId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    public static readonly Guid OrigenId = Guid.Parse("10000000-0000-0000-0000-000000000003");
    public static readonly Guid ReceptoraId = Guid.Parse("10000000-0000-0000-0000-000000000004");
    private static readonly Guid CarreraOrigen = Guid.Parse("10000000-0000-0000-0000-000000000005");
    private static readonly Guid CarreraReceptora = Guid.Parse("10000000-0000-0000-0000-000000000006");
    private static readonly Guid ContenidoO = Guid.Parse("10000000-0000-0000-0000-000000000007");
    private static readonly Guid ContenidoR = Guid.Parse("10000000-0000-0000-0000-000000000008");
    private static readonly Guid ResultadoO = Guid.Parse("10000000-0000-0000-0000-000000000009");
    private static readonly Guid ResultadoR = Guid.Parse("10000000-0000-0000-0000-000000000010");
    public SolicitudParaAnalisis? Solicitud { get; set; } = new(SolicitudId, "Interno", CarreraOrigen, CarreraReceptora,
        [new(AprobadaId, OrigenId, 8, 80, "Aprobada", new DateOnly(2025, 1, 1))]);
    public Dictionary<Guid, AsignaturaParaAnalisis> Materias { get; } = new()
    {
        [OrigenId] = new(OrigenId, CarreraOrigen, "FISEI", true, 8, 80, "plan-sintetico-O", "hora-reloj", "sistema-sintetico", true,
            [new(ContenidoO, "Contenido origen sintetico")], [new(ResultadoO, "Resultado origen sintetico")]),
        [ReceptoraId] = new(ReceptoraId, CarreraReceptora, "FISEI", true, 10, 100, "plan-sintetico-R", "hora-reloj", "sistema-sintetico", true,
            [new(ContenidoR, "Contenido receptor sintetico")], [new(ResultadoR, "Resultado receptor sintetico")])
    };
    public RevisionParaAnalisis Revision { get; set; } = new(true, true,
        [new(ContenidoO.ToString(), ContenidoR.ToString(), "plan-O/U1/C1", "plan-R/U1/C1")],
        [new(ResultadoO.ToString(), ResultadoR.ToString(), "plan-O/U1/RA", "plan-R/U1/RA")]);
    public bool FuenteCaida { get; set; }
    public TipoFalloAgente? FalloAgente { get; set; }
    public int Consultas { get; private set; }
    public int LlamadasAgente { get; private set; }
    public ContextoAgente? Contexto { get; private set; }
    public Task<SolicitudParaAnalisis?> ObtenerSolicitudAsync(Guid id, CancellationToken ct)
    {
        Consultas++;
        if (FuenteCaida) throw new FuenteAnalisisNoDisponibleException();
        return Task.FromResult(id == SolicitudId ? Solicitud : null);
    }
    public Task<AsignaturaParaAnalisis?> ObtenerAsignaturaAsync(Guid id, CancellationToken ct) => Task.FromResult(Materias.GetValueOrDefault(id));
    public Task<RevisionParaAnalisis> ObtenerRevisionAsync(Guid origen, Guid receptora, CancellationToken ct) => Task.FromResult(Revision);
    public Task<ExplicacionAgente> JustificarAsync(ContextoAgente contexto, CancellationToken ct)
    {
        LlamadasAgente++; Contexto = contexto;
        return FalloAgente is { } fallo ? Task.FromException<ExplicacionAgente>(new FalloAgenteException(fallo))
            : Task.FromResult(new ExplicacionAgente("Asistencia sintetica sobre evidencia registrada.", ["C1", "R1"]));
    }
}
internal sealed class RelojAnalisis : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => new(2026, 11, 3, 12, 0, 0, TimeSpan.Zero);
}
internal sealed class UmbralAnalisis : IUmbralEquivalencia { public decimal Valor => 80; }

public class AnalizarSolicitudInternaTests
{
    private static AnalizarSolicitudInterna Caso(EscenarioInterno e) => new(e, e, new(new EvaluarEquivalencia(new UmbralAnalisis()), e), new RelojAnalisis());
    [Theory]
    [InlineData("estado")]
    [InlineData("aprobada-nula")]
    [InlineData("vinculo-nulo")]
    public async Task Fuente_malformada_se_rechaza_sin_invocar_modelo(string caso)
    {
        var e = new EscenarioInterno();
        if (caso == "estado") e.Solicitud = e.Solicitud! with { AsignaturasAprobadas = [e.Solicitud.AsignaturasAprobadas[0] with { Estado = "0" }] };
        if (caso == "aprobada-nula") e.Solicitud = e.Solicitud! with { AsignaturasAprobadas = [null!] };
        if (caso == "vinculo-nulo") e.Revision = e.Revision with { Contenidos = [null!] };
        await Assert.ThrowsAsync<FuenteAnalisisNoDisponibleException>(() => Caso(e).EjecutarAsync(EscenarioInterno.SolicitudId, EscenarioInterno.AprobadaId, EscenarioInterno.ReceptoraId, default));
        Assert.Equal(0, e.LlamadasAgente);
    }
    [Fact]
    public async Task Usa_registro_aprobado_catalogo_y_evidencia_sin_datos_del_cliente()
    {
        var e = new EscenarioInterno();
        var r = await Caso(e).EjecutarAsync(EscenarioInterno.SolicitudId, EscenarioInterno.AprobadaId, EscenarioInterno.ReceptoraId, default);
        Assert.Equal("Completo", r.Estado);
        Assert.True(r.Evaluacion.Homologable);
        Assert.Equal(100, r.Resultado!.PorcentajeAcademico);
        Assert.Equal(80, r.Resultado.PorcentajeHoras);
        Assert.Equal(80, r.Resultado.PorcentajeCreditos);
        Assert.Equal(1, e.LlamadasAgente);
        Assert.True(r.DatosSinteticos);
        Assert.Contains(r.Advertencias, a => a.Contains("períodos"));
        Assert.Contains("Contenido origen sintetico", System.Text.Json.JsonSerializer.Serialize(e.Contexto));
    }
    [Fact]
    public async Task Solicitud_ajena_o_inexistente_no_invoca_modelo()
    {
        var e = new EscenarioInterno { Solicitud = null };
        await Assert.ThrowsAsync<SolicitudAnalisisNoEncontradaException>(() => Caso(e).EjecutarAsync(EscenarioInterno.SolicitudId, EscenarioInterno.AprobadaId, EscenarioInterno.ReceptoraId, default));
        Assert.Equal(0, e.LlamadasAgente);
    }
    [Fact]
    public async Task Asignatura_de_otra_solicitud_no_se_admite()
    {
        var e = new EscenarioInterno();
        await Assert.ThrowsAsync<DatosAnalisisSolicitudException>(() => Caso(e).EjecutarAsync(EscenarioInterno.SolicitudId, Guid.NewGuid(), EscenarioInterno.ReceptoraId, default));
        Assert.Equal(0, e.LlamadasAgente);
    }
    [Fact]
    public async Task No_implementa_el_cambio_externo()
    {
        var e = new EscenarioInterno();
        e.Solicitud = e.Solicitud! with { TipoCambio = "Externo" };
        await Assert.ThrowsAsync<DatosAnalisisSolicitudException>(() => Caso(e).EjecutarAsync(EscenarioInterno.SolicitudId, EscenarioInterno.AprobadaId, EscenarioInterno.ReceptoraId, default));
    }
    [Fact]
    public async Task Receptora_de_otra_carrera_no_se_admite()
    {
        var e = new EscenarioInterno();
        e.Materias[EscenarioInterno.ReceptoraId] = e.Materias[EscenarioInterno.ReceptoraId] with { CarreraId = Guid.NewGuid() };
        await Assert.ThrowsAsync<DatosAnalisisSolicitudException>(() => Caso(e).EjecutarAsync(EscenarioInterno.SolicitudId, EscenarioInterno.AprobadaId, EscenarioInterno.ReceptoraId, default));
    }
    [Theory]
    [InlineData("revision")]
    [InlineData("programa")]
    [InlineData("horas")]
    [InlineData("historico")]
    public async Task Datos_incompletos_obsoletos_o_historicos_no_producen_exito(string caso)
    {
        var e = new EscenarioInterno();
        if (caso == "revision") e.Revision = e.Revision with { Vigente = false };
        if (caso == "programa") e.Materias[EscenarioInterno.OrigenId] = e.Materias[EscenarioInterno.OrigenId] with { PlanAnalitico = null };
        if (caso == "horas") e.Materias[EscenarioInterno.OrigenId] = e.Materias[EscenarioInterno.OrigenId] with { Horas = 90 };
        if (caso == "historico") e.Materias[EscenarioInterno.OrigenId] = e.Materias[EscenarioInterno.OrigenId] with { Facultad = "Facultad histórica sintética" };
        var r = await Caso(e).EjecutarAsync(EscenarioInterno.SolicitudId, EscenarioInterno.AprobadaId, EscenarioInterno.ReceptoraId, default);
        Assert.Equal("Incompleto", r.Estado);
        Assert.False(r.Evaluacion.Homologable);
        Assert.Null(r.Resultado);
        Assert.Equal(0, e.LlamadasAgente);
    }
    [Theory]
    [InlineData("2016-11-03", "Aprobada", "Completo")]
    [InlineData("2016-11-02", "Aprobada", "Incompleto")]
    [InlineData("2025-01-01", "Reprobada", "Incompleto")]
    [InlineData("2027-01-01", "Aprobada", "Incompleto")]
    public async Task Usa_aprobacion_y_antiguedad_de_la_fuente(string fecha, string estado, string esperado)
    {
        var e = new EscenarioInterno();
        e.Solicitud = e.Solicitud! with { AsignaturasAprobadas = [e.Solicitud.AsignaturasAprobadas[0] with { FechaAprobacion = DateOnly.Parse(fecha), Estado = estado }] };
        var r = await Caso(e).EjecutarAsync(EscenarioInterno.SolicitudId, EscenarioInterno.AprobadaId, EscenarioInterno.ReceptoraId, default);
        Assert.Equal(esperado, r.Estado);
        if (esperado == "Incompleto") Assert.Equal(0, e.LlamadasAgente);
    }
    [Fact]
    public async Task Fallo_del_agente_conserva_calculo_pero_no_resultado_exitoso()
    {
        var e = new EscenarioInterno { FalloAgente = TipoFalloAgente.NoDisponible };
        var r = await Caso(e).EjecutarAsync(EscenarioInterno.SolicitudId, EscenarioInterno.AprobadaId, EscenarioInterno.ReceptoraId, default);
        Assert.Equal("Fallido", r.Estado);
        Assert.Null(r.Resultado);
        Assert.True(r.Evaluacion.Homologable);
    }
    [Fact]
    public async Task Dependencia_caida_no_fabrica_datos()
    {
        var e = new EscenarioInterno { FuenteCaida = true };
        await Assert.ThrowsAsync<FuenteAnalisisNoDisponibleException>(() => Caso(e).EjecutarAsync(EscenarioInterno.SolicitudId, EscenarioInterno.AprobadaId, EscenarioInterno.ReceptoraId, default));
    }
}
