using Auth.Domain.Entities;

namespace Auth.Application.Abstractions;

public interface IUserRepository
{
    Task<Usuario?> FindByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken = default);
    Task UpdatePasswordHashAsync(Usuario usuario, string passwordHash, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Usuario>> ListActiveByRoleAsync(string roleName, CancellationToken cancellationToken = default);
}
