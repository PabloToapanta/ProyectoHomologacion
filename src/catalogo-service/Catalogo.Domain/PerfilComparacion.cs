namespace Catalogo.Domain;

public sealed class PerfilComparacion
{
    public Guid AsignaturaId { get; private set; }
    public string PlanAnalitico { get; private set; } = "";
    public string UnidadHoras { get; private set; } = "";
    public string SistemaCreditos { get; private set; } = "";
    public bool EsSintetico { get; private set; }
    private PerfilComparacion() { }
    public PerfilComparacion(Guid asignaturaId, string plan, string unidad, string sistema, bool sintetico)
    {
        if (asignaturaId == Guid.Empty) throw new CatalogoException("validacion", "La asignatura es obligatoria.");
        AsignaturaId = asignaturaId; Actualizar(plan, unidad, sistema, sintetico);
    }
    public void Actualizar(string plan, string unidad, string sistema, bool sintetico)
    {
        PlanAnalitico = Texto(plan, "Referencia al plan", 500);
        UnidadHoras = Texto(unidad, "Unidad de horas", 100);
        SistemaCreditos = Texto(sistema, "Sistema de créditos", 100);
        EsSintetico = sintetico;
    }
    private static string Texto(string? texto, string campo, int maximo)
    {
        if (string.IsNullOrWhiteSpace(texto) || texto.Trim().Length > maximo || texto.Any(char.IsControl))
            throw new CatalogoException("validacion", $"{campo} es obligatorio y admite hasta {maximo} caracteres sin controles.");
        return texto.Trim();
    }
}

public sealed class RevisionComparacion
{
    public Guid OrigenId { get; private set; }
    public Guid ReceptoraId { get; private set; }
    public string FirmaOrigen { get; private set; } = "";
    public string FirmaReceptora { get; private set; } = "";
    public string DatosJson { get; private set; } = "";
    private RevisionComparacion() { }
    public RevisionComparacion(Guid origen, Guid receptora, string firmaOrigen, string firmaReceptora, string json)
    {
        if (origen == receptora || origen == Guid.Empty || receptora == Guid.Empty)
            throw new CatalogoException("validacion", "Selecciona dos asignaturas distintas.");
        OrigenId = origen; ReceptoraId = receptora; Actualizar(firmaOrigen, firmaReceptora, json);
    }
    public void Actualizar(string origen, string receptora, string json)
    { FirmaOrigen = origen; FirmaReceptora = receptora; DatosJson = json; }
}
