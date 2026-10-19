using Catalogo.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Catalogo.Api;

public sealed class CatalogoExceptionHandler(IProblemDetailsService problems) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        if (exception is not CatalogoException error) return false;
        var status = error.Codigo switch { "validacion" => 400, "no_encontrada" => 404, _ => 409 };
        context.Response.StatusCode = status;
        return await problems.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = status, Title = "No se pudo completar la operación de catálogo.", Detail = error.Message,
                Extensions = { ["code"] = error.Codigo }
            }
        });
    }
}
