using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Solicitudes.Domain;
using Solicitudes.Application;
namespace Solicitudes.Api;
// Resuelve errores esperados antes de ExceptionHandlerMiddleware, que registra como fallo todo lo que recibe.
public sealed class SolicitudesExceptionFilter : IExceptionFilter {
 public void OnException(ExceptionContext context) {
  int? status=context.Exception switch {SolicitudException=>400,SolicitudNoEncontradaException=>404,QuipuxDuplicadoException=>409,AsignacionNoPermitidaException=>409,DatosExternosNoEditablesException=>409,_=>null};
  if(status is null) return;
  context.Result=new ObjectResult(new ProblemDetails {Status=status,Title=context.Exception.Message}) {StatusCode=status,ContentTypes={"application/problem+json"}};
  context.ExceptionHandled=true;
 }
}
public sealed class SolicitudesExceptionHandler(IProblemDetailsService problems) : IExceptionHandler {
 public async ValueTask<bool> TryHandleAsync(HttpContext context,Exception exception,CancellationToken ct) {
  var status=exception is CatalogoNoDisponibleException or DirectorioNoDisponibleException?503:500;
  context.Response.StatusCode=status;
  return await problems.TryWriteAsync(new ProblemDetailsContext {HttpContext=context,ProblemDetails=new ProblemDetails {Status=status,Title=status==500?"No se pudo completar la operación.":exception.Message}});
 }
}
