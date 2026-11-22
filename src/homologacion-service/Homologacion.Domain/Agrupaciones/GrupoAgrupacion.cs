namespace Homologacion.Domain.Agrupaciones;

public sealed record AporteGrupo(Guid AsignaturaAprobadaId, Guid AsignaturaReceptoraId, decimal? HorasAportadas, decimal? CreditosAportados);
public sealed class GrupoAgrupacion
{
    public Guid GrupoId { get; private set; }
    public Guid SolicitudId { get; private set; }
    public Guid Version { get; private set; }
    public bool Congelado { get; private set; }
    public IReadOnlyList<Guid> Origenes { get; private set; } = [];
    public IReadOnlyList<Guid> Receptoras { get; private set; } = [];
    public IReadOnlyList<AporteGrupo> Aportes { get; private set; } = [];
    public GrupoAgrupacion(Guid grupoId, Guid solicitudId, Guid version, bool congelado,
        IReadOnlyList<Guid> origenes, IReadOnlyList<Guid> receptoras, IReadOnlyList<AporteGrupo> aportes)
    { GrupoId = grupoId; SolicitudId = solicitudId; Version = version; Congelado = congelado; Origenes = origenes; Receptoras = receptoras; Aportes = aportes; }
    public static GrupoAgrupacion Crear(Guid solicitud, IReadOnlyList<Guid> origenes, IReadOnlyList<Guid> receptoras, IReadOnlyList<AporteGrupo> aportes)
    {
        if (solicitud == Guid.Empty) throw new GrupoInvalidoException("Seleccione la solicitud.");
        Validar(origenes, receptoras, aportes);
        return new(Guid.NewGuid(), solicitud, Guid.NewGuid(), false, origenes.ToArray(), receptoras.ToArray(), aportes.ToArray());
    }
    public void Reemplazar(IReadOnlyList<Guid> origenes, IReadOnlyList<Guid> receptoras, IReadOnlyList<AporteGrupo> aportes)
    {
        if (Congelado) throw new ConflictoGrupoException("El primer análisis guardado congeló la composición.");
        Validar(origenes, receptoras, aportes);
        Origenes = origenes.ToArray(); Receptoras = receptoras.ToArray(); Aportes = aportes.ToArray(); Version = Guid.NewGuid();
    }
    private static void Validar(IReadOnlyList<Guid> origenes, IReadOnlyList<Guid> receptoras, IReadOnlyList<AporteGrupo> aportes)
    {
        if (origenes is null || receptoras is null || aportes is null || origenes.Count is < 1 or > 50 || receptoras.Count is < 1 or > 50
            || aportes.Count is < 2 or > 2500 || origenes.Contains(Guid.Empty) || receptoras.Contains(Guid.Empty)
            || origenes.Distinct().Count() != origenes.Count || receptoras.Distinct().Count() != receptoras.Count)
            throw new GrupoInvalidoException("Seleccione orígenes y receptoras distintos y al menos dos vínculos; para 1:1 use parejas.");
        if (aportes.Any(a => a is null || !origenes.Contains(a.AsignaturaAprobadaId) || !receptoras.Contains(a.AsignaturaReceptoraId))
            || aportes.Select(a => (a.AsignaturaAprobadaId, a.AsignaturaReceptoraId)).Distinct().Count() != aportes.Count
            || origenes.Any(o => !aportes.Any(a => a.AsignaturaAprobadaId == o)) || receptoras.Any(r => !aportes.Any(a => a.AsignaturaReceptoraId == r)))
            throw new GrupoInvalidoException("Cada par debe ser único, pertenecer al grupo y cubrir todas las asignaturas seleccionadas.");
        foreach (var a in aportes)
            if (!Numero(a.HorasAportadas, 9999999999.99m) || !Numero(a.CreditosAportados, 9999.99m))
                throw new GrupoInvalidoException($"Aporte {a.AsignaturaAprobadaId} → {a.AsignaturaReceptoraId}: use cero o positivo con hasta dos decimales, o null pendiente.");
    }
    private static bool Numero(decimal? n, decimal max) => n is null || n >= 0 && n <= max && decimal.Round(n.Value, 2) == n;
    public void Congelar() { Congelado = true; Version = Guid.NewGuid(); }
}
public sealed class GrupoInvalidoException(string mensaje) : Exception(mensaje);
public sealed class ConflictoGrupoException(string mensaje) : Exception(mensaje);
