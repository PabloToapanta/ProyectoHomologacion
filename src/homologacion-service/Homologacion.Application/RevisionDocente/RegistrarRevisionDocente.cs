using Homologacion.Application.AnalisisExterno;
using Homologacion.Application.AnalisisInterno;
using Homologacion.Domain.RevisionDocente;

namespace Homologacion.Application.RevisionDocente;

public sealed record RevisionDocenteInput(Guid AprobadaId, Guid ReceptoraId, string? Postura, string? Decision, string? Observacion);
/// <summary>Análisis propio de un docente sobre una pareja, con la fotografía del análisis del agente contra el que se emitió.</summary>
public sealed record RevisionDocenteRegistrada(Guid SolicitudId, Guid AsignaturaAprobadaId, Guid AsignaturaReceptoraId, Guid DocenteId, string Docente,
    string Postura, string Decision, string? Observacion, string EstadoAgente, string? SugerenciaAgente, DateTimeOffset Fecha);
public interface IRevisionesDocentes
{
    /// <summary>El último registro por solicitud, pareja y docente reemplaza al anterior.</summary>
    Task GuardarAsync(RevisionDocenteRegistrada revision, CancellationToken ct);
    Task<IReadOnlyList<RevisionDocenteRegistrada>> ListarAsync(Guid solicitudId, CancellationToken ct);
}

/// <summary>HU-31: registra y consulta el análisis de los docentes asignados junto al del agente (HU-18/HU-19).</summary>
public sealed class RegistrarRevisionDocente(ISolicitudesAnalisisInterno solicitudes, ICatalogoAnalisisInterno catalogo,
    IResultadosAnalisisInterno internos, IResultadosAnalisisExterno externos, IRevisionesDocentes revisiones, TimeProvider reloj)
{
    public async Task<RevisionDocenteRegistrada> RegistrarAsync(Guid solicitudId, Guid docenteId, string docente, RevisionDocenteInput input, CancellationToken ct)
    {
        if (docenteId == Guid.Empty) throw new SolicitudAnalisisNoEncontradaException();
        // La asignación se comprueba en Solicitudes con la identidad del token en cada petición.
        var solicitud = await Asignada(solicitudId, ct);
        if (input.AprobadaId == Guid.Empty || input.ReceptoraId == Guid.Empty)
            throw new DatosAnalisisSolicitudException("Seleccione la asignatura aprobada y la receptora.");
        if (!solicitud.AsignaturasAprobadas.Any(a => a?.Id == input.AprobadaId))
            throw new DatosAnalisisSolicitudException("La asignatura aprobada no pertenece a esta solicitud.");
        var receptora = await catalogo.ObtenerAsignaturaAsync(input.ReceptoraId, ct);
        if (receptora is null || receptora.Id != input.ReceptoraId || receptora.CarreraId != solicitud.CarreraReceptoraId)
            throw new DatosAnalisisSolicitudException("La asignatura receptora debe pertenecer a la carrera de la solicitud.");
        var guardados = solicitud.TipoCambio == "Externo" ? await externos.ListarAsync(solicitudId, ct) : await internos.ListarAsync(solicitudId, ct);
        var agente = guardados.Where(g => g.Analisis.SolicitudId == solicitudId && g.Analisis.AsignaturaAprobadaId == input.AprobadaId
            && g.Analisis.AsignaturaReceptoraId == input.ReceptoraId).OrderByDescending(g => g.Fecha).FirstOrDefault()?.Analisis;
        var estado = agente?.Estado ?? "SinAnalisis";
        var sugerencia = agente?.Estado == "Completo" ? agente.Resultado?.Sugerencia : null;
        (string Postura, string Decision, string? Observacion) resuelto;
        try { resuelto = ReglaRevisionDocente.Resolver(input.Postura, input.Decision, input.Observacion, estado, sugerencia); }
        catch (RevisionDocenteInvalidaException error) { throw new DatosAnalisisSolicitudException(error.Message); }
        var nombre = string.IsNullOrWhiteSpace(docente) ? docenteId.ToString() : docente.Trim();
        var registro = new RevisionDocenteRegistrada(solicitudId, input.AprobadaId, input.ReceptoraId, docenteId, nombre.Length > 256 ? nombre[..256] : nombre,
            resuelto.Postura, resuelto.Decision, resuelto.Observacion, estado, sugerencia, reloj.GetUtcNow());
        await revisiones.GuardarAsync(registro, ct);
        return registro;
    }

    public async Task<IReadOnlyList<RevisionDocenteRegistrada>> ListarAsync(Guid solicitudId, CancellationToken ct)
    {
        await Asignada(solicitudId, ct);
        return (await revisiones.ListarAsync(solicitudId, ct)).OrderBy(r => r.AsignaturaAprobadaId).ThenBy(r => r.AsignaturaReceptoraId).ThenBy(r => r.Fecha).ToArray();
    }

    private async Task<SolicitudParaAnalisis> Asignada(Guid solicitudId, CancellationToken ct)
    {
        var solicitud = await solicitudes.ObtenerSolicitudAsync(solicitudId, ct) ?? throw new SolicitudAnalisisNoEncontradaException();
        if (solicitud.Id != solicitudId || solicitud.AsignaturasAprobadas is null) throw new FuenteAnalisisNoDisponibleException();
        return solicitud;
    }
}
