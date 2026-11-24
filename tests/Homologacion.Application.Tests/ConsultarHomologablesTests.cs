using Homologacion.Application.AnalisisInterno;
using Homologacion.Application.ConsultaHomologables;
using Homologacion.Application.Equivalencias;
using Xunit;

namespace Homologacion.Application.Tests;

/// <summary>HU-09: consulta de homologables entre carreras FISEI con el motor determinista.</summary>
public class ConsultarHomologablesTests
{
    private static readonly Guid CarreraO = Guid.Parse("09000000-0000-0000-0000-000000000001");
    private static readonly Guid CarreraR = Guid.Parse("09000000-0000-0000-0000-000000000002");
    private sealed class Umbral : IUmbralEquivalencia { public decimal Valor => 80; }
    private sealed class Catalogo : ICatalogoConsultaHomologables
    {
        public ContextoConsultaCatalogo? Contexto { get; set; }
        public int Lecturas { get; private set; }
        public Task<IReadOnlyList<CarreraConsulta>> CarrerasAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<CarreraConsulta>>([]);
        public Task<ContextoConsultaCatalogo?> ContextoAsync(Guid o, Guid r, CancellationToken ct) { Lecturas++; return Task.FromResult(Contexto); }
    }
    private static AsignaturaConsultaCatalogo Asignatura(Guid carrera, string codigo, int horas, int nivel = 1, string? plan = "plan-sintetico")
    {
        var id = Guid.NewGuid();
        var contexto = new AsignaturaParaAnalisis(id, carrera, "FISEI", true, 3, horas, plan, "hora-reloj", "sistema-sintetico", true,
            [new(Guid.Parse("09000000-0000-0000-0000-0000000000c" + codigo[^1]), "Contenido " + codigo)],
            [new(Guid.Parse("09000000-0000-0000-0000-0000000000a" + codigo[^1]), "Resultado " + codigo)]);
        return new(new(id, codigo, "Asignatura " + codigo, nivel), contexto);
    }
    private static ParConsultaCatalogo Par(AsignaturaConsultaCatalogo o, AsignaturaConsultaCatalogo r, bool vigente = true) =>
        new(o.Asignatura.Id, r.Asignatura.Id, new(true, vigente,
            [new(o.Contexto.Contenidos[0].Id.ToString(), r.Contexto.Contenidos[0].Id.ToString(), "plan-O/U1", "plan-R/U1")],
            [new(o.Contexto.Resultados[0].Id.ToString(), r.Contexto.Resultados[0].Id.ToString(), "plan-O/U1", "plan-R/U1")]));
    private static (ConsultarHomologables Caso, Catalogo Catalogo) Crear(params (AsignaturaConsultaCatalogo O, AsignaturaConsultaCatalogo R, bool Vigente)[] pares)
    {
        var catalogo = new Catalogo
        {
            Contexto = new(new(CarreraO, "Origen sintético", "FISEI", true), new(CarreraR, "Receptora sintética", "FISEI", true),
                pares.SelectMany(p => new[] { p.O, p.R }).DistinctBy(a => a.Asignatura.Id).ToArray(),
                pares.Select(p => Par(p.O, p.R, p.Vigente)).ToArray())
        };
        return (new(catalogo, new EvaluarEquivalencia(new Umbral())), catalogo);
    }

    [Fact]
    public async Task CA1_muestra_los_tres_porcentajes_y_el_veredicto_del_motor_por_par()
    {
        var o1 = Asignatura(CarreraO, "O1", 100); var r1 = Asignatura(CarreraR, "R1", 100);
        var o2 = Asignatura(CarreraO, "O2", 50, 2); var r2 = Asignatura(CarreraR, "R2", 100, 2);
        var (caso, _) = Crear((o1, r1, true), (o2, r2, true));
        var r = await caso.EjecutarAsync(CarreraO, CarreraR, default);
        Assert.Equal(2, r.Pares.Count);
        var completo = r.Pares[0];
        Assert.Equal(("O1", "R1", "Homologable"), (completo.Origen.Codigo, completo.Receptora.Codigo, completo.Estado));
        Assert.Equal((100m, 100m, 100m), (completo.PorcentajeAcademico!.Value, completo.PorcentajeHoras!.Value, completo.PorcentajeCreditos!.Value));
        Assert.Equal(80, completo.UmbralPct);
        // Horas al 50 %: el motor mantiene su veredicto.
        Assert.Equal(("NoHomologable", 50m), (r.Pares[1].Estado, r.Pares[1].PorcentajeHoras!.Value));
        Assert.Contains(r.Advertencias, a => a.Contains("no es una homologación", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CA2_revision_desactualizada_o_perfil_faltante_es_informacion_incompleta()
    {
        var o1 = Asignatura(CarreraO, "O1", 100); var r1 = Asignatura(CarreraR, "R1", 100);
        var o2 = Asignatura(CarreraO, "O2", 100, 2, plan: null); var r2 = Asignatura(CarreraR, "R2", 100, 2);
        var (caso, _) = Crear((o1, r1, false), (o2, r2, true));
        var r = await caso.EjecutarAsync(CarreraO, CarreraR, default);
        Assert.All(r.Pares, p => Assert.Equal("InformacionIncompleta", p.Estado));
        Assert.Equal([ConsultarHomologables.FaltaRevisionVigente], r.Pares[0].InformacionFaltante);
        Assert.Null(r.Pares[0].PorcentajeAcademico);
        Assert.NotEmpty(r.Pares[1].InformacionFaltante);
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000", "09000000-0000-0000-0000-000000000002")]
    [InlineData("09000000-0000-0000-0000-000000000001", "09000000-0000-0000-0000-000000000001")]
    public async Task Rechaza_carreras_vacias_o_iguales_sin_consultar_el_catalogo(string o, string r)
    {
        var (caso, catalogo) = Crear();
        await Assert.ThrowsAsync<DatosAnalisisSolicitudException>(() => caso.EjecutarAsync(Guid.Parse(o), Guid.Parse(r), default));
        Assert.Equal(0, catalogo.Lecturas);
    }

    [Fact]
    public async Task Solo_compara_carreras_FISEI()
    {
        var (caso, catalogo) = Crear();
        catalogo.Contexto = catalogo.Contexto! with { Origen = catalogo.Contexto.Origen with { Facultad = "Otra facultad" } };
        await Assert.ThrowsAsync<DatosAnalisisSolicitudException>(() => caso.EjecutarAsync(CarreraO, CarreraR, default));
    }

    [Fact]
    public async Task Carrera_inexistente_y_datos_inconsistentes()
    {
        var (caso, catalogo) = Crear();
        catalogo.Contexto = null;
        await Assert.ThrowsAsync<CarreraConsultaNoEncontradaException>(() => caso.EjecutarAsync(CarreraO, CarreraR, default));
        var o1 = Asignatura(CarreraR, "O1", 100); var r1 = Asignatura(CarreraR, "R1", 100);
        (caso, _) = Crear((o1, r1, true));
        await Assert.ThrowsAsync<FuenteAnalisisNoDisponibleException>(() => caso.EjecutarAsync(CarreraO, CarreraR, default));
    }

    [Fact]
    public async Task Sin_pares_revisados_devuelve_lista_vacia_sin_inferir()
    {
        var (caso, _) = Crear();
        Assert.Empty((await caso.EjecutarAsync(CarreraO, CarreraR, default)).Pares);
    }
}
