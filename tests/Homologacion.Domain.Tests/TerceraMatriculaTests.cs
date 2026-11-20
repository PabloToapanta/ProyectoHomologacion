using Homologacion.Domain.TerceraMatricula;
using Xunit;

namespace Homologacion.Domain.Tests;

/// <summary>HU-17 / RN-07 (decisión del 20/11/2026): se excluye la asignatura objeto de tercera matrícula, no la solicitud.</summary>
public class TerceraMatriculaTests
{
    [Theory]
    [InlineData(1, ResultadoTerceraMatricula.NoAplica)]
    [InlineData(2, ResultadoTerceraMatricula.NoAplica)]
    [InlineData(3, ResultadoTerceraMatricula.Excluida)]
    public void Solo_la_tercera_matricula_excluye_la_asignatura(int intento, ResultadoTerceraMatricula esperado) =>
        Assert.Equal(esperado, ReglaTerceraMatricula.Evaluar(intento));

    [Fact]
    public void Sin_intento_registrado_no_se_asume_primera_matricula() =>
        Assert.Equal(ResultadoTerceraMatricula.IntentoDesconocido, ReglaTerceraMatricula.Evaluar(null));

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(-1)]
    public void Intento_fuera_de_primera_a_tercera_es_invalido(int intento) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => ReglaTerceraMatricula.Evaluar(intento));

    [Fact]
    public void El_motivo_indica_que_solo_se_excluye_la_asignatura()
    {
        Assert.Contains("RN-07", ReglaTerceraMatricula.Motivo);
        Assert.Contains("tercera matrícula", ReglaTerceraMatricula.Motivo);
        Assert.Contains("demás asignaturas", ReglaTerceraMatricula.Motivo);
    }
}
