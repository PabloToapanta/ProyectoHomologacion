using Homologacion.Application.AnalisisInterno;
using Homologacion.Domain.Auditoria;
using Homologacion.Domain.Finalizacion;

namespace Homologacion.Application.Informe;

/// <summary>HU-33: decisión del coordinador en una pareja «Sin acuerdo» (resultado, justificación, autor y fecha).</summary>
public sealed record DecisionCoordinador(Guid SolicitudId, Guid AsignaturaAprobadaId, Guid AsignaturaReceptoraId, string Resultado,
    string Justificacion, Guid CoordinadorId, string Coordinador, DateTimeOffset Fecha);
public sealed record DecisionCoordinadorInput(Guid AsignaturaAprobadaId, Guid AsignaturaReceptoraId, string? Resultado, string? Justificacion);
/// <summary>HU-33: finalización registrada (fecha y coordinador).</summary>
public sealed record FinalizacionInforme(Guid SolicitudId, DateTimeOffset Fecha, Guid FinalizadoPorId, string FinalizadoPor);
/// <summary>HU-33: lo que revisa el coordinador: el informe fijo, quién lo remitió, sus decisiones y, si existe, la finalización.</summary>
public sealed record RevisionInformeCoordinador(InformeHomologacion Informe, DateTimeOffset RemitidoEl, string RemitidoPor,
    IReadOnlyList<DecisionCoordinador> Decisiones, int DecisionesPendientes, FinalizacionInforme? Finalizacion);

public sealed class InformeNoRemitidoException() : Exception("El informe de esta solicitud aún no ha sido remitido al coordinador de carrera.");
public sealed class SolicitudYaFinalizadaException() : Exception("La solicitud ya fue finalizada; no admite cambios.");
/// <summary>Solicitudes rechazó el cambio de estado (409): la solicitud no está «Remitida».</summary>
public sealed class EstadoSolicitudNoPermiteFinalizarException(string message) : Exception(message);

/// <summary>HU-33: una decisión por pareja; se puede corregir hasta finalizar. No ofrece eliminar.</summary>
public interface IDecisionesCoordinador
{
    Task<IReadOnlyList<DecisionCoordinador>> ListarAsync(Guid solicitudId, CancellationToken ct);
    /// <summary>Inserta o reemplaza la decisión de la pareja.</summary>
    Task GuardarAsync(DecisionCoordinador decision, CancellationToken ct);
}

/// <summary>HU-33: el coordinador revisa el informe remitido, decide las parejas «Sin acuerdo» y finaliza la solicitud una sola vez.</summary>
/// <remarks>Decidir y finalizar se ejecutan dentro del cierre del informe (transacción y bloqueo por solicitud), así que no se intercalan.
/// Al finalizar se inserta el evento «Finalizado» (HU-25) y se pide a Solicitudes el estado «Finalizada»; si Solicitudes falla, la
/// transacción no se confirma. Cualquier Coordinador revisa las solicitudes FISEI (confirmación de la PO del 15/11/2026).</remarks>
public sealed class RevisionCoordinadorInforme(IInformesHomologacion informes, IAuditoriaInformes auditoria, IDecisionesCoordinador decisiones,
    IEstadoSolicitudes estado, TimeProvider reloj)
{
    public const string Rol = "Coordinador";

    public async Task<RevisionInformeCoordinador> ConsultarAsync(Guid solicitudId, CancellationToken ct)
    {
        var informe = await informes.ObtenerAsync(solicitudId, ct) ?? throw new InformeNoRemitidoException();
        var eventos = await Eventos(solicitudId, ct);
        var remision = eventos.FirstOrDefault(e => e.Tipo == TipoEventoInforme.Remitido) ?? throw new InformeNoRemitidoException();
        var registradas = (await decisiones.ListarAsync(solicitudId, ct)).Where(d => d.SolicitudId == solicitudId).ToArray();
        var fin = eventos.FirstOrDefault(e => e.Tipo == TipoEventoInforme.Finalizado);
        return new(informe, remision.Fecha, remision.Usuario, registradas, Pendientes(informe, registradas).Length,
            fin is null ? null : new(solicitudId, fin.Fecha, fin.UsuarioId, fin.Usuario));
    }

    public Task<DecisionCoordinador> DecidirAsync(Guid solicitudId, DecisionCoordinadorInput input, Guid coordinadorId, string coordinador, CancellationToken ct)
    {
        if (coordinadorId == Guid.Empty) throw new SolicitudAnalisisNoEncontradaException();
        return informes.ConCierreAsync(solicitudId, async () =>
        {
            var (informe, eventos) = await Remitido(solicitudId, "No se puede registrar la decisión: el informe de esta solicitud no ha sido remitido.", ct);
            if (eventos.Any(e => e.Tipo == TipoEventoInforme.Finalizado)) throw new SolicitudYaFinalizadaException();
            var pareja = informe.Parejas.FirstOrDefault(p => p.AsignaturaAprobadaId == input.AsignaturaAprobadaId && p.AsignaturaReceptoraId == input.AsignaturaReceptoraId)
                ?? throw new DatosAnalisisSolicitudException("La pareja de asignaturas no pertenece al informe.");
            var (resultado, justificacion) = ReglaDecisionCoordinador.Validar(pareja.SugerenciaFinal, input.Resultado, input.Justificacion);
            var decision = new DecisionCoordinador(solicitudId, pareja.AsignaturaAprobadaId, pareja.AsignaturaReceptoraId, resultado, justificacion,
                coordinadorId, Nombre(coordinador, coordinadorId), reloj.GetUtcNow());
            await decisiones.GuardarAsync(decision, ct);
            return decision;
        }, ct);
    }

    public Task<FinalizacionInforme> FinalizarAsync(Guid solicitudId, Guid coordinadorId, string coordinador, CancellationToken ct)
    {
        if (coordinadorId == Guid.Empty) throw new SolicitudAnalisisNoEncontradaException();
        return informes.ConCierreAsync(solicitudId, async () =>
        {
            var (informe, eventos) = await Remitido(solicitudId, "No se puede finalizar: la solicitud no tiene un informe remitido.", ct);
            if (eventos.Any(e => e.Tipo == TipoEventoInforme.Finalizado)) throw new SolicitudYaFinalizadaException();
            var pendientes = Pendientes(informe, await decisiones.ListarAsync(solicitudId, ct));
            if (pendientes.Length > 0)
                throw new DatosAnalisisSolicitudException("Falta la decisión del coordinador en las parejas «Sin acuerdo» con receptora: "
                    + string.Join(", ", pendientes.Select(p => p.ReceptoraCodigo ?? p.AsignaturaReceptoraId.ToString())) + ".");
            var evento = HistorialInforme.Crear(solicitudId, TipoEventoInforme.Finalizado, reloj.GetUtcNow(), coordinadorId, coordinador, Rol);
            try { await auditoria.RegistrarAsync(evento, ct); }
            catch (EventoInformeInvalidoException) { throw new SolicitudYaFinalizadaException(); }
            await estado.MarcarFinalizadaAsync(solicitudId, ct);
            return new FinalizacionInforme(solicitudId, evento.Fecha, coordinadorId, evento.Usuario);
        }, ct);
    }

    private async Task<(InformeHomologacion, IReadOnlyList<EventoInforme>)> Remitido(Guid solicitudId, string mensaje, CancellationToken ct)
    {
        var informe = await informes.ObtenerAsync(solicitudId, ct) ?? throw new DatosAnalisisSolicitudException(mensaje);
        var eventos = await Eventos(solicitudId, ct);
        if (eventos.All(e => e.Tipo != TipoEventoInforme.Remitido)) throw new DatosAnalisisSolicitudException(mensaje);
        return (informe, eventos);
    }

    private async Task<IReadOnlyList<EventoInforme>> Eventos(Guid solicitudId, CancellationToken ct) =>
        (await auditoria.ListarAsync(solicitudId, ct)).Where(e => e.SolicitudId == solicitudId).ToArray();

    private static ParejaEnInforme[] Pendientes(InformeHomologacion informe, IEnumerable<DecisionCoordinador> registradas)
    {
        var decididas = registradas.Select(d => (d.AsignaturaAprobadaId, d.AsignaturaReceptoraId)).ToHashSet();
        return informe.Parejas.Where(p => p.SugerenciaFinal == ReglaDecisionCoordinador.SinAcuerdo
            && !decididas.Contains((p.AsignaturaAprobadaId, p.AsignaturaReceptoraId))).ToArray();
    }

    private static string Nombre(string? usuario, Guid id)
    {
        var nombre = string.IsNullOrWhiteSpace(usuario) ? id.ToString() : usuario.Trim();
        return nombre.Length > HistorialInforme.MaxUsuario ? nombre[..HistorialInforme.MaxUsuario] : nombre;
    }
}
