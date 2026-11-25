using Catalogo.Domain;

namespace Catalogo.Application;

public sealed record ReferenciaNormativaDto(Guid Id, string Codigo);

public sealed record NormativaDto(Guid Id, string Codigo, string Titulo, string Emisor, DateOnly FechaEmision,
    DateOnly VigenteDesde, string Estado, ReferenciaNormativaDto? ReformaA, ReferenciaNormativaDto? ReformadaPor,
    string? MotivoDerogacion, DateTimeOffset? FechaDerogacion, string NombreArchivo, int Tamano, string Sha256,
    DateTimeOffset FechaRegistro, string RegistradaPor);

public sealed record EventoNormativaDto(Guid Id, string Tipo, DateTimeOffset Fecha, string Usuario, string? Detalle);

public sealed record DocumentoNormativa(string NombreArchivo, string Sha256, byte[] Contenido);

public sealed record RegistroNormativa(string? Codigo, string? Titulo, string? Emisor, DateOnly FechaEmision,
    DateOnly VigenteDesde, Guid? ReformaAId, string? NombreArchivo, byte[] Contenido);

public interface INormativaRepository
{
    Task<IReadOnlyList<Normativa>> ListarAsync(EstadoNormativa? estado, CancellationToken ct);
    Task<Normativa?> BuscarAsync(Guid id, CancellationToken ct);
    Task<DocumentoNormativa?> DocumentoAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<EventoNormativa>> HistorialAsync(Guid id, CancellationToken ct);
    /// <summary>
    /// Guarda la normativa y, si reforma otra, la marca como Reformada en la misma transacción.
    /// La norma reformada se vuelve a leer dentro de la transacción para no reformar dos veces la misma.
    /// </summary>
    Task RegistrarAsync(Normativa normativa, string usuario, DateTimeOffset fecha, CancellationToken ct);
    Task<Normativa> DerogarAsync(Guid id, string? motivo, string usuario, DateTimeOffset fecha, CancellationToken ct);
    /// <summary>Elimina un registro equivocado, restituye la norma que reformaba y deja el evento en el historial.</summary>
    Task EliminarAsync(Guid id, string motivo, string usuario, DateTimeOffset fecha, CancellationToken ct);
}

public sealed class NormativaService(INormativaRepository normativas, TimeProvider reloj)
{
    public async Task<IReadOnlyList<NormativaDto>> ListarAsync(string? estado, CancellationToken ct = default)
    {
        EstadoNormativa? filtro = null;
        if (!string.IsNullOrWhiteSpace(estado))
        {
            if (!Enum.TryParse<EstadoNormativa>(estado, true, out var valor) || !Enum.IsDefined(valor))
                throw new CatalogoException("validacion", "El estado debe ser Vigente, Reformada o Derogada.");
            filtro = valor;
        }
        var lista = await normativas.ListarAsync(filtro, ct);
        var todas = filtro is null ? lista : await normativas.ListarAsync(null, ct);
        var codigos = todas.ToDictionary(n => n.Id, n => n.Codigo);
        return lista.Select(n => Dto(n, codigos)).ToArray();
    }

    public async Task<NormativaDto> ObtenerAsync(Guid id, CancellationToken ct = default)
    {
        var normativa = await Buscar(id, ct);
        return await DtoAsync(normativa, ct);
    }

    public async Task<DocumentoNormativa> DocumentoAsync(Guid id, CancellationToken ct = default) =>
        await normativas.DocumentoAsync(id, ct) ?? throw new CatalogoException("no_encontrada", "No se encontró la normativa.");

    public async Task<IReadOnlyList<EventoNormativaDto>> HistorialAsync(Guid id, CancellationToken ct = default)
    {
        var eventos = await normativas.HistorialAsync(id, ct);
        if (eventos.Count == 0 && await normativas.BuscarAsync(id, ct) is null)
            throw new CatalogoException("no_encontrada", "No se encontró la normativa.");
        return eventos.OrderBy(e => e.Fecha).ThenBy(e => e.Tipo)
            .Select(e => new EventoNormativaDto(e.Id, e.Tipo.ToString(), e.Fecha, e.Usuario, e.Detalle)).ToArray();
    }

    public async Task<NormativaDto> RegistrarAsync(RegistroNormativa registro, string? usuario, CancellationToken ct = default)
    {
        var quien = Usuario(usuario);
        var fecha = reloj.GetUtcNow();
        var normativa = new Normativa(registro.Codigo, registro.Titulo, registro.Emisor, registro.FechaEmision,
            registro.VigenteDesde, registro.ReformaAId, registro.NombreArchivo, registro.Contenido, fecha, quien);
        if (registro.ReformaAId is { } reformada)
        {
            var anterior = await Buscar(reformada, ct);
            if (normativa.VigenteDesde < anterior.VigenteDesde)
                throw new CatalogoException("validacion", "La reforma no puede regir antes que la normativa que modifica.");
        }
        await normativas.RegistrarAsync(normativa, quien, fecha, ct);
        return await DtoAsync(normativa, ct);
    }

    public async Task<NormativaDto> DerogarAsync(Guid id, string? motivo, string? usuario, CancellationToken ct = default)
    {
        var quien = Usuario(usuario);
        Normativa.Texto(motivo, 500, "El motivo de la derogación");
        return await DtoAsync(await normativas.DerogarAsync(id, motivo, quien, reloj.GetUtcNow(), ct), ct);
    }

    public async Task EliminarAsync(Guid id, string? motivo, string? usuario, CancellationToken ct = default)
    {
        var quien = Usuario(usuario);
        var texto = Normativa.Texto(motivo, 500, "El motivo de la eliminación");
        await normativas.EliminarAsync(id, texto, quien, reloj.GetUtcNow(), ct);
    }

    private async Task<Normativa> Buscar(Guid id, CancellationToken ct) =>
        await normativas.BuscarAsync(id, ct) ?? throw new CatalogoException("no_encontrada", "No se encontró la normativa.");

    private async Task<NormativaDto> DtoAsync(Normativa n, CancellationToken ct)
    {
        var codigos = new Dictionary<Guid, string>();
        foreach (var id in new[] { n.ReformaAId, n.ReformadaPorId }.OfType<Guid>())
            if (await normativas.BuscarAsync(id, ct) is { } relacionada) codigos[id] = relacionada.Codigo;
        return Dto(n, codigos);
    }

    private static NormativaDto Dto(Normativa n, IReadOnlyDictionary<Guid, string> codigos)
    {
        ReferenciaNormativaDto? Referencia(Guid? id) =>
            id is { } valor && codigos.TryGetValue(valor, out var codigo) ? new(valor, codigo) : null;
        return new(n.Id, n.Codigo, n.Titulo, n.Emisor, n.FechaEmision, n.VigenteDesde, n.Estado.ToString(),
            Referencia(n.ReformaAId), Referencia(n.ReformadaPorId), n.MotivoDerogacion, n.FechaDerogacion,
            n.NombreArchivo, n.Tamano, n.Sha256, n.FechaRegistro, n.RegistradaPor);
    }

    private static string Usuario(string? usuario) =>
        string.IsNullOrWhiteSpace(usuario) ? throw new CatalogoException("validacion", "No se pudo identificar al usuario.") : usuario.Trim();
}
