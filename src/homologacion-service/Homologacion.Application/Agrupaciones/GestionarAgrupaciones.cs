using Homologacion.Application.AnalisisInterno;
using Homologacion.Application.Configuracion;
using Homologacion.Application.Equivalencias;
using Homologacion.Application.Informe;
using Homologacion.Domain.Agrupaciones;
namespace Homologacion.Application.Agrupaciones;

public sealed class GestionarAgrupaciones(ISolicitudesAnalisisInterno solicitudes, ICatalogoAnalisisInterno catalogo,
    IReceptorasAnalisisInterno receptoras, IParametrosHomologacionProvider parametros, PrepararAgrupacion preparar,
    EvaluarAgrupacion motor, IGruposAgrupacion grupos, IInformesHomologacion informes, TimeProvider reloj)
{
    public const string Leyenda = "Homologación por agrupación — regla de análisis pendiente de validación institucional";
    public static string ETag(GrupoAgrupacion grupo) => "\"" + grupo.Version.ToString("N") + "\"";
    private async Task<SolicitudParaAnalisis> Asignada(Guid id, CancellationToken ct)
    {
        var s = await solicitudes.ObtenerSolicitudAsync(id, ct) ?? throw new SolicitudAnalisisNoEncontradaException();
        if (s.Id != id || s.AsignaturasAprobadas is null || s.AsignaturasAprobadas.Any(a => a is null)
            || s.AsignaturasAprobadas.Select(a => a.Id).Distinct().Count() != s.AsignaturasAprobadas.Count)
            throw new FuenteAnalisisNoDisponibleException();
        return s;
    }
    public async Task<IReadOnlyList<DetalleGrupo>> ListarAsync(Guid id, CancellationToken ct)
    { await Asignada(id, ct); return await grupos.ListarAsync(id, ct); }
    public async Task<DetalleGrupo> ObtenerAsync(Guid id, Guid grupo, CancellationToken ct)
    { await Asignada(id, ct); return await grupos.ObtenerAsync(id, grupo, ct) ?? throw new GrupoNoEncontradoException(); }
    public async Task<DetalleGrupo> CrearAsync(Guid id, ComposicionGrupo input, CancellationToken ct)
    {
        var s = await Asignada(id, ct);
        var g = GrupoAgrupacion.Crear(id, input.Origenes, input.Receptoras, input.Aportes);
        await preparar.ConstruirAsync(s, g, await parametros.ObtenerVigentesAsync(ct), ct);
        return await informes.ConCierreAsync(id, async () =>
        {
            await Abierto(id, ct); await grupos.GuardarAsync(g, null, ct); return new DetalleGrupo(g, null);
        }, ct);
    }
    public async Task<DetalleGrupo> ReemplazarAsync(Guid id, Guid grupo, string? version, ComposicionGrupo input, CancellationToken ct)
    {
        var s = await Asignada(id, ct);
        var p = await parametros.ObtenerVigentesAsync(ct);
        return await informes.ConCierreAsync(id, async () =>
        {
            await Abierto(id, ct); var d = await Exigir(id, grupo, version, ct);
            d.Grupo.Reemplazar(input.Origenes, input.Receptoras, input.Aportes);
            await preparar.ConstruirAsync(s, d.Grupo, p, ct); await grupos.GuardarAsync(d.Grupo, null, ct); return d;
        }, ct);
    }
    public async Task EliminarAsync(Guid id, Guid grupo, string? version, CancellationToken ct)
    {
        await Asignada(id, ct);
        await informes.ConCierreAsync(id, async () =>
        {
            await Abierto(id, ct); var d = await Exigir(id, grupo, version, ct);
            if (d.Grupo.Congelado) throw new ConflictoGrupoException("La composición está congelada por el primer análisis guardado.");
            await grupos.EliminarAsync(d.Grupo, ct); return true;
        }, ct);
    }
    public async Task<AnalisisGrupoRespuesta> AnalizarAsync(Guid id, Guid grupo, string? version, CancellationToken ct)
    {
        var s = await Asignada(id, ct); var p = await parametros.ObtenerVigentesAsync(ct);
        // Consulta Catálogo fuera del bloqueo; revalida la versión dentro de la transacción.
        var d = await Exigir(id, grupo, version, ct);
        var entrada = await preparar.ConstruirAsync(s, d.Grupo, p, ct);
        var r = motor.Ejecutar(entrada.Datos, p);
        return await informes.ConCierreAsync(id, async () =>
        {
            await Abierto(id, ct); var actual = await Exigir(id, grupo, version, ct);
            var guardar = actual.Grupo.Aportes.All(a => a.HorasAportadas is not null && a.CreditosAportados is not null);
            if (guardar)
            {
                actual.Grupo.Congelar();
                var a = new AnalisisAgrupacionGuardado(id, grupo, actual.Grupo.Origenes, r, reloj.GetUtcNow());
                await grupos.GuardarAsync(actual.Grupo, a, ct); actual = new(actual.Grupo, a);
            }
            return new AnalisisGrupoRespuesta(guardar, r, actual, entrada.Advertencias);
        }, ct);
    }
    private async Task Abierto(Guid id, CancellationToken ct)
    { if (await informes.ObtenerAsync(id, ct) is not null) throw new InformeYaGeneradoException(); }
    private async Task<DetalleGrupo> Exigir(Guid id, Guid grupo, string? version, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(version)) throw new VersionGrupoRequeridaException();
        var d = await grupos.ObtenerAsync(id, grupo, ct) ?? throw new GrupoNoEncontradoException();
        if (version != ETag(d.Grupo)) throw new ConflictoGrupoException("Versión obsoleta: recargue la agrupación antes de reintentar.");
        return d;
    }
    public async Task<OpcionesAgrupacion> OpcionesAsync(Guid id, CancellationToken ct)
    {
        var s = await Asignada(id, ct); var destinos = await receptoras.ListarAsync(s.CarreraReceptoraId, ct);
        var rs = new List<ReceptoraOpcion>(); var os = new List<OrigenOpcion>(); var vs = new List<VinculoOpcion>();
        foreach (var r in destinos)
        { var p = await catalogo.ObtenerAsignaturaAsync(r.Id, ct) ?? throw new FuenteAnalisisNoDisponibleException(); rs.Add(new(r.Id, r.Codigo, r.Nombre, r.Nivel, p.Horas, p.Creditos)); }
        var parametrosActuales = await parametros.ObtenerVigentesAsync(ct);
        var hoy = DateOnly.FromDateTime(reloj.GetUtcNow().ToOffset(TimeSpan.FromHours(-5)).DateTime);
        foreach (var o in s.AsignaturasAprobadas)
        {
            var motivos = new List<string>(); bool? elegible = null;
            if (o.Estado != "Aprobada" || o.IntentoMatricula >= 3) { elegible = false; motivos.Add("Excluida por aprobación o tercera matrícula."); }
            else if (o.IntentoMatricula is null || o.FechaAprobacion == default || o.FechaAprobacion > hoy) motivos.Add("Información incompleta de matrícula o fecha.");
            else { elegible = o.FechaAprobacion >= hoy.AddYears(-parametrosActuales.AntiguedadMaximaAnios); if (elegible == false) motivos.Add("Antigüedad: requiere validación de conocimientos."); }
            os.Add(new(o.Id, o.Horas, o.Creditos, o.Estado, elegible, motivos));
            foreach (var r in rs)
            {
                var revision = o.AsignaturaOrigenId is { } origen ? await catalogo.ObtenerRevisionAsync(origen, r.Id, ct) : null;
                var habilitado = revision is { Registrada: true, Vigente: true } && elegible != false;
                vs.Add(new(o.Id, r.Id, habilitado, habilitado ? "Revisión vigente." : "Falta revisión vigente o el origen está excluido."));
            }
        }
        return new(os, rs.OrderBy(r => r.Nivel).ThenBy(r => r.Codigo).ToArray(), vs);
    }
}
