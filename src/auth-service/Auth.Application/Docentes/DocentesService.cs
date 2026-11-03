using Auth.Application.Abstractions;

namespace Auth.Application.Docentes;

public sealed record DocenteDto(Guid Id, string NombreUsuario);

/// <summary>Lista los usuarios activos con rol Docente para que otros servicios los asignen (HU-29).</summary>
public sealed class DocentesService(IUserRepository users)
{
    public const string RolDocente = "Docente";

    public async Task<IReadOnlyList<DocenteDto>> ListarActivosAsync(CancellationToken cancellationToken = default)
        => (await users.ListActiveByRoleAsync(RolDocente, cancellationToken))
            .Select(u => new DocenteDto(u.Id, u.NombreUsuario))
            .OrderBy(d => d.NombreUsuario, StringComparer.Ordinal)
            .ToList();
}
