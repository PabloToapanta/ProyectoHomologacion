using Auth.Application.Abstractions;
using Auth.Domain.Entities;
using Auth.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Repositories;

public sealed class UserRepository(AuthDbContext db) : IUserRepository
{
    public Task<Usuario?> FindByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken = default)
        => db.Usuarios.Include(x => x.Rol)
            .SingleOrDefaultAsync(x => x.NombreNormalizado == normalizedName, cancellationToken);

    public async Task UpdatePasswordHashAsync(Usuario usuario, string passwordHash, CancellationToken cancellationToken = default)
    {
        usuario.PasswordHash = passwordHash;
        await db.SaveChangesAsync(cancellationToken);
    }
}
