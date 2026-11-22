using System.Text;

namespace Catalogo.Domain;

/// <summary>HU-07: identidad de una versión de malla, independiente de la vigencia de la carrera.</summary>
public sealed class Malla
{
    public Guid Id { get; private set; }
    public Guid CarreraId { get; private set; }
    public string Nombre { get; private set; } = "";
    public string NombreNormalizado { get; private set; } = "";
    public bool Vigente { get; private set; }

    private Malla() { }

    public Malla(Guid carreraId, string nombre, bool vigente)
    {
        if (carreraId == Guid.Empty)
            throw new CatalogoException("validacion", "La carrera de la malla es obligatoria.");
        Nombre = Limpiar(nombre);
        NombreNormalizado = Limpiar(Nombre.ToUpperInvariant());
        Id = Guid.NewGuid();
        CarreraId = carreraId;
        Vigente = vigente;
    }

    /// <summary>El repositorio aplica la transición entre versiones de una carrera de forma atómica.</summary>
    public void EstablecerVigencia(bool vigente) => Vigente = vigente;

    private static string Limpiar(string? nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new CatalogoException("validacion", "El nombre de la malla es obligatorio.");
        string limpio;
        try
        {
            limpio = string.Join(" ", nombre.Normalize(NormalizationForm.FormKC)
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        }
        catch (ArgumentException)
        {
            throw new CatalogoException("validacion", "El nombre de la malla contiene caracteres no válidos.");
        }
        if (limpio.Length is 0 or > 100 || limpio.Any(char.IsControl))
            throw new CatalogoException("validacion", "El nombre de la malla debe contener entre 1 y 100 caracteres válidos.");
        return limpio;
    }
}
