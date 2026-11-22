namespace Solicitudes.Domain;

public sealed class AdjuntoSolicitud
{
    public const string HistorialAcademico = "Historial académico";
    public const string Silabo = "Sílabo";

    public Guid Id { get; private set; }
    public Guid SolicitudId { get; private set; }
    public string Tipo { get; private set; } = "";
    public string NombreArchivo { get; private set; } = "";
    public long TamanoBytes { get; private set; }
    public string HuellaSha256 { get; private set; } = "";
    public string SubidoPor { get; private set; } = "";
    /// <summary>Nombre de usuario del token de quien subió el archivo (CA3); SubidoPor conserva su identificador.</summary>
    public string SubidoPorNombre { get; private set; } = "";
    public DateTime FechaSubidaUtc { get; private set; }
    public byte[] Contenido { get; private set; } = [];

    private AdjuntoSolicitud() { }

    public AdjuntoSolicitud(Guid solicitudId, string tipo, string nombreArchivo, string subidoPor, string subidoPorNombre, DateTime fechaSubidaUtc, byte[] contenido)
    {
        if (solicitudId == Guid.Empty) throw new SolicitudException("La solicitud es obligatoria.");
        if (tipo is not (HistorialAcademico or Silabo)) throw new SolicitudException("Seleccione historial académico o sílabo.");
        if (string.IsNullOrWhiteSpace(nombreArchivo) || nombreArchivo.Length > 255 || nombreArchivo.Any(char.IsControl)) throw new SolicitudException("El nombre del archivo no es válido.");
        if (string.IsNullOrWhiteSpace(subidoPor) || subidoPor.Length > 256) throw new SolicitudException("No se pudo identificar a quien subió el archivo.");
        if (string.IsNullOrWhiteSpace(subidoPorNombre) || subidoPorNombre.Length > 256 || subidoPorNombre.Any(char.IsControl)) throw new SolicitudException("No se pudo identificar a quien subió el archivo.");
        if (contenido is not { Length: > 0 }) throw new SolicitudException("Seleccione un archivo PDF.");
        if (fechaSubidaUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("La fecha de subida debe estar en UTC.", nameof(fechaSubidaUtc));

        Id = Guid.NewGuid();
        SolicitudId = solicitudId;
        Tipo = tipo;
        NombreArchivo = nombreArchivo;
        TamanoBytes = contenido.LongLength;
        HuellaSha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(contenido));
        SubidoPor = subidoPor;
        SubidoPorNombre = subidoPorNombre;
        FechaSubidaUtc = fechaSubidaUtc;
        Contenido = contenido;
    }
}
