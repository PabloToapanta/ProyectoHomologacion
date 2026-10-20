using System.Text;

namespace Catalogo.Domain;

public sealed class Carrera
{
    public const string FacultadReceptora = "FISEI";
    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = "";
    public string Facultad { get; private set; } = "";
    public string? Modalidad { get; private set; }
    public bool Vigencia { get; private set; }
    public string NombreNormalizado { get; private set; } = "";
    public string FacultadNormalizada { get; private set; } = "";
    private Carrera() { }
    public Carrera(string nombre, string facultad, bool vigencia, string? modalidad = null)
    {
        Actualizar(nombre, facultad, vigencia, modalidad);
        Id = Guid.NewGuid();
    }

    public void Actualizar(string nombre, string facultad, bool vigencia, string? modalidad = null)
    {
        // Validar todo antes de modificar una entidad existente.
        var nuevoNombre = Limpiar(nombre, "Nombre", 200);
        var nuevaFacultad = Limpiar(facultad, "Facultad", 200);
        if (!string.Equals(nuevaFacultad, FacultadReceptora, StringComparison.OrdinalIgnoreCase))
            throw new CatalogoException("validacion", "El catálogo receptor admite únicamente carreras de FISEI.");
        nuevaFacultad = FacultadReceptora;
        var nuevaModalidad = string.IsNullOrWhiteSpace(modalidad) ? null : Limpiar(modalidad, "Modalidad", 100);
        var nombreNormalizado = Limpiar(nuevoNombre.ToUpperInvariant(), "Nombre", 200);
        var facultadNormalizada = Limpiar(nuevaFacultad.ToUpperInvariant(), "Facultad", 200);
        Nombre = nuevoNombre;
        Facultad = nuevaFacultad;
        Modalidad = nuevaModalidad;
        Vigencia = vigencia;
        NombreNormalizado = nombreNormalizado;
        FacultadNormalizada = facultadNormalizada;
    }

    private static string Limpiar(string? valor, string campo, int maximo)
    {
        if (string.IsNullOrWhiteSpace(valor)) throw new CatalogoException("validacion", $"{campo} es obligatorio.");
        string limpio;
        try { limpio = string.Join(" ", valor.Normalize(NormalizationForm.FormKC).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)); }
        catch (ArgumentException) { throw new CatalogoException("validacion", $"{campo} contiene caracteres no válidos."); }
        if (limpio.Length == 0 || limpio.Length > maximo || limpio.Any(char.IsControl))
            throw new CatalogoException("validacion", $"{campo} debe contener entre 1 y {maximo} caracteres válidos.");
        return limpio;
    }
}
