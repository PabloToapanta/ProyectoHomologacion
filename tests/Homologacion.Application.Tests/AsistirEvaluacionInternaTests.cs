using Homologacion.Application.AnalisisInterno;
using Homologacion.Application.Equivalencias;
using Homologacion.Domain.Equivalencias;
using Xunit;

namespace Homologacion.Application.Tests;

public class AsistirEvaluacionInternaTests
{
    private sealed class Umbral(decimal valor) : IUmbralEquivalencia { public decimal Valor => valor; }
    private sealed class Agente : IAgenteAnalisisInterno
    {
        public int Llamadas { get; private set; }
        public ContextoAgente? Contexto { get; private set; }
        public ExplicacionAgente Salida { get; set; } = new("Explicacion sintetica.", ["C1"]);
        public TipoFalloAgente? Fallo { get; set; }
        public Task<ExplicacionAgente> JustificarAsync(ContextoAgente contexto, CancellationToken cancellationToken)
        {
            Llamadas++; Contexto = contexto;
            return Fallo is { } tipo ? Task.FromException<ExplicacionAgente>(new FalloAgenteException(tipo)) : Task.FromResult(Salida);
        }
    }
    private static SolicitudEquivalencia Datos()
    {
        static DatosAsignatura Materia(string id, int horas) => new(id, "plan-sintetico-" + id, horas, "reloj", 8, "sistema-sintetico", ["C"], ["R"]);
        return new([new(Materia("O", 80), true)], Materia("D", 100),
            [new("O", "C", "C", new("plan-O/C", "plan-D/C"))], [new("O", "R", "R", new("plan-O/R", "plan-D/R"))]);
    }
    private static AsistirEvaluacionInterna Caso(Agente agente, decimal umbral = 80) => new(new EvaluarEquivalencia(new Umbral(umbral)), agente);

    [Theory]
    [InlineData(80, true)]
    [InlineData(85, false)]
    public async Task Conserva_porcentajes_y_umbral_del_motor(decimal umbral, bool esperado)
    {
        var agente = new Agente();
        var resultado = await Caso(agente, umbral).EjecutarAsync(Datos());
        Assert.Equal(esperado, resultado.Evaluacion.Homologable);
        Assert.Equal(80, resultado.Evaluacion.CompatibilidadHorariaPct);
        Assert.Equal(100, resultado.Evaluacion.CompatibilidadCreditosPct);
        Assert.Equal(umbral, resultado.Evaluacion.UmbralPct);
        Assert.Equal(EstadoAsistencia.Completada, resultado.Estado);
        Assert.Equal(1, agente.Llamadas);
        Assert.Contains(agente.Contexto!.Evidencias, e => e.Id == "C1" && e.Correspondencia.Evidencia!.UbicacionOrigen == "plan-O/C");
    }
    [Fact]
    public async Task Texto_contrario_del_agente_no_cambia_decision_del_motor()
    {
        var agente = new Agente { Salida = new("Texto sintetico contrario: no homologable.", ["C1"]) };
        var resultado = await Caso(agente).EjecutarAsync(Datos());
        Assert.True(resultado.Evaluacion.Homologable);
        Assert.Equal(EstadoEquivalencia.Homologable, resultado.Evaluacion.Estado);
    }
    [Fact]
    public async Task Informacion_incompleta_no_invoca_agente()
    {
        var agente = new Agente();
        var resultado = await Caso(agente).EjecutarAsync(Datos() with { Receptora = null });
        Assert.Equal(EstadoAsistencia.InformacionIncompleta, resultado.Estado);
        Assert.Null(resultado.Asistencia);
        Assert.Equal(0, agente.Llamadas);
    }
    [Theory]
    [InlineData(TipoFalloAgente.NoDisponible)]
    [InlineData(TipoFalloAgente.TiempoAgotado)]
    [InlineData(TipoFalloAgente.RespuestaInvalida)]
    public async Task Fallo_requiere_revision_manual_sin_exito_ficticio(TipoFalloAgente fallo)
    {
        var resultado = await Caso(new Agente { Fallo = fallo }).EjecutarAsync(Datos());
        Assert.Equal(EstadoAsistencia.RevisionManual, resultado.Estado);
        Assert.Equal(fallo, resultado.Fallo);
        Assert.Null(resultado.Asistencia);
        Assert.True(resultado.Evaluacion.Homologable);
    }
    [Theory]
    [InlineData("")]
    [InlineData("INEXISTENTE")]
    public async Task Tambien_valida_salida_del_puerto_simulado(string referencia)
    {
        var agente = new Agente { Salida = new("Explicacion sintetica.", [referencia]) };
        Assert.Equal(EstadoAsistencia.RevisionManual, (await Caso(agente).EjecutarAsync(Datos())).Estado);
    }
    [Fact]
    public async Task Cancelacion_previa_no_llama_al_modelo()
    {
        var agente = new Agente();
        using var cts = new CancellationTokenSource(); cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Caso(agente).EjecutarAsync(Datos(), cts.Token));
        Assert.Equal(0, agente.Llamadas);
    }
}

