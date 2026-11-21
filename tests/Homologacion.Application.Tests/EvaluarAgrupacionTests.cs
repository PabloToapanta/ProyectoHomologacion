using Homologacion.Application.Equivalencias;
using Homologacion.Domain.Configuracion;
using Homologacion.Domain.Equivalencias;
using Xunit;

namespace Homologacion.Application.Tests;

public sealed class EvaluarAgrupacionTests
{
    private sealed class Umbral(decimal valor) : IUmbralEquivalencia { public decimal Valor => valor; }
    private static DatosAsignatura Materia(string id, decimal horas, decimal creditos, string[]? contenidos = null,
        string[]? resultados = null, string unidad = "hora-reloj", string sistema = "UTA") =>
        new(id, "plan-" + id, horas, unidad, creditos, sistema, contenidos ?? ["C1"], resultados ?? ["R1"]);
    private static CorrespondenciaAcademica Vinculo(string origen, string elementoOrigen, string elementoReceptor) =>
        new(origen, elementoOrigen, elementoReceptor, new("origen/" + elementoOrigen, "receptora/" + elementoReceptor));
    private static AporteAgrupacion Aporte(string origen, string receptora, decimal? horas, decimal? creditos,
        string contenidoOrigen = "C1", string contenidoReceptor = "C1") =>
        new(origen, receptora, horas, creditos,
            [Vinculo(origen, contenidoOrigen, contenidoReceptor)], [Vinculo(origen, "R1", "R1")]);
    private static ResultadoAgrupacion Evaluar(SolicitudAgrupacion solicitud, decimal umbral = 80) =>
        new EvaluarAgrupacion(new Umbral(umbral)).Ejecutar(solicitud);

    [Fact]
    public void Una_materia_puede_repartirse_en_dos_receptoras_sin_duplicar_horas_ni_creditos()
    {
        var entrada = new SolicitudAgrupacion(
            [new(Materia("O1", 100, 4, ["C1", "C2"]), true)],
            [Materia("D1", 60, 2.5m), Materia("D2", 40, 1.5m, ["C2"])],
            [Aporte("O1", "D1", 60, 2.5m), Aporte("O1", "D2", 40, 1.5m, "C2", "C2")]);

        var resultado = Evaluar(entrada);

        Assert.Equal(EstadoEquivalencia.Homologable, resultado.Estado);
        Assert.Equal(2, resultado.Receptoras.Count);
        Assert.All(resultado.Receptoras, r =>
        {
            Assert.Equal(100, r.CompatibilidadAcademicaPct);
            Assert.Equal(100, r.CompatibilidadHorariaPct);
            Assert.Equal(100, r.CompatibilidadCreditosPct);
        });
    }

    [Fact]
    public void Nm_cuenta_cada_elemento_receptor_una_vez_y_permite_evidencia_del_mismo_origen_en_dos_receptoras()
    {
        var entrada = new SolicitudAgrupacion(
            [new(Materia("O1", 50, 2), true), new(Materia("O2", 50, 2), true)],
            [Materia("D1", 60, 2.4m), Materia("D2", 40, 1.6m)],
            [Aporte("O1", "D1", 30, 1.2m), Aporte("O1", "D2", 20, 0.8m),
             Aporte("O2", "D1", 30, 1.2m), Aporte("O2", "D2", 20, 0.8m)]);

        var resultado = Evaluar(entrada);

        Assert.Equal(EstadoEquivalencia.Homologable, resultado.Estado);
        Assert.All(resultado.Receptoras, r => Assert.Equal(100, r.CompatibilidadAcademicaPct));
        Assert.Empty(resultado.InformacionFaltante);
        Assert.Equal(4, resultado.Aportes.Count);
        Assert.Equal(30, resultado.Aportes.Single(a => a.AsignaturaOrigenId == "O1" && a.AsignaturaReceptoraId == "D1").HorasAportadas);
        Assert.Contains(resultado.Evidencias, e => e.AsignaturaOrigenId == "O1" && e.AsignaturaReceptoraId == "D1"
            && e.Categoria == "Contenido" && e.ElementoOrigen == "C1" && e.ElementoReceptor == "C1");
        Assert.Contains(resultado.Evidencias, e => e.AsignaturaOrigenId == "O1" && e.AsignaturaReceptoraId == "D2"
            && e.Categoria == "Contenido" && e.ElementoOrigen == "C1" && e.ElementoReceptor == "C1");
    }

    [Fact]
    public void Una_receptora_bajo_el_umbral_hace_no_homologable_al_grupo_completo()
    {
        var entrada = new SolicitudAgrupacion(
            [new(Materia("O1", 100, 4), true)],
            [Materia("D1", 60, 2.5m), Materia("D2", 80, 1.5m)],
            [Aporte("O1", "D1", 60, 2.5m), Aporte("O1", "D2", 40, 1.5m)]);

        var resultado = Evaluar(entrada);

        Assert.Equal(EstadoEquivalencia.NoHomologable, resultado.Estado);
        Assert.Equal(EstadoEquivalencia.Homologable, resultado.Receptoras[0].Estado);
        Assert.Equal(EstadoEquivalencia.NoHomologable, resultado.Receptoras[1].Estado);
        Assert.Equal(50, resultado.Receptoras[1].CompatibilidadHorariaPct);
    }

    [Fact]
    public void Aporte_pendiente_o_unidades_distintas_dejan_el_grupo_incompleto()
    {
        var baseEntrada = new SolicitudAgrupacion(
            [new(Materia("O1", 100, 4), true)],
            [Materia("D1", 60, 2.5m), Materia("D2", 40, 1.5m)],
            [Aporte("O1", "D1", null, 2.5m), Aporte("O1", "D2", 40, 1.5m)]);

        var pendiente = Evaluar(baseEntrada);
        var unidades = Evaluar(baseEntrada with
        {
            Receptoras = [Materia("D1", 60, 2.5m, unidad: "hora-academica"), baseEntrada.Receptoras![1]],
            Aportes = [Aporte("O1", "D1", 60, 2.5m), Aporte("O1", "D2", 40, 1.5m)]
        });

        Assert.Equal(EstadoEquivalencia.InformacionIncompleta, pendiente.Estado);
        Assert.Contains(pendiente.InformacionFaltante, x => x.Contains("D1") && x.Contains("horas"));
        Assert.Equal(EstadoEquivalencia.InformacionIncompleta, unidades.Estado);
        Assert.Contains(unidades.InformacionFaltante, x => x.Contains("unidades"));
    }

    [Theory]
    [InlineData(60, 41, 2.5, 1.5)]
    [InlineData(60, 40, 2.5, 1.51)]
    public void No_permite_asignar_mas_de_lo_aprobado(decimal horas1, decimal horas2, decimal creditos1, decimal creditos2)
    {
        var entrada = new SolicitudAgrupacion(
            [new(Materia("O1", 100, 4), true)],
            [Materia("D1", 60, 2.5m), Materia("D2", 40, 1.5m)],
            [Aporte("O1", "D1", horas1, creditos1), Aporte("O1", "D2", horas2, creditos2)]);

        Assert.Throws<DatosEvaluacionInvalidosException>(() => Evaluar(entrada));
    }

    [Fact]
    public void Usa_fotografia_del_umbral_vigente()
    {
        var entrada = new SolicitudAgrupacion(
            [new(Materia("O1", 80, 8), true)],
            [Materia("D1", 100, 10, ["C1", "C2", "C3", "C4", "C5"], ["R1"])],
            [Aporte("O1", "D1", 80, 8)]);
        var parametros = new ParametrosHomologacion(Guid.NewGuid(), 85, 10, 1, 1, 2, 2);

        var resultado = new EvaluarAgrupacion(new Umbral(80)).Ejecutar(entrada, parametros);

        Assert.Equal(85, resultado.UmbralPct);
        Assert.Equal(parametros.VersionId, resultado.ConfiguracionVersionId);
        Assert.Equal(EstadoEquivalencia.NoHomologable, resultado.Estado);
    }

    [Theory]
    [InlineData(-0.01, 1)]
    [InlineData(0.001, 1)]
    [InlineData(1, -0.01)]
    [InlineData(1, 0.001)]
    public void Rechaza_aportes_negativos_o_con_mas_de_dos_decimales(decimal horas, decimal creditos)
    {
        var entrada = new SolicitudAgrupacion(
            [new(Materia("O1", 100, 4), true)], [Materia("D1", 50, 2)],
            [Aporte("O1", "D1", horas, creditos)]);

        Assert.Throws<DatosEvaluacionInvalidosException>(() => Evaluar(entrada));
    }

    [Fact]
    public void Cero_es_un_aporte_conocido_y_no_un_dato_faltante()
    {
        var entrada = new SolicitudAgrupacion(
            [new(Materia("O1", 100, 4), true)], [Materia("D1", 50, 2)],
            [Aporte("O1", "D1", 0, 0)]);

        var resultado = Evaluar(entrada);

        Assert.Equal(EstadoEquivalencia.NoHomologable, resultado.Estado);
        Assert.Equal(0, resultado.Receptoras[0].CompatibilidadHorariaPct);
        Assert.Equal(0, resultado.Receptoras[0].CompatibilidadCreditosPct);
    }

    [Fact]
    public void Sin_aprobacion_o_evidencia_no_infiere_cobertura()
    {
        var sinAprobacion = new SolicitudAgrupacion(
            [new(Materia("O1", 100, 4), false)], [Materia("D1", 50, 2)],
            [Aporte("O1", "D1", 50, 2)]);
        var sinEvidencia = sinAprobacion with
        {
            Origenes = [new(Materia("O1", 100, 4), true)],
            Aportes = [Aporte("O1", "D1", 50, 2) with
            { CorrespondenciasContenidos = [Vinculo("O1", "C1", "C1") with { Evidencia = null }] }]
        };

        Assert.Equal(EstadoEquivalencia.InformacionIncompleta, Evaluar(sinAprobacion).Estado);
        var resultado = Evaluar(sinEvidencia);
        Assert.Equal(EstadoEquivalencia.InformacionIncompleta, resultado.Estado);
        Assert.DoesNotContain(resultado.Evidencias, e => e.Categoria == "Contenido");
    }
}
