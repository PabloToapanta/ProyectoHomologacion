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

    public async Task<IReadOnlyList<Usuario>> ListActiveByRoleAsync(string roleName, CancellationToken cancellationToken = default)
        => await db.Usuarios.AsNoTracking().Include(x => x.Rol)
            .Where(x => x.Activo && x.Rol.Nombre == roleName).ToListAsync(cancellationToken);

    public async Task UpdatePasswordHashAsync(Usuario usuario, string passwordHash, CancellationToken cancellationToken = default)
    {
        usuario.PasswordHash = passwordHash;
        await db.SaveChangesAsync(cancellationToken);
    }
}
