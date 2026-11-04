using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using Homologacion.Application.AnalisisInterno;

namespace Homologacion.Infrastructure.AnalisisInterno;

public sealed class AutorizacionFuentesAnalisisHandler(IHttpContextAccessor context, IConfiguration configuration) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!AuthenticationHeaderValue.TryParse(context.HttpContext?.Request.Headers.Authorization.ToString(), out var bearer)
            || !bearer.Scheme.Equals("Bearer", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(bearer.Parameter))
            throw new FuenteAnalisisNoDisponibleException();
        var key = configuration["InternalAccess:Key"];
        if (string.IsNullOrWhiteSpace(key)) throw new FuenteAnalisisNoDisponibleException();
        request.Headers.Authorization = bearer;
        request.Headers.Remove("X-Internal-Key");
        request.Headers.TryAddWithoutValidation("X-Internal-Key", key);
        return base.SendAsync(request, cancellationToken);
    }
}
