using Homologacion.Application.Equivalencias;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Homologacion.Infrastructure.Configuration;

public sealed class EquivalenciaOptions
{
    public decimal UmbralPct { get; set; } = 80;
}

public sealed class UmbralConfigurado(IOptions<EquivalenciaOptions> options) : IUmbralEquivalencia
{
    public decimal Valor => options.Value.UmbralPct;
}

public static class DependencyInjection
{
    public static IServiceCollection AddHomologacionInfrastructure(this IServiceCollection services)
    {
        services.AddOptions<EquivalenciaOptions>().BindConfiguration("Equivalencia")
            .Validate(options => options.UmbralPct is > 0 and <= 100,
                "Equivalencia:UmbralPct debe ser mayor que 0 y menor o igual a 100.").ValidateOnStart();
        services.AddSingleton<IUmbralEquivalencia, UmbralConfigurado>();
        return services;
    }
}
