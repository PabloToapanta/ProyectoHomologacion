using System.Security.Cryptography;
using System.Text;

namespace Catalogo.Domain;

public enum EstadoNormativa { Vigente, Reformada, Derogada }

/// <summary>
/// HU-38: normativa institucional que respalda las reglas de homologación. Rige siempre la aprobada más reciente:
/// registrar una reforma deja la anterior como Reformada. El PDF se conserva con su huella para trazabilidad.
/// </summary>
public sealed class Normativa
{
    public const int MaxBytes = 10 * 1024 * 1024;
    public Guid Id { get; private set; }
    public string Codigo { get; private set; } = "";
    public string CodigoNormalizado { get; private set; } = "";
    public string Titulo { get; private set; } = "";
    public string Emisor { get; private set; } = "";
    public DateOnly FechaEmision { get; private set; }
    public DateOnly VigenteDesde { get; private set; }
    public EstadoNormativa Estado { get; private set; }
    public Guid? ReformaAId { get; private set; }
    public Guid? ReformadaPorId { get; private set; }
    public string? MotivoDerogacion { get; private set; }
    public DateTimeOffset? FechaDerogacion { get; private set; }
    public string NombreArchivo { get; private set; } = "";
    public string Sha256 { get; private set; } = "";
    public int Tamano { get; private set; }
    public byte[] Contenido { get; private set; } = [];
    public DateTimeOffset FechaRegistro { get; private set; }
    public string RegistradaPor { get; private set; } = "";

    private Normativa() { }

    public Normativa(string? codigo, string? titulo, string? emisor, DateOnly fechaEmision, DateOnly vigenteDesde,
        Guid? reformaAId, string? nombreArchivo, byte[]? contenido, DateTimeOffset fechaRegistro, string? registradaPor)
    {
        Codigo = Texto(codigo, 50, "El código de resolución");
        CodigoNormalizado = Codigo.ToUpperInvariant();
        Titulo = Texto(titulo, 300, "El título");
        Emisor = Texto(emisor, 200, "El órgano emisor");
        if (vigenteDesde < fechaEmision)
            throw new CatalogoException("validacion", "La vigencia no puede comenzar antes de la fecha de emisión.");
        if (contenido is null || contenido.Length == 0 || contenido.Length > MaxBytes)
            throw new CatalogoException("validacion", $"El documento debe tener entre 1 byte y {MaxBytes / (1024 * 1024)} MB.");
        // El tipo se comprueba por el contenido, no por el nombre ni el tipo declarado.
        if (!contenido.AsSpan().StartsWith("%PDF-"u8))
            throw new CatalogoException("validacion", "El documento de la normativa debe ser un PDF.");
        if (reformaAId == Guid.Empty) throw new CatalogoException("validacion", "La normativa reformada no es válida.");
        var usuario = registradaPor?.Trim();
        if (string.IsNullOrEmpty(usuario)) throw new CatalogoException("validacion", "No se pudo identificar a quien registra la normativa.");
        Id = Guid.NewGuid();
        FechaEmision = fechaEmision;
        VigenteDesde = vigenteDesde;
        Estado = EstadoNormativa.Vigente;
        ReformaAId = reformaAId;
        Contenido = contenido;
        Tamano = contenido.Length;
        Sha256 = Convert.ToHexString(SHA256.HashData(contenido));
        var nombre = Path.GetFileName((nombreArchivo ?? "").Replace('\\', '/')).Trim();
        NombreArchivo = string.IsNullOrEmpty(nombre) || nombre.Any(char.IsControl) ? "normativa.pdf" : nombre.Length > 200 ? nombre[..200] : nombre;
        FechaRegistro = fechaRegistro;
        RegistradaPor = usuario.Length > 256 ? usuario[..256] : usuario;
    }

    /// <summary>Una reforma deja de regir a la norma vigente que modifica.</summary>
    public void MarcarReformadaPor(Guid reformaId)
    {
        if (Estado != EstadoNormativa.Vigente)
            throw new CatalogoException("conflicto", "Solo se puede reformar una normativa vigente.");
        Estado = EstadoNormativa.Reformada;
        ReformadaPorId = reformaId;
    }

    /// <summary>Al eliminar una reforma registrada por error, la norma que reformaba vuelve a regir.</summary>
    public void Restituir()
    {
        if (Estado != EstadoNormativa.Reformada)
            throw new CatalogoException("conflicto", "Solo se restituye una normativa marcada como reformada.");
        Estado = EstadoNormativa.Vigente;
        ReformadaPorId = null;
    }

    public void Derogar(string? motivo, DateTimeOffset fecha, string? usuario)
    {
        var texto = Texto(motivo, 500, "El motivo de la derogación");
        if (string.IsNullOrWhiteSpace(usuario)) throw new CatalogoException("validacion", "No se pudo identificar a quien deroga la normativa.");
        if (Estado != EstadoNormativa.Vigente)
            throw new CatalogoException("conflicto", "Solo se puede derogar una normativa vigente.");
        Estado = EstadoNormativa.Derogada;
        MotivoDerogacion = texto;
        FechaDerogacion = fecha;
    }

    public static string Texto(string? valor, int maximo, string campo)
    {
        string limpio;
        try
        {
            limpio = string.Join(" ", (valor ?? "").Normalize(NormalizationForm.FormKC)
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        }
        catch (ArgumentException)
        {
            throw new CatalogoException("validacion", $"{campo} contiene caracteres no válidos.");
        }
        if (limpio.Length == 0 || limpio.Length > maximo || limpio.Any(char.IsControl))
            throw new CatalogoException("validacion", $"{campo} es obligatorio y admite hasta {maximo} caracteres.");
        return limpio;
    }
}

public enum TipoEventoNormativa { Registrada, Reformada, Restituida, Derogada, Eliminada }

/// <summary>HU-38: historial de solo inserción; sobrevive a la eliminación de la normativa.</summary>
public sealed class EventoNormativa
{
    public Guid Id { get; private set; }
    public Guid NormativaId { get; private set; }
    public string CodigoNormativa { get; private set; } = "";
    public TipoEventoNormativa Tipo { get; private set; }
    public DateTimeOffset Fecha { get; private set; }
    public string Usuario { get; private set; } = "";
    public string? Detalle { get; private set; }

    private EventoNormativa() { }

    public EventoNormativa(Normativa normativa, TipoEventoNormativa tipo, DateTimeOffset fecha, string usuario, string? detalle)
    {
        Id = Guid.NewGuid();
        NormativaId = normativa.Id;
        CodigoNormativa = normativa.Codigo;
        Tipo = tipo;
        Fecha = fecha;
        Usuario = usuario.Trim().Length > 256 ? usuario.Trim()[..256] : usuario.Trim();
        Detalle = detalle is null ? null : detalle.Length > 600 ? detalle[..600] : detalle;
    }
}
