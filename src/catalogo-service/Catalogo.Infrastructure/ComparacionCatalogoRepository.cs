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
    public async Task GuardarRevisionAsync(RevisionComparacion r, CancellationToken ct)
    { if (db.Entry(r).State == EntityState.Detached) db.Add(r); await db.SaveChangesAsync(ct); }
}
