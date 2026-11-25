namespace Catalogo.Application;

/// <summary>HU-09: asignatura del par con su contexto académico, sin datos de estudiantes.</summary>
public sealed record AsignaturaConsultaHomologables(AsignaturaDto Asignatura, AsignaturaContextoInterno Contexto);
public sealed record ParConsultaHomologables(Guid OrigenId, Guid ReceptoraId, RevisionComparacionDto Revision);
public sealed record ContextoConsultaHomologables(CarreraDto Origen, CarreraDto Receptora,
    IReadOnlyList<AsignaturaConsultaHomologables> Asignaturas, IReadOnlyList<ParConsultaHomologables> Pares);

/// <summary>HU-09: solo lectura. No crea ni modifica correspondencias; solo devuelve los pares con revisión registrada.</summary>
public sealed class ConsultaHomologablesService(CarreraService carreras, AsignaturaService asignaturas, ComparacionCatalogoService comparaciones,
    IComparacionCatalogoRepository revisiones, IUnidadRepository unidades)
{
    public async Task<IReadOnlyList<CarreraDto>> CarrerasAsync(CancellationToken ct) =>
        (await carreras.ListarAsync(ct)).Where(EsFisei).OrderBy(c => c.Nombre, StringComparer.CurrentCultureIgnoreCase).ToArray();

    public async Task<ContextoConsultaHomologables> ContextoAsync(Guid origenId, Guid receptoraId, CancellationToken ct)
    {
        var origen = await carreras.ObtenerAsync(origenId, ct);
        var receptora = await carreras.ObtenerAsync(receptoraId, ct);
        // Origen: todas las versiones de malla (el estudiante pudo cursar una anterior). Receptora: solo la vigente.
        var deOrigen = await asignaturas.ListarAsync(origenId, ct);
        var deReceptora = (await asignaturas.ListarAsync(receptoraId, ct)).Where(a => a.Malla?.Vigente == true).ToArray();
        var pares = await revisiones.RevisionesEntreAsync(deOrigen.Select(a => a.Id).ToArray(), deReceptora.Select(a => a.Id).ToArray(), ct);
        var resultado = new List<ParConsultaHomologables>();
        foreach (var (o, r) in pares) resultado.Add(new(o, r, await comparaciones.RevisionAsync(o, r, ct)));
        var usadas = pares.SelectMany(p => new[] { p.Origen, p.Receptora }).ToHashSet();
        var contextos = new List<AsignaturaConsultaHomologables>();
        foreach (var a in deOrigen.Concat(deReceptora).Where(a => usadas.Contains(a.Id)))
        {
            var carrera = a.CarreraId == origenId ? origen : receptora;
            var perfil = await comparaciones.PerfilAsync(a.Id, ct);
            var programa = (await unidades.ListarAsync(a.Id, ct)).Select(UnidadDto.From).ToArray();
            contextos.Add(new(a, AsignaturaContextoInterno.Desde(a, carrera, perfil, programa)));
        }
        return new(origen, receptora, contextos, resultado);
    }

    private static bool EsFisei(CarreraDto c) => string.Equals(c.Facultad.Trim(), Catalogo.Domain.Carrera.FacultadReceptora, StringComparison.OrdinalIgnoreCase);
}
