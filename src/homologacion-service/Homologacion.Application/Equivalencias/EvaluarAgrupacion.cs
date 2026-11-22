using Homologacion.Domain.Configuracion;
using Homologacion.Domain.Equivalencias;

namespace Homologacion.Application.Equivalencias;

public sealed record AporteAgrupacion(string? AsignaturaOrigenId, string? AsignaturaReceptoraId,
    decimal? HorasAportadas, decimal? CreditosAportados,
    IReadOnlyList<CorrespondenciaAcademica?>? CorrespondenciasContenidos,
    IReadOnlyList<CorrespondenciaAcademica?>? CorrespondenciasResultadosAprendizaje);

public sealed record SolicitudAgrupacion(IReadOnlyList<OrigenComparacion?>? Origenes,
    IReadOnlyList<DatosAsignatura?>? Receptoras, IReadOnlyList<AporteAgrupacion?>? Aportes);

public sealed record AporteEvaluadoAgrupacion(string AsignaturaOrigenId, string AsignaturaReceptoraId,
    decimal? HorasAportadas, decimal? CreditosAportados);

public sealed record EvidenciaAgrupacion(string AsignaturaOrigenId, string AsignaturaReceptoraId,
    string Categoria, string ElementoOrigen, string ElementoReceptor,
    string UbicacionOrigen, string UbicacionReceptora);

public sealed record ResultadoAgrupacion(IReadOnlyList<EvaluacionEquivalencia> Receptoras,
    decimal UmbralPct, EstadoEquivalencia Estado, string Justificacion,
    IReadOnlyList<string> InformacionFaltante,
    IReadOnlyList<AporteEvaluadoAgrupacion> Aportes,
    IReadOnlyList<EvidenciaAgrupacion> Evidencias)
{
    public bool Homologable => Estado == EstadoEquivalencia.Homologable;
    public Guid? ConfiguracionVersionId { get; init; }
}

/// <summary>HU-21/T62: evaluación determinista de una agrupación; no persiste ni decide académicamente.</summary>
public sealed class EvaluarAgrupacion(IUmbralEquivalencia umbral)
{
    public ResultadoAgrupacion Ejecutar(SolicitudAgrupacion solicitud) => Ejecutar(solicitud, umbral.Valor, null);

    public ResultadoAgrupacion Ejecutar(SolicitudAgrupacion solicitud, ParametrosHomologacion parametros)
    {
        ArgumentNullException.ThrowIfNull(parametros);
        return Ejecutar(solicitud, parametros.UmbralPct, parametros.VersionId);
    }

    private static ResultadoAgrupacion Ejecutar(SolicitudAgrupacion solicitud, decimal umbralPct, Guid? version)
    {
        ArgumentNullException.ThrowIfNull(solicitud);
        if (umbralPct is <= 0 or > 100) throw new ArgumentOutOfRangeException(nameof(umbralPct));
        if (solicitud.Origenes is null or { Count: 0 } || solicitud.Receptoras is null or { Count: 0 }
            || solicitud.Aportes is null or { Count: 0 })
            throw new DatosEvaluacionInvalidosException("La agrupación requiere orígenes, receptoras y aportes.");
        if (solicitud.Origenes.Any(x => x?.Asignatura is null) || solicitud.Receptoras.Any(x => x is null)
            || solicitud.Aportes.Any(x => x is null))
            throw new DatosEvaluacionInvalidosException("La agrupación no admite elementos nulos.");

        var origenes = solicitud.Origenes.Select(x => x!).ToArray();
        var receptoras = solicitud.Receptoras.Select(x => x!).ToArray();
        var aportes = solicitud.Aportes.Select(x => x!).ToArray();
        var porOrigen = Indexar(origenes.Select(x => x.Asignatura!).ToArray(), "origen");
        var porReceptora = Indexar(receptoras, "receptora");
        if (aportes.Any(a => string.IsNullOrWhiteSpace(a.AsignaturaOrigenId)
            || string.IsNullOrWhiteSpace(a.AsignaturaReceptoraId)
            || !porOrigen.ContainsKey(a.AsignaturaOrigenId) || !porReceptora.ContainsKey(a.AsignaturaReceptoraId)))
            throw new DatosEvaluacionInvalidosException("Un aporte referencia una asignatura ajena a la agrupación.");
        if (aportes.Select(a => (a.AsignaturaOrigenId, a.AsignaturaReceptoraId)).Distinct().Count() != aportes.Length)
            throw new DatosEvaluacionInvalidosException("Cada par origen-receptora se registra una sola vez.");
        if (porOrigen.Keys.Any(id => !aportes.Any(a => a.AsignaturaOrigenId == id))
            || porReceptora.Keys.Any(id => !aportes.Any(a => a.AsignaturaReceptoraId == id)))
            throw new DatosEvaluacionInvalidosException("Cada asignatura debe participar en al menos un aporte.");
        foreach (var aporte in aportes) ValidarAporte(aporte);
        foreach (var (id, origen) in porOrigen)
        {
            var suyos = aportes.Where(a => a.AsignaturaOrigenId == id).ToArray();
            if (origen.Horas is { } horas && suyos.Sum(a => a.HorasAportadas ?? 0) > horas)
                throw new DatosEvaluacionInvalidosException($"Las horas asignadas de {id} superan las aprobadas.");
            if (origen.Creditos is { } creditos && suyos.Sum(a => a.CreditosAportados ?? 0) > creditos)
                throw new DatosEvaluacionInvalidosException($"Los créditos asignados de {id} superan los aprobados.");
        }

        var elegibles = origenes.ToDictionary(o => o.Asignatura!.Id!, o => o.Aprobada == true, StringComparer.Ordinal);
        var evaluaciones = receptoras.Select(receptora => EvaluarReceptora(receptora,
            aportes.Where(a => a.AsignaturaReceptoraId == receptora.Id).ToArray(), porOrigen, elegibles,
            umbralPct, version)).ToArray();
        var faltantes = evaluaciones.SelectMany(e => e.InformacionFaltante.Select(f => $"{e.AsignaturaReceptora}: {f}"))
            .Distinct(StringComparer.Ordinal).ToArray();
        var estado = evaluaciones.Any(e => e.Estado == EstadoEquivalencia.InformacionIncompleta)
            ? EstadoEquivalencia.InformacionIncompleta
            : evaluaciones.All(e => e.Estado == EstadoEquivalencia.Homologable)
                ? EstadoEquivalencia.Homologable : EstadoEquivalencia.NoHomologable;
        var justificacion = estado switch
        {
            EstadoEquivalencia.InformacionIncompleta => "Información incompleta: " + string.Join("; ", faltantes) + ".",
            EstadoEquivalencia.Homologable => "Todas las receptoras alcanzan los tres criterios del umbral configurado.",
            _ => "No homologable: " + string.Join("; ", evaluaciones.Where(e => !e.Homologable)
                .Select(e => $"{e.AsignaturaReceptora}: {e.Justificacion}"))
        };
        var aportesUsados = aportes.Select(a => new AporteEvaluadoAgrupacion(a.AsignaturaOrigenId!,
            a.AsignaturaReceptoraId!, a.HorasAportadas, a.CreditosAportados)).ToArray();
        var evidencias = aportes.SelectMany(a => Fotografiar(a, porOrigen[a.AsignaturaOrigenId!],
            porReceptora[a.AsignaturaReceptoraId!], elegibles[a.AsignaturaOrigenId!])).Distinct().ToArray();
        return new(evaluaciones, umbralPct, estado, justificacion, faltantes, aportesUsados, evidencias)
        { ConfiguracionVersionId = version };
    }

    private static IReadOnlyDictionary<string, DatosAsignatura> Indexar(DatosAsignatura[] materias, string rol)
    {
        if (materias.Any(m => string.IsNullOrWhiteSpace(m.Id)))
            throw new DatosEvaluacionInvalidosException($"Cada asignatura {rol} requiere ID.");
        if (materias.Select(m => m.Id).Distinct(StringComparer.Ordinal).Count() != materias.Length)
            throw new DatosEvaluacionInvalidosException($"Hay asignaturas {rol} duplicadas.");
        foreach (var materia in materias)
        {
            if (materia.Horas is <= 0 || materia.Creditos is <= 0)
                throw new DatosEvaluacionInvalidosException($"Horas y créditos de {materia.Id} deben ser positivos.");
            foreach (var inventario in new[] { materia.Contenidos, materia.ResultadosAprendizaje })
                if (inventario is not null && (inventario.Any(string.IsNullOrWhiteSpace)
                    || inventario.Distinct(StringComparer.Ordinal).Count() != inventario.Count))
                    throw new DatosEvaluacionInvalidosException($"Inventario inválido de {materia.Id}.");
        }
        return materias.ToDictionary(m => m.Id!, StringComparer.Ordinal);
    }

    private static void ValidarAporte(AporteAgrupacion aporte)
    {
        foreach (var (nombre, cantidad) in new[] { ("horas", aporte.HorasAportadas), ("créditos", aporte.CreditosAportados) })
            if (cantidad is { } valor && (valor < 0 || decimal.Round(valor, 2) != valor))
                throw new DatosEvaluacionInvalidosException($"El aporte de {nombre} debe ser no negativo y tener hasta dos decimales.");
    }

    private static IEnumerable<EvidenciaAgrupacion> Fotografiar(AporteAgrupacion aporte, DatosAsignatura origen,
        DatosAsignatura receptora, bool elegible)
    {
        if (!elegible || string.IsNullOrWhiteSpace(origen.PlanAnalitico) || string.IsNullOrWhiteSpace(receptora.PlanAnalitico))
            yield break;
        foreach (var (categoria, correspondencias) in new[]
        {
            ("Contenido", aporte.CorrespondenciasContenidos),
            ("ResultadoAprendizaje", aporte.CorrespondenciasResultadosAprendizaje)
        })
            foreach (var c in correspondencias ?? [])
                if (c is not null && !string.IsNullOrWhiteSpace(c.Evidencia?.UbicacionOrigen)
                    && !string.IsNullOrWhiteSpace(c.Evidencia.UbicacionReceptora))
                    yield return new(origen.Id!, receptora.Id!, categoria, c.ElementoOrigen!, c.ElementoReceptor!,
                        c.Evidencia.UbicacionOrigen, c.Evidencia.UbicacionReceptora);
    }

    private static EvaluacionEquivalencia EvaluarReceptora(DatosAsignatura receptora, AporteAgrupacion[] aportes,
        IReadOnlyDictionary<string, DatosAsignatura> origenes, IReadOnlyDictionary<string, bool> elegibles,
        decimal umbralPct, Guid? version)
    {
        var faltantes = new List<string>();
        DatosAcademicos(receptora, faltantes);
        if (receptora.Contenidos is { Count: 0 } && receptora.ResultadosAprendizaje is { Count: 0 })
            faltantes.Add("al menos un contenido o resultado de aprendizaje receptor");
        foreach (var aporte in aportes)
        {
            var origen = origenes[aporte.AsignaturaOrigenId!];
            DatosAcademicos(origen, faltantes);
            if (!elegibles[origen.Id!]) faltantes.Add($"aprobación del origen {origen.Id}");
        }
        var contenidos = Coberturas(aportes, receptora, origenes, elegibles, false, faltantes);
        var resultados = Coberturas(aportes, receptora, origenes, elegibles, true, faltantes);
        var cobertura = CoberturaAcademica.Calcular(receptora.Contenidos?.Select(x => x!).ToArray(),
            receptora.ResultadosAprendizaje?.Select(x => x!).ToArray(), contenidos, resultados);
        var academica = faltantes.Count == 0 ? cobertura.Porcentaje : null;
        var horas = Cantidad(aportes, receptora, origenes, elegibles, true, faltantes);
        var creditos = Cantidad(aportes, receptora, origenes, elegibles, false, faltantes);
        var estado = faltantes.Count > 0 ? EstadoEquivalencia.InformacionIncompleta
            : MotorEquivalencia.Evaluar(academica, horas, creditos, umbralPct).Estado;
        var justificacion = estado switch
        {
            EstadoEquivalencia.InformacionIncompleta => "Información incompleta: " + string.Join("; ", faltantes.Distinct()) + ".",
            EstadoEquivalencia.Homologable => "Los tres criterios alcanzan el umbral configurado.",
            _ => "Uno o más criterios no alcanzan el umbral configurado."
        };
        return new(aportes.Select(a => a.AsignaturaOrigenId!).Distinct(StringComparer.Ordinal).ToArray(),
            receptora.Id, academica, horas, creditos, umbralPct, estado, justificacion,
            cobertura.ContenidosSinCobertura, cobertura.ResultadosSinCobertura, faltantes.Distinct().ToArray())
        { ConfiguracionVersionId = version };
    }

    private static void DatosAcademicos(DatosAsignatura materia, List<string> faltantes)
    {
        if (string.IsNullOrWhiteSpace(materia.PlanAnalitico)) faltantes.Add($"plan analítico de {materia.Id}");
        if (materia.Contenidos is null) faltantes.Add($"contenidos de {materia.Id}");
        if (materia.ResultadosAprendizaje is null) faltantes.Add($"resultados de {materia.Id}");
    }

    private static IReadOnlyList<string>? Coberturas(AporteAgrupacion[] aportes, DatosAsignatura receptora,
        IReadOnlyDictionary<string, DatosAsignatura> origenes, IReadOnlyDictionary<string, bool> elegibles,
        bool resultados, List<string> faltantes)
    {
        var nombre = resultados ? "resultados" : "contenidos";
        var cubiertos = new HashSet<string>(StringComparer.Ordinal);
        foreach (var aporte in aportes)
        {
            var origen = origenes[aporte.AsignaturaOrigenId!];
            var inventarioOrigen = resultados ? origen.ResultadosAprendizaje : origen.Contenidos;
            var inventarioReceptor = resultados ? receptora.ResultadosAprendizaje : receptora.Contenidos;
            var correspondencias = resultados ? aporte.CorrespondenciasResultadosAprendizaje : aporte.CorrespondenciasContenidos;
            if (correspondencias is null)
            { faltantes.Add($"correspondencias revisadas de {nombre} para {origen.Id} → {receptora.Id}"); continue; }
            foreach (var c in correspondencias)
            {
                if (c is null || c.AsignaturaOrigen != origen.Id || string.IsNullOrWhiteSpace(c.ElementoOrigen)
                    || string.IsNullOrWhiteSpace(c.ElementoReceptor))
                    throw new DatosEvaluacionInvalidosException($"Correspondencia inválida de {nombre} para {origen.Id}.");
                if ((inventarioOrigen is not null && !inventarioOrigen.Contains(c.ElementoOrigen, StringComparer.Ordinal))
                    || (inventarioReceptor is not null && !inventarioReceptor.Contains(c.ElementoReceptor, StringComparer.Ordinal)))
                    throw new DatosEvaluacionInvalidosException($"Correspondencia de {nombre} con elemento inexistente.");
                if (inventarioOrigen is null || inventarioReceptor is null) continue;
                if (string.IsNullOrWhiteSpace(c.Evidencia?.UbicacionOrigen)
                    || string.IsNullOrWhiteSpace(c.Evidencia.UbicacionReceptora))
                { faltantes.Add($"evidencia de {nombre} {origen.Id}/{c.ElementoOrigen} → {receptora.Id}/{c.ElementoReceptor}"); continue; }
                if (elegibles[origen.Id!] && !string.IsNullOrWhiteSpace(origen.PlanAnalitico)
                    && !string.IsNullOrWhiteSpace(receptora.PlanAnalitico)) cubiertos.Add(c.ElementoReceptor);
            }
        }
        return cubiertos.ToArray();
    }

    private static decimal? Cantidad(AporteAgrupacion[] aportes, DatosAsignatura receptora,
        IReadOnlyDictionary<string, DatosAsignatura> origenes, IReadOnlyDictionary<string, bool> elegibles,
        bool horas, List<string> faltantes)
    {
        var nombre = horas ? "horas" : "créditos";
        var total = horas ? receptora.Horas : receptora.Creditos;
        if (total is null or <= 0)
        { faltantes.Add($"total positivo de {nombre} de {receptora.Id}"); return null; }
        var unidadReceptora = horas ? receptora.UnidadHoras : receptora.SistemaCreditos;
        decimal suma = 0;
        var comparable = true;
        foreach (var aporte in aportes)
        {
            var origen = origenes[aporte.AsignaturaOrigenId!];
            var unidadOrigen = horas ? origen.UnidadHoras : origen.SistemaCreditos;
            if (string.IsNullOrWhiteSpace(unidadReceptora) || string.IsNullOrWhiteSpace(unidadOrigen)
                || !string.Equals(unidadReceptora, unidadOrigen, StringComparison.Ordinal))
            { faltantes.Add($"unidades comparables de {nombre} para {origen.Id} → {receptora.Id}"); comparable = false; }
            var cantidadOrigen = horas ? origen.Horas : origen.Creditos;
            if (cantidadOrigen is null or <= 0 || !elegibles[origen.Id!])
            { faltantes.Add($"cantidad aprobada de {nombre} de {origen.Id}"); comparable = false; }
            var aporteCantidad = horas ? aporte.HorasAportadas : aporte.CreditosAportados;
            if (aporteCantidad is null)
            { faltantes.Add($"aporte de {nombre} pendiente de {origen.Id} → {receptora.Id}"); comparable = false; }
            else suma += aporteCantidad.Value;
        }
        if (!comparable) return null;
        return suma >= total ? 100 : suma / total.Value * 100;
    }
}
