using Catalogo.Domain;
namespace Catalogo.Application;
public record UnidadInput(int Numero, string Nombre, int Horas, string ResultadoAprendizaje, IReadOnlyList<string>? Contenidos, IReadOnlyList<TematicaDatos>? Tematicas = null);
public record ContenidoDto(Guid Id, int Orden, string Tema);
public record TematicaDto(Guid Id, int Orden, string Nombre, IReadOnlyList<ContenidoDto> Contenidos);
public record UnidadDto(Guid Id, Guid AsignaturaId, int Numero, string Nombre, int Horas, string ResultadoAprendizaje, IReadOnlyList<ContenidoDto> Contenidos, IReadOnlyList<TematicaDto> Tematicas, IReadOnlyList<ContenidoDto> ContenidosSinClasificar)
{
    public static UnidadDto From(UnidadCurricular u) => new(u.Id, u.AsignaturaId, u.Numero, u.Nombre, u.Horas, u.ResultadoAprendizaje,
        u.Contenidos.OrderBy(c => c.Orden).Select(Mapear).ToList(),
        u.Tematicas.OrderBy(t => t.Orden).Select(t => new TematicaDto(t.Id, t.Orden, t.Nombre, u.Contenidos.Where(c => c.UnidadTematicaId == t.Id).OrderBy(c => c.Orden).Select(Mapear).ToList())).ToList(),
        u.Contenidos.Where(c => c.UnidadTematicaId == null).OrderBy(c => c.Orden).Select(Mapear).ToList());
    private static ContenidoDto Mapear(Contenido c) => new(c.Id, c.Orden, c.Tema);
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
    private static IReadOnlyList<string> Textos(UnidadInput input)
    {
        if (input.Tematicas is not null && input.Contenidos is not null)
            throw new CatalogoException("validacion", "Envía temáticas o contenidos anteriores, no ambos.");
        if (input.Tematicas is not null)
        {
            if (input.Tematicas.Any(t => t is null || t.Contenidos is null)) throw new CatalogoException("validacion", "Temáticas incompletas.");
            return input.Tematicas.SelectMany(t => t.Contenidos).ToList();
        }
        return input.Contenidos ?? throw new CatalogoException("validacion", "Faltan unidades temáticas y contenidos.");
    }
    public async Task<UnidadDto> ActualizarAsync(Guid asignaturaId, Guid unidadId, UnidadInput input, CancellationToken ct)
    {
        var unidad = await unidades.BuscarAsync(asignaturaId, unidadId, ct)
            ?? throw new CatalogoException("no_encontrada", "No se encontró la unidad en esta asignatura.");
        if ((await unidades.ListarAsync(asignaturaId, ct)).Any(u => u.Id != unidadId && u.Numero == input.Numero))
            throw new CatalogoException("duplicado", "Ya existe ese número de unidad en la asignatura.");
        if (unidad.Tematicas.Count > 0 && input.Tematicas is null) throw new CatalogoException("validacion", "Esta unidad requiere sus unidades temáticas al editar.");
        unidad.Actualizar(input.Numero, input.Nombre, input.Horas, input.ResultadoAprendizaje, Textos(input));
        if (input.Tematicas is not null) unidad.Clasificar(input.Tematicas);
        await unidades.GuardarAsync(ct);
        return UnidadDto.From(unidad);
    }
    public async Task<UnidadDto> CrearAsync(Guid asignaturaId, UnidadInput input, CancellationToken ct)
    {
        var unidad = new UnidadCurricular(asignaturaId, input.Numero, input.Nombre, input.Horas, input.ResultadoAprendizaje, Textos(input));
        if (await asignaturas.BuscarAsync(asignaturaId, ct) is null)
            throw new CatalogoException("no_encontrada", "No se encontró la asignatura.");
        if ((await unidades.ListarAsync(asignaturaId, ct)).Any(u => u.Numero == input.Numero))
            throw new CatalogoException("duplicado", "Ya existe ese número de unidad en la asignatura.");
        if (input.Tematicas is not null) unidad.Clasificar(input.Tematicas);
        await unidades.CrearAsync(unidad, ct);
        return UnidadDto.From(unidad);
    }
}
