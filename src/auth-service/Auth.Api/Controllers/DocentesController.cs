using Auth.Application.Docentes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Api.Controllers;

/// <summary>Contrato de lectura HU-29 para asignar docentes desde Solicitudes; no expone contraseñas ni administra usuarios.</summary>
[ApiController]
[Route("api/usuarios/docentes")]
[Authorize(Roles = "Coordinador")]
public sealed class DocentesController(DocentesService service) : ControllerBase
{
    /// <summary>Lista identificador y nombre de usuario de las cuentas activas con rol Docente.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DocenteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<DocenteDto>>> Listar(CancellationToken cancellationToken)
        => Ok(await service.ListarActivosAsync(cancellationToken));
}
