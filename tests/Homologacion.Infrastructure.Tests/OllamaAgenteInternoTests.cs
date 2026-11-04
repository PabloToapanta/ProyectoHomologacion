using System.Net;
using System.Text;
using System.Text.Json;
using Homologacion.Application.AnalisisInterno;
using Homologacion.Application.Equivalencias;
using Homologacion.Domain.Equivalencias;
using Homologacion.Infrastructure.AnalisisInterno;
using Xunit;

namespace Homologacion.Infrastructure.Tests;

public class OllamaAgenteInternoTests
{
    private sealed class Umbral : IUmbralEquivalencia { public decimal Valor => 80; }

    [Theory]
    [InlineData(200, EstadoAsistencia.Completada)]
    [InlineData(503, EstadoAsistencia.RevisionManual)]
    public async Task Motor_adaptador_y_HTTP_simulado_integrados(int status, EstadoAsistencia esperado)
    {
        using var handler = new Handler(Envelope(Valid), (HttpStatusCode)status);
        using var client = new HttpClient(handler);
        var caso = new AsistirEvaluacionInterna(new EvaluarEquivalencia(new Umbral()), Crear(client));
        var resultado = await caso.EjecutarAsync(OllamaRealTests.DatosSinteticos());
        Assert.Equal(esperado, resultado.Estado);
        Assert.True(resultado.Evaluacion.Homologable);
        Assert.Equal(100, resultado.Evaluacion.CompatibilidadAcademicaPct);
        Assert.Equal(80, resultado.Evaluacion.CompatibilidadHorariaPct);
        Assert.Equal(80, resultado.Evaluacion.CompatibilidadCreditosPct);
        if (status == 503) Assert.Null(resultado.Asistencia);
    }
    public static ContextoAgente Contexto() => new(new(["O"], "D", 80, 80, 80, 80,
        EstadoEquivalencia.Homologable, "Ejemplo sintetico del motor.", [], [], []),
        [new("C1", "Contenido", new("O", "C", "C", new("plan-O/C", "plan-D/C")))]);
    private static string Envelope(string content, bool done = true) => JsonSerializer.Serialize(new { done, message = new { role = "assistant", content } });
    private const string Valid = """{"resumen":"Explicacion sintetica.","evidenciasCitadas":["C1"]}""";
    private sealed class Handler(string body, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        public string? RequestBody { get; private set; }
        public Uri? Uri { get; private set; }
        public bool EsperarCancelacion { get; set; }
        public bool CortarLectura { get; set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Uri = request.RequestUri;
            RequestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            if (EsperarCancelacion) await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new(status) { Content = CortarLectura ? new StreamContent(new BrokenStream()) : new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }
    private static OllamaAgenteInterno Crear(HttpClient client, TimeSpan? timeout = null) => new(client,
        new(new Uri("http://ollama.test/"), "modelo-sintetico", timeout ?? TimeSpan.FromSeconds(5)));

    [Theory]
    [InlineData("1")]
    [InlineData("{}")]
    public async Task Tipo_invalido_en_metadatos_es_fallo_controlado(string reasonJson)
    {
        using var reason = JsonDocument.Parse(reasonJson);
        var body = JsonSerializer.Serialize(new { done = true, done_reason = reason.RootElement, message = new { role = "assistant", content = Valid } });
        using var handler = new Handler(body); using var client = new HttpClient(handler);
        Assert.Equal(TipoFalloAgente.RespuestaInvalida, (await Assert.ThrowsAsync<FalloAgenteException>(() => Crear(client).JustificarAsync(Contexto(), default))).Tipo);
    }
    [Fact]
    public async Task Corte_del_stream_es_fallo_controlado_sin_diagnostico_privado()
    {
        using var handler = new Handler("") { CortarLectura = true }; using var client = new HttpClient(handler);
        var error = await Assert.ThrowsAsync<FalloAgenteException>(() => Crear(client).JustificarAsync(Contexto(), default));
        Assert.Equal(TipoFalloAgente.NoDisponible, error.Tipo);
        Assert.DoesNotContain("diagnostico-privado", error.Message);
    }
    private sealed class BrokenStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => throw new IOException("diagnostico-privado-sintetico");
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => ValueTask.FromException<int>(new IOException("diagnostico-privado-sintetico"));
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    [Fact]
    public async Task Modelo_configurado_schema_y_respuesta_validada()
    {
        using var handler = new Handler(Envelope(Valid)); using var client = new HttpClient(handler);
        var result = await Crear(client).JustificarAsync(Contexto(), default);
        Assert.Equal("Explicacion sintetica.", result.Resumen);
        Assert.Equal(new[] { "C1" }, result.EvidenciasCitadas);
        using var doc = JsonDocument.Parse(handler.RequestBody!);
        Assert.Equal("modelo-sintetico", doc.RootElement.GetProperty("model").GetString());
        Assert.False(doc.RootElement.GetProperty("stream").GetBoolean());
        Assert.False(doc.RootElement.GetProperty("think").GetBoolean());
        Assert.False(doc.RootElement.GetProperty("format").GetProperty("additionalProperties").GetBoolean());
        Assert.Equal("/api/chat", handler.Uri!.AbsolutePath);
        Assert.Contains("plan-O/C", handler.RequestBody);
    }
    [Theory]
    [InlineData("{")]
    [InlineData("{}")]
    [InlineData("""{"resumen":" ","evidenciasCitadas":["C1"]}""")]
    [InlineData("""{"resumen":"x","evidenciasCitadas":["INEXISTENTE"]}""")]
    [InlineData("""{"resumen":"x","evidenciasCitadas":["C1","C1"]}""")]
    [InlineData("""{"resumen":"x","evidenciasCitadas":null}""")]
    [InlineData("""{"resumen":"x","evidenciasCitadas":["C1"],"homologable":false}""")]
    [InlineData("""{"resumen":"x","resumen":"y","evidenciasCitadas":["C1"]}""")]
    public async Task Rechaza_salida_invalida_o_evidencia_inventada(string content)
    {
        using var handler = new Handler(Envelope(content)); using var client = new HttpClient(handler);
        var error = await Assert.ThrowsAsync<FalloAgenteException>(() => Crear(client).JustificarAsync(Contexto(), default));
        Assert.Equal(TipoFalloAgente.RespuestaInvalida, error.Tipo);
        Assert.DoesNotContain(content, error.Message);
    }
    [Fact]
    public async Task Http_fallido_no_expone_cuerpo_del_servidor()
    {
        using var handler = new Handler("diagnostico-privado-sintetico", HttpStatusCode.ServiceUnavailable); using var client = new HttpClient(handler);
        var error = await Assert.ThrowsAsync<FalloAgenteException>(() => Crear(client).JustificarAsync(Contexto(), default));
        Assert.Equal(TipoFalloAgente.NoDisponible, error.Tipo);
        Assert.DoesNotContain("diagnostico-privado", error.Message);
    }
    [Fact]
    public async Task Respuesta_incompleta_no_es_exito()
    {
        using var handler = new Handler(Envelope(Valid, false)); using var client = new HttpClient(handler);
        Assert.Equal(TipoFalloAgente.RespuestaInvalida, (await Assert.ThrowsAsync<FalloAgenteException>(() => Crear(client).JustificarAsync(Contexto(), default))).Tipo);
    }
    [Fact]
    public async Task Timeout_controlado()
    {
        using var handler = new Handler("") { EsperarCancelacion = true }; using var client = new HttpClient(handler);
        var error = await Assert.ThrowsAsync<FalloAgenteException>(() => Crear(client, TimeSpan.FromMilliseconds(50)).JustificarAsync(Contexto(), default));
        Assert.Equal(TipoFalloAgente.TiempoAgotado, error.Tipo);
    }
    [Fact]
    public async Task Cancelacion_del_usuario_se_propaga()
    {
        using var handler = new Handler(Envelope(Valid)); using var client = new HttpClient(handler);
        using var cts = new CancellationTokenSource(); cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Crear(client).JustificarAsync(Contexto(), cts.Token));
        Assert.Null(handler.RequestBody);
    }
    [Fact]
    public async Task Respuesta_excesiva_se_rechaza()
    {
        using var handler = new Handler(new string('x', 65537)); using var client = new HttpClient(handler);
        Assert.Equal(TipoFalloAgente.RespuestaInvalida, (await Assert.ThrowsAsync<FalloAgenteException>(() => Crear(client).JustificarAsync(Contexto(), default))).Tipo);
    }
}
