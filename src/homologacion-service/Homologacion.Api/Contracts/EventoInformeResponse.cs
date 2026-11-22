namespace Homologacion.Api.Contracts;

/// <summary>HU-25: un evento del historial del informe. Tipo: Generado, Remitido o Finalizado.</summary>
public sealed record EventoInformeResponse(Guid Id, string Tipo, DateTimeOffset Fecha, Guid UsuarioId, string Usuario, string Rol);
