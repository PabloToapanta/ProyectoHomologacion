using Catalogo.Application;
using Catalogo.Domain;
using Xunit;

namespace Catalogo.Tests;

public class AsignaturaServiceTests
{
    [Fact]
    public async Task Crea_consulta_y_ordena_por_nivel_y_codigo()
    {
        var repo = new Repository();
        var service = new AsignaturaService(repo, repo);
        var a = await service.CrearAsync(new(repo.Carrera.Id, "Z", "Segundo nivel", 2, 3, 144));
        await service.CrearAsync(new(repo.Carrera.Id, "B", "Primero B", 1, 3, 144));
        await service.CrearAsync(new(repo.Carrera.Id, "A", "Primero A", 1, 3, 144));
        Assert.Equal(a, await service.ObtenerAsync(a.Id));
        Assert.Equal(new[] { "A", "B", "Z" }, (await service.ListarAsync(repo.Carrera.Id)).Select(x => x.Codigo));
    }

    [Fact]
    public async Task Carrera_inexistente_no_registra_y_listado_devuelve_404()
    {
        var repo = new Repository();
        var service = new AsignaturaService(repo, repo);
        var id = Guid.NewGuid();
        Assert.Equal("no_encontrada", (await Assert.ThrowsAsync<CatalogoException>(() => service.CrearAsync(new(id, "C", "N", 1, 3, 144)))).Codigo);
        Assert.Equal("no_encontrada", (await Assert.ThrowsAsync<CatalogoException>(() => service.ListarAsync(id))).Codigo);
        Assert.Empty(repo.Items);
    }

    [Fact]
    public async Task Rechaza_codigo_repetido_en_carrera()
    {
        var repo = new Repository();
        var service = new AsignaturaService(repo, repo);
        await service.CrearAsync(new(repo.Carrera.Id, "ALG", "N", 1, 3, 144));
        var error = await Assert.ThrowsAsync<CatalogoException>(() => service.CrearAsync(new(repo.Carrera.Id, " alg ", "Otra", 2, 4, 100)));
        Assert.Equal("duplicado", error.Codigo);
        Assert.Single(repo.Items);
    }

    [Fact]
    public async Task Carrera_sin_asignaturas_y_asignatura_inexistente()
    {
        var repo = new Repository();
        var service = new AsignaturaService(repo, repo);
        Assert.Empty(await service.ListarAsync(repo.Carrera.Id));
        Assert.Equal("no_encontrada", (await Assert.ThrowsAsync<CatalogoException>(() => service.ObtenerAsync(Guid.NewGuid()))).Codigo);
    }

    private sealed class Repository : IAsignaturaRepository, ICarreraRepository
    {
        public Carrera Carrera { get; } = new("Carrera de prueba", "FISEI", true);
        public List<Asignatura> Items { get; } = [];
        public Task<Asignatura?> BuscarAsync(Guid id, CancellationToken ct) => Task.FromResult(Items.SingleOrDefault(a => a.Id == id));
        public Task<IReadOnlyList<Asignatura>> ListarPorCarreraAsync(Guid carreraId, CancellationToken ct) => Task.FromResult<IReadOnlyList<Asignatura>>(Items.Where(a => a.CarreraId == carreraId).OrderBy(a => a.Nivel).ThenBy(a => a.CodigoNormalizado).ToList());
        public Task<bool> ExisteCodigoAsync(Guid carreraId, string codigoNormalizado, CancellationToken ct) => Task.FromResult(Items.Any(a => a.CarreraId == carreraId && a.CodigoNormalizado == codigoNormalizado));
        public Task CrearAsync(Asignatura asignatura, CancellationToken ct) { Items.Add(asignatura); return Task.CompletedTask; }
        Task<Carrera?> ICarreraRepository.BuscarAsync(Guid id, CancellationToken ct) => Task.FromResult(id == Carrera.Id ? Carrera : null);
        public Task<IReadOnlyList<Carrera>> ListarAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<Carrera>>([Carrera]);
        public Task<bool> ExisteNombreAsync(string nombre, string facultad, Guid? excluirId, CancellationToken ct) => Task.FromResult(false);
        public Task<bool> TieneAsignaturasAsync(Guid id, CancellationToken ct) => Task.FromResult(Items.Any(a => a.CarreraId == id));
        public Task GuardarAsync(Carrera carrera, bool nueva, CancellationToken ct) => Task.CompletedTask;
        public Task EliminarAsync(Carrera carrera, CancellationToken ct) => Task.CompletedTask;
    }
}
