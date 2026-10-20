using Catalogo.Domain;
using Xunit;

namespace Catalogo.Tests;

public class CarreraTests
{
    [Theory]
    [InlineData(" fisei ")]
    [InlineData("FiSeI")]
    public void Canoniza_facultad_receptora(string facultad)
    {
        var carrera = new Carrera("Software", facultad, true);
        Assert.Equal("FISEI", carrera.Facultad);
        Assert.Equal("FISEI", carrera.FacultadNormalizada);
    }

    [Theory]
    [InlineData("Otra facultad")]
    [InlineData("FISEI UTA")]
    [InlineData("Facultad de Ingeniería en Sistemas, Electrónica e Industrial")]
    public void Rechaza_facultad_fuera_del_catalogo_receptor(string facultad)
    {
        var error = Assert.Throws<CatalogoException>(() => new Carrera("Software", facultad, true));
        Assert.Equal("validacion", error.Codigo);
        Assert.Contains("FISEI", error.Message);
    }

    [Fact]
    public void Cambiar_facultad_no_modifica_ningun_dato()
    {
        var carrera = new Carrera("Original", "FISEI", true);
        var id = carrera.Id;
        Assert.Throws<CatalogoException>(() => carrera.Actualizar("Cambio", "Otra", false));
        Assert.Equal(id, carrera.Id);
        Assert.Equal("Original", carrera.Nombre);
        Assert.Equal("FISEI", carrera.Facultad);
        Assert.True(carrera.Vigencia);
    }

    [Fact]
    public void Crear_normaliza_campos_y_conserva_vigencia()
    {
        var carrera = new Carrera("  Software   de prueba ", " FISEI ", false, " Presencial ");
        Assert.NotEqual(Guid.Empty, carrera.Id);
        Assert.Equal("Software de prueba", carrera.Nombre);
        Assert.Equal("FISEI", carrera.Facultad);
        Assert.Equal("Presencial", carrera.Modalidad);
        Assert.False(carrera.Vigencia);
    }

    [Theory]
    [InlineData(null, "FISEI")]
    [InlineData(" ", "FISEI")]
    [InlineData("Software", null)]
    [InlineData("Software", "\t")]
    public void Rechaza_campos_requeridos(string? nombre, string? facultad) =>
        Assert.Throws<CatalogoException>(() => new Carrera(nombre!, facultad!, true));

    [Fact]
    public void Rechaza_longitudes_excesivas()
    {
        Assert.Throws<CatalogoException>(() => new Carrera(new string('a', 201), "FISEI", true));
        Assert.Throws<CatalogoException>(() => new Carrera("N", new string('a', 201), true));
        Assert.Throws<CatalogoException>(() => new Carrera("N", "FISEI", true, new string('a', 101)));
    }

    [Fact]
    public void Clave_ignora_caso_espacios_y_variantes_unicode()
    {
        var a = new Carrera("  Ingeniería   de prueba", " Fisei ", true);
        var b = new Carrera("ingenieri\u0301a de prueba", "FISEI", false);
        Assert.Equal(a.NombreNormalizado, b.NombreNormalizado);
        Assert.Equal(a.FacultadNormalizada, b.FacultadNormalizada);
    }

    [Fact]
    public void Actualiza_sin_cambiar_id_y_validacion_fallida_no_modifica_datos()
    {
        var c = new Carrera("Original", "FISEI", true);
        var id = c.Id;
        c.Actualizar("Editada", " fisei ", false, "Virtual");
        Assert.Equal(id, c.Id);
        Assert.Equal("Editada", c.Nombre);
        Assert.False(c.Vigencia);
        Assert.Throws<CatalogoException>(() => c.Actualizar("Cambio", "", true));
        Assert.Equal("Editada", c.Nombre);
    }
}
