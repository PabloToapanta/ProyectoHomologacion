using Homologacion.Application.AnalisisExterno;
using Homologacion.Application.AnalisisInterno;
using Homologacion.Infrastructure.AnalisisExterno;
using Homologacion.Application.RevisionDocente;
using Homologacion.Infrastructure.RevisionDocente;
using Homologacion.Application.Informe;
using Homologacion.Infrastructure.Informe;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;

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
        services.AddDbContext<ResultadosDbContext>((p, options) => options.UseSqlServer(
            p.GetRequiredService<IConfiguration>().GetConnectionString("Homologacion")
            ?? throw new InvalidOperationException("Configure ConnectionStrings:Homologacion para guardar análisis.")));
        services.AddScoped<IResultadosAnalisisInterno, ResultadosAnalisisSql>();
        services.AddScoped<GestionarAnalisisInterno>();
        services.AddScoped<IReceptorasAnalisisInterno>(p => p.GetRequiredService<FuentesAnalisisHttp>());
        // HU-19: mismas fuentes autorizadas y base; el análisis externo no usa el agente (D2).
        services.AddScoped<ISolicitudesAnalisisExterno>(p => p.GetRequiredService<FuentesAnalisisHttp>());
        services.AddScoped<IResultadosAnalisisExterno, ResultadosAnalisisExternoSql>();
        services.AddScoped<AnalizarSolicitudExterna>();
        services.AddScoped<GestionarAnalisisExterno>();
        // HU-31: análisis propio de los docentes asignados junto al del agente.
        services.AddScoped<IRevisionesDocentes, RevisionesDocentesSql>();
        services.AddScoped<IInformesHomologacion, InformesHomologacionSql>();
        // HU-23: con el informe generado, RegistrarRevisionDocente rechaza correcciones.
        services.AddScoped(p => new RegistrarRevisionDocente(p.GetRequiredService<ISolicitudesAnalisisInterno>(), p.GetRequiredService<ICatalogoAnalisisInterno>(),
            p.GetRequiredService<IResultadosAnalisisInterno>(), p.GetRequiredService<IResultadosAnalisisExterno>(), p.GetRequiredService<IRevisionesDocentes>(),
            p.GetRequiredService<TimeProvider>(), p.GetRequiredService<IInformesHomologacion>()));
        services.AddScoped<GenerarInformeHomologacion>();
        return services;
    }

    private sealed class AgenteNoConfigurado : IAgenteAnalisisInterno
    {
        public Task<ExplicacionAgente> JustificarAsync(ContextoAgente contexto, CancellationToken cancellationToken) =>
            Task.FromException<ExplicacionAgente>(new FalloAgenteException(TipoFalloAgente.NoDisponible));
    }
}
