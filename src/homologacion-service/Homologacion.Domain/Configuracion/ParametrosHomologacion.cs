namespace Homologacion.Domain.Configuracion;

/// <summary>Snapshot inmutable de los parámetros aplicables a una evaluación.</summary>
public sealed record ParametrosHomologacion
{
    public const int UmbralPredeterminado = 80;
    public const int AntiguedadPredeterminadaAnios = 10;

    public Guid VersionId { get; }
    public int UmbralPct { get; }
    public int AntiguedadMaximaAnios { get; }
    public int PeriodosMinimosInterno { get; }
    public int AsignaturasHomologablesMinimasInterno { get; }
    public int PeriodosMinimosExterno { get; }
    public int AsignaturasHomologablesMinimasExterno { get; }

    public ParametrosHomologacion(Guid versionId, int umbralPct, int antiguedadMaximaAnios,
        int periodosMinimosInterno, int asignaturasHomologablesMinimasInterno,
        int periodosMinimosExterno, int asignaturasHomologablesMinimasExterno)
    {
        if (versionId == Guid.Empty) throw new ArgumentException("La versión de parámetros es obligatoria.", nameof(versionId));
        if (umbralPct is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(umbralPct), "El umbral debe estar entre 1 y 100.");
        ValidarPositivoHasta50(antiguedadMaximaAnios, nameof(antiguedadMaximaAnios));
        ValidarPositivoHasta50(periodosMinimosInterno, nameof(periodosMinimosInterno));
        ValidarPositivoHasta50(asignaturasHomologablesMinimasInterno, nameof(asignaturasHomologablesMinimasInterno));
        ValidarPositivoHasta50(periodosMinimosExterno, nameof(periodosMinimosExterno));
        ValidarPositivoHasta50(asignaturasHomologablesMinimasExterno, nameof(asignaturasHomologablesMinimasExterno));

        VersionId = versionId;
        UmbralPct = umbralPct;
        AntiguedadMaximaAnios = antiguedadMaximaAnios;
        PeriodosMinimosInterno = periodosMinimosInterno;
        AsignaturasHomologablesMinimasInterno = asignaturasHomologablesMinimasInterno;
        PeriodosMinimosExterno = periodosMinimosExterno;
        AsignaturasHomologablesMinimasExterno = asignaturasHomologablesMinimasExterno;
    }

    public static ParametrosHomologacion Predeterminados(Guid? versionId = null)
        => new(versionId ?? Guid.NewGuid(), UmbralPredeterminado, AntiguedadPredeterminadaAnios,
            1, 1, 2, 2);

    private static void ValidarPositivoHasta50(int valor, string parametro)
    {
        if (valor is < 1 or > 50)
            throw new ArgumentOutOfRangeException(parametro, "El valor debe ser un entero entre 1 y 50.");
    }
}
