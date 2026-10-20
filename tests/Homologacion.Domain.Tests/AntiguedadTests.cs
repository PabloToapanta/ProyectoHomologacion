using Homologacion.Domain.Antiguedad;
using Xunit;

namespace Homologacion.Domain.Tests;

public class AntiguedadTests
{
    [Theory]
    [InlineData(2017, 10, 20, EstadoAntiguedad.Elegible)]
    [InlineData(2016, 10, 21, EstadoAntiguedad.Elegible)]
    [InlineData(2016, 10, 20, EstadoAntiguedad.Elegible)]
    [InlineData(2016, 10, 19, EstadoAntiguedad.RequiereValidacionConocimientos)]
    [InlineData(2015, 10, 20, EstadoAntiguedad.RequiereValidacionConocimientos)]
    [InlineData(2026, 10, 20, EstadoAntiguedad.Elegible)]
    public void Evalua_aprobacion_por_aniversario(int anio, int mes, int dia, EstadoAntiguedad esperado)
        => Assert.Equal(esperado, ValidadorAntiguedad.Validar(EstadoAsignatura.Aprobada,
            new DateOnly(anio, mes, dia), new DateOnly(2026, 10, 20)).Estado);

    [Theory]
    [InlineData(EstadoAsignatura.Reprobada)]
    [InlineData(EstadoAsignatura.NoCursada)]
    public void Sin_aprobacion_no_es_elegible(EstadoAsignatura estado)
        => Assert.Equal(EstadoAntiguedad.NoElegible,
            ValidadorAntiguedad.Validar(estado, null, new DateOnly(2026, 10, 20)).Estado);

    [Theory]
    [InlineData(2, 27, EstadoAntiguedad.Elegible)]
    [InlineData(2, 28, EstadoAntiguedad.Elegible)]
    [InlineData(3, 1, EstadoAntiguedad.RequiereValidacionConocimientos)]
    public void Aniversario_de_29_febrero_en_anio_no_bisiesto(int mes, int dia, EstadoAntiguedad esperado)
        => Assert.Equal(esperado, ValidadorAntiguedad.Validar(EstadoAsignatura.Aprobada,
            new DateOnly(2016, 2, 29), new DateOnly(2026, mes, dia)).Estado);

    [Fact]
    public void Fecha_futura_es_dato_invalido()
        => Assert.Throws<ArgumentException>(() => ValidadorAntiguedad.Validar(
            EstadoAsignatura.Aprobada, new DateOnly(2026, 10, 21), new DateOnly(2026, 10, 20)));

    [Fact]
    public void Aprobada_sin_fecha_es_informacion_incompleta()
        => Assert.Throws<ArgumentException>(() => ValidadorAntiguedad.Validar(
            EstadoAsignatura.Aprobada, null, new DateOnly(2026, 10, 20)));

    [Fact]
    public void Estado_desconocido_no_se_interpreta_como_aprobacion()
        => Assert.Throws<ArgumentException>(() => ValidadorAntiguedad.Validar(
            (EstadoAsignatura)99, null, new DateOnly(2026, 10, 20)));

    [Fact]
    public void Anios_finales_del_calendario_no_desbordan()
        => Assert.Equal(EstadoAntiguedad.Elegible, ValidadorAntiguedad.Validar(
            EstadoAsignatura.Aprobada, new DateOnly(9998, 1, 1), new DateOnly(9999, 1, 1)).Estado);
}
