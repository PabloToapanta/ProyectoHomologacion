using Auth.Application.Abstractions;
using Auth.Domain.Entities;

namespace Auth.Application.Usuarios;

public sealed record UsuarioResumen(Guid Id, string NombreUsuario, string Rol, bool Activo);
public enum ResultadoGestion { Correcto, Invalido, NoEncontrado, Duplicado }
public sealed record AltaUsuario(ResultadoGestion Resultado, UsuarioResumen? Usuario = null);

public interface IUsuariosAdministracionRepository
{
    Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken cancellationToken);
    Task<Usuario?> ObtenerAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExisteNombreAsync(string nombreNormalizado, CancellationToken cancellationToken);
    Task<Rol?> ObtenerRolAsync(string nombre, CancellationToken cancellationToken);
    Task<bool> CrearAsync(Usuario usuario, CancellationToken cancellationToken);
    Task GuardarAsync(CancellationToken cancellationToken);
}

public sealed class UsuariosService(IUsuariosAdministracionRepository usuarios, IPasswordService passwords)
{
    public async Task<IReadOnlyList<UsuarioResumen>> ListarAsync(CancellationToken cancellationToken)
        => (await usuarios.ListarAsync(cancellationToken))
            .Where(u => EsRolGestionable(u.Rol.Nombre)).Select(Resumen)
            .OrderBy(u => u.NombreUsuario, StringComparer.OrdinalIgnoreCase).ToList();

    public async Task<UsuarioResumen?> ObtenerAsync(Guid id, CancellationToken cancellationToken)
    {
        var usuario = await usuarios.ObtenerAsync(id, cancellationToken);
        return usuario is not null && EsRolGestionable(usuario.Rol.Nombre) ? Resumen(usuario) : null;
    }

    public async Task<AltaUsuario> CrearAsync(string? nombre, string? rol, string? contrasena, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Length > 256 || !EsRolGestionable(rol)
            || !ContrasenaValida(contrasena))
            return new(ResultadoGestion.Invalido);

        var limpio = nombre.Trim();
        var normalizado = limpio.ToUpperInvariant();
        if (await usuarios.ExisteNombreAsync(normalizado, cancellationToken))
            return new(ResultadoGestion.Duplicado);
        var entidadRol = await usuarios.ObtenerRolAsync(rol!, cancellationToken);
        if (entidadRol is null) return new(ResultadoGestion.Invalido);
        var usuario = new Usuario { NombreUsuario = limpio, NombreNormalizado = normalizado, Rol = entidadRol };
        usuario.PasswordHash = passwords.Hash(usuario, contrasena!);
        if (!await usuarios.CrearAsync(usuario, cancellationToken))
            return new(ResultadoGestion.Duplicado);
        return new(ResultadoGestion.Correcto, Resumen(usuario));
    }

    public async Task<ResultadoGestion> CambiarEstadoAsync(Guid id, Guid administradorId, bool activo, CancellationToken cancellationToken)
    {
        if (id == administradorId && !activo) return ResultadoGestion.Invalido;
        var usuario = await usuarios.ObtenerAsync(id, cancellationToken);
        if (usuario is null) return ResultadoGestion.NoEncontrado;
        if (!EsRolGestionable(usuario.Rol.Nombre)) return ResultadoGestion.Invalido;
        usuario.Activo = activo;
        await usuarios.GuardarAsync(cancellationToken);
        return ResultadoGestion.Correcto;
    }

    public async Task<ResultadoGestion> CambiarRolAsync(Guid id, string? rol, CancellationToken cancellationToken)
    {
        if (!EsRolGestionable(rol)) return ResultadoGestion.Invalido;
        var usuario = await usuarios.ObtenerAsync(id, cancellationToken);
        if (usuario is null) return ResultadoGestion.NoEncontrado;
        if (!EsRolGestionable(usuario.Rol.Nombre)) return ResultadoGestion.Invalido;
        var nuevoRol = await usuarios.ObtenerRolAsync(rol!, cancellationToken);
        if (nuevoRol is null) return ResultadoGestion.Invalido;
        usuario.Rol = nuevoRol;
        usuario.RolId = nuevoRol.Id;
        await usuarios.GuardarAsync(cancellationToken);
        return ResultadoGestion.Correcto;
    }

    public async Task<ResultadoGestion> CambiarContrasenaAsync(Guid id, string? contrasena, CancellationToken cancellationToken)
    {
        if (!ContrasenaValida(contrasena)) return ResultadoGestion.Invalido;
        var usuario = await usuarios.ObtenerAsync(id, cancellationToken);
        if (usuario is null) return ResultadoGestion.NoEncontrado;
        if (!EsRolGestionable(usuario.Rol.Nombre)) return ResultadoGestion.Invalido;
        usuario.PasswordHash = passwords.Hash(usuario, contrasena!);
        await usuarios.GuardarAsync(cancellationToken);
        return ResultadoGestion.Correcto;
    }

    private static bool EsRolGestionable(string? rol) => rol is "Docente" or "Coordinador";
    private static bool ContrasenaValida(string? valor) => !string.IsNullOrWhiteSpace(valor) && valor.Length is >= 12 and <= 1024;
    private static UsuarioResumen Resumen(Usuario usuario)
        => new(usuario.Id, usuario.NombreUsuario, usuario.Rol.Nombre, usuario.Activo);
}
