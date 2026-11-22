using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
namespace Homologacion.Api.Controllers;

public sealed class AgrupacionesOpenApi : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.DeclaringType != typeof(AgrupacionesController)) return;
        foreach (var p in operation.Parameters.Where(p => p.Name == "If-Match"))
        { p.Required = true; p.Description = "ETag fuerte exacto del grupo. Ausente: 428; obsoleto, débil o comodín: 409 sin escritura parcial."; }
        if (context.MethodInfo.Name is "Crear" or "Obtener" or "Reemplazar" or "Analizar")
            foreach (var r in operation.Responses.Where(r => r.Key is "200" or "201"))
                r.Value.Headers["ETag"] = new() { Description = "Versión fuerte y opaca de la composición/análisis guardado.", Schema = new() { Type = "string" } };
    }
}
