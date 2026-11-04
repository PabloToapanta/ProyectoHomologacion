using Homologacion.Application.AnalisisInterno;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Homologacion.Infrastructure.AnalisisInterno;

public static class RegistroAnalisisInterno
{
    public static IServiceCollection AddAnalisisInterno(this IServiceCollection services)
    {
        services.AddOptions<ConfiguracionAnalisisInterno>().BindConfiguration(ConfiguracionAnalisisInterno.Section)
            .ValidateDataAnnotations().ValidateOnStart();
        services.TryAddSingleton<TimeProvider>(TimeProvider.System);
        services.AddHttpContextAccessor();
        services.AddTransient<AutorizacionFuentesAnalisisHandler>();
        services.AddHttpClient<FuentesAnalisisHttp>(http => http.Timeout = TimeSpan.FromSeconds(10))
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false })
            .AddHttpMessageHandler<AutorizacionFuentesAnalisisHandler>();
        services.AddScoped<ISolicitudesAnalisisInterno>(p => p.GetRequiredService<FuentesAnalisisHttp>());
        services.AddScoped<ICatalogoAnalisisInterno>(p => p.GetRequiredService<FuentesAnalisisHttp>());
        // El cliente del modelo no recibe el handler de credenciales internas ni Bearer.
        services.AddHttpClient("hu18-ollama", http => http.Timeout = Timeout.InfiniteTimeSpan)
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        services.AddScoped<IAgenteAnalisisInterno>(p =>
        {
            var config = p.GetRequiredService<IOptions<ConfiguracionAnalisisInterno>>().Value;
            return config.Habilitado
                ? new OllamaAgenteInterno(p.GetRequiredService<IHttpClientFactory>().CreateClient("hu18-ollama"),
                    new(new Uri(config.OllamaUrl), config.Modelo, TimeSpan.FromSeconds(config.TimeoutSegundos)))
                : new AgenteNoConfigurado();
        });
        services.AddScoped<AsistirEvaluacionInterna>();
        services.AddScoped<AnalizarSolicitudInterna>();
        return services;
    }

    private sealed class AgenteNoConfigurado : IAgenteAnalisisInterno
    {
        public Task<ExplicacionAgente> JustificarAsync(ContextoAgente contexto, CancellationToken cancellationToken) =>
            Task.FromException<ExplicacionAgente>(new FalloAgenteException(TipoFalloAgente.NoDisponible));
    }
}
