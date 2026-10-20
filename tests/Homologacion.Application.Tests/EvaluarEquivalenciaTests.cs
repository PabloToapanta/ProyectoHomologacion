using Homologacion.Application.Equivalencias;
using Homologacion.Domain.Equivalencias;
using Xunit;

namespace Homologacion.Application.Tests;

public class EvaluarEquivalenciaTests
{
    private sealed class Umbral(decimal valor) : IUmbralEquivalencia { public decimal Valor => valor; }
    private static string[] Ids(string prefijo, int n) => Enumerable.Range(1, n).Select(i => prefijo + i).ToArray();
    private static DatosAsignatura Asignatura(string id, decimal horas, decimal creditos) =>
        new(id, $"plan-sintetico-{id}-v1", horas, "hora-reloj", creditos, "sistema-sintetico-A", Ids("C", 10), Ids("R", 5));
    private static CorrespondenciaAcademica Correspondencia(string id, string elemento) =>
        new(id, elemento, elemento, new("unidad-sintetica/" + elemento, "unidad-sintetica/" + elemento));
    private static SolicitudEquivalencia Completa(int contenidos = 8, int resultados = 4) => new(
        [new(Asignatura("O1", 80, 8), true)], Asignatura("D", 100, 10),
        Ids("C", contenidos).Select(c => Correspondencia("O1", c)).ToArray(),
        Ids("R", resultados).Select(r => Correspondencia("O1", r)).ToArray());
    private static EvaluacionEquivalencia Ejecutar(SolicitudEquivalencia solicitud, decimal umbral = 80)
        => new EvaluarEquivalencia(new Umbral(umbral)).Ejecutar(solicitud);

    [Fact]
    public void Tres_criterios_al_ochenta_con_evidencia_son_homologables()
    {
        var r = Ejecutar(Completa());
        Assert.Equal(80, r.CompatibilidadAcademicaPct);
        Assert.Equal(80, r.CompatibilidadHorariaPct);
        Assert.Equal(80, r.CompatibilidadCreditosPct);
        Assert.Equal(EstadoEquivalencia.Homologable, r.Estado);
        Assert.True(r.Homologable);
        Assert.Equal(new[] { "C9", "C10" }, r.ContenidosSinCobertura);
        Assert.Equal(new[] { "R5" }, r.ResultadosAprendizajeSinCobertura);
        Assert.Empty(r.InformacionFaltante);
        Assert.Contains("tres criterios", r.Justificacion);
    }

    [Fact]
    public void Siete_contenidos_y_cuatro_resultados_no_alcanzan_ochenta()
    {
        var r = Ejecutar(Completa(7, 4));
        Assert.Equal(11m / 15 * 100, r.CompatibilidadAcademicaPct);
        Assert.Equal(EstadoEquivalencia.NoHomologable, r.Estado);
        Assert.False(r.Homologable);
        Assert.Contains("académica", r.Justificacion);
    }

    [Fact]
    public void Umbral_configurable_ochenta_y_cinco_no_se_compensa_con_otros_criterios()
    {
        var r = Ejecutar(Completa(), 85);
        Assert.Equal(85, r.UmbralPct);
        Assert.Equal(EstadoEquivalencia.NoHomologable, r.Estado);
    }

    [Fact]
    public void Varios_origenes_unen_cobertura_sin_duplicar_y_no_suman_horas_ni_creditos()
    {
        var solicitud = Completa() with
        {
            Origenes = [new(Asignatura("O1", 50, 5), true), new(Asignatura("O2", 50, 5), true)],
            CorrespondenciasContenidos = Ids("C", 8).Select((c, i) => Correspondencia(i < 4 ? "O1" : "O2", c))
                .Append(Correspondencia("O2", "C1")).ToArray(),
            CorrespondenciasResultadosAprendizaje = Ids("R", 4).Select((r, i) => Correspondencia(i < 2 ? "O1" : "O2", r))
                .Append(Correspondencia("O2", "R1")).ToArray()
        };
        var r = Ejecutar(solicitud);
        Assert.Equal(80, r.CompatibilidadAcademicaPct);
        Assert.Null(r.CompatibilidadHorariaPct);
        Assert.Null(r.CompatibilidadCreditosPct);
        Assert.Equal(EstadoEquivalencia.InformacionIncompleta, r.Estado);
        Assert.False(r.Homologable);
        Assert.Contains("agregación", r.Justificacion);
    }

    [Theory]
    [InlineData("origen")] [InlineData("contenidoOrigen")] [InlineData("contenidoReceptor")]
    [InlineData("resultadoOrigen")] [InlineData("resultadoReceptor")]
    public void Identificadores_inexistentes_se_rechazan(string tipo)
    {
        var solicitud = Completa();
        var c = Correspondencia("O1", "C1");
        var r = Correspondencia("O1", "R1");
        solicitud = tipo switch
        {
            "origen" => solicitud with { CorrespondenciasContenidos = [c with { AsignaturaOrigen = "otro" }] },
            "contenidoOrigen" => solicitud with { CorrespondenciasContenidos = [c with { ElementoOrigen = "otro" }] },
            "contenidoReceptor" => solicitud with { CorrespondenciasContenidos = [c with { ElementoReceptor = "otro" }] },
            "resultadoOrigen" => solicitud with { CorrespondenciasResultadosAprendizaje = [r with { ElementoOrigen = "otro" }] },
            _ => solicitud with { CorrespondenciasResultadosAprendizaje = [r with { ElementoReceptor = "otro" }] }
        };
        Assert.Throws<DatosEvaluacionInvalidosException>(() => Ejecutar(solicitud));
    }

    [Fact]
    public void Correspondencia_sin_evidencia_no_cubre_elemento_ni_homologa()
    {
        var solicitud = Completa() with { CorrespondenciasContenidos = [Correspondencia("O1", "C1") with { Evidencia = null }] };
        var r = Ejecutar(solicitud);
        Assert.Null(r.CompatibilidadAcademicaPct);
        Assert.Contains("C1", r.ContenidosSinCobertura);
        Assert.Equal(EstadoEquivalencia.InformacionIncompleta, r.Estado);
        Assert.Contains("evidencia", r.Justificacion);
    }

    [Theory]
    [InlineData("plan")] [InlineData("contenidos")] [InlineData("resultados")]
    [InlineData("id")] [InlineData("horas")] [InlineData("creditos")]
    [InlineData("unidadHoras")] [InlineData("sistemaCreditos")]
    public void Informacion_obligatoria_ausente_impide_homologacion(string campo)
    {
        var solicitud = Completa();
        var origen = solicitud.Origenes![0]!;
        var datos = origen.Asignatura!;
        datos = campo switch
        {
            "plan" => datos with { PlanAnalitico = null }, "contenidos" => datos with { Contenidos = null },
            "resultados" => datos with { ResultadosAprendizaje = null }, "id" => datos with { Id = null },
            "horas" => datos with { Horas = null }, "creditos" => datos with { Creditos = null },
            "unidadHoras" => datos with { UnidadHoras = null }, _ => datos with { SistemaCreditos = null }
        };
        // No se envían correspondencias que referencien inventarios/identificadores ausentes.
        var r = Ejecutar(solicitud with { Origenes = [origen with { Asignatura = datos }],
            CorrespondenciasContenidos = [], CorrespondenciasResultadosAprendizaje = [] });
        Assert.False(r.Homologable);
        Assert.Equal(EstadoEquivalencia.InformacionIncompleta, r.Estado);
        Assert.NotEmpty(r.InformacionFaltante);
    }

    [Theory]
    [InlineData(false)] [InlineData(null)]
    public void No_se_asume_que_una_asignatura_este_aprobada(bool? aprobada)
    {
        var solicitud = Completa();
        var origen = solicitud.Origenes![0]! with { Aprobada = aprobada };
        var r = Ejecutar(solicitud with { Origenes = [origen] });
        Assert.Equal(EstadoEquivalencia.InformacionIncompleta, r.Estado);
        Assert.False(r.Homologable);
        Assert.Contains("aprobación", r.Justificacion);
    }

    [Fact]
    public void Unidades_no_comparables_no_se_convierten_ni_se_homologan()
    {
        var solicitud = Completa();
        var origen = solicitud.Origenes![0]!;
        var r = Ejecutar(solicitud with { Origenes = [origen with { Asignatura = origen.Asignatura! with
            { UnidadHoras = "hora-academica", SistemaCreditos = "sistema-externo" } }] });
        Assert.Equal(80, r.CompatibilidadAcademicaPct);
        Assert.Null(r.CompatibilidadHorariaPct);
        Assert.Null(r.CompatibilidadCreditosPct);
        Assert.False(r.Homologable);
    }

    [Fact]
    public void Listas_vacias_conocidas_declaran_cero_cobertura_y_no_informacion_incompleta()
    {
        var r = Ejecutar(Completa() with { CorrespondenciasContenidos = [], CorrespondenciasResultadosAprendizaje = [] });
        Assert.Equal(0, r.CompatibilidadAcademicaPct);
        Assert.Equal(EstadoEquivalencia.NoHomologable, r.Estado);
    }

    [Fact]
    public void Solicitud_sin_origenes_o_sin_elementos_receptores_es_incompleta()
    {
        Assert.Equal(EstadoEquivalencia.InformacionIncompleta, Ejecutar(new(null, null, null, null)).Estado);
        var solicitud = Completa();
        var r = Ejecutar(solicitud with { Receptora = solicitud.Receptora! with { Contenidos = [], ResultadosAprendizaje = [] },
            CorrespondenciasContenidos = [], CorrespondenciasResultadosAprendizaje = [] });
        Assert.Null(r.CompatibilidadAcademicaPct);
        Assert.False(r.Homologable);
    }

    [Theory]
    [InlineData(0)] [InlineData(-1)]
    public void Cantidades_invalidas_son_errores_de_entrada(decimal cantidad)
    {
        var solicitud = Completa();
        Assert.Throws<DatosEvaluacionInvalidosException>(() => Ejecutar(solicitud with
        { Receptora = solicitud.Receptora! with { Horas = cantidad } }));
        Assert.Throws<DatosEvaluacionInvalidosException>(() => Ejecutar(solicitud with
        { Receptora = solicitud.Receptora! with { Creditos = cantidad } }));
    }

    [Fact]
    public void Origenes_y_elementos_duplicados_en_los_inventarios_son_invalidos()
    {
        var solicitud = Completa();
        Assert.Throws<DatosEvaluacionInvalidosException>(() => Ejecutar(solicitud with
        { Origenes = [solicitud.Origenes![0], solicitud.Origenes[0]] }));
        Assert.Throws<DatosEvaluacionInvalidosException>(() => Ejecutar(solicitud with
        { Receptora = solicitud.Receptora! with { Contenidos = ["C1", "C1"] } }));
        Assert.Throws<DatosEvaluacionInvalidosException>(() => Ejecutar(solicitud with
        { Receptora = solicitud.Receptora! with { ResultadosAprendizaje = [null] } }));
    }
}
