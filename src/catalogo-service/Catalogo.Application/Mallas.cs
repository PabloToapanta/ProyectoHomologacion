using Catalogo.Domain;

namespace Catalogo.Application;

public sealed record MallaDto(Guid Id, Guid CarreraId, string Nombre, bool Vigente)
{
    public static MallaDto From(Malla malla) => new(malla.Id, malla.CarreraId, malla.Nombre, malla.Vigente);
}

public interface IMallaRepository
{
    Task<IReadOnlyList<Malla>> ListarAsync(Guid carreraId, CancellationToken ct);
    Task<Malla?> BuscarAsync(Guid id, CancellationToken ct);
    /// <summary>Si es vigente, desactiva la anterior y crea la nueva en la misma transacción.</summary>
    Task CrearAsync(Malla malla, CancellationToken ct);
    /// <summary>Activación exclusiva y atómica por carrera; conserva las asignaturas y las otras versiones.</summary>
    Task<Malla> ActivarAsync(Guid id, CancellationToken ct);
}

public sealed class MallaService(IMallaRepository mallas, ICarreraRepository carreras)
{
    public async Task<IReadOnlyList<MallaDto>> ListarAsync(Guid carreraId, CancellationToken ct = default)
    {
        await Carrera(carreraId, ct);
        return (await mallas.ListarAsync(carreraId, ct)).Select(MallaDto.From).ToArray();
    }

    public async Task<MallaDto> ObtenerAsync(Guid id, CancellationToken ct = default) =>
        MallaDto.From(await mallas.BuscarAsync(id, ct) ?? throw new CatalogoException("no_encontrada", "No se encontró la malla."));

    public async Task<MallaDto> CrearAsync(Guid carreraId, string nombre, bool vigente, CancellationToken ct = default)
    {
        var malla = new Malla(carreraId, nombre, vigente);
        await VerificarEscritura(carreraId, ct);
        await mallas.CrearAsync(malla, ct);
        return MallaDto.From(malla);
    }

    public async Task<MallaDto> ActivarAsync(Guid id, CancellationToken ct = default)
    {
        var malla = await mallas.BuscarAsync(id, ct) ?? throw new CatalogoException("no_encontrada", "No se encontró la malla.");
        await VerificarEscritura(malla.CarreraId, ct);
        return MallaDto.From(await mallas.ActivarAsync(id, ct));
    }

    private async Task<Carrera> Carrera(Guid id, CancellationToken ct) =>
        await carreras.BuscarAsync(id, ct) ?? throw new CatalogoException("no_encontrada", "No se encontró la carrera.");

    private async Task VerificarEscritura(Guid id, CancellationToken ct)
    {
        var carrera = await Carrera(id, ct);
        if (!string.Equals(carrera.Facultad.Trim(), Catalogo.Domain.Carrera.FacultadReceptora, StringComparison.OrdinalIgnoreCase))
            throw new CatalogoException("validacion", "La carrera histórica requiere revisión: solo se gestionan nuevas mallas de FISEI.");
    }

    /// <summary>No crea versiones ni infiere planes cuando falta una vigente.</summary>
    public static async Task<Malla> ResolverAsync(IMallaRepository mallas, Guid carreraId, Guid? mallaId, CancellationToken ct)
    {
        var malla = mallaId.HasValue
            ? await mallas.BuscarAsync(mallaId.Value, ct)
            : (await mallas.ListarAsync(carreraId, ct)).SingleOrDefault(m => m.Vigente);
        if (malla is null)
            throw new CatalogoException(mallaId.HasValue ? "no_encontrada" : "conflicto", mallaId.HasValue
                ? "No se encontró la malla." : "La carrera no tiene una malla vigente. Selecciona o activa una versión.");
        if (malla.CarreraId != carreraId)
            throw new CatalogoException("validacion", "La malla no pertenece a la carrera seleccionada.");
        return malla;
    }
}
