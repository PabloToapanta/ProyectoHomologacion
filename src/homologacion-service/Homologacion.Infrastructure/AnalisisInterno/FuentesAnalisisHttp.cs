using Homologacion.Application.AnalisisExterno;
using Homologacion.Application.AnalisisInterno;
using Homologacion.Application.Informe;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;

namespace Homologacion.Infrastructure.AnalisisInterno;

public sealed class FuentesAnalisisHttp(HttpClient http, IOptions<ConfiguracionAnalisisInterno> options)
    : ISolicitudesAnalisisInterno, ICatalogoAnalisisInterno, IReceptorasAnalisisInterno, ISolicitudesAnalisisExterno, IEstadoSolicitudes
{
    private const int MaxBytes = 2 * 1024 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { MaxDepth = 32 };
    public async Task<IReadOnlyList<ReceptoraAnalisis>> ListarAsync(Guid carreraId, CancellationToken ct) =>
        await Leer<ReceptoraAnalisis[]>(options.Value.CatalogoUrl, $"api/carreras/{carreraId}/analisis-interno/asignaturas", ct)
        ?? throw new FuenteAnalisisNoDisponibleException();
    public async Task<SolicitudParaAnalisis?> ObtenerSolicitudAsync(Guid id, CancellationToken ct)
    {
        var r = await Leer<SolicitudParaAnalisis>(options.Value.SolicitudesUrl, $"api/solicitudes/{id}/contexto-analisis-interno", ct);
        if (r is not null && (r.Id != id || r.AsignaturasAprobadas is null)) throw new FuenteAnalisisNoDisponibleException();
        return r;
    }
    public async Task<SolicitudExternaParaAnalisis?> ObtenerSolicitudExternaAsync(Guid id, CancellationToken ct)
    {
        var r = await Leer<SolicitudExternaParaAnalisis>(options.Value.SolicitudesUrl, $"api/solicitudes/{id}/contexto-analisis-externo", ct);
        if (r is not null && (r.Id != id || r.AsignaturasAprobadas is null)) throw new FuenteAnalisisNoDisponibleException();
        return r;
    }
    public async Task<AsignaturaParaAnalisis?> ObtenerAsignaturaAsync(Guid id, CancellationToken ct)
    {
        var r = await Leer<AsignaturaParaAnalisis>(options.Value.CatalogoUrl, $"api/asignaturas/{id}/analisis-interno/contexto", ct);
        if (r is not null && (r.Id != id || r.Contenidos is null || r.Resultados is null)) throw new FuenteAnalisisNoDisponibleException();
        return r;
    }
    public async Task<RevisionParaAnalisis> ObtenerRevisionAsync(Guid origen, Guid receptora, CancellationToken ct)
    {
        var r = await Leer<RevisionParaAnalisis>(options.Value.CatalogoUrl, $"api/asignaturas/{origen}/analisis-interno/correspondencias/{receptora}", ct);
        if (r is null || r.Contenidos is null || r.Resultados is null) throw new FuenteAnalisisNoDisponibleException();
        return r;
    }
    /// <summary>HU-32: ruta interna de Solicitudes (no publicada en el Gateway). Solo 204 confirma; 404 y 409 se conservan.</summary>
    public async Task MarcarRemitidaAsync(Guid solicitudId, CancellationToken ct)
    {
        try
        {
            using var response = await http.PostAsync(new Uri(new Uri(options.Value.SolicitudesUrl), $"api/solicitudes/{solicitudId}/remision"), null, ct);
            if (response.StatusCode == HttpStatusCode.NoContent) return;
            if (response.StatusCode == HttpStatusCode.NotFound) throw new SolicitudAnalisisNoEncontradaException();
            if (response.StatusCode == HttpStatusCode.Conflict) throw new EstadoSolicitudNoPermiteRemitirException(await TituloConflicto(response, ct));
            throw new FuenteAnalisisNoDisponibleException();
        }
        catch (HttpRequestException) { throw new FuenteAnalisisNoDisponibleException(); }
        catch (IOException) { throw new FuenteAnalisisNoDisponibleException(); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { throw new FuenteAnalisisNoDisponibleException(); }
    }

    private static async Task<string> TituloConflicto(HttpResponseMessage response, CancellationToken ct)
    {
        const string generico = "El estado de la solicitud no permite remitir el informe.";
        if (response.Content.Headers.ContentLength > 4096) return generico;
        try
        {
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            return json.RootElement.ValueKind == JsonValueKind.Object && json.RootElement.TryGetProperty("title", out var t)
                && t.GetString() is { Length: > 0 and <= 300 } titulo ? titulo : generico;
        }
        catch (JsonException) { return generico; }
    }

    private async Task<T?> Leer<T>(string baseUrl, string path, CancellationToken ct) where T : class
    {
        try
        {
            using var response = await http.GetAsync(new Uri(new Uri(baseUrl), path), HttpCompletionOption.ResponseHeadersRead, ct);
            if (response.StatusCode == HttpStatusCode.NotFound) return null;
            if (!response.IsSuccessStatusCode || response.Content.Headers.ContentLength > MaxBytes) throw new FuenteAnalisisNoDisponibleException();
            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var buffer = new MemoryStream();
            var block = new byte[8192];
            int read;
            while ((read = await stream.ReadAsync(block.AsMemory(), ct)) > 0)
            {
                if (buffer.Length + read > MaxBytes) throw new FuenteAnalisisNoDisponibleException();
                buffer.Write(block, 0, read);
            }
            return JsonSerializer.Deserialize<T>(buffer.ToArray(), JsonOptions) ?? throw new FuenteAnalisisNoDisponibleException();
        }
        catch (HttpRequestException) { throw new FuenteAnalisisNoDisponibleException(); }
        catch (IOException) { throw new FuenteAnalisisNoDisponibleException(); }
        catch (JsonException) { throw new FuenteAnalisisNoDisponibleException(); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { throw new FuenteAnalisisNoDisponibleException(); }
    }
}
