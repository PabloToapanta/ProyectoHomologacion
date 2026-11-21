using System.Security.Cryptography;
using System.Text;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Auth.Api.Security;

// Adaptador HTTP local al servicio: no introduce dependencias en Domain/Application.
public static class InternalAccess
{
    public const string Header = "X-Internal-Key";

    public static IServiceCollection AddInternalAccess(this IServiceCollection services)
    {
        services.AddSingleton(provider =>
        {
            var key = provider.GetRequiredService<IConfiguration>()["InternalAccess:Key"];
            if (key is null || key.Length is < 32 or > 512 || key.Any(c => c < '!' || c > '~'))
                throw new InvalidOperationException("Configure InternalAccess:Key con 32 a 512 caracteres ASCII imprimibles sin espacios.");
            return new InternalKeyDigest(SHA256.HashData(Encoding.UTF8.GetBytes(key)));
        });
        services.AddSwaggerGen(options => options.DocumentFilter<InternalAccessDocumentFilter>());
        return services;
    }

    public static IApplicationBuilder UseInternalAccess(this IApplicationBuilder app)
    {
        var expected = app.ApplicationServices.GetRequiredService<InternalKeyDigest>();
        return app.Use(async (context, next) =>
        {
            var values = context.Request.Headers[Header];
            context.Request.Headers.Remove(Header);
            var value = values.Count == 1 ? values[0] : null;
            if (value is null || value.Length is < 32 or > 512
                || value.Any(c => c < '!' || c > '~')
                || !CryptographicOperations.FixedTimeEquals(expected.Bytes, SHA256.HashData(Encoding.UTF8.GetBytes(value))))
            {
                await Results.Problem(statusCode: StatusCodes.Status403Forbidden,
                    title: "Acceso interno no autorizado.").ExecuteAsync(context);
                return;
            }
            await next(context);
        });
    }

    private sealed record InternalKeyDigest(byte[] Bytes);
}

public sealed class InternalAccessDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument document, DocumentFilterContext context)
    {
        document.Info.Description = (document.Info.Description ?? "") +
            " Acceso interno: todas las peticiones requieren la cabecera reservada X-Internal-Key " +
            "agregada por un servidor autorizado; la ausencia o invalidez responde 403. " +
            "El Gateway la incorpora; no enviarla desde el navegador. JWT y roles siguen siendo obligatorios donde corresponda.";
        foreach (var operation in document.Paths.Values.SelectMany(path => path.Operations.Values))
            operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Clave interna inválida o permisos insuficientes." });
    }
}
