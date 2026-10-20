using Catalogo.Application;
using Catalogo.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Catalogo.Infrastructure;

public sealed class AsignaturaRepository(CatalogoDbContext db) : IAsignaturaRepository
{
    public Task<Asignatura?> BuscarAsync(Guid id, CancellationToken ct) => db.Asignaturas.AsNoTracking().SingleOrDefaultAsync(a => a.Id == id, ct);
    public async Task<IReadOnlyList<Asignatura>> ListarPorCarreraAsync(Guid carreraId, CancellationToken ct) =>
        await db.Asignaturas.AsNoTracking().Where(a => a.CarreraId == carreraId).OrderBy(a => a.Nivel).ThenBy(a => a.CodigoNormalizado).ToListAsync(ct);
    public Task<bool> ExisteCodigoAsync(Guid carreraId, string codigoNormalizado, CancellationToken ct) =>
        db.Asignaturas.AnyAsync(a => a.CarreraId == carreraId && a.CodigoNormalizado == codigoNormalizado, ct);
    public async Task CrearAsync(Asignatura asignatura, CancellationToken ct)
    {
        db.Asignaturas.Add(asignatura);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new CatalogoException("duplicado", "Ya existe una asignatura con ese código en la carrera.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 547 })
        {
            throw new CatalogoException("no_encontrada", "No se pudo registrar la asignatura; comprueba que su carrera todavía existe.");
        }
    }
}
