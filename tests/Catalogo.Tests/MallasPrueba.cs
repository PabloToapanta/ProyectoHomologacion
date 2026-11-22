using Catalogo.Application;
using Catalogo.Domain;
using Catalogo.Infrastructure;

namespace Catalogo.Tests;

/// <summary>Datos sintéticos: las pruebas antiguas conservan sus aserciones y añaden la relación obligatoria.</summary>
internal sealed class MallasPrueba(Guid carreraId) : IMallaRepository
{
    private readonly Malla inicial = new(carreraId, "Inicial", true);
    public Task<IReadOnlyList<Malla>> ListarAsync(Guid id, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Malla>>(id == carreraId ? [inicial] : []);
    public Task<Malla?> BuscarAsync(Guid id, CancellationToken ct) => Task.FromResult<Malla?>(id == inicial.Id ? inicial : null);
    public Task CrearAsync(Malla malla, CancellationToken ct) => throw new NotSupportedException();
    public Task<Malla> ActivarAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
}

internal static class MallasSeed
{
    public static Asignatura EnMalla(this Asignatura asignatura, CatalogoDbContext db)
    {
        var malla = db.Mallas.Local.FirstOrDefault(m => m.CarreraId == asignatura.CarreraId)
            ?? db.Mallas.FirstOrDefault(m => m.CarreraId == asignatura.CarreraId);
        if (malla is null)
        {
            malla = new Malla(asignatura.CarreraId, "Inicial de prueba", true);
            db.Mallas.Add(malla);
        }
        asignatura.AsignarMalla(malla);
        return asignatura;
    }
}
