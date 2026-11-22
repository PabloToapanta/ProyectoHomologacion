using Homologacion.Application.AnalisisInterno;
using Homologacion.Application.Informe;
using Homologacion.Domain.Equivalencias;
using Homologacion.Domain.RevisionDocente;

namespace Homologacion.Application.RevisionDocente;

public sealed record RevisionAgrupacionInput(string? Postura, string? Decision, string? Observacion);

/// <summary>Leslie implementa este almacén con clave lógica (grupoId, docenteId).</summary>
public interface IRevisionesAgrupacion : IRevisionesAgrupacionInforme
{
    Task GuardarAsync(RevisionAgrupacionRegistrada revision, CancellationToken ct);
}

/// <summary>HU-21/T63: una revisión humana por docente y grupo, cerrada por el informe.</summary>
public sealed class RegistrarRevisionAgrupacion(ISolicitudesAnalisisInterno solicitudes,
    IResultadosAgrupacionInforme analisis, IRevisionesAgrupacion revisiones,
    IInformesHomologacion informes, TimeProvider reloj)
{
    public async Task<RevisionAgrupacionRegistrada> RegistrarAsync(Guid solicitudId, Guid grupoId,
        Guid docenteId, string docente, RevisionAgrupacionInput input, CancellationToken ct)
    {
        if (solicitudId == Guid.Empty || grupoId == Guid.Empty || docenteId == Guid.Empty)
            throw new DatosAnalisisSolicitudException("Seleccione la solicitud, la agrupación y el docente.");
        ArgumentNullException.ThrowIfNull(input);
        var solicitud = await solicitudes.ObtenerSolicitudAsync(solicitudId, ct)
            ?? throw new SolicitudAnalisisNoEncontradaException();
        if (solicitud.Id != solicitudId) throw new FuenteAnalisisNoDisponibleException();
        return await informes.ConCierreAsync(solicitudId, async () =>
        {
            if (await informes.ObtenerAsync(solicitudId, ct) is not null) throw new InformeYaGeneradoException();
            var guardados = await analisis.ListarAsync(solicitudId, ct);
            var delGrupo = guardados.Where(a => a.SolicitudId == solicitudId && a.GrupoId == grupoId)
                .OrderByDescending(a => a.Fecha).FirstOrDefault();
            if (delGrupo is null)
                throw new DatosAnalisisSolicitudException("La agrupación requiere un análisis guardado antes de la revisión docente.");
            var estado = delGrupo.Evaluacion.Estado == EstadoEquivalencia.InformacionIncompleta ? "Incompleto" : "Completo";
            var sugerencia = estado == "Completo" ? delGrupo.Evaluacion.Estado.ToString() : null;
            (string Postura, string Decision, string? Observacion) resuelto;
            try { resuelto = ReglaRevisionDocente.Resolver(input.Postura, input.Decision, input.Observacion, estado, sugerencia); }
            catch (RevisionDocenteInvalidaException error) { throw new DatosAnalisisSolicitudException(error.Message); }
            var nombre = string.IsNullOrWhiteSpace(docente) ? docenteId.ToString() : docente.Trim();
            var registro = new RevisionAgrupacionRegistrada(solicitudId, grupoId, docenteId,
                nombre.Length > 256 ? nombre[..256] : nombre, resuelto.Postura, resuelto.Decision,
                resuelto.Observacion, reloj.GetUtcNow());
            await revisiones.GuardarAsync(registro, ct);
            return registro;
        }, ct);
    }

    public async Task<IReadOnlyList<RevisionAgrupacionRegistrada>> ListarAsync(Guid solicitudId, CancellationToken ct)
    {
        var solicitud = await solicitudes.ObtenerSolicitudAsync(solicitudId, ct)
            ?? throw new SolicitudAnalisisNoEncontradaException();
        if (solicitud.Id != solicitudId) throw new FuenteAnalisisNoDisponibleException();
        return (await revisiones.ListarAsync(solicitudId, ct))
            .Where(r => r.SolicitudId == solicitudId).OrderBy(r => r.GrupoId).ThenBy(r => r.DocenteId).ToArray();
    }
}
