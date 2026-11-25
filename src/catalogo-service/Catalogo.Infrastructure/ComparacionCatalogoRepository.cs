using Catalogo.Application;
using Catalogo.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catalogo.Infrastructure;

public sealed class ComparacionCatalogoRepository(CatalogoDbContext db) : IComparacionCatalogoRepository
{
    public Task<PerfilComparacion?> PerfilAsync(Guid id, CancellationToken ct) => db.Set<PerfilComparacion>().SingleOrDefaultAsync(p => p.AsignaturaId == id, ct);
    public Task<RevisionComparacion?> RevisionAsync(Guid origen, Guid receptora, CancellationToken ct) => db.Set<RevisionComparacion>().SingleOrDefaultAsync(p => p.OrigenId == origen && p.ReceptoraId == receptora, ct);
    public async Task GuardarPerfilAsync(PerfilComparacion p, CancellationToken ct)
    { if (db.Entry(p).State == EntityState.Detached) db.Add(p); await db.SaveChangesAsync(ct); }
    public async Task<IReadOnlyList<(Guid Origen, Guid Receptora)>> RevisionesEntreAsync(IReadOnlyCollection<Guid> origenes, IReadOnlyCollection<Guid> receptoras, CancellationToken ct) =>
        (await db.Set<RevisionComparacion>().AsNoTracking().Where(r => origenes.Contains(r.OrigenId) && receptoras.Contains(r.ReceptoraId))
            .Select(r => new { r.OrigenId, r.ReceptoraId }).ToListAsync(ct)).Select(r => (r.OrigenId, r.ReceptoraId)).ToArray();
    public async Task GuardarRevisionAsync(RevisionComparacion r, CancellationToken ct)
    { if (db.Entry(r).State == EntityState.Detached) db.Add(r); await db.SaveChangesAsync(ct); }
}
