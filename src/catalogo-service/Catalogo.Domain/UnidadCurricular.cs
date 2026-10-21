namespace Catalogo.Domain;

public sealed class UnidadCurricular
{
    public Guid Id { get; private set; }
    public Guid AsignaturaId { get; private set; }
    public int Numero { get; private set; }
    public string Nombre { get; private set; } = "";
    public int Horas { get; private set; }
    public string ResultadoAprendizaje { get; private set; } = "";
    public List<Contenido> Contenidos { get; private set; } = [];
    public List<UnidadTematica> Tematicas { get; private set; } = [];
    private UnidadCurricular() { }
    public UnidadCurricular(Guid asignaturaId, int numero, string nombre, int horas, string resultadoAprendizaje, IReadOnlyList<string>? contenidos)
    {
        if (asignaturaId == Guid.Empty || numero <= 0 || horas <= 0)
            throw new CatalogoException("validacion", "Asignatura, número de unidad y horas positivas son obligatorios.");
        Id = Guid.NewGuid(); AsignaturaId = asignaturaId; Numero = numero; Horas = horas;
        Nombre = Texto(nombre, "Nombre", 200);
        ResultadoAprendizaje = Texto(resultadoAprendizaje, "Resultado de aprendizaje", 4000);
        if (contenidos is null || contenidos.Count == 0 || contenidos.Count > 200)
            throw new CatalogoException("validacion", "Registra entre 1 y 200 contenidos por unidad.");
        Contenidos = contenidos.Select((texto, i) => new Contenido(Id, i + 1, Texto(texto, "Contenido", 4000))).ToList();
    }
    public void Actualizar(int numero, string nombre, int horas, string resultado, IReadOnlyList<string> contenidos)
    {
        // Validar todo antes de modificar el agregado cargado.
        var datos = new UnidadCurricular(AsignaturaId, numero, nombre, horas, resultado, contenidos);
        Numero = datos.Numero; Nombre = datos.Nombre; Horas = datos.Horas; ResultadoAprendizaje = datos.ResultadoAprendizaje;
        foreach (var contenido in Contenidos.ToList())
        {
            if (contenido.Orden > datos.Contenidos.Count) Contenidos.Remove(contenido);
            else contenido.Actualizar(datos.Contenidos[contenido.Orden - 1].Tema);
        }
        for (var i = Contenidos.Count; i < datos.Contenidos.Count; i++)
            Contenidos.Add(new Contenido(Id, i + 1, datos.Contenidos[i].Tema));
    }
    public void Clasificar(IReadOnlyList<TematicaDatos> tematicas)
    {
        if (tematicas.Count is < 1 or > 100) throw new CatalogoException("validacion", "Registra entre 1 y 100 unidades temáticas.");
        var validadas = tematicas.Select(t => new TematicaDatos(Texto(t.Nombre, "Nombre de temática", 200),
            t.Contenidos is { Count: > 0 and <= 200 } ? t.Contenidos.Select(c => Texto(c, "Contenido", 4000)).ToList()
            : throw new CatalogoException("validacion", "Cada temática necesita entre 1 y 200 contenidos."))).ToList();
        if (validadas.Select(t => t.Nombre.ToUpperInvariant()).Distinct().Count() != validadas.Count)
            throw new CatalogoException("validacion", "No repitas nombres de unidades temáticas.");
        if (validadas.Sum(t => t.Contenidos.Count) != Contenidos.Count)
            throw new CatalogoException("validacion", "La clasificación debe incluir todos los contenidos.");
        Tematicas = Tematicas.OrderBy(t => t.Orden).ToList();
        while (Tematicas.Count > validadas.Count) Tematicas.RemoveAt(Tematicas.Count - 1);
        var indice = 0;
        for (var i = 0; i < validadas.Count; i++)
        {
            if (i >= Tematicas.Count) Tematicas.Add(new UnidadTematica(Id, i + 1, validadas[i].Nombre));
            else Tematicas[i].Renombrar(validadas[i].Nombre);
            foreach (var texto in validadas[i].Contenidos)
                Contenidos.OrderBy(c => c.Orden).ElementAt(indice++).Clasificar(Tematicas[i].Id);
        }
    }
    private static string Texto(string? value, string campo, int max)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > max)
            throw new CatalogoException("validacion", $"{campo} es obligatorio y admite hasta {max} caracteres.");
        return value.Trim();
    }
}

public sealed class Contenido
{
    public Guid Id { get; private set; }
    public Guid UnidadCurricularId { get; private set; }
    public Guid? UnidadTematicaId { get; private set; }
    public int Orden { get; private set; }
    public string Tema { get; private set; } = "";
    private Contenido() { }
    internal void Clasificar(Guid id) => UnidadTematicaId = id;
    internal void Actualizar(string tema) => Tema = tema;
    internal Contenido(Guid unidadId, int orden, string tema)
    { Id = Guid.NewGuid(); UnidadCurricularId = unidadId; Orden = orden; Tema = tema; }
}

public record TematicaDatos(string Nombre, IReadOnlyList<string> Contenidos);
public sealed class UnidadTematica
{
    public Guid Id { get; private set; }
    public Guid UnidadCurricularId { get; private set; }
    public int Orden { get; private set; }
    public string Nombre { get; private set; } = "";
    private UnidadTematica() { }
    internal UnidadTematica(Guid unidadId, int orden, string nombre)
    { Id = Guid.NewGuid(); UnidadCurricularId = unidadId; Orden = orden; Nombre = nombre; }
    internal void Renombrar(string nombre) => Nombre = nombre;
}
