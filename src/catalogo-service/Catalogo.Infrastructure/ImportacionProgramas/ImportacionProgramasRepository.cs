using Catalogo.Application.ImportacionProgramas;
using Catalogo.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Catalogo.Infrastructure.ImportacionProgramas;

public sealed class ImportacionProgramasRepository(CatalogoDbContext db) : IImportacionProgramasRepository
{
    /// <summary>Una sola llamada a SaveChanges: EF la ejecuta en una transacción, así que no queda una asignatura sin sus unidades o su PDF.</summary>
    public async Task GuardarAsync(Asignatura asignatura, IReadOnlyList<UnidadCurricular> unidades, ProgramaAnalitico programa, CancellationToken ct)
    {
        db.Add(asignatura);
        db.AddRange(unidades);
        db.Add(programa);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        { throw new CatalogoException("duplicado", "Ya existe una asignatura con ese código en la carrera."); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 547 })
        { throw new CatalogoException("no_encontrada", "No se pudo registrar la asignatura; comprueba que su carrera todavía existe."); }
    }

    public Task<ProgramaAnalitico?> ObtenerProgramaAsync(Guid asignaturaId, CancellationToken ct) =>
        db.Set<ProgramaAnalitico>().AsNoTracking().SingleOrDefaultAsync(p => p.AsignaturaId == asignaturaId, ct);
}
