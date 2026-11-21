using Yarp.ReverseProxy.Transforms;

namespace Gateway.Security;

public static class InternalAccess
{
    public static IReverseProxyBuilder AddInternalGateway(this IReverseProxyBuilder proxy)
    {
        proxy.Services.AddSingleton<InternalGatewayKey>();
        proxy.AddTransforms(builder =>
        {
            var key = builder.Services.GetRequiredService<InternalGatewayKey>().Value;
            builder.AddRequestTransform(context =>
            {
                // Nunca confiar en una cabecera aportada por el cliente, ni concatenarla.
                context.ProxyRequest.Headers.Remove("X-Internal-Key");
                context.ProxyRequest.Headers.TryAddWithoutValidation("X-Internal-Key", key);
                return ValueTask.CompletedTask;
            });
        });
        return proxy;
    }
}

public sealed class InternalGatewayKey
{
    public string Value { get; }

    public InternalGatewayKey(IConfiguration configuration)
    {
        var key = configuration["InternalAccess:Key"];
        if (key is null || key.Length is < 32 or > 512 || key.Any(c => c < '!' || c > '~'))
            throw new InvalidOperationException("Configure InternalAccess:Key con 32 a 512 caracteres ASCII imprimibles sin espacios.");
        Value = key;
    }
}
