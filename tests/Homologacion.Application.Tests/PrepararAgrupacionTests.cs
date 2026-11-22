using Homologacion.Application.Agrupaciones;
using Homologacion.Application.AnalisisInterno;
using Homologacion.Application.Equivalencias;
using Homologacion.Domain.Agrupaciones;
using Homologacion.Domain.Configuracion;
using Xunit;
namespace Homologacion.Application.Tests;

public class PrepararAgrupacionTests
{
    [Theory]
    [InlineData(1, "Aprobada", 1, true)]
    [InlineData(3, "Aprobada", 1, false)]
    [InlineData(1, "Reprobada", 1, false)]
    [InlineData(1, "Aprobada", 11, false)]
    [InlineData(null, "Aprobada", 1, null)]
    public async Task Elegibilidad_individual_y_evidencia_no_se_inventan(int? intento, string estado, int edad, bool? esperado)
    {
        var c = new Catalogo(); var s = Guid.NewGuid(); var a = Guid.NewGuid();
        var solicitud = new SolicitudParaAnalisis(s, "Interno", c.CO, c.CR, [new(a, c.Origen, 4, 100, estado, DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-edad)) { IntentoMatricula = intento }]);
        var g = GrupoAgrupacion.Crear(s, [a], [c.R1, c.R2], [new(a, c.R1, 60, 2.5m), new(a, c.R2, 40, 1.5m)]);
        var entrada = await new PrepararAgrupacion(c, TimeProvider.System).ConstruirAsync(solicitud, g, ParametrosHomologacion.Predeterminados(), default);
        Assert.Equal(esperado, entrada.Datos.Origenes![0]!.Aprobada);
        var resultado = new EvaluarAgrupacion(new Umbral()).Ejecutar(entrada.Datos);
        if (esperado != true) { Assert.Empty(resultado.Evidencias); Assert.False(resultado.Homologable); Assert.NotEmpty(entrada.Advertencias); }
    }
    private sealed class Umbral : IUmbralEquivalencia { public decimal Valor => 80; }
    private sealed class Catalogo : ICatalogoAnalisisInterno
    {
        public Guid Origen = Guid.NewGuid(), O2 = Guid.NewGuid(), R1 = Guid.NewGuid(), R2 = Guid.NewGuid(), CO = Guid.NewGuid(), CR = Guid.NewGuid();
        public bool Vigente = true;
        public Task<AsignaturaParaAnalisis?> ObtenerAsignaturaAsync(Guid id, CancellationToken ct) => Task.FromResult<AsignaturaParaAnalisis?>(new(id, id == Origen || id == O2 ? CO : CR, "FISEI", true, 4, 100, "plan", "hora", "credito", true, [new(id, "tema")], [new(id, "resultado")]));
        public Task<RevisionParaAnalisis> ObtenerRevisionAsync(Guid o, Guid r, CancellationToken ct) => Task.FromResult(new RevisionParaAnalisis(true, Vigente, [new(o.ToString(), r.ToString(), "O/C", "R/C")], [new(o.ToString(), r.ToString(), "O/RA", "R/RA")]));
    }
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Solo_revision_vigente_aporta_evidencias(bool vigente)
    {
        var c = new Catalogo { Vigente = vigente }; var s = Guid.NewGuid(); var a = Guid.NewGuid();
        var solicitud = new SolicitudParaAnalisis(s, "Interno", c.CO, c.CR, [new(a, c.Origen, 4, 100, "Aprobada", DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-1))]);
        var g = GrupoAgrupacion.Crear(s, [a], [c.R1, c.R2], [new(a, c.R1, 60, 2.5m), new(a, c.R2, 40, 1.5m)]);
        var entrada = await new PrepararAgrupacion(c, TimeProvider.System).ConstruirAsync(solicitud, g, ParametrosHomologacion.Predeterminados(), default);
        Assert.All(entrada.Datos.Aportes!, x => { if (vigente) Assert.Single(x!.CorrespondenciasContenidos!); else Assert.Null(x!.CorrespondenciasContenidos); });
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Varias_parejas_y_nm_construidos_sin_suma_global_ni_doble_cobertura(bool nm)
    {
        var c = new Catalogo(); var s = Guid.NewGuid(); var a = Guid.NewGuid(); var b = Guid.NewGuid();
        var fecha = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-1);
        var solicitud = new SolicitudParaAnalisis(s, "Interno", c.CO, c.CR, [new(a, c.Origen, 4, 100, "Aprobada", fecha) { IntentoMatricula = 1 }, new(b, c.O2, 4, 100, "Aprobada", fecha) { IntentoMatricula = 1 }]);
        AporteGrupo[] aportes = nm ? [new(a, c.R1, 50, 2), new(a, c.R2, 50, 2), new(b, c.R1, 50, 2), new(b, c.R2, 50, 2)] : [new(a, c.R1, 100, 4), new(b, c.R2, 100, 4)];
        var g = GrupoAgrupacion.Crear(s, [a, b], [c.R1, c.R2], aportes);
        var entrada = await new PrepararAgrupacion(c, TimeProvider.System).ConstruirAsync(solicitud, g, ParametrosHomologacion.Predeterminados(), default);
        var r = new EvaluarAgrupacion(new Umbral()).Ejecutar(entrada.Datos);
        Assert.True(r.Homologable); Assert.Equal(2, r.Receptoras.Count);
        Assert.All(r.Receptoras, x => { Assert.Equal(100, x.CompatibilidadAcademicaPct); Assert.Equal(100, x.CompatibilidadHorariaPct); Assert.Equal(100, x.CompatibilidadCreditosPct); });
        Assert.Equal(nm ? 8 : 4, r.Evidencias.Count);
    }
    [Fact]
    public async Task Rechaza_receptora_ajena_y_exceso_por_origen()
    {
        var c = new Catalogo(); var s = Guid.NewGuid(); var a = Guid.NewGuid();
        var solicitud = new SolicitudParaAnalisis(s, "Interno", c.CO, Guid.NewGuid(), [new(a, c.Origen, 4, 100, "Aprobada", DateOnly.FromDateTime(DateTime.UtcNow))]);
        var g = GrupoAgrupacion.Crear(s, [a], [c.R1, c.R2], [new(a, c.R1, 60, 2), new(a, c.R2, 60, 2)]);
        await Assert.ThrowsAsync<GrupoInvalidoException>(() => new PrepararAgrupacion(c, TimeProvider.System).ConstruirAsync(solicitud, g, ParametrosHomologacion.Predeterminados(), default));
    }
}
