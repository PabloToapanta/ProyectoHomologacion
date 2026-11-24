using Catalogo.Domain;
using Xunit;

namespace Catalogo.Tests;

/// <summary>HU-07 / Issue #68: reglas propias de una versión, sin infraestructura.</summary>
public class MallaTests
{
    [Fact]
    public void Registra_identidad_carrera_nombre_normalizado_y_vigencia_independiente()
    {
        var carrera = Guid.NewGuid();
        var malla = new Malla(carrera, "  Plan   2026  ", false);
        Assert.NotEqual(Guid.Empty, malla.Id);
        Assert.Equal(carrera, malla.CarreraId);
        Assert.Equal("Plan 2026", malla.Nombre);
        Assert.Equal("PLAN 2026", malla.NombreNormalizado);
        Assert.False(malla.Vigente);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Plan\u0000")]
    public void Rechaza_nombre_invalido(string? nombre)
    {
        var error = Assert.Throws<CatalogoException>(() => new Malla(Guid.NewGuid(), nombre!, true));
        Assert.Equal("validacion", error.Codigo);
    }

    [Fact]
    public void Rechaza_nombre_fuera_del_limite_y_carrera_vacia()
    {
        Assert.Throws<CatalogoException>(() => new Malla(Guid.NewGuid(), new string('A', 101), false));
        Assert.Throws<CatalogoException>(() => new Malla(Guid.Empty, "Plan", true));
        Assert.Equal(100, new Malla(Guid.NewGuid(), new string('A', 100), true).Nombre.Length);
    }

    [Fact]
    public void Rechaza_unicode_mal_formado()
    {
        // Construirlo en ejecución: un argumento de atributo no conserva un surrogate aislado.
        var nombre = new string((char)0xD800, 1);
        Assert.Throws<CatalogoException>(() => new Malla(Guid.NewGuid(), nombre, true));
    }

    [Fact]
    public void Normaliza_unicode_antes_de_comparar_nombres()
    {
        var carrera = Guid.NewGuid();
        Assert.Equal(new Malla(carrera, "Ｐｌａｎ 2026", true).NombreNormalizado,
            new Malla(carrera, "plan 2026", false).NombreNormalizado);
    }

    [Fact]
    public void Cambiar_vigencia_conserva_identificador_carrera_y_nombre()
    {
        var malla = new Malla(Guid.NewGuid(), "Plan anterior", true);
        var identidad = (malla.Id, malla.CarreraId, malla.Nombre, malla.NombreNormalizado);
        malla.EstablecerVigencia(false);
        Assert.False(malla.Vigente);
        malla.EstablecerVigencia(true);
        Assert.True(malla.Vigente);
        Assert.Equal(identidad, (malla.Id, malla.CarreraId, malla.Nombre, malla.NombreNormalizado));
    }
}
