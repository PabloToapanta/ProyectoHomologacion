using Catalogo.Application;
using Catalogo.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Catalogo.Infrastructure;

public sealed class UnidadRepository(CatalogoDbContext db) : IUnidadRepository
{
    public async Task<IReadOnlyList<UnidadCurricular>> ListarAsync(Guid asignaturaId, CancellationToken ct) =>
        await db.Set<UnidadCurricular>().AsNoTracking().Include(u => u.Contenidos).Include(u => u.Tematicas).Where(u => u.AsignaturaId == asignaturaId).OrderBy(u => u.Numero).ToListAsync(ct);
    public async Task CrearAsync(UnidadCurricular unidad, CancellationToken ct)
    {
        db.Add(unidad);
        await GuardarAsync(ct);
    }
    public Task<UnidadCurricular?> BuscarAsync(Guid asignaturaId, Guid unidadId, CancellationToken ct) =>
        db.Set<UnidadCurricular>().Include(u => u.Contenidos).Include(u => u.Tematicas).SingleOrDefaultAsync(u => u.Id == unidadId && u.AsignaturaId == asignaturaId, ct);
    public async Task GuardarAsync(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        { throw new CatalogoException("duplicado", "Ya existe ese número de unidad en la asignatura."); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 547 })
        { throw new CatalogoException("no_encontrada", "La asignatura ya no existe."); }
    }
}
