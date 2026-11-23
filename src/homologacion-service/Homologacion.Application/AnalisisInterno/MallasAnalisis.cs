namespace Homologacion.Application.AnalisisInterno;

/// <summary>Contrato de lectura de Catálogo; no es una entidad EF compartida entre servicios.</summary>
public sealed record MallaCatalogoAnalisis(Guid Id, Guid CarreraId, string Nombre, bool? Vigente);

/// <summary>Datos capturados al analizar, independientes de la vigencia actual del Catálogo.</summary>
public sealed record MallaAnalisis(Guid Id, Guid CarreraId, string Nombre, bool VigenteAlAnalizar);
public sealed record AsignaturaMallaAnalisis(Guid? AsignaturaAprobadaId, Guid? AsignaturaCatalogoId, MallaAnalisis? Malla)
{
    public static AsignaturaMallaAnalisis Capturar(AsignaturaParaAnalisis? asignatura, Guid? aprobadaId = null)
    {
        var fuente = asignatura?.Malla;
        if (fuente is null) return new(aprobadaId, asignatura?.Id, null);
        if (fuente.Id == Guid.Empty || fuente.CarreraId != asignatura!.CarreraId || fuente.CarreraId == Guid.Empty
            || string.IsNullOrWhiteSpace(fuente.Nombre) || fuente.Nombre.Length > 100 || fuente.Vigente is not bool vigente)
            throw new FuenteAnalisisNoDisponibleException();
        return new(aprobadaId, asignatura.Id, new(fuente.Id, fuente.CarreraId, fuente.Nombre, vigente));
    }
}

/// <summary>Null en resultados históricos sin versión; el origen externo no autorizado conserva Malla null.</summary>
public sealed record MallasAnalisis(IReadOnlyList<AsignaturaMallaAnalisis> Origenes, IReadOnlyList<AsignaturaMallaAnalisis> Receptoras)
{
    public static MallasAnalisis Pareja(Guid aprobadaId, AsignaturaParaAnalisis? origen, AsignaturaParaAnalisis receptora) =>
        new([AsignaturaMallaAnalisis.Capturar(origen, aprobadaId)], [AsignaturaMallaAnalisis.Capturar(receptora)]);
}
