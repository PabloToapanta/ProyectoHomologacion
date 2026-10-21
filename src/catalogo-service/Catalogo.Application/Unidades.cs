using Catalogo.Domain;

namespace Catalogo.Application;

public record UnidadInput(int Numero, string Nombre, int Horas, string ResultadoAprendizaje, IReadOnlyList<string> Contenidos);
public record ContenidoDto(Guid Id, int Orden, string Tema);
public record UnidadDto(Guid Id, Guid AsignaturaId, int Numero, string Nombre, int Horas, string ResultadoAprendizaje, IReadOnlyList<ContenidoDto> Contenidos)
{
    public static UnidadDto From(UnidadCurricular u) => new(u.Id, u.AsignaturaId, u.Numero, u.Nombre, u.Horas, u.ResultadoAprendizaje,
        u.Contenidos.OrderBy(c => c.Orden).Select(c => new ContenidoDto(c.Id, c.Orden, c.Tema)).ToList());
}
public interface IUnidadRepository
{
    Task<IReadOnlyList<UnidadCurricular>> ListarAsync(Guid asignaturaId, CancellationToken ct);
    Task CrearAsync(UnidadCurricular unidad, CancellationToken ct);
    Task<UnidadCurricular?> BuscarAsync(Guid asignaturaId, Guid unidadId, CancellationToken ct);
    Task GuardarAsync(CancellationToken ct);
}
public sealed class UnidadService(IUnidadRepository unidades, IAsignaturaRepository asignaturas)
{
    public async Task<UnidadDto> ActualizarAsync(Guid asignaturaId, Guid unidadId, UnidadInput input, CancellationToken ct)
    {
        var unidad = await unidades.BuscarAsync(asignaturaId, unidadId, ct)
            ?? throw new CatalogoException("no_encontrada", "No se encontró la unidad en esta asignatura.");
        if ((await unidades.ListarAsync(asignaturaId, ct)).Any(u => u.Id != unidadId && u.Numero == input.Numero))
            throw new CatalogoException("duplicado", "Ya existe ese número de unidad en la asignatura.");
        unidad.Actualizar(input.Numero, input.Nombre, input.Horas, input.ResultadoAprendizaje, input.Contenidos);
        await unidades.GuardarAsync(ct);
        return UnidadDto.From(unidad);
    }
    public async Task<UnidadDto> CrearAsync(Guid asignaturaId, UnidadInput input, CancellationToken ct)
    {
        var unidad = new UnidadCurricular(asignaturaId, input.Numero, input.Nombre, input.Horas, input.ResultadoAprendizaje, input.Contenidos);
        if (await asignaturas.BuscarAsync(asignaturaId, ct) is null)
            throw new CatalogoException("no_encontrada", "No se encontró la asignatura.");
        if ((await unidades.ListarAsync(asignaturaId, ct)).Any(u => u.Numero == input.Numero))
            throw new CatalogoException("duplicado", "Ya existe ese número de unidad en la asignatura.");
        await unidades.CrearAsync(unidad, ct);
        return UnidadDto.From(unidad);
    }
}
