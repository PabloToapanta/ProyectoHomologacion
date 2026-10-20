using Homologacion.Domain.Equivalencias;
using Xunit;

namespace Homologacion.Domain.Tests;

public class EquivalenciaTests
{
    [Theory]
    [InlineData(144, 144, 100)] [InlineData(200, 144, 100)]
    [InlineData(4, 3, 100)] [InlineData(3, 3, 100)]
    [InlineData(80, 100, 80)] [InlineData(799, 1000, 79.9)]
    public void Relacion_respecto_a_receptora_limitada_a_cien(decimal origen, decimal receptora, decimal esperado)
        => Assert.Equal(esperado, Porcentaje.DesdeRelacion(origen, receptora)!.Value.Valor);

    [Theory]
    [InlineData(79.9, 100, 100, 80, false)]
    [InlineData(100, 79.9, 100, 80, false)]
    [InlineData(100, 100, 79.9, 80, false)]
    [InlineData(80, 80, 80, 80, true)]
    [InlineData(82, 100, 100, 85, false)]
    [InlineData(100, 82, 100, 85, false)]
    [InlineData(100, 100, 82, 85, false)]
    [InlineData(85, 85, 85, 85, true)]
    [InlineData(79.9999, 100, 100, 80, false)]
    public void Tres_criterios_individuales_sin_promedio(decimal academica, decimal horas, decimal creditos, decimal umbral, bool esperado)
    {
        var resultado = MotorEquivalencia.Evaluar(academica, horas, creditos, umbral);
        Assert.Equal(esperado, resultado.Homologable);
        Assert.Equal(esperado ? EstadoEquivalencia.Homologable : EstadoEquivalencia.NoHomologable, resultado.Estado);
    }

    [Theory]
    [InlineData(null, 80, 80)] [InlineData(80, null, 80)] [InlineData(80, 80, null)]
    [InlineData(-1, 80, 80)] [InlineData(80, 101, 80)] [InlineData(80, 80, -1)]
    public void Datos_ausentes_o_invalidos_nunca_homologan(int? academica, int? horas, int? creditos)
    {
        var resultado = MotorEquivalencia.Evaluar(academica, horas, creditos, 80);
        Assert.False(resultado.Homologable);
        Assert.Equal(EstadoEquivalencia.InformacionIncompleta, resultado.Estado);
    }

    [Theory]
    [InlineData(null, 10)] [InlineData(10, null)] [InlineData(0, 10)]
    [InlineData(10, 0)] [InlineData(-1, 10)] [InlineData(10, -1)]
    public void Cantidades_invalidas_no_producen_porcentaje(int? origen, int? receptora)
        => Assert.Null(Porcentaje.DesdeRelacion(origen, receptora));

    [Fact]
    public void Valores_extremos_no_desbordan_ni_superan_cien()
    {
        Assert.Equal(100, Porcentaje.DesdeRelacion(decimal.MaxValue, 0.0000001m)!.Value.Valor);
        Assert.InRange(Porcentaje.DesdeRelacion(0.0000001m, decimal.MaxValue)!.Value.Valor, 0, 100);
        Assert.Null(Porcentaje.DesdeValor(100.01m));
    }

    [Theory]
    [InlineData(0)] [InlineData(-1)] [InlineData(101)]
    public void Umbral_invalido_es_error_de_configuracion(decimal umbral)
        => Assert.Throws<ArgumentOutOfRangeException>(() => MotorEquivalencia.Evaluar(100, 100, 100, umbral));
}
