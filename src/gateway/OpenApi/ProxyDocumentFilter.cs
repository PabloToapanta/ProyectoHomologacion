using System.Text.RegularExpressions;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Yarp.ReverseProxy.Configuration;

namespace Gateway.OpenApi;

// Documentar el transporte sin duplicar los DTO ni reglas de los microservicios.
public sealed class ProxyDocumentFilter(IProxyConfigProvider proxy) : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        foreach (var route in proxy.GetConfig().Routes)
        {
            var path = route.Match.Path!;
            if (!swaggerDoc.Paths.TryGetValue(path, out var item))
                swaggerDoc.Paths[path] = item = new OpenApiPathItem();
            foreach (var method in route.Match.Methods ?? [])
            {
                var anonymous = string.Equals(route.AuthorizationPolicy, "anonymous", StringComparison.OrdinalIgnoreCase);
                var operation = new OpenApiOperation
                {
                    Summary = $"Reenviar a {route.ClusterId}",
                    Description = "Conserva método, ruta, query, cuerpo y respuesta del servicio. El contrato del payload pertenece al microservicio.",
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new() { Description = "Respuesta del microservicio" },
                        ["400"] = new() { Description = "Solicitud inválida según el microservicio" },
                        ["401"] = new() { Description = anonymous ? "Credenciales incorrectas en auth-service" : "JWT ausente o inválido" },
                        ["502"] = new() { Description = "No se pudo contactar al destino" }
                    },
                    Security = []
                };
                if (!anonymous)
                {
                    operation.Responses["403"] = new() { Description = "JWT válido sin uno de los roles admitidos" };
                    operation.Security.Add(new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = []
                    });
                }
                foreach (Match parameter in Regex.Matches(path, "\\{([^}]+)\\}"))
                    operation.Parameters.Add(new OpenApiParameter
                    {
                        Name = parameter.Groups[1].Value, In = ParameterLocation.Path,
                        Required = true, Schema = new OpenApiSchema { Type = "string" }
                    });
                item.Operations[Enum.Parse<OperationType>(method, ignoreCase: true)] = operation;
            }
        }
    }
}
