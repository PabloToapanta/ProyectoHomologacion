using Catalogo.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Catalogo.Api.Controllers;
/// <summary>Contrato de lectura HU-10 para registro de solicitudes; no concede escritura sobre el catálogo.</summary>
[ApiController,Route("api/carreras-solicitudes"),Authorize(Roles="Coordinador")]
public sealed class CarrerasSolicitudesController(CarreraService service,AsignaturaService asignaturas) : ControllerBase {
 /// <summary>Lista las asignaturas de una carrera para registrar las aprobadas de una solicitud (HU-11).</summary>
 [HttpGet("{id:guid}/asignaturas"),ProducesResponseType(typeof(IReadOnlyList<AsignaturaSolicitudDto>),200)]
 [ProducesResponseType(typeof(ProblemDetails),401),ProducesResponseType(typeof(ProblemDetails),403),ProducesResponseType(typeof(ProblemDetails),404)]
 public async Task<IActionResult> Asignaturas(Guid id,CancellationToken ct)=>Ok((await asignaturas.ListarAsync(id,ct)).Select(a=>new AsignaturaSolicitudDto(a.Id,a.Codigo,a.Nombre,a.Nivel,a.Creditos,a.Horas)).ToArray());
 /// <summary>Lista identificadores y nombres de carreras FISEI vigentes.</summary>
 [HttpGet,ProducesResponseType(typeof(IReadOnlyList<CarreraSolicitudDto>),200)]
 [ProducesResponseType(typeof(ProblemDetails),401),ProducesResponseType(typeof(ProblemDetails),403)]
 public async Task<IActionResult> Listar(CancellationToken ct)=>Ok((await service.ListarAsync(ct)).Where(c=>c.Vigencia&&c.Facultad=="FISEI").Select(c=>new CarreraSolicitudDto(c.Id,c.Nombre)).ToArray());
}
public sealed record CarreraSolicitudDto(Guid Id,string Nombre);
public sealed record AsignaturaSolicitudDto(Guid Id,string Codigo,string Nombre,int Nivel,decimal Creditos,int Horas);
