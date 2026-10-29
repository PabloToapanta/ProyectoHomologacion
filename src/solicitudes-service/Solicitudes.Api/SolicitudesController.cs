using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Solicitudes.Application;
using Solicitudes.Domain;
namespace Solicitudes.Api;
public sealed record SolicitudRequest(
 [Required,StringLength(100)] string NumeroQuipux,
 [Required,StringLength(50)] string IdentificacionEstudiante,
 [Required,StringLength(200)] string NombreEstudiante,
 [Required] string TipoCambio,
 Guid? CarreraOrigenId,
 [StringLength(200)] string? InstitucionOrigen,
 [StringLength(200)] string? CarreraOrigenExterna,
 Guid CarreraReceptoraId);
[ApiController,Route("api/solicitudes"),Authorize(Roles="Coordinador")]
[ProducesResponseType(typeof(ProblemDetails),401)]
[ProducesResponseType(typeof(ProblemDetails),403)]
public sealed class SolicitudesController(SolicitudService service,ICatalogoCarreras catalogo) : ControllerBase {
 /// <summary>Registra manualmente una solicitud recibida por Quipux. Número de trámite único.</summary>
 /// <remarks>Interno: carreras FISEI distintas. Externo: universidad ecuatoriana y carrera de origen separadas del catálogo receptor. No integra la API de Quipux.</remarks>
 [HttpPost,ProducesResponseType(typeof(Solicitud),201),ProducesResponseType(typeof(ProblemDetails),400),ProducesResponseType(typeof(ProblemDetails),409),ProducesResponseType(typeof(ProblemDetails),503)]
 public async Task<ActionResult<Solicitud>> Crear(SolicitudRequest input,CancellationToken ct) {
  var s=await service.Crear(new(input.NumeroQuipux,input.IdentificacionEstudiante,input.NombreEstudiante,input.TipoCambio,input.CarreraOrigenId,input.InstitucionOrigen,input.CarreraOrigenExterna,input.CarreraReceptoraId),ct);
  return CreatedAtAction(nameof(Obtener),new{id=s.Id},s);
 }
 /// <summary>Lista solicitudes en páginas de 50 registros, recientes primero. Solo Coordinador.</summary>
 [HttpGet,ProducesResponseType(typeof(IReadOnlyList<Solicitud>),200),ProducesResponseType(typeof(ProblemDetails),400)]
 public async Task<IActionResult> Listar([FromQuery]int pagina=1,CancellationToken ct=default)=>Ok(await service.Listar(pagina,ct));
 /// <summary>Consulta el detalle de una solicitud registrada.</summary>
 [HttpGet("{id:guid}"),ProducesResponseType(typeof(Solicitud),200),ProducesResponseType(typeof(ProblemDetails),404)]
 public async Task<IActionResult> Obtener(Guid id,CancellationToken ct) {var s=await service.Obtener(id,ct);return s is null?NotFound(new ProblemDetails {Title="Solicitud no encontrada.",Status=404}):Ok(s);}
 /// <summary>Consulta carreras FISEI vigentes para el registro, mediante el servicio de catálogo.</summary>
 [HttpGet("carreras"),ProducesResponseType(typeof(IReadOnlyList<CarreraReferencia>),200),ProducesResponseType(typeof(ProblemDetails),503)]
 public async Task<IActionResult> Carreras(CancellationToken ct)=>Ok(await catalogo.Listar(ct));
}
