using Homologacion.Domain.RequisitosCambio;
using Homologacion.Domain.Configuracion;
using Xunit;

namespace Homologacion.Domain.Tests;

public class RequisitosCambioTests
{
    [Theory]
    [InlineData(TipoCambio.Interno, 1, 1, 0, true)]
    [InlineData(TipoCambio.Interno, 2, 3, 0, true)]
    [InlineData(TipoCambio.Interno, 1, 0, 0, false)]
    [InlineData(TipoCambio.Interno, 0, 1, 0, false)]
    [InlineData(TipoCambio.Interno, 1, 1, 1, true)]
    [InlineData(TipoCambio.Externo, 2, 2, 0, true)]
    [InlineData(TipoCambio.Externo, 3, 4, 0, true)]
    [InlineData(TipoCambio.Externo, 2, 1, 0, false)]
    [InlineData(TipoCambio.Externo, 1, 2, 0, false)]
    [InlineData(TipoCambio.Externo, 2, 2, 1, false)]
    [InlineData(TipoCambio.Externo, 0, 0, 0, false)]
    public void Valida_minimos_sin_compensar_requisitos(TipoCambio tipo, int periodos,
        int homologables, int cambios, bool esperado)
    {
        var resultado = ValidadorRequisitosCambio.Validar(tipo, periodos, homologables, cambios);
        Assert.Equal(esperado, resultado.Cumple);
        Assert.Equal(esperado, resultado.Incumplimientos.Count == 0);
    }

    [Theory]
    [InlineData(-1, 1, 0)]
    [InlineData(1, -1, 0)]
    [InlineData(1, 1, -1)]
    public void Cantidades_negativas_son_datos_invalidos(int periodos, int homologables, int cambios)
        => Assert.Throws<ArgumentException>(() =>
            ValidadorRequisitosCambio.Validar(TipoCambio.Interno, periodos, homologables, cambios));

    [Fact]
    public void Tipo_desconocido_no_se_interpreta_como_interno()
        => Assert.Throws<ArgumentException>(() => ValidadorRequisitosCambio.Validar((TipoCambio)99, 2, 2, 0));

    [Fact]
    public void Explica_todos_los_requisitos_incumplidos()
    {
        var resultado = ValidadorRequisitosCambio.Validar(TipoCambio.Externo, 0, 0, 1);
        Assert.Equal(3, resultado.Incumplimientos.Count);
        Assert.Contains(resultado.Incumplimientos, x => x.Contains("períodos"));
        Assert.Contains(resultado.Incumplimientos, x => x.Contains("asignaturas"));
        Assert.Contains(resultado.Incumplimientos, x => x.Contains("previos"));
    }

    [Fact]
    public void Usa_minimos_configurados_por_tipo_y_conserva_regla_de_cambios_previos()
    {
        var parametros = new ParametrosHomologacion(Guid.NewGuid(), 80, 10, 3, 4, 5, 6);

        Assert.False(ValidadorRequisitosCambio.Validar(TipoCambio.Interno, 2, 4, 0, parametros).Cumple);
        Assert.True(ValidadorRequisitosCambio.Validar(TipoCambio.Interno, 3, 4, 5, parametros).Cumple);
        Assert.False(ValidadorRequisitosCambio.Validar(TipoCambio.Externo, 5, 6, 1, parametros).Cumple);
        Assert.True(ValidadorRequisitosCambio.Validar(TipoCambio.Externo, 5, 6, 0, parametros).Cumple);
    }

    [Fact]
    public void Configuracion_predeterminada_conserva_minimos_vigentes()
    {
        var parametros = ParametrosHomologacion.Predeterminados();

        Assert.True(ValidadorRequisitosCambio.Validar(TipoCambio.Interno, 1, 1, 0, parametros).Cumple);
        Assert.True(ValidadorRequisitosCambio.Validar(TipoCambio.Externo, 2, 2, 0, parametros).Cumple);
        Assert.False(ValidadorRequisitosCambio.Validar(TipoCambio.Externo, 2, 2, 1, parametros).Cumple);
    }
}
