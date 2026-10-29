using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Solicitudes.Domain;
using Solicitudes.Application;
namespace Solicitudes.Api;
public sealed class SolicitudesExceptionHandler(IProblemDetailsService problems) : IExceptionHandler {
 public async ValueTask<bool> TryHandleAsync(HttpContext context,Exception exception,CancellationToken ct) {
  var status=exception switch {SolicitudException=>400,QuipuxDuplicadoException=>409,CatalogoNoDisponibleException=>503,_=>500};
  context.Response.StatusCode=status;
  return await problems.TryWriteAsync(new ProblemDetailsContext {HttpContext=context,ProblemDetails=new ProblemDetails {Status=status,Title=status==500?"No se pudo completar la operación.":exception.Message}});
 }
}
