using Catalogo.Domain;

namespace Catalogo.Application;

public record AsignaturaInput(Guid CarreraId, string Codigo, string Nombre, int Nivel, decimal Creditos, int Horas);
public record AsignaturaDto(Guid Id, Guid CarreraId, string Codigo, string Nombre, int Nivel, decimal Creditos, int Horas)
{
    public static AsignaturaDto From(Asignatura a) => new(a.Id, a.CarreraId, a.Codigo, a.Nombre, a.Nivel, a.Creditos, a.Horas);
}

public interface IAsignaturaRepository
{
    Task<Asignatura?> BuscarAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Asignatura>> ListarPorCarreraAsync(Guid carreraId, CancellationToken ct);
    Task<bool> ExisteCodigoAsync(Guid carreraId, string codigoNormalizado, CancellationToken ct);
    Task CrearAsync(Asignatura asignatura, CancellationToken ct);
}

public sealed class AsignaturaService(IAsignaturaRepository asignaturas, ICarreraRepository carreras)
{
    public async Task<AsignaturaDto> CrearAsync(AsignaturaInput input, CancellationToken ct = default)
    {
        var asignatura = new Asignatura(input.CarreraId, input.Codigo, input.Nombre, input.Nivel, input.Creditos, input.Horas);
        await VerificarCarrera(input.CarreraId, ct);
        if (await asignaturas.ExisteCodigoAsync(input.CarreraId, asignatura.CodigoNormalizado, ct))
            throw new CatalogoException("duplicado", "Ya existe una asignatura con ese código en la carrera.");
        await asignaturas.CrearAsync(asignatura, ct);
        return AsignaturaDto.From(asignatura);
    }

    public async Task<AsignaturaDto> ObtenerAsync(Guid id, CancellationToken ct = default) =>
        AsignaturaDto.From(await asignaturas.BuscarAsync(id, ct) ?? throw new CatalogoException("no_encontrada", "No se encontró la asignatura."));

    public async Task<IReadOnlyList<AsignaturaDto>> ListarAsync(Guid carreraId, CancellationToken ct = default)
    {
        await VerificarCarrera(carreraId, ct);
        return (await asignaturas.ListarPorCarreraAsync(carreraId, ct)).Select(AsignaturaDto.From).ToList();
    }

    private async Task VerificarCarrera(Guid id, CancellationToken ct)
    {
        if (await carreras.BuscarAsync(id, ct) is null) throw new CatalogoException("no_encontrada", "No se encontró la carrera.");
    }
}
