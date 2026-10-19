namespace Catalogo.Domain;

public sealed class CatalogoException(string codigo, string mensaje) : Exception(mensaje)
{
    public string Codigo { get; } = codigo;
}
