using Homologacion.Domain.Agrupaciones;
using Xunit;
namespace Homologacion.Domain.Tests;
public class GrupoAgrupacionTests
{
    [Fact] public void Null_y_cero_se_conservan_y_el_primer_analisis_congela()
    {
        var o = Guid.NewGuid(); var r = Guid.NewGuid(); var s = Guid.NewGuid();
        var g = GrupoAgrupacion.Crear(s, [o], [r, s], [new(o, r, null, 0), new(o, s, 10.25m, 1.5m)]);
        Assert.Null(g.Aportes[0].HorasAportadas); Assert.Equal(0, g.Aportes[0].CreditosAportados);
        var v = g.Version; g.Congelar(); Assert.NotEqual(v, g.Version);
        Assert.Throws<ConflictoGrupoException>(() => g.Reemplazar([o], [r,s], g.Aportes));
    }
    [Theory] [InlineData(-1)] [InlineData(0.001)] [InlineData(10000000000)]
    public void Rechaza_aporte_fuera_de_precision_o_rango(decimal cantidad)
    {
        var o=Guid.NewGuid();var r=Guid.NewGuid();var t=Guid.NewGuid();
        Assert.Throws<GrupoInvalidoException>(()=>GrupoAgrupacion.Crear(t,[o],[r,t],[new(o,r,cantidad,1),new(o,t,0,0)]));
    }
    [Fact] public void Rechaza_duplicados_ajenos_y_uno_a_uno()
    {
        var o=Guid.NewGuid();var r=Guid.NewGuid();var t=Guid.NewGuid();
        Assert.Throws<GrupoInvalidoException>(()=>GrupoAgrupacion.Crear(t,[o],[r],[new(o,r,1,1)]));
        Assert.Throws<GrupoInvalidoException>(()=>GrupoAgrupacion.Crear(t,[o,o],[r,t],[new(o,r,1,1),new(o,t,1,1)]));
        Assert.Throws<GrupoInvalidoException>(()=>GrupoAgrupacion.Crear(t,[o],[r,t],[new(o,r,1,1),new(o,r,1,1)]));
    }
}
