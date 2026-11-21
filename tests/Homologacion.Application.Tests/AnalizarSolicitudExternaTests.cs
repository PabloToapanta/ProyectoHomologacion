using System.Text.Json;
using Homologacion.Application.AnalisisExterno;
using Homologacion.Application.AnalisisInterno;
using Homologacion.Application.Equivalencias;
using Homologacion.Domain.Antiguedad;
using Xunit;

namespace Homologacion.Application.Tests;

internal sealed class EscenarioExterno : ISolicitudesAnalisisExterno, ICatalogoAnalisisInterno
{
    public static readonly Guid SolicitudId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    public static readonly Guid AprobadaId = Guid.Parse("20000000-0000-0000-0000-000000000002");
    public static readonly Guid OtraAprobadaId = Guid.Parse("20000000-0000-0000-0000-000000000003");
    public static readonly Guid ReceptoraId = Guid.Parse("20000000-0000-0000-0000-000000000004");
    public static readonly Guid CarreraReceptora = Guid.Parse("20000000-0000-0000-0000-000000000005");
    private static readonly Guid OtraCarrera = Guid.Parse("20000000-0000-0000-0000-000000000006");
    public static readonly Guid AjenaId = Guid.Parse("20000000-0000-0000-0000-000000000007");
    public SolicitudExternaParaAnalisis? Solicitud { get; set; } = new(SolicitudId, "Externo", CarreraReceptora,
        "Universidad sintética Ecuador", "Carrera externa sintética", 3, 0,
        [new(AprobadaId, "Programación externa sintética", 80, "hora-reloj", 8, "sistema-sintetico", "programa-externo-sintetico", "Aprobada", new DateOnly(2025, 1, 1)),
         new(OtraAprobadaId, "Cálculo externo sintético", 90, "hora-reloj", 9, "sistema-sintetico", "programa-externo-sintetico-2", "Aprobada", new DateOnly(2025, 1, 1))]);
    public Dictionary<Guid, AsignaturaParaAnalisis> Materias { get; } = new()
    {
        [ReceptoraId] = new(ReceptoraId, CarreraReceptora, "FISEI", true, 10, 100, "plan-sintetico-R", "hora-reloj", "sistema-sintetico", true,
            [new(Guid.Parse("20000000-0000-0000-0000-000000000008"), "Contenido receptor sintetico")],
            [new(Guid.Parse("20000000-0000-0000-0000-000000000009"), "Resultado receptor sintetico")]),
        [AjenaId] = new(AjenaId, OtraCarrera, "FISEI", true, 10, 100, "plan-sintetico-A", "hora-reloj", "sistema-sintetico", true, [], [])
    };
    public Task<SolicitudExternaParaAnalisis?> ObtenerSolicitudExternaAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(id == SolicitudId ? Solicitud : null);
    public Task<AsignaturaParaAnalisis?> ObtenerAsignaturaAsync(Guid id, CancellationToken ct) => Task.FromResult(Materias.GetValueOrDefault(id));
    // HU-19 no consulta correspondencias: no existen contenidos externos registrados (D2).
    public Task<RevisionParaAnalisis> ObtenerRevisionAsync(Guid origen, Guid receptora, CancellationToken ct) => throw new InvalidOperationException("No debe consultarse.");
    public void Aprobada(Func<AprobadaExternaParaAnalisis, AprobadaExternaParaAnalisis> cambio) =>
        Solicitud = Solicitud! with { AsignaturasAprobadas = [cambio(Solicitud.AsignaturasAprobadas[0]), Solicitud.AsignaturasAprobadas[1]] };
}

public class AnalizarSolicitudExternaTests
{
    private static AnalizarSolicitudExterna Caso(EscenarioExterno e) => new(e, e, new EvaluarEquivalencia(new UmbralAnalisis()), new RelojAnalisis());
    private static Task<ResultadoInternoSolicitud> Ejecutar(EscenarioExterno e, Guid? receptora = null) =>
        Caso(e).EjecutarAsync(EscenarioExterno.SolicitudId, EscenarioExterno.AprobadaId, receptora ?? EscenarioExterno.ReceptoraId, default);

    [Fact]
    public async Task Unidades_identicas_comparan_horas_y_creditos_pero_el_academico_queda_incompleto()
    {
        var r = await Ejecutar(new EscenarioExterno());
        Assert.Equal("Incompleto", r.Estado);
        Assert.Null(r.Resultado);
        Assert.Null(r.Evaluacion.CompatibilidadAcademicaPct);
        Assert.Equal(80, r.Evaluacion.CompatibilidadHorariaPct);
        Assert.Equal(80, r.Evaluacion.CompatibilidadCreditosPct);
        Assert.False(r.Evaluacion.Homologable);
        Assert.Contains(r.Evaluacion.InformacionFaltante, f => f.Contains("programa externo", StringComparison.Ordinal));
        Assert.Equal(EstadoAntiguedad.Elegible, r.Antiguedad!.Estado);
    }

    [Fact]
    public async Task Unidades_con_espacios_y_mayusculas_distintas_se_consideran_iguales()
    {
        var e = new EscenarioExterno();
        e.Aprobada(a => a with { UnidadHoras = "  HORA-RELOJ ", SistemaCreditos = "Sistema-Sintetico" });
        var r = await Ejecutar(e);
        Assert.Equal(80, r.Evaluacion.CompatibilidadHorariaPct);
        Assert.Equal(80, r.Evaluacion.CompatibilidadCreditosPct);
    }

    [Theory]
    [InlineData("hora-academica", "sistema-sintetico", true)]
    [InlineData("hora-reloj", "ECTS", false)]
    [InlineData(null, "sistema-sintetico", true)]
    [InlineData("hora-reloj", " ", false)]
    public async Task Unidades_distintas_o_ausentes_no_se_convierten(string? unidadHoras, string? sistemaCreditos, bool fallanHoras)
    {
        var e = new EscenarioExterno();
        e.Aprobada(a => a with { UnidadHoras = unidadHoras, SistemaCreditos = sistemaCreditos });
        var r = await Ejecutar(e);
        Assert.Equal("Incompleto", r.Estado);
        if (fallanHoras) { Assert.Null(r.Evaluacion.CompatibilidadHorariaPct); Assert.Equal(80, r.Evaluacion.CompatibilidadCreditosPct); }
        else { Assert.Null(r.Evaluacion.CompatibilidadCreditosPct); Assert.Equal(80, r.Evaluacion.CompatibilidadHorariaPct); }
        Assert.Contains(r.Evaluacion.InformacionFaltante, f => f.Contains("sin conversión aprobada", StringComparison.Ordinal)
            && f.Contains(fallanHoras ? "horas" : "créditos", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Horas_superiores_a_la_receptora_quedan_acotadas_a_100()
    {
        var e = new EscenarioExterno();
        e.Aprobada(a => a with { Horas = 400, Creditos = 40 });
        var r = await Ejecutar(e);
        Assert.Equal(100, r.Evaluacion.CompatibilidadHorariaPct);
        Assert.Equal(100, r.Evaluacion.CompatibilidadCreditosPct);
    }

    [Fact]
    public async Task Reprobada_o_antigua_se_informan_sin_resultado()
    {
        var e = new EscenarioExterno();
        e.Aprobada(a => a with { Estado = "Reprobada" });
        var reprobada = await Ejecutar(e);
        Assert.Equal(EstadoAntiguedad.NoElegible, reprobada.Antiguedad!.Estado);
        Assert.Contains(reprobada.Evaluacion.InformacionFaltante, f => f.Contains("no consta como aprobada", StringComparison.Ordinal));
        e = new EscenarioExterno();
        e.Aprobada(a => a with { FechaAprobacion = new DateOnly(2015, 1, 1) });
        var antigua = await Ejecutar(e);
        Assert.Equal(EstadoAntiguedad.RequiereValidacionConocimientos, antigua.Antiguedad!.Estado);
        Assert.Null(antigua.Resultado);
    }

    [Fact]
    public async Task No_exige_apostilla_ni_tipo_de_institucion()
    {
        var r = await Ejecutar(new EscenarioExterno());
        var texto = JsonSerializer.Serialize(r);
        Assert.DoesNotContain("apostill", texto, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("extranjer", texto, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Solicitud_interna_receptora_ajena_y_aprobada_ajena_se_rechazan()
    {
        var e = new EscenarioExterno();
        await Assert.ThrowsAsync<DatosAnalisisSolicitudException>(() => Ejecutar(e, EscenarioExterno.AjenaId));
        await Assert.ThrowsAsync<DatosAnalisisSolicitudException>(() => Ejecutar(e, Guid.NewGuid()));
        await Assert.ThrowsAsync<DatosAnalisisSolicitudException>(() =>
            Caso(e).EjecutarAsync(EscenarioExterno.SolicitudId, Guid.NewGuid(), EscenarioExterno.ReceptoraId, default));
        e.Solicitud = e.Solicitud! with { TipoCambio = "Interno" };
        await Assert.ThrowsAsync<DatosAnalisisSolicitudException>(() => Ejecutar(e));
    }

    [Fact]
    public async Task Solicitud_no_asignada_o_fuente_malformada()
    {
        var e = new EscenarioExterno { Solicitud = null };
        await Assert.ThrowsAsync<SolicitudAnalisisNoEncontradaException>(() => Ejecutar(e));
        e = new EscenarioExterno();
        e.Solicitud = e.Solicitud! with { AsignaturasAprobadas = [null!] };
        await Assert.ThrowsAsync<FuenteAnalisisNoDisponibleException>(() => Ejecutar(e));
        e = new EscenarioExterno();
        e.Aprobada(a => a with { Estado = "0" });
        await Assert.ThrowsAsync<FuenteAnalisisNoDisponibleException>(() => Ejecutar(e));
    }
}

public class GestionarAnalisisExternoTests
{
    private sealed class Almacen : IResultadosAnalisisExterno
    {
        public bool Fallar { get; set; }
        public List<AnalisisGuardado> Datos { get; } = [];
        public Task GuardarAsync(AnalisisGuardado r, CancellationToken ct)
        {
            if (Fallar) throw new AlmacenAnalisisNoDisponibleException();
            Datos.RemoveAll(x => x.Analisis.SolicitudId == r.Analisis.SolicitudId && x.Analisis.AsignaturaAprobadaId == r.Analisis.AsignaturaAprobadaId
                && x.Analisis.AsignaturaReceptoraId == r.Analisis.AsignaturaReceptoraId);
            Datos.Add(r); return Task.CompletedTask;
        }
        public Task<IReadOnlyList<AnalisisGuardado>> ListarAsync(Guid id, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<AnalisisGuardado>>(Datos.Where(x => x.Analisis.SolicitudId == id).ToArray());
    }
    private static GestionarAnalisisExterno Caso(EscenarioExterno e, Almacen a) =>
        new(new(e, e, new EvaluarEquivalencia(new UmbralAnalisis()), new RelojAnalisis()), e, a, new RelojAnalisis());
    private static AnalisisGuardado Guardado(Guid aprobada, string estado, string? sugerencia) => new(DateTimeOffset.UnixEpoch,
        new(EscenarioExterno.SolicitudId, aprobada, Guid.NewGuid(), estado, "sintético",
            sugerencia is null ? null : new(100, 100, 100, sugerencia, "sintético", []),
            new([], null, null, null, null, 80, Domain.Equivalencias.EstadoEquivalencia.InformacionIncompleta, "", [], [], []), null, true, []));

    [Fact]
    public async Task Guarda_y_recupera_el_analisis_externo()
    {
        var e = new EscenarioExterno(); var a = new Almacen(); var caso = Caso(e, a);
        await caso.EjecutarAsync(EscenarioExterno.SolicitudId, EscenarioExterno.AprobadaId, EscenarioExterno.ReceptoraId, default);
        await caso.EjecutarAsync(EscenarioExterno.SolicitudId, EscenarioExterno.AprobadaId, EscenarioExterno.ReceptoraId, default);
        var r = Assert.Single(await caso.ConsultarAsync(EscenarioExterno.SolicitudId, default));
        Assert.Equal("Incompleto", r.Analisis.Estado);
        Assert.Null(r.Analisis.Resultado);
    }

    [Fact]
    public async Task Fallo_de_almacen_y_perdida_de_asignacion()
    {
        var e = new EscenarioExterno(); var a = new Almacen { Fallar = true };
        await Assert.ThrowsAsync<AlmacenAnalisisNoDisponibleException>(() =>
            Caso(e, a).EjecutarAsync(EscenarioExterno.SolicitudId, EscenarioExterno.AprobadaId, EscenarioExterno.ReceptoraId, default));
        Assert.Empty(a.Datos);
        e.Solicitud = null;
        await Assert.ThrowsAsync<SolicitudAnalisisNoEncontradaException>(() => Caso(e, new Almacen()).ConsultarAsync(EscenarioExterno.SolicitudId, default));
        await Assert.ThrowsAsync<SolicitudAnalisisNoEncontradaException>(() => Caso(e, new Almacen()).RequisitosAsync(EscenarioExterno.SolicitudId, default));
    }

    [Fact]
    public async Task Sin_datos_ni_analisis_completos_el_resultado_es_incompleto_no_incumplimiento()
    {
        var e = new EscenarioExterno(); e.Solicitud = e.Solicitud! with { PeriodosCursados = null, CambiosPreviosCarrera = null };
        var a = new Almacen(); var caso = Caso(e, a);
        await caso.EjecutarAsync(EscenarioExterno.SolicitudId, EscenarioExterno.AprobadaId, EscenarioExterno.ReceptoraId, default);
        var r = await caso.RequisitosAsync(EscenarioExterno.SolicitudId, default);
        Assert.Equal("Incompleto", r.Estado);
        Assert.Empty(r.Incumplimientos);
        Assert.Equal(3, r.Faltantes.Count);
        Assert.Equal(0, r.AsignaturasHomologables);
        Assert.Contains(r.Faltantes, f => f.Contains("0 de 2", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(1, 0, "Se requieren al menos 2 períodos cursados.")]
    [InlineData(2, 1, "El cambio externo requiere no tener cambios previos de carrera.")]
    public async Task Dato_declarado_por_debajo_del_minimo_incumple_aunque_falten_analisis(int periodos, int cambios, string incumplimiento)
    {
        var e = new EscenarioExterno(); e.Solicitud = e.Solicitud! with { PeriodosCursados = periodos, CambiosPreviosCarrera = cambios };
        var r = await Caso(e, new Almacen()).RequisitosAsync(EscenarioExterno.SolicitudId, default);
        Assert.Equal("NoCumple", r.Estado);
        Assert.Equal([incumplimiento], r.Incumplimientos);
        Assert.NotEmpty(r.Faltantes);
    }

    [Fact]
    public async Task Cumple_con_dos_homologables_de_analisis_completos_y_datos_declarados()
    {
        var e = new EscenarioExterno(); var a = new Almacen();
        a.Datos.Add(Guardado(EscenarioExterno.AprobadaId, "Completo", "Homologable"));
        a.Datos.Add(Guardado(EscenarioExterno.OtraAprobadaId, "Completo", "Homologable"));
        a.Datos.Add(Guardado(Guid.NewGuid(), "Completo", "Homologable"));
        var r = await Caso(e, a).RequisitosAsync(EscenarioExterno.SolicitudId, default);
        Assert.Equal("Cumple", r.Estado);
        Assert.Equal(2, r.AsignaturasHomologables);
        Assert.Empty(r.Faltantes);
        Assert.Empty(r.Incumplimientos);
    }

    [Fact]
    public async Task Todas_analizadas_con_una_homologable_incumple()
    {
        var e = new EscenarioExterno(); var a = new Almacen();
        a.Datos.Add(Guardado(EscenarioExterno.AprobadaId, "Completo", "Homologable"));
        a.Datos.Add(Guardado(EscenarioExterno.OtraAprobadaId, "Completo", "No homologable"));
        var r = await Caso(e, a).RequisitosAsync(EscenarioExterno.SolicitudId, default);
        Assert.Equal("NoCumple", r.Estado);
        Assert.Equal(["Se requieren al menos 2 asignaturas homologables."], r.Incumplimientos);
    }

    [Fact]
    public async Task Requisitos_externos_rechazan_solicitud_interna()
    {
        var e = new EscenarioExterno(); e.Solicitud = e.Solicitud! with { TipoCambio = "Interno" };
        await Assert.ThrowsAsync<DatosAnalisisSolicitudException>(() => Caso(e, new Almacen()).RequisitosAsync(EscenarioExterno.SolicitudId, default));
    }
}
