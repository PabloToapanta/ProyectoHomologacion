namespace Homologacion.Application.AnalisisInterno;

public sealed record AnalisisGuardado(DateTimeOffset Fecha, ResultadoInternoSolicitud Analisis);
public interface IResultadosAnalisisInterno
{
    Task GuardarAsync(AnalisisGuardado resultado, CancellationToken ct);
    Task<IReadOnlyList<AnalisisGuardado>> ListarAsync(Guid solicitudId, CancellationToken ct);
}
public sealed record ReceptoraAnalisis(Guid Id, string Codigo, string Nombre, int Nivel);
public interface IReceptorasAnalisisInterno
{
    Task<IReadOnlyList<ReceptoraAnalisis>> ListarAsync(Guid carreraId, CancellationToken ct);
}
public sealed class AlmacenAnalisisNoDisponibleException() : Exception("No fue posible guardar o recuperar el análisis. Intente nuevamente; no se confirma ningún guardado.");
public sealed class GestionarAnalisisInterno(AnalizarSolicitudInterna analisis, ISolicitudesAnalisisInterno solicitudes,
    IResultadosAnalisisInterno resultados, TimeProvider reloj)
{
    public async Task<ResultadoInternoSolicitud> EjecutarAsync(Guid solicitud, Guid aprobada, Guid receptora, CancellationToken ct)
    {
        var resultado = await analisis.EjecutarAsync(solicitud, aprobada, receptora, ct);
        await resultados.GuardarAsync(new(reloj.GetUtcNow(), resultado), ct);
        return resultado;
    }
    public async Task<IReadOnlyList<AnalisisGuardado>> ConsultarAsync(Guid solicitud, CancellationToken ct)
    {
        if (await solicitudes.ObtenerSolicitudAsync(solicitud, ct) is not { } contexto || contexto.Id != solicitud)
            throw new SolicitudAnalisisNoEncontradaException();
        return await resultados.ListarAsync(solicitud, ct);
    }
}
