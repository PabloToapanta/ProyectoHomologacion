using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Solicitudes.Application;
using Solicitudes.Domain;
namespace Solicitudes.Api;
/// <summary>Consulta docente de solicitudes asignadas (HU-30 / Issue #41). Solo lectura: la revisión propia es HU-31.</summary>
[ApiController,Route("api/solicitudes/asignadas"),Authorize(Roles="Docente")]
[ProducesResponseType(typeof(ProblemDetails),401)]
[ProducesResponseType(typeof(ProblemDetails),403)]
public sealed class SolicitudesDocenteController(SolicitudService service) : ControllerBase {
 /// <summary>Lista las solicitudes donde el docente autenticado figura entre los dos asignados, recientes primero.</summary>
 /// <remarks>El docente se identifica por el claim `sub` del token; no se acepta como parámetro.</remarks>
 [HttpGet,ProducesResponseType(typeof(IReadOnlyList<Solicitud>),200)]
 public async Task<IActionResult> Listar(CancellationToken ct)=>DocenteId() is Guid docente?Ok(await service.ListarAsignadas(docente,ct)):Forbid();
 /// <summary>Consulta una solicitud asignada al docente y el estado de su análisis automatizado.</summary>
 /// <remarks>404 si no existe o no está asignada a quien consulta, sin distinguir ambos casos. Hasta que HU-18/HU-20 persistan resultados, `analisis.estado` es «Pendiente» y `resultado` es null.</remarks>
 [HttpGet("{id:guid}"),ProducesResponseType(typeof(SolicitudAsignada),200),ProducesResponseType(typeof(ProblemDetails),404)]
 public async Task<IActionResult> Obtener(Guid id,CancellationToken ct) {
  if(DocenteId() is not Guid docente) return Forbid();
  var s=await service.ObtenerAsignada(id,docente,ct);
  return s is null?NotFound(new ProblemDetails {Title="Solicitud no encontrada entre sus asignaciones.",Status=404}):Ok(s);
 }
 Guid? DocenteId()=>Guid.TryParse(User.FindFirst("sub")?.Value,out var id)?id:null;
}
