using System.Text;

namespace Catalogo.Domain;

public sealed class Asignatura
{
    public Guid Id { get; private set; }
    public Guid CarreraId { get; private set; }
    public string Codigo { get; private set; } = "";
    public string CodigoNormalizado { get; private set; } = "";
    public string Nombre { get; private set; } = "";
    public int Nivel { get; private set; }
    public decimal Creditos { get; private set; }
    public int Horas { get; private set; }
    private Asignatura() { }
    public Asignatura(Guid carreraId, string codigo, string nombre, int nivel, decimal creditos, int horas)
    {
        if (carreraId == Guid.Empty) throw new CatalogoException("validacion", "La carrera es obligatoria.");
        if (nivel <= 0 || creditos <= 0 || horas <= 0)
            throw new CatalogoException("validacion", "Nivel, créditos y horas deben ser positivos.");
        // Límite técnico de decimal(9,2); rechazar en vez de redondear datos académicos.
        if (creditos > 9999999.99m || decimal.Round(creditos, 2) != creditos)
            throw new CatalogoException("validacion", "Créditos admite hasta dos decimales y un máximo técnico de 9999999,99.");
        Codigo = Limpiar(codigo, "Código", 50);
        CodigoNormalizado = Limpiar(Codigo.ToUpperInvariant(), "Código", 50);
        Nombre = Limpiar(nombre, "Nombre", 200);
        Id = Guid.NewGuid();
        CarreraId = carreraId;
        Nivel = nivel;
        Creditos = creditos;
        Horas = horas;
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
