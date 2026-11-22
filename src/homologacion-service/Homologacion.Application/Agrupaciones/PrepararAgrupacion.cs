using Homologacion.Application.AnalisisInterno;
using Homologacion.Application.Equivalencias;
using Homologacion.Domain.Configuracion;
using Homologacion.Domain.Agrupaciones;
using Homologacion.Domain.Antiguedad;
namespace Homologacion.Application.Agrupaciones;

public sealed record EntradaAgrupacion(SolicitudAgrupacion Datos, IReadOnlyList<string> Advertencias);
public sealed class PrepararAgrupacion(ICatalogoAnalisisInterno catalogo, TimeProvider reloj)
{
    public async Task<EntradaAgrupacion> ConstruirAsync(SolicitudParaAnalisis solicitud, GrupoAgrupacion grupo, ParametrosHomologacion parametros, CancellationToken ct)
    {
        var advertencias = new List<string>();
        var origenes = new Dictionary<Guid, OrigenComparacion>();
        var programas = new Dictionary<Guid, AsignaturaParaAnalisis>();
        foreach (var id in grupo.Origenes)
        {
            var a = solicitud.AsignaturasAprobadas.SingleOrDefault(a => a.Id == id) ?? throw new GrupoInvalidoException($"Origen {id}: no pertenece a la solicitud.");
            var suyos = grupo.Aportes.Where(x => x.AsignaturaAprobadaId == id).ToArray();
            if (suyos.Sum(x => x.HorasAportadas ?? 0) > a.Horas || suyos.Sum(x => x.CreditosAportados ?? 0) > a.Creditos)
                throw new GrupoInvalidoException($"Origen {id}: las horas o créditos asignados superan sus cantidades aprobadas.");
            DatosAsignatura datos;
            if (solicitud.TipoCambio == "Interno" && a.AsignaturaOrigenId is { } catalogoId)
            {
                var p = await catalogo.ObtenerAsignaturaAsync(catalogoId, ct) ?? throw new FuenteAnalisisNoDisponibleException();
                Validar(p, catalogoId);
                if (p.CarreraId != solicitud.CarreraOrigenId) throw new GrupoInvalidoException($"Origen {id}: carrera ajena a la solicitud.");
                programas[id] = p;
                datos = Datos(p) with { Id = id.ToString(), Horas = a.Horas, Creditos = a.Creditos };
                if (p.Facultad != "FISEI" || !p.CarreraVigente || p.Horas != a.Horas || p.Creditos != a.Creditos)
                { datos = datos with { PlanAnalitico = null }; advertencias.Add($"Origen {id}: revisar histórico, vigencia o diferencias con catálogo; no se reasigna."); }
            }
            else
            {
                // D2: no existe programa analítico externo registrado. No se inventan elementos ni unidades.
                datos = new(id.ToString(), null, a.Horas, null, a.Creditos, null, [], []);
                advertencias.Add($"Origen {id}: información incompleta; falta programa analítico de origen externo (D2).");
            }
            bool? elegible = null;
            var hoy = DateOnly.FromDateTime(reloj.GetUtcNow().ToOffset(TimeSpan.FromHours(-5)).DateTime);
            if (a.Estado == "Reprobada" || a.Estado == "NoCursada" || a.IntentoMatricula >= 3)
            { elegible = false; advertencias.Add($"Origen {id}: excluido por aprobación o tercera matrícula; no aporta cobertura, horas ni créditos."); }
            else if (a.Estado == "Aprobada" && a.IntentoMatricula is >= 1 and < 3 && a.FechaAprobacion != default && a.FechaAprobacion <= hoy)
            {
                var v = ValidadorAntiguedad.Validar(EstadoAsignatura.Aprobada, a.FechaAprobacion, hoy, parametros.AntiguedadMaximaAnios);
                elegible = v.Estado == EstadoAntiguedad.Elegible;
                if (elegible == false) advertencias.Add($"Origen {id}: {v.Motivo}");
            }
            else advertencias.Add($"Origen {id}: no puede verificarse aprobación, fecha o intento de matrícula.");
            origenes[id] = new(datos, elegible);
        }
        var receptoras = new Dictionary<Guid, DatosAsignatura>();
        foreach (var id in grupo.Receptoras)
        {
            var p = await catalogo.ObtenerAsignaturaAsync(id, ct) ?? throw new GrupoInvalidoException($"Receptora {id}: inexistente.");
            Validar(p, id);
            if (p.CarreraId != solicitud.CarreraReceptoraId) throw new GrupoInvalidoException($"Receptora {id}: carrera ajena a la solicitud.");
            receptoras[id] = Datos(p) with { PlanAnalitico = p.Facultad == "FISEI" && p.CarreraVigente ? p.PlanAnalitico : null };
        }
        var aportes = new List<AporteAgrupacion>();
        foreach (var a in grupo.Aportes)
        {
            RevisionParaAnalisis? revision = null;
            if (programas.TryGetValue(a.AsignaturaAprobadaId, out var programa))
                revision = await catalogo.ObtenerRevisionAsync(programa.Id, a.AsignaturaReceptoraId, ct);
            var vigente = revision is { Registrada: true, Vigente: true };
            if (!vigente) advertencias.Add($"{a.AsignaturaAprobadaId} → {a.AsignaturaReceptoraId}: faltan correspondencias revisadas y vigentes.");
            aportes.Add(new(a.AsignaturaAprobadaId.ToString(), a.AsignaturaReceptoraId.ToString(), a.HorasAportadas, a.CreditosAportados,
                vigente ? Vinculos(a.AsignaturaAprobadaId, revision!.Contenidos) : null,
                vigente ? Vinculos(a.AsignaturaAprobadaId, revision!.Resultados) : null));
        }
        return new(new(origenes.Values.ToArray(), receptoras.Values.ToArray(), aportes.ToArray()), advertencias);
    }
    private static void Validar(AsignaturaParaAnalisis p, Guid id)
    {
        if (p.Id != id || p.Contenidos is null || p.Resultados is null || p.Contenidos.Concat(p.Resultados).Any(x => x is null || x.Id == Guid.Empty))
            throw new FuenteAnalisisNoDisponibleException();
    }
    private static DatosAsignatura Datos(AsignaturaParaAnalisis p) => new(p.Id.ToString(), p.PlanAnalitico, p.Horas, p.UnidadHoras,
        p.Creditos, p.SistemaCreditos, p.Contenidos.Select(x => x.Id.ToString()).ToArray(), p.Resultados.Select(x => x.Id.ToString()).ToArray());
    private static IReadOnlyList<CorrespondenciaAcademica?> Vinculos(Guid aprobada, IReadOnlyList<VinculoParaAnalisis> vinculos)
    {
        if (vinculos is null || vinculos.Any(v => v is null)) throw new FuenteAnalisisNoDisponibleException();
        return vinculos.Select(v => (CorrespondenciaAcademica?)new CorrespondenciaAcademica(aprobada.ToString(), v.ElementoOrigen, v.ElementoReceptor,
            new(v.UbicacionOrigen, v.UbicacionReceptora))).ToArray();
    }
}
