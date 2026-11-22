using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Solicitudes.Application;
namespace Solicitudes.Api;
/// <summary>HU-32 / Issue #63: cambio de estado a «Remitida». Ruta interna para Homologación; no se publica en el Gateway.</summary>
[ApiController,Route("api/solicitudes/{id:guid}/remision"),Authorize(Roles="Docente")]
[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class RemisionSolicitudController(SolicitudService service) : ControllerBase {
 /// <summary>Marca la solicitud como «Remitida» cuando Homologación remite el informe al coordinador.</summary>
 /// <remarks>La invoca Homologación con el token del docente y la clave interna, después de comprobar el informe. Solo un docente
 /// asignado (claim `sub`): 404 si no existe o no está asignada, sin distinguir ambos casos. Transición única «Docentes asignados» →
 /// «Remitida»; repetirla responde 204 sin cambios para admitir reintentos; desde otro estado, 409.</remarks>
 [HttpPost,ProducesResponseType(204),ProducesResponseType(401),ProducesResponseType(403),ProducesResponseType(typeof(ProblemDetails),404),ProducesResponseType(typeof(ProblemDetails),409)]
 public async Task<IActionResult> Remitir(Guid id,CancellationToken ct) {
  if(!Guid.TryParse(User.FindFirst("sub")?.Value,out var docente)||docente==Guid.Empty) return Forbid();
  await service.MarcarRemitida(id,docente,ct);
  return NoContent();
 }
}
