using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Solicitudes.Api;
using Xunit;

namespace Solicitudes.Tests;

public class CatalogoInternalAccessTests
{
    [Fact]
    public async Task Cliente_catalogo_inyecta_clave_privada_y_conserva_Bearer()
    {
        using var factory = new SolicitudesFactory();
        using var bootstrap = factory.CreateClient();
        var accessor = factory.Services.GetRequiredService<IHttpContextAccessor>();
        accessor.HttpContext = new DefaultHttpContext();
        accessor.HttpContext.Request.Headers.Authorization = "Bearer token-sintetico";
        accessor.HttpContext.Request.Headers["X-Internal-Key"] = "cabecera-no-confiable";
        using var handler = factory.Services.GetRequiredService<CatalogoAuthorizationHandler>();
        using var capture = new CaptureHandler();
        handler.InnerHandler = capture;
        using var client = new HttpClient(handler);
        using var response = await client.GetAsync("http://catalogo.test/api/carreras-solicitudes");
        Assert.Equal("hu36-test-only-internal-key-64-characters-for-isolated-tests", capture.InternalKey);
        Assert.Equal("Bearer token-sintetico", capture.Bearer);
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public string? InternalKey { get; private set; }
        public string? Bearer { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            InternalKey = request.Headers.TryGetValues("X-Internal-Key", out var values) ? values.Single() : null;
            Bearer = request.Headers.Authorization?.ToString();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
