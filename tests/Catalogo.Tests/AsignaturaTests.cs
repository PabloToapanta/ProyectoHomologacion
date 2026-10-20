using Catalogo.Domain;
using Xunit;

namespace Catalogo.Tests;

public class AsignaturaTests
{
    [Fact]
    public void Registra_datos_positivos_con_identidad_y_normaliza_textos()
    {
        var carreraId = Guid.NewGuid();
        var a = new Asignatura(carreraId, " demo-alg-001 ", " Álgebra   Lineal ", 1, 3.5m, 144);
        Assert.NotEqual(Guid.Empty, a.Id);
        Assert.Equal(carreraId, a.CarreraId);
        Assert.Equal("demo-alg-001", a.Codigo);
        Assert.Equal("DEMO-ALG-001", a.CodigoNormalizado);
        Assert.Equal("Álgebra Lineal", a.Nombre);
        Assert.Equal(1, a.Nivel);
        Assert.Equal(3.5m, a.Creditos);
        Assert.Equal(144, a.Horas);
    }

    [Theory]
    [InlineData(0, 3, 144)]
    [InlineData(-1, 3, 144)]
    [InlineData(1, 0, 144)]
    [InlineData(1, -1, 144)]
    [InlineData(1, 3, 0)]
    [InlineData(1, 3, -1)]
    public void Rechaza_nivel_creditos_u_horas_no_positivos(int nivel, decimal creditos, int horas) =>
        Assert.Throws<CatalogoException>(() => new Asignatura(Guid.NewGuid(), "C", "N", nivel, creditos, horas));

    [Theory]
    [InlineData(null, "N")]
    [InlineData(" ", "N")]
    [InlineData("C", null)]
    [InlineData("C", " ")]
    public void Rechaza_campos_vacios(string? codigo, string? nombre) =>
        Assert.Throws<CatalogoException>(() => new Asignatura(Guid.NewGuid(), codigo!, nombre!, 1, 3, 144));

    [Fact]
    public void Rechaza_carrera_vacia_y_valores_no_representables()
    {
        Assert.Throws<CatalogoException>(() => new Asignatura(Guid.Empty, "C", "N", 1, 3, 144));
        Assert.Throws<CatalogoException>(() => new Asignatura(Guid.NewGuid(), new string('c', 51), "N", 1, 3, 144));
        Assert.Throws<CatalogoException>(() => new Asignatura(Guid.NewGuid(), "C", new string('n', 201), 1, 3, 144));
        Assert.Throws<CatalogoException>(() => new Asignatura(Guid.NewGuid(), "C", "N", 1, 0.001m, 144));
        Assert.Throws<CatalogoException>(() => new Asignatura(Guid.NewGuid(), "C", "N", 1, 10000000m, 144));
    }
}
