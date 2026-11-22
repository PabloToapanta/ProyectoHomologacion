using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Homologacion.Application.AnalisisInterno;

/// <summary>HU-37: identifica el modelo y la plantilla con que el agente genera la explicación. Sin ella no se reutiliza.</summary>
public interface IDescripcionAgente
{
    string Modelo { get; }
    string VersionPlantilla { get; }
}

/// <summary>HU-37: explicación del agente guardada para reutilizarla. No contiene datos del estudiante.</summary>
public sealed record ExplicacionGuardada(Guid Id, string Clave, DateTimeOffset Fecha, string Modelo, string VersionPlantilla,
    Guid VersionParametros, ExplicacionAgente Explicacion, Guid? ForzadoPorId, string? Motivo);

/// <summary>HU-37: almacén solo de inserción; no ofrece modificar ni eliminar explicaciones.</summary>
public interface IExplicacionesAgente
{
    /// <summary>La explicación más reciente con esa clave, o null.</summary>
    Task<ExplicacionGuardada?> BuscarAsync(string clave, CancellationToken ct);
    Task GuardarAsync(ExplicacionGuardada explicacion, CancellationToken ct);
}

/// <summary>HU-37 / CA4: el docente asignado pide un análisis nuevo del agente con un motivo.</summary>
public sealed record ForzarAnalisis
{
    public const int MaxMotivo = 500;
    public Guid DocenteId { get; }
    public string Motivo { get; }
    private ForzarAnalisis(Guid docenteId, string motivo) { DocenteId = docenteId; Motivo = motivo; }
    public static ForzarAnalisis Crear(Guid docenteId, string? motivo)
    {
        if (docenteId == Guid.Empty) throw new DatosAnalisisSolicitudException("No se pudo identificar al docente.");
        var texto = motivo?.Trim() ?? "";
        if (texto.Length == 0) throw new DatosAnalisisSolicitudException("Indique el motivo para forzar un análisis nuevo.");
        if (texto.Length > MaxMotivo) throw new DatosAnalisisSolicitudException($"El motivo admite hasta {MaxMotivo} caracteres.");
        if (texto.Any(c => char.IsControl(c) && c is not ('\n' or '\r' or '\t')))
            throw new DatosAnalisisSolicitudException("El motivo contiene caracteres no permitidos.");
        return new(docenteId, texto);
    }
}

/// <summary>HU-37: firma de la pareja (asignaturas, programas y correspondencias) y la petición de forzar.</summary>
public sealed record ReutilizacionAgente(string FirmaPareja, ForzarAnalisis? Forzar = null);

/// <summary>HU-37: bloque «agente» del resultado. Con «Incompleto» o «Fallido» no existe.</summary>
public sealed record AgenteAnalisis(Guid ExplicacionId, bool Reutilizada, Guid? ExplicacionOriginalId, DateTimeOffset? FechaOriginal,
    string Modelo, string VersionPlantilla);

/// <summary>HU-37: huellas SHA-256 en hexadecimal de un JSON canónico.</summary>
public static class HuellaReutilizacion
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    public static string De(object valor) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(valor, Json)))).ToLowerInvariant();

    /// <summary>Clave del contrato: firma de la pareja, versión de parámetros, modelo, plantilla y huella del contexto enviado al agente.</summary>
    public static string Clave(string firmaPareja, Guid versionParametros, string modelo, string versionPlantilla, ContextoAgente contexto) =>
        De(new { firmaPareja, versionParametros, modelo, versionPlantilla, contexto = De(contexto) });
}
