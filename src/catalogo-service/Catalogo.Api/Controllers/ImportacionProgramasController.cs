using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using Catalogo.Application;
using Catalogo.Application.ImportacionProgramas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Catalogo.Api.Controllers;

public sealed class AnalisisProgramasForm
{
    [Required] public Guid? CarreraId { get; set; }
    /// <summary>Entre 1 y 20 PDF con texto, de hasta 5 MB cada uno.</summary>
    public List<IFormFile> Archivos { get; set; } = [];
}

public sealed class ConfirmacionProgramaForm
{
    /// <summary>Borrador revisado en JSON: carreraId, codigo, nombre, nivel, creditos, horas y unidades con sus temáticas.</summary>
    [Required] public string? Programa { get; set; }
    /// <summary>PDF original del que se obtuvo el borrador; se conserva para trazabilidad.</summary>
    [Required] public IFormFile? Archivo { get; set; }
}

/// <summary>HU-08 / Issue #50: carga masiva de programas analíticos FISEI con revisión humana. Solo Administrador.</summary>
[ApiController]
[Authorize(Roles = "Administrador")]
[ProducesResponseType(typeof(ProblemDetails), 401)]
[ProducesResponseType(typeof(ProblemDetails), 403)]
public sealed class ImportacionProgramasController(ImportacionProgramasService service) : ControllerBase
{
    private const long MaxBytesLote = 25L * 1024 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, MaxDepth = 16 };

    /// <summary>Analiza uno o varios PDF del plan analítico FISEI y devuelve un borrador por archivo, sin guardar en el catálogo.</summary>
    /// <remarks>Extracción determinista sin OCR ni agente. Estados por archivo: Borrador, SinTexto o Invalido; un archivo fallido no detiene el resto.
    /// El tipo se comprueba por el contenido, no por el nombre ni el tipo declarado. Los datos no encontrados quedan vacíos en `faltantes`.</remarks>
    [HttpPost("api/importaciones-programas/analisis"), RequestSizeLimit(MaxBytesLote), RequestFormLimits(MultipartBodyLengthLimit = MaxBytesLote)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ResultadoAnalisisLote), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<ActionResult<ResultadoAnalisisLote>> Analizar([FromForm] AnalisisProgramasForm form, CancellationToken ct)
    {
        if (form.Archivos.Count > ImportacionProgramasService.MaxArchivos)
            return Problem(statusCode: 400, title: $"Selecciona entre 1 y {ImportacionProgramasService.MaxArchivos} archivos PDF.");
        var archivos = new List<ArchivoPrograma>();
        foreach (var archivo in form.Archivos) archivos.Add(new(archivo.FileName, await Leer(archivo, ct)));
        return Ok(await service.AnalizarAsync(form.CarreraId!.Value, archivos, ct));
    }

    /// <summary>Guarda en el catálogo el borrador revisado y conserva el PDF original vinculado a la asignatura.</summary>
    /// <remarks>Aplica las reglas de HU-05 y HU-06 y guarda asignatura, unidades, temáticas y PDF en una sola transacción.
    /// 409 si el código ya existe en la carrera: no se sobrescribe. Registra hash SHA-256, fecha y usuario.</remarks>
    [HttpPost("api/importaciones-programas/confirmacion"), RequestSizeLimit(MaxBytesLote), RequestFormLimits(MultipartBodyLengthLimit = MaxBytesLote)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(AsignaturaDto), 201)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> Confirmar([FromForm] ConfirmacionProgramaForm form, CancellationToken ct)
    {
        var usuario = User.FindFirst("name")?.Value ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(usuario)) return Forbid();
        ProgramaConfirmado? programa;
        try { programa = JsonSerializer.Deserialize<ProgramaConfirmado>(form.Programa!, JsonOptions); }
        catch (JsonException) { return Problem(statusCode: 400, title: "El borrador enviado no es un JSON válido."); }
        if (programa is null) return Problem(statusCode: 400, title: "Falta el borrador del programa.");
        var resultado = await service.ConfirmarAsync(programa, new(form.Archivo!.FileName, await Leer(form.Archivo, ct)), usuario, ct);
        return Created($"/api/asignaturas/{resultado.Id}", resultado);
    }

    /// <summary>Descarga el PDF original del programa analítico con el que se registró la asignatura.</summary>
    [HttpGet("api/asignaturas/{id:guid}/programa-analitico")]
    [ProducesResponseType(typeof(FileContentResult), 200, "application/pdf")]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> Descargar(Guid id, CancellationToken ct)
    {
        var programa = await service.ObtenerProgramaAsync(id, ct);
        if (programa is null) return Problem(statusCode: 404, title: "La asignatura no tiene un programa analítico cargado.");
        Response.Headers["X-Content-SHA256"] = programa.Sha256;
        Response.Headers["X-Cargado-Por"] = Uri.EscapeDataString(programa.CargadoPor);
        Response.Headers["X-Fecha-Carga"] = programa.FechaCarga.ToString("O");
        return File(programa.Contenido, "application/pdf", programa.NombreArchivo);
    }

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
            if (destino.Length > ImportacionProgramasService.MaxBytesArchivo) break;
        }
        return destino.ToArray();
    }
}
