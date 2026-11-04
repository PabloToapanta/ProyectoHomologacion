using Homologacion.Application.AnalisisInterno;
using Homologacion.Application.Equivalencias;
using Xunit;

namespace Homologacion.Application.Tests;

public class ResultadosAnalisisInternoTests
{
    private sealed class Almacen : IResultadosAnalisisInterno
    {
        public bool Fallar { get; set; }
        public List<AnalisisGuardado> Datos { get; } = [];
        public Task GuardarAsync(AnalisisGuardado r, CancellationToken ct)
        {
            if (Fallar) throw new AlmacenAnalisisNoDisponibleException();
            Datos.RemoveAll(x => x.Analisis.SolicitudId == r.Analisis.SolicitudId && x.Analisis.AsignaturaAprobadaId == r.Analisis.AsignaturaAprobadaId && x.Analisis.AsignaturaReceptoraId == r.Analisis.AsignaturaReceptoraId);
            Datos.Add(r); return Task.CompletedTask;
        }
        public Task<IReadOnlyList<AnalisisGuardado>> ListarAsync(Guid id, CancellationToken ct) => Task.FromResult<IReadOnlyList<AnalisisGuardado>>(Datos.Where(x => x.Analisis.SolicitudId == id).ToArray());
    }
    private static GestionarAnalisisInterno Caso(EscenarioInterno e, Almacen a) => new(
        new(e, e, new(new EvaluarEquivalencia(new UmbralAnalisis()), e), new RelojAnalisis()), e, a, new RelojAnalisis());
    [Theory]
    [InlineData("Completo")]
    [InlineData("Fallido")]
    [InlineData("Incompleto")]
    public async Task Guarda_y_recupera_estado_sin_fabricar_resultado(string estado)
    {
        var e = new EscenarioInterno(); var a = new Almacen(); var caso = Caso(e, a);
        if (estado == "Fallido") e.FalloAgente = TipoFalloAgente.NoDisponible;
        if (estado == "Incompleto") e.Revision = e.Revision with { Vigente = false };
        await caso.EjecutarAsync(EscenarioInterno.SolicitudId, EscenarioInterno.AprobadaId, EscenarioInterno.ReceptoraId, default);
        var r = Assert.Single(await caso.ConsultarAsync(EscenarioInterno.SolicitudId, default));
        Assert.Equal(estado, r.Analisis.Estado);
        Assert.Equal(estado == "Completo", r.Analisis.Resultado is not null);
    }
    [Fact]
    public async Task Perdida_de_asignacion_impide_consultar_resultados_guardados()
    {
        var e = new EscenarioInterno(); var a = new Almacen(); var caso = Caso(e, a);
        await caso.EjecutarAsync(EscenarioInterno.SolicitudId, EscenarioInterno.AprobadaId, EscenarioInterno.ReceptoraId, default);
        e.Solicitud = null;
        await Assert.ThrowsAsync<SolicitudAnalisisNoEncontradaException>(() => caso.ConsultarAsync(EscenarioInterno.SolicitudId, default));
    }
    [Fact]
    public async Task Fallo_de_almacen_no_confirma_exito()
    {
        var e = new EscenarioInterno(); var a = new Almacen { Fallar = true };
        await Assert.ThrowsAsync<AlmacenAnalisisNoDisponibleException>(() => Caso(e, a).EjecutarAsync(EscenarioInterno.SolicitudId, EscenarioInterno.AprobadaId, EscenarioInterno.ReceptoraId, default));
        Assert.Empty(a.Datos);
    }
}
