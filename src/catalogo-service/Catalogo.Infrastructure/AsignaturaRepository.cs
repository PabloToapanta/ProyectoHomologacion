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
    public Task<bool> ExisteCodigoAsync(Guid carreraId, string codigoNormalizado, CancellationToken ct, Guid? mallaId = null) =>
        db.Asignaturas.AnyAsync(a => a.CarreraId == carreraId && a.CodigoNormalizado == codigoNormalizado && (!mallaId.HasValue || a.MallaId == mallaId), ct);
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

    public async Task<bool> EliminarAsync(Guid id, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        try
        {
            if (!await db.Asignaturas.AnyAsync(a => a.Id == id, ct)) return false;
            var unidades = db.Set<UnidadCurricular>().Where(u => u.AsignaturaId == id).Select(u => u.Id);
            // Orden explícito: las claves foráneas del catálogo son restrictivas.
            await db.Set<Contenido>().Where(c => unidades.Contains(c.UnidadCurricularId)).ExecuteDeleteAsync(ct);
            await db.Set<UnidadTematica>().Where(t => unidades.Contains(t.UnidadCurricularId)).ExecuteDeleteAsync(ct);
            await db.Set<UnidadCurricular>().Where(u => u.AsignaturaId == id).ExecuteDeleteAsync(ct);
            await db.Set<RevisionComparacion>().Where(r => r.OrigenId == id || r.ReceptoraId == id).ExecuteDeleteAsync(ct);
            await db.Set<PerfilComparacion>().Where(p => p.AsignaturaId == id).ExecuteDeleteAsync(ct);
            await db.Set<ProgramaAnalitico>().Where(p => p.AsignaturaId == id).ExecuteDeleteAsync(ct);
            await db.Asignaturas.Where(a => a.Id == id).ExecuteDeleteAsync(ct);
            await transaction.CommitAsync(ct);
            return true;
        }
        catch (SqlException ex) when (ex.Number is 547 or 1205)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw new CatalogoException("conflicto", "La asignatura cambió mientras se eliminaba. Actualiza el listado e inténtalo de nuevo.");
        }
    }
}
