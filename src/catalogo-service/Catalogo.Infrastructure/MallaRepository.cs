using System.Data;
using Catalogo.Application;
using Catalogo.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Catalogo.Infrastructure;

public sealed class MallaRepository(CatalogoDbContext db) : IMallaRepository
{
    public async Task<IReadOnlyList<Malla>> ListarAsync(Guid carreraId, CancellationToken ct) =>
        await db.Mallas.AsNoTracking().Where(m => m.CarreraId == carreraId)
            .OrderByDescending(m => m.Vigente).ThenBy(m => m.NombreNormalizado).ToArrayAsync(ct);

    public Task<Malla?> BuscarAsync(Guid id, CancellationToken ct) =>
        db.Mallas.AsNoTracking().SingleOrDefaultAsync(m => m.Id == id, ct);

    public async Task CrearAsync(Malla malla, CancellationToken ct)
    {
        await Transaccion(malla.CarreraId, async () =>
        {
            if (await db.Mallas.AnyAsync(m => m.CarreraId == malla.CarreraId && m.NombreNormalizado == malla.NombreNormalizado, ct))
                throw new CatalogoException("duplicado", "Ya existe una malla con ese nombre en la carrera.");
            if (malla.Vigente) await Desactivar(malla.CarreraId, ct);
            db.Mallas.Add(malla);
            await db.SaveChangesAsync(ct);
        }, ct);
    }

    public async Task<Malla> ActivarAsync(Guid id, CancellationToken ct)
    {
        var malla = await BuscarAsync(id, ct) ?? throw new CatalogoException("no_encontrada", "No se encontró la malla.");
        await Transaccion(malla.CarreraId, async () =>
        {
            if (!await db.Mallas.AnyAsync(m => m.Id == id, ct))
                throw new CatalogoException("no_encontrada", "No se encontró la malla.");
            await Desactivar(malla.CarreraId, ct);
            await db.Mallas.Where(m => m.Id == id).ExecuteUpdateAsync(s => s.SetProperty(m => m.Vigente, true), ct);
            malla.EstablecerVigencia(true);
        }, ct);
        return malla;
    }

    private Task<int> Desactivar(Guid carreraId, CancellationToken ct) =>
        db.Mallas.Where(m => m.CarreraId == carreraId && m.Vigente)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.Vigente, false), ct);

    private async Task Transaccion(Guid carreraId, Func<Task> operacion, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            // Serializa las activaciones incluso cuando todavía no existe una versión vigente.
            if (db.Database.IsSqlServer())
                await db.Database.ExecuteSqlInterpolatedAsync($"SELECT [Id] FROM [Carreras] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {carreraId}", ct);
            if (!await db.Carreras.AnyAsync(c => c.Id == carreraId, ct))
                throw new CatalogoException("no_encontrada", "No se encontró la carrera.");
            await operacion();
            await transaction.CommitAsync(ct);
        }
        catch (Exception ex) when (ex is DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 or 547 or 1205 } }
            or SqlException { Number: 1205 })
        {
            await transaction.RollbackAsync(CancellationToken.None);
            db.ChangeTracker.Clear();
            throw new CatalogoException("conflicto", "La malla cambió o ya existe. Actualiza el listado e inténtalo de nuevo.");
        }
    }
}
