using Homologacion.Domain.Finalizacion;
using Xunit;

namespace Homologacion.Domain.Tests;

/// <summary>HU-33 / Issue #64: decisión propia y motivada del coordinador en las parejas «Sin acuerdo» (decisión del 20/11/2026).</summary>
public class DecisionCoordinadorTests
{
    [Theory]
    [InlineData("Convalidar")]
    [InlineData("NoConvalidar")]
    public void En_sin_acuerdo_registra_resultado_y_justificacion(string resultado)
    {
        var (r, j) = ReglaDecisionCoordinador.Validar("SinAcuerdo", resultado, "  Cubre los resultados de aprendizaje.\nVer sílabo.  ");
        Assert.Equal(resultado, r);
        Assert.Equal("Cubre los resultados de aprendizaje.\nVer sílabo.", j);
    }

    [Theory]
    [InlineData("Convalidar")]
    [InlineData("NoConvalidar")]
    [InlineData(null)]
    public void Solo_las_parejas_sin_acuerdo_reciben_decision(string? sugerenciaFinal)
    {
        // Las demás conservan la sugerencia coincidente de los docentes.
        var error = Assert.Throws<DecisionCoordinadorInvalidaException>(() => ReglaDecisionCoordinador.Validar(sugerenciaFinal, "Convalidar", "Motivo."));
        Assert.Contains("Sin acuerdo", error.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Homologable")]
    [InlineData("convalidar")]
    [InlineData("SinAcuerdo")]
    public void El_resultado_es_convalidar_o_no_convalidar(string? resultado) =>
        Assert.Throws<DecisionCoordinadorInvalidaException>(() => ReglaDecisionCoordinador.Validar("SinAcuerdo", resultado, "Motivo."));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void La_decision_debe_ser_motivada(string? justificacion)
    {
        var error = Assert.Throws<DecisionCoordinadorInvalidaException>(() => ReglaDecisionCoordinador.Validar("SinAcuerdo", "Convalidar", justificacion));
        Assert.Contains("justificación", error.Message);
    }

    [Fact]
    public void La_justificacion_tiene_un_limite_y_no_admite_caracteres_de_control()
    {
        Assert.Equal(ReglaDecisionCoordinador.MaxJustificacion,
            ReglaDecisionCoordinador.Validar("SinAcuerdo", "Convalidar", new string('a', ReglaDecisionCoordinador.MaxJustificacion)).Justificacion.Length);
        Assert.Throws<DecisionCoordinadorInvalidaException>(() =>
            ReglaDecisionCoordinador.Validar("SinAcuerdo", "Convalidar", new string('a', ReglaDecisionCoordinador.MaxJustificacion + 1)));
        Assert.Throws<DecisionCoordinadorInvalidaException>(() => ReglaDecisionCoordinador.Validar("SinAcuerdo", "Convalidar", "Motivo\u0000oculto"));
    }
}
