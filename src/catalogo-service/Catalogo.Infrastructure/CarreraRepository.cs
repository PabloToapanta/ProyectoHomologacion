using Catalogo.Application;
using Catalogo.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Catalogo.Infrastructure;

public sealed class CarreraRepository(CatalogoDbContext db) : ICarreraRepository
{
    public async Task<IReadOnlyList<Carrera>> ListarAsync(CancellationToken ct) =>
        await db.Carreras.AsNoTracking().OrderBy(c => c.Nombre).ThenBy(c => c.Facultad).ToListAsync(ct);
    public Task<Carrera?> BuscarAsync(Guid id, CancellationToken ct) => db.Carreras.SingleOrDefaultAsync(c => c.Id == id, ct);
    public Task<bool> ExisteNombreAsync(string nombre, string facultad, Guid? excluirId, CancellationToken ct) =>
        db.Carreras.AnyAsync(c => c.NombreNormalizado == nombre && c.FacultadNormalizada == facultad && c.Id != excluirId, ct);
    public Task<bool> TieneAsignaturasAsync(Guid id, CancellationToken ct) => db.Asignaturas.AnyAsync(a => a.CarreraId == id, ct);

    public async Task GuardarAsync(Carrera carrera, bool nueva, CancellationToken ct)
    {
        if (nueva) db.Carreras.Add(carrera);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            // La restricción única cubre también dos solicitudes concurrentes.
            throw new CatalogoException("duplicado", "Ya existe una carrera con ese nombre en la facultad.");
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new CatalogoException("no_encontrada", "No se encontró la carrera.");
        }
    }

    public async Task EliminarAsync(Carrera carrera, CancellationToken ct)
    {
        db.Carreras.Remove(carrera);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 547 })
        {
            throw new CatalogoException("asignaturas_relacionadas", "La carrera tiene asignaturas relacionadas y no puede eliminarse.");
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new CatalogoException("no_encontrada", "No se encontró la carrera.");
        }
    }
}
