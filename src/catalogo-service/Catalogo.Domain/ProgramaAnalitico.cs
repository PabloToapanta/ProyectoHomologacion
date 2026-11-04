using System.Security.Cryptography;

namespace Catalogo.Domain;

/// <summary>HU-08: PDF del programa analítico del que se originó una asignatura, conservado para trazabilidad.</summary>
public sealed class ProgramaAnalitico
{
    public const int MaxBytes = 5 * 1024 * 1024;
    public Guid Id { get; private set; }
    public Guid AsignaturaId { get; private set; }
    public string NombreArchivo { get; private set; } = "";
    public string Sha256 { get; private set; } = "";
    public int Tamano { get; private set; }
    public byte[] Contenido { get; private set; } = [];
    public DateTimeOffset FechaCarga { get; private set; }
    public string CargadoPor { get; private set; } = "";
    private ProgramaAnalitico() { }

    public ProgramaAnalitico(Guid asignaturaId, string? nombreArchivo, byte[] contenido, DateTimeOffset fecha, string? cargadoPor)
    {
        if (asignaturaId == Guid.Empty) throw new CatalogoException("validacion", "La asignatura del programa analítico es obligatoria.");
        if (contenido is null || contenido.Length == 0 || contenido.Length > MaxBytes)
            throw new CatalogoException("validacion", $"El programa analítico debe tener entre 1 byte y {MaxBytes / (1024 * 1024)} MB.");
        if (string.IsNullOrWhiteSpace(cargadoPor)) throw new CatalogoException("validacion", "No se pudo identificar a quien carga el programa.");
        Id = Guid.NewGuid(); AsignaturaId = asignaturaId; Contenido = contenido; Tamano = contenido.Length;
        Sha256 = Convert.ToHexString(SHA256.HashData(contenido));
        // Solo el nombre visible, sin rutas del cliente; no se usa para decidir el tipo del archivo.
        var nombre = Path.GetFileName((nombreArchivo ?? "").Replace('\\', '/')).Trim();
        NombreArchivo = string.IsNullOrEmpty(nombre) || nombre.Any(char.IsControl) ? "programa.pdf" : nombre.Length > 200 ? nombre[..200] : nombre;
        FechaCarga = fecha; CargadoPor = cargadoPor.Trim().Length > 256 ? cargadoPor.Trim()[..256] : cargadoPor.Trim();
    }
}
