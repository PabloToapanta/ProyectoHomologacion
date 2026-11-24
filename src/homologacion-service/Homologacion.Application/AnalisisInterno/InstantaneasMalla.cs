namespace Homologacion.Application.AnalisisInterno;

/// <summary>Contrato propio de Homologación para la malla que entrega Catálogo.</summary>
public sealed record MallaParaAnalisis(Guid Id, Guid CarreraId, string Nombre, bool? Vigente);

/// <summary>HU-07: estado de la malla al consultar el programa, independiente de CarreraVigente.</summary>
public sealed record InstantaneaMalla(Guid Id, Guid CarreraId, string Nombre, bool VigenteAlAnalizar);

/// <summary>La aprobada pertenece a Solicitudes; el identificador de Catálogo es distinto y puede faltar en origen externo.</summary>
public sealed record MallaAsignaturaAnalizada(Guid? AsignaturaCatalogoId, Guid? AsignaturaAprobadaId, InstantaneaMalla? Malla)
{
    internal static MallaAsignaturaAnalizada Capturar(AsignaturaParaAnalisis? asignatura, Guid? aprobadaId = null)
    {
        var m = asignatura?.Malla;
        if (m is null) return new(asignatura?.Id, aprobadaId, null);
        if (m.Id == Guid.Empty || m.CarreraId == Guid.Empty || m.CarreraId != asignatura!.CarreraId
            || string.IsNullOrWhiteSpace(m.Nombre) || m.Nombre.Length > 100 || m.Vigente is not bool vigente)
            throw new FuenteAnalisisNoDisponibleException();
        return new(asignatura.Id, aprobadaId, new(m.Id, m.CarreraId, m.Nombre, vigente));
    }
}

/// <summary>Metadatos del resultado, fuera del motor, ContextoAgente y las firmas de reutilización de HU-37.</summary>
public sealed record MallasAnalisis(IReadOnlyList<MallaAsignaturaAnalizada> Origenes, IReadOnlyList<MallaAsignaturaAnalizada> Receptoras);
