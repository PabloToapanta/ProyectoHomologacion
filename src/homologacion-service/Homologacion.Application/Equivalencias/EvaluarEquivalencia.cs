using Homologacion.Domain.Equivalencias;

namespace Homologacion.Application.Equivalencias;

public sealed class EvaluarEquivalencia(IUmbralEquivalencia umbral)
{
    public EvaluacionEquivalencia Ejecutar(SolicitudEquivalencia solicitud)
    {
        ArgumentNullException.ThrowIfNull(solicitud);
        ValidarAsignatura(solicitud.Origen, "origen");
        ValidarAsignatura(solicitud.Receptora, "receptora");
        ValidarCorrespondencias(solicitud);

        var origen = solicitud.Origen;
        var receptora = solicitud.Receptora;
        var horas = Porcentaje.DesdeRelacion(origen?.Horas, receptora?.Horas)?.Valor;
        var creditos = Porcentaje.DesdeRelacion(origen?.Creditos, receptora?.Creditos)?.Valor;
        decimal? contenido = null;
        IReadOnlyList<string> temasSinCobertura = [];
        var pendientes = new List<string>();
        if (origen?.Temas is { Count: > 0 } && receptora?.Temas is { Count: > 0 }
            && solicitud.Correspondencias is not null)
        {
            var cubiertos = solicitud.Correspondencias.Select(c => c!.TemaReceptor!).ToHashSet(StringComparer.Ordinal);
            contenido = Porcentaje.DesdeCobertura(cubiertos.Count, receptora.Temas.Count)?.Valor;
            temasSinCobertura = receptora.Temas.Where(t => !cubiertos.Contains(t!)).Select(t => t!).ToArray();
        }
        else pendientes.Add("temas estructurados y correspondencias explícitas de contenido");

        if (origen?.Id is null || receptora?.Id is null) pendientes.Add("identificadores de las asignaturas");
        if (horas is null) pendientes.Add("horas de origen y receptora");
        if (creditos is null) pendientes.Add("créditos de origen y receptora");
        // No existe un criterio de profundidad acordado. No inferirlo de textos, niveles ni porcentajes del cliente.
        decimal? profundidad = null;
        pendientes.Add("criterio académico verificable de profundidad pendiente de definición");
        var valorUmbral = umbral.Valor;
        var decision = MotorEquivalencia.Evaluar(contenido, profundidad, horas, creditos, valorUmbral);
        return new(origen?.Id, receptora?.Id, contenido, profundidad, horas, creditos, valorUmbral,
            decision.Estado, $"Información incompleta: {string.Join("; ", pendientes)}. La propuesta del equipo requiere validación académica.",
            temasSinCobertura);
    }

    private static void ValidarAsignatura(DatosAsignatura? datos, string nombre)
    {
        if (datos is null) return;
        if (datos.Id is not null && string.IsNullOrWhiteSpace(datos.Id))
            throw new DatosEvaluacionInvalidosException($"El identificador de {nombre} no puede estar vacío.");
        if (datos.Horas is <= 0 || datos.Creditos is <= 0)
            throw new DatosEvaluacionInvalidosException($"Las horas y créditos de {nombre} deben ser mayores que cero cuando se proporcionan.");
        if (datos.Temas is not null && (datos.Temas.Any(string.IsNullOrWhiteSpace)
            || datos.Temas.Distinct(StringComparer.Ordinal).Count() != datos.Temas.Count))
            throw new DatosEvaluacionInvalidosException($"Los identificadores de temas de {nombre} deben ser únicos y no vacíos.");
    }

    private static void ValidarCorrespondencias(SolicitudEquivalencia solicitud)
    {
        if (solicitud.Correspondencias is null) return;
        foreach (var correspondencia in solicitud.Correspondencias)
        {
            if (correspondencia is null || string.IsNullOrWhiteSpace(correspondencia.TemaOrigen)
                || string.IsNullOrWhiteSpace(correspondencia.TemaReceptor)
                || solicitud.Origen?.Temas?.Contains(correspondencia.TemaOrigen, StringComparer.Ordinal) != true
                || solicitud.Receptora?.Temas?.Contains(correspondencia.TemaReceptor, StringComparer.Ordinal) != true)
                throw new DatosEvaluacionInvalidosException("Cada correspondencia debe referir temas existentes de origen y receptora.");
        }
    }
}
