using System.Text.Json;
using System.Text.Json.Nodes;
using Homologacion.Application.Agrupaciones;
using Homologacion.Application.AnalisisExterno;
using Homologacion.Application.AnalisisInterno;
using Homologacion.Application.Equivalencias;
using Homologacion.Domain.Agrupaciones;
using Homologacion.Domain.Configuracion;
using Xunit;

namespace Homologacion.Application.Tests;

public class MallasAnalisisTests
{
    [Fact]
    public void JSON_publicado_antes_de_conciliar_conserva_los_dos_espacios_de_identificadores()
    {
        var aprobada = Guid.NewGuid(); var catalogo = Guid.NewGuid(); var malla = Guid.NewGuid(); var carrera = Guid.NewGuid();
        // Orden de campos de la PR #96 antes del merge: aprobada primero, Catálogo después.
        var contenido = """
            {"origenes":[{"asignaturaAprobadaId":"@aprobada@","asignaturaCatalogoId":"@catalogo@",
            "malla":{"id":"@malla@","carreraId":"@carrera@","nombre":"Plan conservado","vigenteAlAnalizar":false}}],"receptoras":[]}
            """.Replace("@aprobada@", aprobada.ToString()).Replace("@catalogo@", catalogo.ToString())
            .Replace("@malla@", malla.ToString()).Replace("@carrera@", carrera.ToString());
        var leidas = JsonSerializer.Deserialize<MallasAnalisis>(contenido, Json)!;
        var origen = Assert.Single(leidas.Origenes);
        Assert.Equal(aprobada, origen.AsignaturaAprobadaId);
        Assert.Equal(catalogo, origen.AsignaturaCatalogoId);
        Assert.Equal(malla, origen.Malla!.Id);
        Assert.False(origen.Malla.VigenteAlAnalizar);
    }

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private sealed class ParametrosFijos : Homologacion.Application.Configuracion.IParametrosHomologacionProvider
    {
        private static readonly ParametrosHomologacion Valor = ParametrosHomologacion.Predeterminados();
        public Task<ParametrosHomologacion> ObtenerVigentesAsync(CancellationToken ct) => Task.FromResult(Valor);
    }
    private static AnalizarSolicitudInterna Interno(EscenarioInterno e) =>
        new(e, e, new(new EvaluarEquivalencia(new UmbralAnalisis()), e), new RelojAnalisis(), new ParametrosFijos());

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public async Task Fotografia_interna_conserva_ambas_mallas_sin_cambiar_el_motor(int intento)
    {
        var e = new EscenarioInterno();
        e.Solicitud = e.Solicitud! with { AsignaturasAprobadas = [e.Solicitud.AsignaturasAprobadas[0] with { IntentoMatricula = intento }] };
        var anterior = await Interno(e).EjecutarAsync(EscenarioInterno.SolicitudId, EscenarioInterno.AprobadaId, EscenarioInterno.ReceptoraId, default);
        foreach (var id in e.Materias.Keys.ToArray())
        {
            var asignatura = e.Materias[id];
            e.Materias[id] = asignatura with { Malla = new(Guid.NewGuid(), asignatura.CarreraId, "Plan " + id, id == EscenarioInterno.ReceptoraId) };
        }
        var resultado = await Interno(e).EjecutarAsync(EscenarioInterno.SolicitudId, EscenarioInterno.AprobadaId, EscenarioInterno.ReceptoraId, default);
        Assert.Equal(JsonSerializer.Serialize(anterior.Evaluacion, Json), JsonSerializer.Serialize(resultado.Evaluacion, Json));
        Assert.Equal(anterior.Estado, resultado.Estado);
        var origen = Assert.Single(resultado.Mallas!.Origenes);
        var receptora = Assert.Single(resultado.Mallas.Receptoras);
        Assert.Equal(EscenarioInterno.AprobadaId, origen.AsignaturaAprobadaId);
        Assert.Equal(EscenarioInterno.OrigenId, origen.AsignaturaCatalogoId);
        Assert.False(origen.Malla!.VigenteAlAnalizar);
        Assert.True(receptora.Malla!.VigenteAlAnalizar);
        e.Materias[EscenarioInterno.OrigenId] = e.Materias[EscenarioInterno.OrigenId] with { Malla = null };
        var copia = JsonSerializer.Deserialize<AnalisisGuardado>(JsonSerializer.Serialize(new AnalisisGuardado(DateTimeOffset.UtcNow, resultado), Json), Json)!;
        Assert.Equal(origen.Malla, copia.Analisis.Mallas!.Origenes[0].Malla);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public async Task Origen_externo_sin_malla_autorizada_permanece_null(int intento)
    {
        var e = new EscenarioExterno();
        e.Aprobada(a => a with { IntentoMatricula = intento });
        var r = e.Materias[EscenarioExterno.ReceptoraId];
        e.Materias[r.Id] = r with { Malla = new(Guid.NewGuid(), r.CarreraId, "Plan receptor", true) };
        var resultado = await new AnalizarSolicitudExterna(e, e, new EvaluarEquivalencia(new UmbralAnalisis()), new RelojAnalisis())
            .EjecutarAsync(EscenarioExterno.SolicitudId, EscenarioExterno.AprobadaId, r.Id, default);
        var origen = Assert.Single(resultado.Mallas!.Origenes);
        Assert.Null(origen.Malla); Assert.Null(origen.AsignaturaCatalogoId);
        Assert.NotNull(Assert.Single(resultado.Mallas.Receptoras).Malla);
    }

    [Fact]
    public async Task Agrupacion_fotografia_cada_asignatura_sin_confundir_aprobada_y_catalogo()
    {
        var e = new EscenarioInterno();
        foreach (var id in e.Materias.Keys.ToArray())
            e.Materias[id] = e.Materias[id] with { Malla = new(Guid.NewGuid(), e.Materias[id].CarreraId, "Plan grupo", true) };
        var r2 = Guid.NewGuid();
        e.Materias[r2] = e.Materias[EscenarioInterno.ReceptoraId] with { Id = r2 };
        var grupo = GrupoAgrupacion.Crear(EscenarioInterno.SolicitudId, [EscenarioInterno.AprobadaId], [EscenarioInterno.ReceptoraId, r2],
            [new(EscenarioInterno.AprobadaId, EscenarioInterno.ReceptoraId, 40, 4), new(EscenarioInterno.AprobadaId, r2, 40, 4)]);
        var entrada = await new PrepararAgrupacion(e, new RelojAnalisis()).ConstruirAsync(e.Solicitud!, grupo, ParametrosHomologacion.Predeterminados(), default);
        var mallasGrupo = Assert.IsType<MallasAnalisis>(entrada.Mallas);
        Assert.Equal(EscenarioInterno.AprobadaId, Assert.Single(mallasGrupo.Origenes).AsignaturaAprobadaId);
        Assert.Equal(EscenarioInterno.OrigenId, mallasGrupo.Origenes[0].AsignaturaCatalogoId);
        Assert.Equal(2, mallasGrupo.Receptoras.Count);
    }

    [Theory]
    [InlineData("id")]
    [InlineData("carrera")]
    [InlineData("nombre")]
    [InlineData("vigencia")]
    public async Task Metadatos_de_malla_incoherentes_se_rechazan_antes_de_invocar_agente(string caso)
    {
        var e = new EscenarioInterno(); var o = e.Materias[EscenarioInterno.OrigenId];
        e.Materias[o.Id] = o with
        {
            Malla = new(caso == "id" ? Guid.Empty : Guid.NewGuid(),
            caso == "carrera" ? Guid.NewGuid() : o.CarreraId, caso == "nombre" ? " " : "Plan", caso == "vigencia" ? null : true)
        };
        await Assert.ThrowsAsync<FuenteAnalisisNoDisponibleException>(() => Interno(e).EjecutarAsync(EscenarioInterno.SolicitudId, EscenarioInterno.AprobadaId, EscenarioInterno.ReceptoraId, default));
        Assert.Equal(0, e.LlamadasAgente);
    }

    [Fact]
    public async Task Historico_sin_mallas_sigue_deserializando_sin_atribuirle_una_version()
    {
        var e = new EscenarioInterno();
        var resultado = await Interno(e).EjecutarAsync(EscenarioInterno.SolicitudId, EscenarioInterno.AprobadaId, EscenarioInterno.ReceptoraId, default);
        var node = JsonNode.Parse(JsonSerializer.Serialize(new AnalisisGuardado(DateTimeOffset.UtcNow, resultado), Json))!;
        node["analisis"]!.AsObject().Remove("mallas");
        var historico = node.Deserialize<AnalisisGuardado>(Json)!;
        Assert.Null(historico.Analisis.Mallas);
        Assert.Equal(resultado.Estado, historico.Analisis.Estado);
        Assert.Equal(resultado.Evaluacion.Homologable, historico.Analisis.Evaluacion.Homologable);
    }
}
