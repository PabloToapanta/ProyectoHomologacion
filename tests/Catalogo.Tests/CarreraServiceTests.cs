using Catalogo.Application;
using Catalogo.Domain;
using Xunit;

namespace Catalogo.Tests;

public class CarreraServiceTests
{
    private readonly MemoryRepository repository = new();
    private CarreraService Service => new(repository);
    private static CarreraInput Input(string nombre = "Software sintético", string facultad = "FISEI", bool vigencia = true) => new(nombre, facultad, vigencia);

    [Fact]
    public async Task CRUD_completo()
    {
        var creada = await Service.CrearAsync(Input());
        Assert.Equal(creada, Assert.Single(await Service.ListarAsync()));
        Assert.Equal(creada, await Service.ObtenerAsync(creada.Id));
        var editada = await Service.ActualizarAsync(creada.Id, Input("Editada", vigencia: false));
        Assert.Equal("Editada", editada.Nombre);
        Assert.False(editada.Vigencia);
        await Service.EliminarAsync(creada.Id);
        Assert.Empty(await Service.ListarAsync());
    }

    [Fact]
    public async Task Rechaza_duplicados_normalizados_en_misma_facultad()
    {
        await Service.CrearAsync(Input());
        var error = await Assert.ThrowsAsync<CatalogoException>(() => Service.CrearAsync(Input(" software   sintético ", " fisei ")));
        Assert.Equal("duplicado", error.Codigo);
        Assert.Single(await Service.ListarAsync());
    }

    [Fact]
    public async Task Permite_mismo_nombre_en_otra_facultad()
    {
        await Service.CrearAsync(Input());
        await Service.CrearAsync(Input(facultad: "Otra facultad"));
        Assert.Equal(2, (await Service.ListarAsync()).Count);
    }

    [Fact]
    public async Task Editar_excluye_propio_id_y_rechaza_duplicado_sin_mutar()
    {
        var a = await Service.CrearAsync(Input());
        var b = await Service.CrearAsync(Input("Otra"));
        await Service.ActualizarAsync(a.Id, Input());
        var error = await Assert.ThrowsAsync<CatalogoException>(() => Service.ActualizarAsync(b.Id, Input()));
        Assert.Equal("duplicado", error.Codigo);
        Assert.Equal("Otra", (await Service.ObtenerAsync(b.Id)).Nombre);
    }

    [Fact]
    public async Task Impide_eliminar_con_asignaturas_incluso_no_vigente()
    {
        var carrera = await Service.CrearAsync(Input(vigencia: false));
        repository.Relacionadas.Add(carrera.Id);
        var error = await Assert.ThrowsAsync<CatalogoException>(() => Service.EliminarAsync(carrera.Id));
        Assert.Equal("asignaturas_relacionadas", error.Codigo);
        Assert.Single(await Service.ListarAsync());
    }

    [Fact]
    public async Task Validacion_no_persiste()
    {
        var error = await Assert.ThrowsAsync<CatalogoException>(() => Service.CrearAsync(Input(" ")));
        Assert.Equal("validacion", error.Codigo);
        Assert.Empty(await Service.ListarAsync());
    }

    [Fact]
    public async Task Id_inexistente_devuelve_no_encontrada()
    {
        var id = Guid.NewGuid();
        Assert.Equal("no_encontrada", (await Assert.ThrowsAsync<CatalogoException>(() => Service.ObtenerAsync(id))).Codigo);
        Assert.Equal("no_encontrada", (await Assert.ThrowsAsync<CatalogoException>(() => Service.ActualizarAsync(id, Input()))).Codigo);
        Assert.Equal("no_encontrada", (await Assert.ThrowsAsync<CatalogoException>(() => Service.EliminarAsync(id))).Codigo);
    }

    private sealed class MemoryRepository : ICarreraRepository
    {
        private readonly List<Carrera> items = [];
        public HashSet<Guid> Relacionadas { get; } = [];
        public Task<IReadOnlyList<Carrera>> ListarAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<Carrera>>(items.ToList());
        public Task<Carrera?> BuscarAsync(Guid id, CancellationToken ct) => Task.FromResult(items.SingleOrDefault(c => c.Id == id));
        public Task<bool> ExisteNombreAsync(string nombre, string facultad, Guid? excluirId, CancellationToken ct) =>
            Task.FromResult(items.Any(c => c.NombreNormalizado == nombre && c.FacultadNormalizada == facultad && c.Id != excluirId));
        public Task<bool> TieneAsignaturasAsync(Guid id, CancellationToken ct) => Task.FromResult(Relacionadas.Contains(id));
        public Task GuardarAsync(Carrera carrera, bool nueva, CancellationToken ct) { if (nueva) items.Add(carrera); return Task.CompletedTask; }
        public Task EliminarAsync(Carrera carrera, CancellationToken ct) { items.Remove(carrera); return Task.CompletedTask; }
    }
}
