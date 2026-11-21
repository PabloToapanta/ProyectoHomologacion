using System.Net;
using System.Text;
using Homologacion.Application.AnalisisInterno;
using Homologacion.Application.Informe;
using Homologacion.Infrastructure.AnalisisInterno;
using Microsoft.Extensions.Options;
using Xunit;

namespace Homologacion.Infrastructure.Tests;

/// <summary>HU-32: Homologación pide a Solicitudes el estado «Remitida» por la ruta interna.</summary>
public class RemisionSolicitudHttpTests
{
    private static readonly Guid Id = Guid.Parse("93200000-0000-0000-0000-000000000099");
    private sealed class Capture(HttpStatusCode status, string body = "") : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Request = request;
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/problem+json") });
        }
    }
    private static IEstadoSolicitudes Crear(HttpMessageHandler handler) =>
        new FuentesAnalisisHttp(new HttpClient(handler), Options.Create(new ConfiguracionAnalisisInterno()));

    [Fact]
    public async Task Envia_POST_a_la_ruta_interna_de_remision()
    {
        using var capture = new Capture(HttpStatusCode.NoContent);
        await Crear(capture).MarcarRemitidaAsync(Id, default);
        Assert.Equal(HttpMethod.Post, capture.Request!.Method);
        Assert.Equal($"http://solicitudes-service:8080/api/solicitudes/{Id}/remision", capture.Request.RequestUri!.ToString());
    }

    [Fact]
    public async Task No_asignada_se_informa_como_no_encontrada()
    {
        using var capture = new Capture(HttpStatusCode.NotFound);
        await Assert.ThrowsAsync<SolicitudAnalisisNoEncontradaException>(() => Crear(capture).MarcarRemitidaAsync(Id, default));
    }

    [Fact]
    public async Task Conflicto_de_estado_se_informa_con_el_titulo_de_Solicitudes()
    {
        using var capture = new Capture(HttpStatusCode.Conflict, """{"status":409,"title":"La solicitud en estado «Registrada» no puede remitirse."}""");
        var error = await Assert.ThrowsAsync<EstadoSolicitudNoPermiteRemitirException>(() => Crear(capture).MarcarRemitidaAsync(Id, default));
        Assert.Equal("La solicitud en estado «Registrada» no puede remitirse.", error.Message);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task Otra_respuesta_no_confirma_la_remision(HttpStatusCode status)
    {
        using var capture = new Capture(status, "diagnostico-privado-sintetico");
        var error = await Assert.ThrowsAsync<FuenteAnalisisNoDisponibleException>(() => Crear(capture).MarcarRemitidaAsync(Id, default));
        Assert.DoesNotContain("diagnostico-privado", error.Message);
    }
}

/// <summary>HU-33: Homologación pide a Solicitudes el estado «Finalizada» por la ruta interna.</summary>
public class FinalizacionSolicitudHttpTests
{
    private static readonly Guid Id = Guid.Parse("93300000-0000-0000-0000-000000000099");
    private sealed class Capture(HttpStatusCode status, string body = "") : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Request = request;
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/problem+json") });
        }
    }
    private static IEstadoSolicitudes Crear(HttpMessageHandler handler) =>
        new FuentesAnalisisHttp(new HttpClient(handler), Options.Create(new ConfiguracionAnalisisInterno()));

    [Fact]
    public async Task Envia_POST_a_la_ruta_interna_de_finalizacion()
    {
        using var capture = new Capture(HttpStatusCode.NoContent);
        await Crear(capture).MarcarFinalizadaAsync(Id, default);
        Assert.Equal(HttpMethod.Post, capture.Request!.Method);
        Assert.Equal($"http://solicitudes-service:8080/api/solicitudes/{Id}/finalizacion", capture.Request.RequestUri!.ToString());
    }

    [Fact]
    public async Task Inexistente_se_informa_como_no_encontrada()
    {
        using var capture = new Capture(HttpStatusCode.NotFound);
        await Assert.ThrowsAsync<SolicitudAnalisisNoEncontradaException>(() => Crear(capture).MarcarFinalizadaAsync(Id, default));
    }

    [Fact]
    public async Task Conflicto_de_estado_se_informa_con_el_titulo_de_Solicitudes()
    {
        using var capture = new Capture(HttpStatusCode.Conflict, """{"status":409,"title":"La solicitud en estado «Docentes asignados» no puede finalizarse."}""");
        var error = await Assert.ThrowsAsync<EstadoSolicitudNoPermiteFinalizarException>(() => Crear(capture).MarcarFinalizadaAsync(Id, default));
        Assert.Equal("La solicitud en estado «Docentes asignados» no puede finalizarse.", error.Message);
    }

    [Fact]
    public async Task Conflicto_sin_titulo_usa_un_mensaje_generico_de_finalizacion()
    {
        using var capture = new Capture(HttpStatusCode.Conflict, "no-json");
        var error = await Assert.ThrowsAsync<EstadoSolicitudNoPermiteFinalizarException>(() => Crear(capture).MarcarFinalizadaAsync(Id, default));
        Assert.Contains("finalizar", error.Message);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task Otra_respuesta_no_confirma_la_finalizacion(HttpStatusCode status)
    {
        using var capture = new Capture(status, "diagnostico-privado-sintetico");
        var error = await Assert.ThrowsAsync<FuenteAnalisisNoDisponibleException>(() => Crear(capture).MarcarFinalizadaAsync(Id, default));
        Assert.DoesNotContain("diagnostico-privado", error.Message);
    }
}
