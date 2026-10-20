using System.Globalization;
using Homologacion.Domain.Equivalencias;

namespace Homologacion.Application.Equivalencias;

public sealed class EvaluarEquivalencia(IUmbralEquivalencia umbral)
{
    public EvaluacionEquivalencia Ejecutar(SolicitudEquivalencia solicitud)
    {
        ArgumentNullException.ThrowIfNull(solicitud);
        var origenes = solicitud.Origenes ?? [];
        if (origenes.Any(o => o is null)) throw new DatosEvaluacionInvalidosException("La lista de orígenes no admite elementos nulos.");
        foreach (var origen in origenes) ValidarAsignatura(origen!.Asignatura);
        ValidarAsignatura(solicitud.Receptora);
        var ids = origenes.Select(o => o!.Asignatura?.Id).Where(id => id is not null).Select(id => id!).ToArray();
        if (ids.Distinct(StringComparer.Ordinal).Count() != ids.Length)
            throw new DatosEvaluacionInvalidosException("Los identificadores de asignaturas de origen deben ser únicos.");
        var porId = origenes.Where(o => o!.Asignatura?.Id is not null).ToDictionary(o => o!.Asignatura!.Id!, o => o!, StringComparer.Ordinal);
        var faltantes = new List<string>();
        if (origenes.Count == 0) faltantes.Add("asignaturas de origen aprobadas");
        foreach (var origen in origenes)
        {
            RegistrarDatosAcademicos(origen!.Asignatura, "origen", faltantes);
            if (origen.Aprobada != true) faltantes.Add($"constancia explícita de aprobación del origen {origen.Asignatura?.Id ?? "sin identificar"}");
        }
        RegistrarDatosAcademicos(solicitud.Receptora, "receptora", faltantes);
        if (solicitud.Receptora?.Contenidos is { Count: 0 } && solicitud.Receptora.ResultadosAprendizaje is { Count: 0 })
            faltantes.Add("al menos un contenido o resultado de aprendizaje receptor");

        var contenidos = Coberturas(solicitud.CorrespondenciasContenidos, false, porId, solicitud.Receptora, faltantes);
        var resultados = Coberturas(solicitud.CorrespondenciasResultadosAprendizaje, true, porId, solicitud.Receptora, faltantes);
        var cobertura = CoberturaAcademica.Calcular(solicitud.Receptora?.Contenidos?.Select(c => c!).ToArray(),
            solicitud.Receptora?.ResultadosAprendizaje?.Select(r => r!).ToArray(), contenidos, resultados);
        var academica = faltantes.Count == 0 ? cobertura.Porcentaje : null;
        var horas = CompararCantidad(origenes, solicitud.Receptora, true, faltantes);
        var creditos = CompararCantidad(origenes, solicitud.Receptora, false, faltantes);
        var valorUmbral = umbral.Valor;
        var decision = MotorEquivalencia.Evaluar(academica, horas, creditos, valorUmbral);
        var estado = faltantes.Count > 0 ? EstadoEquivalencia.InformacionIncompleta : decision.Estado;
        var justificacion = estado switch
        {
            EstadoEquivalencia.InformacionIncompleta => "Información incompleta: " + string.Join("; ", faltantes.Distinct()) + ".",
            EstadoEquivalencia.Homologable => $"Los tres criterios alcanzan individualmente el umbral de {Texto(valorUmbral)} %.",
            _ => "No homologable: " + string.Join("; ", new[]
            {
                Insuficiencia("compatibilidad académica", academica, valorUmbral),
                Insuficiencia("compatibilidad horaria", horas, valorUmbral),
                Insuficiencia("compatibilidad de créditos", creditos, valorUmbral)
            }.Where(m => m is not null)) + "."
        };
        return new(ids, solicitud.Receptora?.Id, academica, horas, creditos, valorUmbral, estado, justificacion,
            cobertura.ContenidosSinCobertura, cobertura.ResultadosSinCobertura, faltantes.Distinct().ToArray());
    }

    private static string Texto(decimal valor) => valor.ToString(CultureInfo.InvariantCulture);
    private static string? Insuficiencia(string nombre, decimal? valor, decimal umbral)
        => valor < umbral ? $"{nombre} {Texto(valor!.Value)} % inferior a {Texto(umbral)} %" : null;

    private static void RegistrarDatosAcademicos(DatosAsignatura? datos, string rol, List<string> faltantes)
    {
        if (datos?.Id is null) faltantes.Add($"identificador de asignatura {rol}");
        var nombre = datos?.Id ?? rol;
        if (string.IsNullOrWhiteSpace(datos?.PlanAnalitico)) faltantes.Add($"referencia al plan analítico de {nombre}");
        if (datos?.Contenidos is null) faltantes.Add($"inventario de contenidos de {nombre}");
        if (datos?.ResultadosAprendizaje is null) faltantes.Add($"inventario de resultados de aprendizaje de {nombre}");
    }

    private static IReadOnlyList<string>? Coberturas(IReadOnlyList<CorrespondenciaAcademica?>? correspondencias,
        bool resultados, IReadOnlyDictionary<string, OrigenComparacion> origenes, DatosAsignatura? receptora, List<string> faltantes)
    {
        var categoria = resultados ? "resultados de aprendizaje" : "contenidos";
        if (correspondencias is null) { faltantes.Add($"correspondencias explícitas de {categoria}"); return null; }
        var cubiertos = new List<string>();
        foreach (var c in correspondencias)
        {
            if (c is null || string.IsNullOrWhiteSpace(c.AsignaturaOrigen) || string.IsNullOrWhiteSpace(c.ElementoOrigen)
                || string.IsNullOrWhiteSpace(c.ElementoReceptor) || !origenes.TryGetValue(c.AsignaturaOrigen, out var origen))
                throw new DatosEvaluacionInvalidosException($"Correspondencia de {categoria} con identificadores de origen inválidos o inexistentes.");
            var inventarioOrigen = resultados ? origen.Asignatura!.ResultadosAprendizaje : origen.Asignatura!.Contenidos;
            var inventarioReceptor = resultados ? receptora?.ResultadosAprendizaje : receptora?.Contenidos;
            if ((inventarioOrigen is not null && !inventarioOrigen.Contains(c.ElementoOrigen, StringComparer.Ordinal))
                || (inventarioReceptor is not null && !inventarioReceptor.Contains(c.ElementoReceptor, StringComparer.Ordinal)))
                throw new DatosEvaluacionInvalidosException($"Correspondencia de {categoria} con elemento inexistente en el inventario de su asignatura.");
            if (inventarioOrigen is null || inventarioReceptor is null) continue;
            if (string.IsNullOrWhiteSpace(c.Evidencia?.UbicacionOrigen) || string.IsNullOrWhiteSpace(c.Evidencia.UbicacionReceptora))
            { faltantes.Add($"evidencia explícita de {categoria}: {c.AsignaturaOrigen}/{c.ElementoOrigen} → {c.ElementoReceptor}"); continue; }
            if (origen.Aprobada == true && !string.IsNullOrWhiteSpace(origen.Asignatura.PlanAnalitico)
                && !string.IsNullOrWhiteSpace(receptora?.PlanAnalitico)) cubiertos.Add(c.ElementoReceptor);
        }
        return cubiertos;
    }

    private static decimal? CompararCantidad(IReadOnlyList<OrigenComparacion?> origenes, DatosAsignatura? receptora,
        bool horas, List<string> faltantes)
    {
        var nombre = horas ? "horas" : "créditos";
        if (origenes.Count > 1)
        { faltantes.Add($"regla aprobada de agregación de {nombre} para varias asignaturas de origen"); return null; }
        var origen = origenes.Count == 1 ? origenes[0]?.Asignatura : null;
        var unidadOrigen = horas ? origen?.UnidadHoras : origen?.SistemaCreditos;
        var unidadReceptora = horas ? receptora?.UnidadHoras : receptora?.SistemaCreditos;
        if (string.IsNullOrWhiteSpace(unidadOrigen) || string.IsNullOrWhiteSpace(unidadReceptora)
            || !string.Equals(unidadOrigen, unidadReceptora, StringComparison.Ordinal))
        { faltantes.Add($"unidades comparables de {nombre}, sin factores de conversión inferidos"); return null; }
        var porcentaje = Porcentaje.DesdeRelacion(horas ? origen?.Horas : origen?.Creditos, horas ? receptora?.Horas : receptora?.Creditos)?.Valor;
        if (porcentaje is null) faltantes.Add($"cantidades positivas de {nombre} de origen y receptora");
        return porcentaje;
    }

    private static void ValidarAsignatura(DatosAsignatura? datos)
    {
        if (datos is null) return;
        if (datos.Id is not null && string.IsNullOrWhiteSpace(datos.Id))
            throw new DatosEvaluacionInvalidosException("Los identificadores de asignatura no pueden estar vacíos.");
        if (datos.Horas is <= 0 || datos.Creditos is <= 0)
            throw new DatosEvaluacionInvalidosException("Las horas y créditos deben ser mayores que cero cuando se proporcionan.");
        foreach (var inventario in new[] { datos.Contenidos, datos.ResultadosAprendizaje })
            if (inventario is not null && (inventario.Any(string.IsNullOrWhiteSpace) || inventario.Distinct(StringComparer.Ordinal).Count() != inventario.Count))
                throw new DatosEvaluacionInvalidosException("Los identificadores de cada inventario deben ser únicos y no vacíos.");
    }
}
