using Homologacion.Domain.Equivalencias;
using Xunit;

namespace Homologacion.Domain.Tests;

public class CoberturaAcademicaTests
{
    private static string[] Ids(string prefijo, int cantidad) => Enumerable.Range(1, cantidad).Select(i => prefijo + i).ToArray();

    [Theory]
    [InlineData(8, 4, true)] [InlineData(7, 4, false)]
    public void Formula_combina_elementos_con_igual_peso(int contenidos, int resultados, bool alcanzaOchenta)
    {
        var cobertura = CoberturaAcademica.Calcular(Ids("C", 10), Ids("R", 5), Ids("C", contenidos), Ids("R", resultados));
        Assert.Equal((decimal)(contenidos + resultados) / 15 * 100, cobertura.Porcentaje);
        Assert.Equal(alcanzaOchenta, cobertura.Porcentaje >= 80);
        Assert.Equal(10 - contenidos, cobertura.ContenidosSinCobertura.Count);
        Assert.Equal(5 - resultados, cobertura.ResultadosSinCobertura.Count);
    }

    [Fact]
    public void No_utiliza_promedios_separados_ni_ponderacion_cincuenta_cincuenta()
        => Assert.Equal(10, CoberturaAcademica.Calcular(["C"], Ids("R", 9), ["C"], []).Porcentaje);

    [Fact]
    public void Repeticiones_no_duplican_elementos_receptores_y_categorias_son_independientes()
    {
        var resultado = CoberturaAcademica.Calcular(["1"], ["1"], ["1", "1"], ["1", "1"]);
        Assert.Equal(100, resultado.Porcentaje);
        Assert.Empty(resultado.ContenidosSinCobertura);
        Assert.Empty(resultado.ResultadosSinCobertura);
    }

    [Fact]
    public void Datos_ausentes_total_cero_o_ids_invalidos_son_incompletos()
    {
        Assert.Null(CoberturaAcademica.Calcular(null, ["R"], [], []).Porcentaje);
        Assert.Null(CoberturaAcademica.Calcular(["C"], null, [], []).Porcentaje);
        Assert.Null(CoberturaAcademica.Calcular([], [], [], []).Porcentaje);
        Assert.Null(CoberturaAcademica.Calcular([""], [], [], []).Porcentaje);
        Assert.Null(CoberturaAcademica.Calcular(["C", "C"], [], [], []).Porcentaje);
        Assert.Null(CoberturaAcademica.Calcular(["C"], [], ["inexistente"], []).Porcentaje);
        Assert.Null(CoberturaAcademica.Calcular(["C"], [], null, []).Porcentaje);
    }

    [Fact]
    public void Categoria_vacia_conocida_es_valida_si_existe_otro_elemento_receptor()
        => Assert.Equal(100, CoberturaAcademica.Calcular(["C"], [], ["C"], []).Porcentaje);
}
