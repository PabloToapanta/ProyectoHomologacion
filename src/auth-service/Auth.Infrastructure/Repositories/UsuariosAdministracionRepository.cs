using Auth.Application.Usuarios;
using Auth.Domain.Entities;
using Auth.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Repositories;

public sealed class UsuariosAdministracionRepository(AuthDbContext db) : IUsuariosAdministracionRepository
{
    public async Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken cancellationToken)
        => await db.Usuarios.AsNoTracking().Include(u => u.Rol).ToListAsync(cancellationToken);

    public Task<Usuario?> ObtenerAsync(Guid id, CancellationToken cancellationToken)
        => db.Usuarios.Include(u => u.Rol).SingleOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<bool> ExisteNombreAsync(string nombreNormalizado, CancellationToken cancellationToken)
        => db.Usuarios.AnyAsync(u => u.NombreNormalizado == nombreNormalizado, cancellationToken);

    public Task<Rol?> ObtenerRolAsync(string nombre, CancellationToken cancellationToken)
        => db.Roles.SingleOrDefaultAsync(r => r.Nombre == nombre, cancellationToken);

    public async Task<bool> CrearAsync(Usuario usuario, CancellationToken cancellationToken)
    {
        db.Usuarios.Add(usuario);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException error) when (EsNombreDuplicado(error))
        {
            db.Entry(usuario).State = EntityState.Detached;
            return false;
        }
    }

    public Task GuardarAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);

    private static bool EsNombreDuplicado(DbUpdateException error)
        => error.InnerException is SqlException sql && sql.Number is 2601 or 2627;
}
