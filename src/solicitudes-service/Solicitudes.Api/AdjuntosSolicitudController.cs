using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Solicitudes.Application;
namespace Solicitudes.Api;

public sealed class AdjuntoSolicitudForm
{
    [Required] public string Tipo { get; set; } = "";
    [Required] public IFormFile? Archivo { get; set; }
}

/// <summary>Adjuntos PDF de expediente (HU-12 / Issue #70). Los bytes permanecen en la base de Solicitudes.</summary>
[ApiController, Route("api/solicitudes/{id:guid}/adjuntos"), Authorize]
[ProducesResponseType(typeof(ProblemDetails), 401)]
public sealed class AdjuntosSolicitudController(SolicitudService service) : ControllerBase
{
    const long MaximoCuerpoMultipart = SolicitudService.TamanoMaximoAdjuntoBytes + 65_536;

    /// <summary>Adjunta historial académico o sílabo a una solicitud. Solo Coordinador.</summary>
    /// <remarks>El PDF no supera 20 MiB. El contenido, tamaño, SHA-256, usuario y fecha quedan en la base del servicio Solicitudes.</remarks>
    [HttpPost, Authorize(Roles = "Coordinador"), Consumes("multipart/form-data"),
     RequestSizeLimit(MaximoCuerpoMultipart), RequestFormLimits(MultipartBodyLengthLimit = MaximoCuerpoMultipart),
     ProducesResponseType(typeof(AdjuntoSolicitudMetadata), 201), ProducesResponseType(typeof(ProblemDetails), 400), ProducesResponseType(typeof(ProblemDetails), 403),
     ProducesResponseType(typeof(ProblemDetails), 404), ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<ActionResult<AdjuntoSolicitudMetadata>> Subir(Guid id, [FromForm] AdjuntoSolicitudForm form, CancellationToken ct)
    {
        if (form.Archivo is null) return BadRequest(new ProblemDetails { Status = 400, Title = "Seleccione un archivo PDF." });
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var coordinadorId)) return Forbid();
        if (form.Archivo.Length > SolicitudService.TamanoMaximoAdjuntoBytes)
            return BadRequest(new ProblemDetails { Status = 400, Title = "El archivo PDF no puede superar 20 MiB." });

        await using var stream = new MemoryStream((int)form.Archivo.Length);
        await form.Archivo.CopyToAsync(stream, ct);
        var metadata = await service.SubirAdjunto(id,
            new(form.Tipo, form.Archivo.FileName, form.Archivo.ContentType, stream.ToArray(), coordinadorId.ToString()), ct);
        return Created($"/api/solicitudes/{id}/adjuntos/{metadata.Id}", metadata);
    }

    /// <summary>Lista metadatos de adjuntos. Coordinador o docente asignado.</summary>
    [HttpGet, Authorize(Roles = "Coordinador,Docente"),
     ProducesResponseType(typeof(IReadOnlyList<AdjuntoSolicitudMetadata>), 200), ProducesResponseType(typeof(ProblemDetails), 403), ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> Listar(Guid id, CancellationToken ct)
    {
        var docenteId = DocenteId();
        if (User.IsInRole("Docente") && docenteId is null) return Forbid();
        return Ok(await service.ListarAdjuntos(id, docenteId, ct));
    }

    /// <summary>Descarga un PDF del expediente. Coordinador o docente asignado.</summary>
    [HttpGet("{adjuntoId:guid}"), Authorize(Roles = "Coordinador,Docente"),
     ProducesResponseType(typeof(FileContentResult), 200), ProducesResponseType(typeof(ProblemDetails), 403), ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> Descargar(Guid id, Guid adjuntoId, CancellationToken ct)
    {
        var docenteId = DocenteId();
        if (User.IsInRole("Docente") && docenteId is null) return Forbid();
        var adjunto = await service.ObtenerAdjunto(id, adjuntoId, docenteId, ct);
        return File(adjunto.Contenido, "application/pdf", adjunto.NombreArchivo);
    }

    Guid? DocenteId() => Guid.TryParse(User.FindFirst("sub")?.Value, out var id) ? id : null;
}
