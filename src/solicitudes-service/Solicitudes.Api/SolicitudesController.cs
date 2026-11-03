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
public sealed record AsignaturaAprobadaRequest(
 Guid? AsignaturaOrigenId,
 [StringLength(200)] string? NombreAsignatura,
 decimal? Creditos,
 [StringLength(100)] string? SistemaCreditos,
 int? Horas,
 [StringLength(100)] string? UnidadHoras,
 [StringLength(200)] string? ReferenciaPrograma,
 [Required] decimal? Nota,
 decimal? NotaMaximaEscala,
 decimal? NotaMinimaAprobacion,
 [Required] DateOnly? FechaAprobacion,
 [Required,StringLength(50)] string NumeroMatricula,
 [Required] int? IntentoMatricula);
[ApiController,Route("api/solicitudes"),Authorize(Roles="Coordinador")]
[ProducesResponseType(typeof(ProblemDetails),401)]
[ProducesResponseType(typeof(ProblemDetails),403)]
public sealed class SolicitudesController(SolicitudService service,ICatalogoCarreras catalogo) : ControllerBase {
 /// <summary>Registra manualmente una solicitud recibida por Quipux. Número de trámite único.</summary>
 /// <remarks>Interno: carreras FISEI distintas. Externo: universidad ecuatoriana y carrera de origen separadas del catálogo receptor. No integra la API de Quipux.</remarks>
 [HttpPost,ProducesResponseType(typeof(Solicitud),201),ProducesResponseType(typeof(ProblemDetails),400),ProducesResponseType(typeof(ProblemDetails),409),ProducesResponseType(typeof(ProblemDetails),503)]
 public async Task<ActionResult<Solicitud>> Crear(SolicitudRequest input,CancellationToken ct) {
  var s=await service.Crear(new(input.NumeroQuipux,input.IdentificacionEstudiante,input.NombreEstudiante,input.TipoCambio,input.CarreraOrigenId,input.InstitucionOrigen,input.CarreraOrigenExterna,input.CarreraReceptoraId),ct);
  // Relativa: tras el Gateway el host de la petición es el nombre interno del servicio.
  return Created($"/api/solicitudes/{s.Id}",s);
 }
 /// <summary>Lista solicitudes en páginas de 50 registros, recientes primero. Solo Coordinador.</summary>
 [HttpGet,ProducesResponseType(typeof(IReadOnlyList<Solicitud>),200),ProducesResponseType(typeof(ProblemDetails),400)]
 public async Task<IActionResult> Listar([FromQuery]int pagina=1,CancellationToken ct=default)=>Ok(await service.Listar(pagina,ct));
 /// <summary>Registra una asignatura aprobada del estudiante dentro de una solicitud existente.</summary>
 /// <remarks>Interno: `asignaturaOrigenId` del catálogo y escala FISEI de 0 a 10, aprobada desde 7. Externo: nombre, créditos, horas, referencia al programa y escala de origen; no se convierte la nota. `estado` se deriva de la nota y no vuelve elegible la asignatura: aprobación y antigüedad se validan con HU-15.</remarks>
 [HttpPost("{id:guid}/asignaturas-aprobadas"),ProducesResponseType(typeof(AsignaturaAprobada),201),ProducesResponseType(typeof(ProblemDetails),400),ProducesResponseType(typeof(ProblemDetails),404),ProducesResponseType(typeof(ProblemDetails),503)]
 public async Task<ActionResult<AsignaturaAprobada>> RegistrarAsignatura(Guid id,AsignaturaAprobadaRequest input,CancellationToken ct) {
  var a=await service.RegistrarAsignatura(id,new(input.AsignaturaOrigenId,input.NombreAsignatura,input.Creditos,input.SistemaCreditos,input.Horas,input.UnidadHoras,input.ReferenciaPrograma,input.Nota!.Value,input.NotaMaximaEscala,input.NotaMinimaAprobacion,input.FechaAprobacion!.Value,input.NumeroMatricula,input.IntentoMatricula!.Value),ct);
  return Created($"/api/solicitudes/{id}",a);
 }
 /// <summary>Lista las asignaturas del catálogo que pueden seleccionarse como origen. Vacía en cambios externos.</summary>
 [HttpGet("{id:guid}/asignaturas-origen"),ProducesResponseType(typeof(IReadOnlyList<AsignaturaCatalogo>),200),ProducesResponseType(typeof(ProblemDetails),404),ProducesResponseType(typeof(ProblemDetails),503)]
 public async Task<IActionResult> AsignaturasOrigen(Guid id,CancellationToken ct)=>Ok(await service.AsignaturasOrigen(id,ct));
 /// <summary>Consulta el detalle de una solicitud registrada con sus asignaturas aprobadas.</summary>
 [HttpGet("{id:guid}"),ProducesResponseType(typeof(SolicitudDetalle),200),ProducesResponseType(typeof(ProblemDetails),404)]
 public async Task<IActionResult> Obtener(Guid id,CancellationToken ct) {var s=await service.Obtener(id,ct);return s is null?NotFound(new ProblemDetails {Title="Solicitud no encontrada.",Status=404}):Ok(s);}
 /// <summary>Consulta carreras FISEI vigentes para el registro, mediante el servicio de catálogo.</summary>
 [HttpGet("carreras"),ProducesResponseType(typeof(IReadOnlyList<CarreraReferencia>),200),ProducesResponseType(typeof(ProblemDetails),503)]
 public async Task<IActionResult> Carreras(CancellationToken ct)=>Ok(await catalogo.Listar(ct));
}
