using Homologacion.Application.AnalisisInterno;
using Homologacion.Domain.Auditoria;

namespace Homologacion.Application.Informe;

/// <summary>HU-25: historial solo de inserción. No ofrece modificar ni eliminar eventos.</summary>
public interface IAuditoriaInformes
{
    /// <summary>Inserta el evento; si el mismo tipo ya existe para la solicitud lanza <see cref="EventoInformeInvalidoException"/>.</summary>
    Task RegistrarAsync(EventoInforme evento, CancellationToken ct);
    Task<IReadOnlyList<EventoInforme>> ListarAsync(Guid solicitudId, CancellationToken ct);
}

/// <summary>HU-25: el coordinador consulta el historial del informe de una solicitud en orden cronológico.</summary>
public sealed class ConsultarHistorialInforme(IAuditoriaInformes auditoria)
{
    public async Task<IReadOnlyList<EventoInforme>> EjecutarAsync(Guid solicitudId, CancellationToken ct)
    {
        if (solicitudId == Guid.Empty) throw new DatosAnalisisSolicitudException("Seleccione la solicitud.");
        return HistorialInforme.Ordenar((await auditoria.ListarAsync(solicitudId, ct)).Where(e => e.SolicitudId == solicitudId));
    }
}
