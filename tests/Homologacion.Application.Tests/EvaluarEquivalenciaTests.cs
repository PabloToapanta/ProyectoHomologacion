using Homologacion.Application.Equivalencias;
using Homologacion.Domain.Equivalencias;
using Xunit;

namespace Homologacion.Application.Tests;

public class EvaluarEquivalenciaTests
{
    private sealed class Umbral(decimal valor) : IUmbralEquivalencia { public decimal Valor => valor; }
    private static readonly DatosAsignatura Origen = new("O", 144, 3, ["O1", "O2"]);
    private static readonly DatosAsignatura Receptora = new("R", 144, 3, ["R1", "R2", "R3", "R4", "R5"]);
    private static SolicitudEquivalencia Completa() => new(Origen, Receptora,
        [new("O1", "R1"), new("O1", "R2"), new("O2", "R3"), new("O2", "R4")]);

    [Fact]
    public void Calcula_cobertura_de_temas_receptores_sin_inferir_profundidad()
    {
        var resultado = new EvaluarEquivalencia(new Umbral(80)).Ejecutar(Completa());
        Assert.Equal(80, resultado.ContenidoPct);
        Assert.Equal(100, resultado.CargaHorariaPct);
        Assert.Equal(100, resultado.CreditosPct);
        Assert.Equal(new[] { "R5" }, resultado.TemasSinCobertura);
        Assert.Null(resultado.ProfundidadPct);
        Assert.Equal(EstadoEquivalencia.InformacionIncompleta, resultado.Estado);
        Assert.False(resultado.Homologable);
        Assert.Contains("Información incompleta", resultado.Justificacion);
        Assert.Contains("profundidad", resultado.Justificacion);
    }

    [Fact]
    public void Umbral_del_adaptador_se_refleja_en_el_resultado()
        => Assert.Equal(85, new EvaluarEquivalencia(new Umbral(85)).Ejecutar(Completa()).UmbralPct);

    [Fact]
    public void Correspondencias_vacias_declaran_cero_cobertura()
    {
        var resultado = new EvaluarEquivalencia(new Umbral(80)).Ejecutar(Completa() with { Correspondencias = [] });
        Assert.Equal(0, resultado.ContenidoPct);
        Assert.Equal(5, resultado.TemasSinCobertura.Count);
    }

    [Fact]
    public void Varios_temas_origen_para_un_receptor_no_inflan_el_porcentaje()
    {
        var resultado = new EvaluarEquivalencia(new Umbral(80)).Ejecutar(Completa() with
        { Correspondencias = [new("O1", "R1"), new("O2", "R1")] });
        Assert.Equal(20, resultado.ContenidoPct);
    }

    [Fact]
    public void Sin_datos_se_devuelve_informacion_incompleta()
    {
        var resultado = new EvaluarEquivalencia(new Umbral(80)).Ejecutar(new(null, null, null));
        Assert.Null(resultado.ContenidoPct);
        Assert.Null(resultado.CargaHorariaPct);
        Assert.Null(resultado.CreditosPct);
        Assert.False(resultado.Homologable);
    }

    [Fact]
    public void Lista_de_correspondencias_ausente_no_equivale_a_cero_cobertura()
        => Assert.Null(new EvaluarEquivalencia(new Umbral(80)).Ejecutar(Completa() with { Correspondencias = null }).ContenidoPct);

    [Fact]
    public void Temas_vacios_no_permiten_division_por_cero()
        => Assert.Null(new EvaluarEquivalencia(new Umbral(80)).Ejecutar(
            new(Origen, Receptora with { Temas = [] }, [])).ContenidoPct);

    [Theory]
    [InlineData(0)] [InlineData(-1)]
    public void Horas_y_creditos_invalidos_son_errores_explicitos(decimal cantidad)
    {
        Assert.Throws<DatosEvaluacionInvalidosException>(() => new EvaluarEquivalencia(new Umbral(80)).Ejecutar(
            Completa() with { Origen = Origen with { Horas = cantidad } }));
        Assert.Throws<DatosEvaluacionInvalidosException>(() => new EvaluarEquivalencia(new Umbral(80)).Ejecutar(
            Completa() with { Receptora = Receptora with { Creditos = cantidad } }));
    }

    [Fact]
    public void Correspondencia_hacia_tema_inexistente_es_invalida()
        => Assert.Throws<DatosEvaluacionInvalidosException>(() => new EvaluarEquivalencia(new Umbral(80)).Ejecutar(
            Completa() with { Correspondencias = [new("O1", "inexistente")] }));

    [Theory]
    [InlineData(null)] [InlineData("")] [InlineData("  ")]
    public void Identificador_de_tema_nulo_o_vacio_es_invalido(string? tema)
        => Assert.Throws<DatosEvaluacionInvalidosException>(() => new EvaluarEquivalencia(new Umbral(80)).Ejecutar(
            Completa() with { Origen = Origen with { Temas = [tema] } }));

    [Fact]
    public void Temas_duplicados_son_invalidos()
        => Assert.Throws<DatosEvaluacionInvalidosException>(() => new EvaluarEquivalencia(new Umbral(80)).Ejecutar(
            Completa() with { Receptora = Receptora with { Temas = ["R1", "R1"] } }));
}
