using Catalogo.Domain;

namespace Catalogo.Application;

public record CarreraInput(string Nombre, string Facultad, bool Vigencia, string? Modalidad = null);
public record CarreraDto(Guid Id, string Nombre, string Facultad, bool Vigencia, string? Modalidad)
{
    public static CarreraDto From(Carrera c) => new(c.Id, c.Nombre, c.Facultad, c.Vigencia, c.Modalidad);
}

public interface ICarreraRepository
{
    Task<IReadOnlyList<Carrera>> ListarAsync(CancellationToken ct);
    Task<Carrera?> BuscarAsync(Guid id, CancellationToken ct);
    Task<bool> ExisteNombreAsync(string nombre, string facultad, Guid? excluirId, CancellationToken ct);
    Task<bool> TieneAsignaturasAsync(Guid id, CancellationToken ct);
    Task GuardarAsync(Carrera carrera, bool nueva, CancellationToken ct);
    Task EliminarAsync(Carrera carrera, CancellationToken ct);
}

public sealed class CarreraService(ICarreraRepository repository)
{
    public async Task<IReadOnlyList<CarreraDto>> ListarAsync(CancellationToken ct = default) =>
        (await repository.ListarAsync(ct)).Select(CarreraDto.From).ToList();

    public async Task<CarreraDto> ObtenerAsync(Guid id, CancellationToken ct = default) => CarreraDto.From(await Buscar(id, ct));

    public async Task<CarreraDto> CrearAsync(CarreraInput input, CancellationToken ct = default)
    {
        var carrera = new Carrera(input.Nombre, input.Facultad, input.Vigencia, input.Modalidad);
        await ValidarDuplicado(carrera, null, ct);
        await repository.GuardarAsync(carrera, true, ct);
        return CarreraDto.From(carrera);
    }

    public async Task<CarreraDto> ActualizarAsync(Guid id, CarreraInput input, CancellationToken ct = default)
    {
        var carrera = await Buscar(id, ct);
        var propuesta = new Carrera(input.Nombre, input.Facultad, input.Vigencia, input.Modalidad);
        await ValidarDuplicado(propuesta, id, ct);
        carrera.Actualizar(propuesta.Nombre, propuesta.Facultad, propuesta.Vigencia, propuesta.Modalidad);
        await repository.GuardarAsync(carrera, false, ct);
        return CarreraDto.From(carrera);
    }

    public async Task EliminarAsync(Guid id, CancellationToken ct = default)
    {
        var carrera = await Buscar(id, ct);
        if (await repository.TieneAsignaturasAsync(id, ct))
            throw new CatalogoException("asignaturas_relacionadas", "La carrera tiene asignaturas relacionadas y no puede eliminarse.");
        await repository.EliminarAsync(carrera, ct);
    }

    private async Task<Carrera> Buscar(Guid id, CancellationToken ct) =>
        await repository.BuscarAsync(id, ct) ?? throw new CatalogoException("no_encontrada", "No se encontró la carrera.");

    private async Task ValidarDuplicado(Carrera carrera, Guid? excluir, CancellationToken ct)
    {
        if (await repository.ExisteNombreAsync(carrera.NombreNormalizado, carrera.FacultadNormalizada, excluir, ct))
            throw new CatalogoException("duplicado", "Ya existe una carrera con ese nombre en la facultad.");
    }
}
