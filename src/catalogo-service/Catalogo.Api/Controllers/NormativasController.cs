using System.ComponentModel.DataAnnotations;
using Catalogo.Application;
using Catalogo.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Catalogo.Api.Controllers;

public sealed class NormativaForm
{
    /// <summary>Código de la resolución, único (por ejemplo RES-CAU-P-005-2023).</summary>
    [Required] public string? Codigo { get; set; }
    [Required] public string? Titulo { get; set; }
    /// <summary>Órgano que emite la normativa.</summary>
    [Required] public string? Emisor { get; set; }
    [Required] public DateOnly? FechaEmision { get; set; }
    [Required] public DateOnly? VigenteDesde { get; set; }
    /// <summary>Si es una reforma, la normativa vigente que modifica; esa pasa a Reformada.</summary>
    public Guid? ReformaAId { get; set; }
    /// <summary>PDF oficial de hasta 10 MB; el tipo se comprueba por el contenido.</summary>
    [Required] public IFormFile? Archivo { get; set; }
}

public sealed record MotivoNormativaRequest([Required] string? Motivo);

/// <summary>
/// HU-38: catálogo de normativas institucionales. Consultan Administrador, Coordinador y Docente;
/// registran y derogan Administrador y Coordinador; solo el Administrador elimina un registro equivocado.
/// </summary>
[ApiController, Route("api/normativas")]
[Authorize(Roles = "Administrador,Coordinador,Docente")]
[ProducesResponseType(typeof(ProblemDetails), 401), ProducesResponseType(typeof(ProblemDetails), 403)]
public sealed class NormativasController(NormativaService service) : ControllerBase
{
    private const long MaxBytesSolicitud = Normativa.MaxBytes + 1024 * 1024;

    /// <summary>Lista las normativas, la más reciente primero. Filtro opcional: Vigente, Reformada o Derogada.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<NormativaDto>), 200), ProducesResponseType(typeof(ProblemDetails), 400)]
    public async Task<IActionResult> Listar([FromQuery] string? estado, CancellationToken ct) => Ok(await service.ListarAsync(estado, ct));

    /// <summary>Consulta una normativa, con la que reforma y la que la reformó.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(NormativaDto), 200), ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> Obtener(Guid id, CancellationToken ct) => Ok(await service.ObtenerAsync(id, ct));

    /// <summary>Descarga el PDF oficial con su huella SHA-256.</summary>
    [HttpGet("{id:guid}/pdf")]
    [ProducesResponseType(typeof(FileContentResult), 200, "application/pdf"), ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> Pdf(Guid id, CancellationToken ct)
    {
        var documento = await service.DocumentoAsync(id, ct);
        Response.Headers["X-Content-SHA256"] = documento.Sha256;
        return File(documento.Contenido, "application/pdf", documento.NombreArchivo);
    }

    /// <summary>Historial de registro, reforma, restitución, derogación y eliminación, con fecha y usuario.</summary>
    [HttpGet("{id:guid}/historial")]
    [ProducesResponseType(typeof(IReadOnlyList<EventoNormativaDto>), 200), ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> Historial(Guid id, CancellationToken ct) => Ok(await service.HistorialAsync(id, ct));

    /// <summary>Registra una normativa con su PDF. Si reforma otra, la anterior pasa a Reformada.</summary>
    /// <remarks>409 si el código ya existe o la normativa que se reforma no está vigente.</remarks>
    [HttpPost, Authorize(Roles = "Administrador,Coordinador")]
    [RequestSizeLimit(MaxBytesSolicitud), RequestFormLimits(MultipartBodyLengthLimit = MaxBytesSolicitud)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(NormativaDto), 201), ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404), ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> Registrar([FromForm] NormativaForm form, CancellationToken ct)
    {
        var contenido = await Leer(form.Archivo!, ct);
        var normativa = await service.RegistrarAsync(new RegistroNormativa(form.Codigo, form.Titulo, form.Emisor,
            form.FechaEmision!.Value, form.VigenteDesde!.Value, form.ReformaAId, form.Archivo!.FileName, contenido), Usuario(), ct);
        return CreatedAtAction(nameof(Obtener), new { id = normativa.Id }, normativa);
    }

    /// <summary>Deroga una normativa vigente con un motivo. No la elimina.</summary>
    [HttpPost("{id:guid}/derogacion"), Authorize(Roles = "Administrador,Coordinador")]
    [ProducesResponseType(typeof(NormativaDto), 200), ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404), ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> Derogar(Guid id, MotivoNormativaRequest request, CancellationToken ct) =>
        Ok(await service.DerogarAsync(id, request.Motivo, Usuario(), ct));

    /// <summary>Elimina una normativa registrada por error. Solo Administrador y con motivo.</summary>
    /// <remarks>Si era una reforma, la normativa que reformaba vuelve a Vigente. La eliminación queda en el historial.
    /// 409 si la normativa ya fue reformada: primero se elimina la reforma.</remarks>
    [HttpDelete("{id:guid}"), Authorize(Roles = "Administrador")]
    [ProducesResponseType(204), ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404), ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> Eliminar(Guid id, MotivoNormativaRequest request, CancellationToken ct)
    {
        await service.EliminarAsync(id, request.Motivo, Usuario(), ct);
        return NoContent();
    }

    private string? Usuario() => User.FindFirst("name")?.Value ?? User.FindFirst("sub")?.Value;

    /// <summary>Lee como máximo un byte más que el límite, para rechazar el exceso sin cargarlo completo.</summary>
    private static async Task<byte[]> Leer(IFormFile archivo, CancellationToken ct)
    {
        await using var origen = archivo.OpenReadStream();
        using var destino = new MemoryStream();
        var buffer = new byte[81920];
        int leidos;
        while ((leidos = await origen.ReadAsync(buffer, ct)) > 0)
        {
            destino.Write(buffer, 0, leidos);
            if (destino.Length > Normativa.MaxBytes) break;
        }
        return destino.ToArray();
    }
}
