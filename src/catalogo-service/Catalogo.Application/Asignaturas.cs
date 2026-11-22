using Catalogo.Domain;

namespace Catalogo.Application;

public record AsignaturaInput(Guid CarreraId, string Codigo, string Nombre, int Nivel, decimal Creditos, int Horas)
{
    public Guid? MallaId { get; init; }
}
public record AsignaturaDto(Guid Id, Guid CarreraId, string Codigo, string Nombre, int Nivel, decimal Creditos, int Horas)
{
    public Guid MallaId { get; init; }
    public MallaDto? Malla { get; init; }
    public static AsignaturaDto From(Asignatura a, Malla? malla = null) => new(a.Id, a.CarreraId, a.Codigo, a.Nombre, a.Nivel, a.Creditos, a.Horas)
    { MallaId = a.MallaId, Malla = malla is null ? null : MallaDto.From(malla) };
}

public interface IAsignaturaRepository
{
    Task<Asignatura?> BuscarAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Asignatura>> ListarPorCarreraAsync(Guid carreraId, CancellationToken ct);
    Task<bool> ExisteCodigoAsync(Guid carreraId, string codigoNormalizado, CancellationToken ct, Guid? mallaId = null);
    Task CrearAsync(Asignatura asignatura, CancellationToken ct);
}

public sealed class AsignaturaService(IAsignaturaRepository asignaturas, ICarreraRepository carreras, IMallaRepository mallas)
{
    public async Task<AsignaturaDto> CrearAsync(AsignaturaInput input, CancellationToken ct = default)
    {
        var asignatura = new Asignatura(input.CarreraId, input.Codigo, input.Nombre, input.Nivel, input.Creditos, input.Horas);
        await VerificarCarrera(input.CarreraId, ct);
        var malla = await MallaService.ResolverAsync(mallas, input.CarreraId, input.MallaId, ct);
        asignatura.AsignarMalla(malla);
        if (await asignaturas.ExisteCodigoAsync(input.CarreraId, asignatura.CodigoNormalizado, ct, malla.Id))
            throw new CatalogoException("duplicado", "Ya existe una asignatura con ese código en la malla.");
        await asignaturas.CrearAsync(asignatura, ct);
        return AsignaturaDto.From(asignatura, malla);
    }

    public async Task<AsignaturaDto> ObtenerAsync(Guid id, CancellationToken ct = default)
    {
        var asignatura = await asignaturas.BuscarAsync(id, ct) ?? throw new CatalogoException("no_encontrada", "No se encontró la asignatura.");
        return AsignaturaDto.From(asignatura, await mallas.BuscarAsync(asignatura.MallaId, ct));
    }

    public async Task<IReadOnlyList<AsignaturaDto>> ListarAsync(Guid carreraId, CancellationToken ct = default, Guid? mallaId = null)
    {
        await VerificarCarrera(carreraId, ct);
        if (mallaId.HasValue) await MallaService.ResolverAsync(mallas, carreraId, mallaId, ct);
        var versiones = (await mallas.ListarAsync(carreraId, ct)).ToDictionary(m => m.Id);
        return (await asignaturas.ListarPorCarreraAsync(carreraId, ct))
            .Where(a => !mallaId.HasValue || a.MallaId == mallaId)
            .Select(a => AsignaturaDto.From(a, versiones.GetValueOrDefault(a.MallaId))).ToList();
    }

    private async Task VerificarCarrera(Guid id, CancellationToken ct)
    {
        if (await carreras.BuscarAsync(id, ct) is null) throw new CatalogoException("no_encontrada", "No se encontró la carrera.");
    }
}
