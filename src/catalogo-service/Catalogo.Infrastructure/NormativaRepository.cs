using System.Data;
using Catalogo.Application;
using Catalogo.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Catalogo.Infrastructure;

public sealed class NormativaRepository(CatalogoDbContext db) : INormativaRepository
{
    public async Task<IReadOnlyList<Normativa>> ListarAsync(EstadoNormativa? estado, CancellationToken ct)
    {
        var consulta = db.Normativas.AsNoTracking();
        if (estado is { } valor) consulta = consulta.Where(n => n.Estado == valor);
        // Más reciente primero: la vigencia decide qué norma rige.
        return (await consulta.ToArrayAsync(ct)).OrderByDescending(n => n.VigenteDesde).ThenBy(n => n.CodigoNormalizado).ToArray();
    }

    public Task<Normativa?> BuscarAsync(Guid id, CancellationToken ct) =>
        db.Normativas.AsNoTracking().SingleOrDefaultAsync(n => n.Id == id, ct);

    public async Task<DocumentoNormativa?> DocumentoAsync(Guid id, CancellationToken ct) =>
        await db.Normativas.AsNoTracking().Where(n => n.Id == id)
            .Select(n => new DocumentoNormativa(n.NombreArchivo, n.Sha256, n.Contenido)).SingleOrDefaultAsync(ct);

    public async Task<IReadOnlyList<EventoNormativa>> HistorialAsync(Guid id, CancellationToken ct) =>
        await db.EventosNormativa.AsNoTracking().Where(e => e.NormativaId == id).ToArrayAsync(ct);

    public Task RegistrarAsync(Normativa normativa, string usuario, DateTimeOffset fecha, CancellationToken ct) =>
        Transaccion(async () =>
        {
            if (await db.Normativas.AnyAsync(n => n.CodigoNormalizado == normativa.CodigoNormalizado, ct))
                throw new CatalogoException("duplicado", "Ya existe una normativa con ese código de resolución.");
            db.Normativas.Add(normativa);
            db.EventosNormativa.Add(new EventoNormativa(normativa, TipoEventoNormativa.Registrada, fecha, usuario,
                normativa.ReformaAId is null ? null : "Registrada como reforma."));
            if (normativa.ReformaAId is { } reformadaId)
            {
                var anterior = await db.Normativas.SingleOrDefaultAsync(n => n.Id == reformadaId, ct)
                    ?? throw new CatalogoException("no_encontrada", "No se encontró la normativa que se reforma.");
                anterior.MarcarReformadaPor(normativa.Id);
                db.EventosNormativa.Add(new EventoNormativa(anterior, TipoEventoNormativa.Reformada, fecha, usuario,
                    $"Reformada por {normativa.Codigo}."));
            }
            await db.SaveChangesAsync(ct);
        }, ct);

    public async Task<Normativa> DerogarAsync(Guid id, string? motivo, string usuario, DateTimeOffset fecha, CancellationToken ct)
    {
        Normativa? normativa = null;
        await Transaccion(async () =>
        {
            normativa = await db.Normativas.SingleOrDefaultAsync(n => n.Id == id, ct)
                ?? throw new CatalogoException("no_encontrada", "No se encontró la normativa.");
            normativa.Derogar(motivo, fecha, usuario);
            db.EventosNormativa.Add(new EventoNormativa(normativa, TipoEventoNormativa.Derogada, fecha, usuario, normativa.MotivoDerogacion));
            await db.SaveChangesAsync(ct);
        }, ct);
        return normativa!;
    }

    public Task EliminarAsync(Guid id, string motivo, string usuario, DateTimeOffset fecha, CancellationToken ct) =>
        Transaccion(async () =>
        {
            var normativa = await db.Normativas.SingleOrDefaultAsync(n => n.Id == id, ct)
                ?? throw new CatalogoException("no_encontrada", "No se encontró la normativa.");
            if (normativa.Estado == EstadoNormativa.Reformada)
                throw new CatalogoException("conflicto", "Esta normativa ya fue reformada: elimina primero la reforma.");
            if (normativa.ReformaAId is { } anteriorId
                && await db.Normativas.SingleOrDefaultAsync(n => n.Id == anteriorId, ct) is { } anterior
                && anterior.ReformadaPorId == normativa.Id)
            {
                anterior.Restituir();
                db.EventosNormativa.Add(new EventoNormativa(anterior, TipoEventoNormativa.Restituida, fecha, usuario,
                    $"Vuelve a regir: se eliminó la reforma {normativa.Codigo}."));
            }
            db.EventosNormativa.Add(new EventoNormativa(normativa, TipoEventoNormativa.Eliminada, fecha, usuario, motivo));
            db.Normativas.Remove(normativa);
            await db.SaveChangesAsync(ct);
        }, ct);

    private async Task Transaccion(Func<Task> operacion, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            await operacion();
            await transaction.CommitAsync(ct);
        }
        catch (Exception ex) when (ex is DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 or 547 or 1205 } }
            or SqlException { Number: 1205 } or DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            db.ChangeTracker.Clear();
            throw new CatalogoException("conflicto", "La normativa cambió o ya existe. Actualiza el listado e inténtalo de nuevo.");
        }
    }
}
