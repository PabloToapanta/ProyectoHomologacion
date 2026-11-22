namespace Homologacion.Domain.Auditoria;

/// <summary>HU-25: eventos del informe. Remitido (HU-32) y Finalizado (HU-33) los registran esas historias.</summary>
public enum TipoEventoInforme { Generado = 1, Remitido = 2, Finalizado = 3 }

/// <summary>Evento inmutable del historial: fecha, usuario y rol de quien realizó la acción.</summary>
public sealed record EventoInforme(Guid Id, Guid SolicitudId, TipoEventoInforme Tipo, DateTimeOffset Fecha, Guid UsuarioId, string Usuario, string Rol);

public sealed class EventoInformeInvalidoException(string mensaje) : Exception(mensaje);

public static class HistorialInforme
{
    public const int MaxUsuario = 256;
    public const int MaxRol = 30;

    public static EventoInforme Crear(Guid solicitudId, TipoEventoInforme tipo, DateTimeOffset fecha, Guid usuarioId, string? usuario, string? rol)
    {
        if (solicitudId == Guid.Empty) throw new EventoInformeInvalidoException("El evento requiere la solicitud.");
        if (usuarioId == Guid.Empty) throw new EventoInformeInvalidoException("El evento requiere el usuario que realizó la acción.");
        if (!Enum.IsDefined(tipo)) throw new EventoInformeInvalidoException("Tipo de evento del informe desconocido.");
        if (string.IsNullOrWhiteSpace(rol) || rol.Trim().Length > MaxRol) throw new EventoInformeInvalidoException("El evento requiere el rol del usuario.");
        var nombre = string.IsNullOrWhiteSpace(usuario) ? usuarioId.ToString() : usuario.Trim();
        return new(Guid.NewGuid(), solicitudId, tipo, fecha, usuarioId, nombre.Length > MaxUsuario ? nombre[..MaxUsuario] : nombre, rol.Trim());
    }

    /// <summary>Orden cronológico; a igual fecha, el orden del flujo (generado, remitido, finalizado).</summary>
    public static IReadOnlyList<EventoInforme> Ordenar(IEnumerable<EventoInforme> eventos) =>
        eventos.OrderBy(e => e.Fecha).ThenBy(e => e.Tipo).ThenBy(e => e.Id).ToArray();

    /// <summary>Cada evento ocurre una sola vez y después del anterior del flujo.</summary>
    public static void ValidarSiguiente(IReadOnlyCollection<EventoInforme> previos, TipoEventoInforme tipo)
    {
        if (previos.Any(e => e.Tipo == tipo)) throw new EventoInformeInvalidoException($"El evento «{tipo}» del informe ya está registrado.");
        var anterior = tipo - 1;
        if (tipo != TipoEventoInforme.Generado && previos.All(e => e.Tipo != anterior))
            throw new EventoInformeInvalidoException($"El evento «{tipo}» requiere antes el evento «{anterior}».");
    }
}
