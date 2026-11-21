using System.Security.Claims;
using Auth.Application.Usuarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Api.Controllers;

public sealed record CrearUsuarioRequest(string? NombreUsuario, string? Rol, string? Contrasena);
public sealed record CambiarEstadoRequest(bool? Activo);
public sealed record CambiarRolRequest(string? Rol);
public sealed record CambiarContrasenaRequest(string? Contrasena);

/// <summary>Administración HU-02. La contraseña se recibe para calcular el hash y nunca aparece en la respuesta.</summary>
[ApiController]
[Route("api/usuarios")]
[Authorize(Roles = "Administrador")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class UsuariosController(UsuariosService usuarios) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UsuarioResumen>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UsuarioResumen>>> Listar(CancellationToken cancellationToken)
        => Ok(await usuarios.ListarAsync(cancellationToken));

    [HttpGet("{id:guid}", Name = "ConsultarUsuarioHu02")]
    [ProducesResponseType(typeof(UsuarioResumen), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UsuarioResumen>> Consultar(Guid id, CancellationToken cancellationToken)
    {
        var usuario = await usuarios.ObtenerAsync(id, cancellationToken);
        return usuario is null ? NotFound() : Ok(usuario);
    }

    [HttpPost]
    [ProducesResponseType(typeof(UsuarioResumen), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UsuarioResumen>> Crear(CrearUsuarioRequest request, CancellationToken cancellationToken)
    {
        var resultado = await usuarios.CrearAsync(request.NombreUsuario, request.Rol, request.Contrasena, cancellationToken);
        return resultado.Resultado switch
        {
            ResultadoGestion.Correcto => CreatedAtRoute("ConsultarUsuarioHu02", new { id = resultado.Usuario!.Id }, resultado.Usuario),
            ResultadoGestion.Duplicado => Problem(statusCode: 409, title: "El nombre de usuario ya existe."),
            _ => Problem(statusCode: 400, title: "Nombre, rol o contraseña inválidos.")
        };
    }

    [HttpPatch("{id:guid}/estado")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CambiarEstado(Guid id, CambiarEstadoRequest request, CancellationToken cancellationToken)
    {
        if (request.Activo is null) return Problem(statusCode: 400, title: "El estado es obligatorio.");
        var administradorId = Guid.TryParse(User.FindFirstValue("sub"), out var parsed) ? parsed : Guid.Empty;
        if (administradorId == Guid.Empty) return Problem(statusCode: 401, title: "Identidad de administrador inválida.");
        var resultado = await usuarios.CambiarEstadoAsync(id, administradorId, request.Activo.Value, cancellationToken);
        return Responder(resultado);
    }

    [HttpPut("{id:guid}/rol")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CambiarRol(Guid id, CambiarRolRequest request, CancellationToken cancellationToken)
        => Responder(await usuarios.CambiarRolAsync(id, request.Rol, cancellationToken));

    [HttpPut("{id:guid}/contrasena")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CambiarContrasena(Guid id, CambiarContrasenaRequest request, CancellationToken cancellationToken)
        => Responder(await usuarios.CambiarContrasenaAsync(id, request.Contrasena, cancellationToken));

    private IActionResult Responder(ResultadoGestion resultado) => resultado switch
    {
        ResultadoGestion.Correcto => Ok(),
        ResultadoGestion.NoEncontrado => Problem(statusCode: 404, title: "Usuario no encontrado."),
        _ => Problem(statusCode: 400, title: "Operación de usuario inválida.")
    };
}
