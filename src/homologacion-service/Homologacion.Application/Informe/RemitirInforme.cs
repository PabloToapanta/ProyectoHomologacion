using Homologacion.Application.AnalisisInterno;
using Homologacion.Domain.Auditoria;

namespace Homologacion.Application.Informe;

/// <summary>HU-32: remisión registrada del informe (fecha y docente que remitió).</summary>
public sealed record RemisionInforme(Guid SolicitudId, DateTimeOffset Fecha, Guid RemitidoPorId, string RemitidoPor);
public sealed class InformeYaRemitidoException() : Exception("El informe ya fue remitido al coordinador de carrera; la remisión registrada no cambia.");
/// <summary>Solicitudes rechazó el cambio de estado (409): la solicitud no está en «Docentes asignados».</summary>
public sealed class EstadoSolicitudNoPermiteRemitirException(string message) : Exception(message);

/// <summary>HU-32: puerto hacia Solicitudes, dueño del estado de la solicitud. Marcar «Remitida» de nuevo no cambia nada.</summary>
public interface IEstadoSolicitudes
{
    Task MarcarRemitidaAsync(Guid solicitudId, CancellationToken ct);
}

/// <summary>HU-32: un docente asignado remite el informe generado al coordinador, una sola vez.</summary>
/// <remarks>Dentro del cierre del informe se inserta el evento «Remitido» (HU-25) y se pide a Solicitudes el estado «Remitida».
/// Si Solicitudes falla, la transacción no se confirma y no queda remisión. Si Solicitudes confirma y luego falla la confirmación
/// local, el reintento converge porque marcar «Remitida» es idempotente.</remarks>
public sealed class RemitirInformeHomologacion(ISolicitudesAnalisisInterno solicitudes, IInformesHomologacion informes,
    IAuditoriaInformes auditoria, IEstadoSolicitudes estado, TimeProvider reloj)
{
    public async Task<RemisionInforme> RemitirAsync(Guid solicitudId, Guid docenteId, string docente, CancellationToken ct)
    {
        if (docenteId == Guid.Empty) throw new SolicitudAnalisisNoEncontradaException();
        // Solicitudes valida la asignación con la identidad del token.
        var solicitud = await solicitudes.ObtenerSolicitudAsync(solicitudId, ct) ?? throw new SolicitudAnalisisNoEncontradaException();
        if (solicitud.Id != solicitudId) throw new FuenteAnalisisNoDisponibleException();
        return await informes.ConCierreAsync(solicitudId, () => Remitir(solicitudId, docenteId, docente, ct), ct);
    }

    private async Task<RemisionInforme> Remitir(Guid solicitudId, Guid docenteId, string docente, CancellationToken ct)
    {
        var informe = await informes.ObtenerAsync(solicitudId, ct)
            ?? throw new DatosAnalisisSolicitudException("No se puede remitir: el informe de esta solicitud aún no se ha generado.");
        // Regla del flujo (20/11): solo con los análisis de los dos docentes en cada pareja; las discrepancias van como «Sin acuerdo».
        if (informe.Parejas.Count == 0 || informe.Parejas.Any(p => p.Docentes.Select(d => d.DocenteId).Distinct().Count() < GenerarInformeHomologacion.DocentesAsignados))
            throw new DatosAnalisisSolicitudException("No se puede remitir: cada pareja del informe debe tener el análisis de los dos docentes.");
        var previos = await auditoria.ListarAsync(solicitudId, ct);
        if (previos.Any(e => e.SolicitudId == solicitudId && e.Tipo == TipoEventoInforme.Remitido)) throw new InformeYaRemitidoException();
        // Un informe anterior a HU-25 no tiene evento «Generado»: el informe guardado acredita la generación y no se inventa el evento.
        var evento = HistorialInforme.Crear(solicitudId, TipoEventoInforme.Remitido, reloj.GetUtcNow(), docenteId, docente, "Docente");
        try { await auditoria.RegistrarAsync(evento, ct); }
        catch (EventoInformeInvalidoException) { throw new InformeYaRemitidoException(); }
        await estado.MarcarRemitidaAsync(solicitudId, ct);
        return new(solicitudId, evento.Fecha, docenteId, evento.Usuario);
    }
}
