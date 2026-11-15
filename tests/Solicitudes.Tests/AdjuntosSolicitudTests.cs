using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Solicitudes.Infrastructure;
using Xunit;

namespace Solicitudes.Tests;

public sealed class AdjuntosSolicitudTests
{
    static readonly byte[] Pdf = "%PDF-1.7\narchivo sintético\n"u8.ToArray();

    static async Task<Guid> CrearSolicitud(SolicitudesFactory f, HttpClient c)
    {
        var response = await c.PostAsJsonAsync("/api/solicitudes", f.Input());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<SolicitudesApiTests.SolicitudView>())!.Id;
    }

    static MultipartFormDataContent Form(string tipo = "Historial académico", byte[]? pdf = null, string nombre = "historial.pdf", string contentType = "application/pdf")
    {
        var form = new MultipartFormDataContent { { new StringContent(tipo), "tipo" } };
        var archivo = new ByteArrayContent(pdf ?? Pdf);
        archivo.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        form.Add(archivo, "archivo", nombre);
        return form;
    }

    [Fact]
    public async Task CoordinadorGuardaPdfMetadatosHashYDocenteAsignadoLoDescarga()
    {
        using var f = new SolicitudesFactory();
        var coordinadorId = Guid.NewGuid();
        using var coordinador = f.Client("Coordinador", coordinadorId.ToString());
        var solicitudId = await CrearSolicitud(f, coordinador);
        var subida = await coordinador.PostAsync($"/api/solicitudes/{solicitudId}/adjuntos", Form());
        Assert.Equal(HttpStatusCode.Created, subida.StatusCode);
        var metadata = await subida.Content.ReadFromJsonAsync<AdjuntoView>();
        Assert.NotNull(metadata);
        Assert.Equal("Historial académico", metadata.Tipo);
        Assert.Equal("historial.pdf", metadata.NombreArchivo);
        Assert.Equal(Pdf.LongLength, metadata.TamanoBytes);
        Assert.Equal(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Pdf)), metadata.HuellaSha256);
        Assert.Equal(coordinadorId.ToString(), metadata.SubidoPor);
        Assert.EndsWith("Z", metadata.FechaSubidaUtc);

        using var docenteAsignado = f.Client("Docente", f.Directorio.Docentes.Ana.Id.ToString());
        var asignacion = await coordinador.PostAsJsonAsync($"/api/solicitudes/{solicitudId}/docentes", new { docenteIds = new[] { f.Directorio.Docentes.Ana.Id, f.Directorio.Docentes.Luis.Id } });
        Assert.Equal(HttpStatusCode.OK, asignacion.StatusCode);
        var listado = await docenteAsignado.GetFromJsonAsync<List<AdjuntoView>>($"/api/solicitudes/{solicitudId}/adjuntos");
        Assert.Equal(metadata, Assert.Single(listado!));
        var descarga = await docenteAsignado.GetAsync($"/api/solicitudes/{solicitudId}/adjuntos/{metadata.Id}");
        Assert.Equal(HttpStatusCode.OK, descarga.StatusCode);
        Assert.Equal("application/pdf", descarga.Content.Headers.ContentType?.MediaType);
        Assert.Equal(Pdf, await descarga.Content.ReadAsByteArrayAsync());
    }

    [Theory]
    [InlineData("image/png", "historial.pdf", "%PDF-1.7")]
    [InlineData("application/pdf", "historial.txt", "%PDF-1.7")]
    [InlineData("application/pdf", "historial.pdf", "no es pdf")]
    [InlineData("application/pdf", "historial.pdf", "%PDF-")]
    public async Task RechazaArchivosQueNoSonPdf(string mediaType, string nombre, string contenido)
    {
        using var f = new SolicitudesFactory(); using var c = f.Client("Coordinador", Guid.NewGuid().ToString());
        var id = await CrearSolicitud(f, c);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsync($"/api/solicitudes/{id}/adjuntos", Form(pdf: System.Text.Encoding.UTF8.GetBytes(contenido), nombre: nombre, contentType: mediaType))).StatusCode);
        Assert.Empty(await c.GetFromJsonAsync<List<AdjuntoView>>($"/api/solicitudes/{id}/adjuntos"));
    }

    [Fact]
    public async Task RechazaArchivoMayorA20MiBSinGuardar()
    {
        using var f = new SolicitudesFactory(); using var c = f.Client("Coordinador", Guid.NewGuid().ToString());
        var id = await CrearSolicitud(f, c);
        var contenido = new byte[20 * 1024 * 1024 + 1]; "%PDF-"u8.CopyTo(contenido);
        var response = await c.PostAsync($"/api/solicitudes/{id}/adjuntos", Form(pdf: contenido));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("20 MiB", (await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("title").GetString());
        Assert.Empty(await c.GetFromJsonAsync<List<AdjuntoView>>($"/api/solicitudes/{id}/adjuntos"));
    }

    [Fact]
    public async Task SoloCoordinadorSubeYDocenteNoAsignadoRecibe404()
    {
        using var f = new SolicitudesFactory(); using var coordinador = f.Client("Coordinador", Guid.NewGuid().ToString());
        var id = await CrearSolicitud(f, coordinador);
        using var anonimo = f.Client(null);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.PostAsync($"/api/solicitudes/{id}/adjuntos", Form())).StatusCode);
        using var administrador = f.Client("Administrador");
        Assert.Equal(HttpStatusCode.Forbidden, (await administrador.PostAsync($"/api/solicitudes/{id}/adjuntos", Form())).StatusCode);
        using var docente = f.Client("Docente", Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.NotFound, (await docente.GetAsync($"/api/solicitudes/{id}/adjuntos")).StatusCode);
    }

    [Fact]
    public async Task NoAdjuntaSiLaSolicitudEstaFinalizada()
    {
        using var f = new SolicitudesFactory(); using var c = f.Client("Coordinador", Guid.NewGuid().ToString());
        var id = await CrearSolicitud(f, c);
        using (var scope = f.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SolicitudesDbContext>();
            var solicitud = await db.Solicitudes.SingleAsync(x => x.Id == id);
            typeof(Solicitudes.Domain.Solicitud).GetProperty(nameof(Solicitudes.Domain.Solicitud.Estado))!.SetValue(solicitud, "Finalizada");
            await db.SaveChangesAsync();
        }
        var response = await c.PostAsync($"/api/solicitudes/{id}/adjuntos", Form());
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Empty(await c.GetFromJsonAsync<List<AdjuntoView>>($"/api/solicitudes/{id}/adjuntos"));
    }

    public sealed record AdjuntoView(Guid Id, Guid SolicitudId, string Tipo, string NombreArchivo, long TamanoBytes, string HuellaSha256, string SubidoPor, string FechaSubidaUtc);
}
