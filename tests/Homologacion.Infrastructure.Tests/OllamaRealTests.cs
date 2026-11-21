using System.Text.Json;
using Homologacion.Application.AnalisisInterno;
using Homologacion.Application.Equivalencias;
using Homologacion.Infrastructure.AnalisisInterno;
using Xunit;

namespace Homologacion.Infrastructure.Tests;

public sealed class ModeloRealFactAttribute : FactAttribute
{
    public ModeloRealFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("HU18_OLLAMA_URL"))
            || string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("HU18_OLLAMA_MODEL")))
            Skip = "Requiere modelo real configurado con HU18_OLLAMA_URL y HU18_OLLAMA_MODEL. No se sustituye por un mock.";
    }
}

public class OllamaRealTests
{
    private sealed class Umbral : IUmbralEquivalencia { public decimal Valor => 80; }

    public static SolicitudEquivalencia DatosSinteticos() => new(
        [new(new("O", "plan-sintetico-O", 80, "hora-reloj", 8, "sistema-sintetico", ["C"], ["R"]), true)],
        new("D", "plan-sintetico-D", 100, "hora-reloj", 10, "sistema-sintetico", ["C"], ["R"]),
        [new("O", "C", "C", new("plan-sintetico-O/C", "plan-sintetico-D/C"))],
        [new("O", "R", "R", new("plan-sintetico-O/R", "plan-sintetico-D/R"))]);

    [ModeloRealFact]
    public async Task Explica_datos_sinteticos_con_motor_y_modelo_reales()
    {
        var url = new Uri(Environment.GetEnvironmentVariable("HU18_OLLAMA_URL")!);
        var modelo = Environment.GetEnvironmentVariable("HU18_OLLAMA_MODEL")!;
        using var handler = new HttpClientHandler { AllowAutoRedirect = false };
        using var http = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
        var agente = new OllamaAgenteInterno(http, new(url, modelo, TimeSpan.FromSeconds(90)));
        var motor = new EvaluarEquivalencia(new Umbral());
        var datos = DatosSinteticos();
        var resultado = await new AsistirEvaluacionInterna(motor, agente).EjecutarAsync(datos);
        Assert.Equal(EstadoAsistencia.Completada, resultado.Estado);
        Assert.True(resultado.Evaluacion.Homologable);
        Assert.NotNull(resultado.Asistencia);
        var folder = Environment.GetEnvironmentVariable("HU18_EVIDENCE_DIR");
        if (!string.IsNullOrWhiteSpace(folder))
        {
            Directory.CreateDirectory(folder);
            await File.WriteAllTextAsync(Path.Combine(folder, "modelo-real-sintetico.json"),
                JsonSerializer.Serialize(new { modeloConfigurado = modelo, datosSinteticos = datos, resultado }, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
