using System.Text.Json;
using Homologacion.Application.AnalisisInterno;
using Homologacion.Application.AnalisisExterno;
using Homologacion.Application.Agrupaciones;
using Homologacion.Application.Equivalencias;
using Homologacion.Application.Configuracion;
using Homologacion.Domain.Agrupaciones;
using Homologacion.Domain.Configuracion;
using Xunit;

namespace Homologacion.Application.Tests;

public class InstantaneasMallaTests
{
    private sealed class Agente(EscenarioInterno e) : IAgenteAnalisisInterno, IDescripcionAgente
    {
        public string Modelo => "modelo-sintetico";
        public string VersionPlantilla => "hu18-v1";
        public Task<ExplicacionAgente> JustificarAsync(ContextoAgente contexto, CancellationToken ct) => e.JustificarAsync(contexto, ct);
    }
    private sealed class Almacen : IExplicacionesAgente
    {
        public List<ExplicacionGuardada> Filas { get; } = [];
        public Task<ExplicacionGuardada?> BuscarAsync(string clave, CancellationToken ct) => Task.FromResult(Filas.LastOrDefault(f => f.Clave == clave));
        public Task GuardarAsync(ExplicacionGuardada fila, CancellationToken ct) { Filas.Add(fila); return Task.CompletedTask; }
    }
    private sealed class Parametros : IParametrosHomologacionProvider
    {
        private static readonly ParametrosHomologacion Valor = ParametrosHomologacion.Predeterminados(Guid.Parse("07000000-0000-0000-0000-000000000001"));
        public Task<ParametrosHomologacion> ObtenerVigentesAsync(CancellationToken ct) => Task.FromResult(Valor);
    }
    private static AsignaturaParaAnalisis ConMalla(AsignaturaParaAnalisis a, bool vigente = true) =>
        a with { Malla = new(Guid.NewGuid(), a.CarreraId, "Plan sintético", vigente) };
    private static AnalizarSolicitudInterna Caso(EscenarioInterno e, Almacen? almacen = null) =>
        new(e, e, new(new EvaluarEquivalencia(new UmbralAnalisis()), new Agente(e), almacen, new RelojAnalisis()), new RelojAnalisis(), new Parametros());
    private static Task<ResultadoInternoSolicitud> Analizar(AnalizarSolicitudInterna caso) =>
        caso.EjecutarAsync(EscenarioInterno.SolicitudId, EscenarioInterno.AprobadaId, EscenarioInterno.ReceptoraId, default);

    [Theory]
    [InlineData("Completo")]
    [InlineData("Incompleto")]
    [InlineData("Fallido")]
    [InlineData("Excluida")]
    public async Task Pareja_captura_ambas_mallas_en_todos_los_estados(string estado)
    {
        var e = new EscenarioInterno();
        foreach (var id in e.Materias.Keys.ToArray()) e.Materias[id] = ConMalla(e.Materias[id], id != EscenarioInterno.OrigenId);
        if (estado == "Incompleto") e.Revision = e.Revision with { Vigente = false };
        if (estado == "Fallido") e.FalloAgente = TipoFalloAgente.NoDisponible;
        if (estado == "Excluida") e.Solicitud = e.Solicitud! with { AsignaturasAprobadas = [e.Solicitud.AsignaturasAprobadas[0] with { IntentoMatricula = 3 }] };
        var r = await Analizar(Caso(e));
        Assert.Equal(estado == "Excluida" ? "Completo" : estado, r.Estado);
        if (estado == "Excluida") { Assert.False(r.Evaluacion.Homologable); Assert.Contains("tercera matrícula", r.Mensaje); }
        var o = Assert.Single(r.Mallas!.Origenes);
        Assert.Equal(EscenarioInterno.AprobadaId, o.AsignaturaAprobadaId);
        Assert.Equal(EscenarioInterno.OrigenId, o.AsignaturaCatalogoId);
        Assert.Equal(e.Materias[EscenarioInterno.OrigenId].Malla!.Id, o.Malla!.Id);
        Assert.False(o.Malla.VigenteAlAnalizar);
        var d = Assert.Single(r.Mallas.Receptoras);
        Assert.Equal(EscenarioInterno.ReceptoraId, d.AsignaturaCatalogoId);
        Assert.Null(d.AsignaturaAprobadaId);
        Assert.True(d.Malla!.VigenteAlAnalizar);
    }

    [Fact]
    public async Task Cambiar_solo_malla_conserva_contexto_motor_y_clave_HU37_y_captura_datos_actuales()
    {
        var e = new EscenarioInterno(); var almacen = new Almacen(); var caso = Caso(e, almacen);
        // Catálogo anterior a #96: sin malla. La ampliación tampoco invalida explicaciones anteriores.
        var primero = await Analizar(caso);
        var contexto = JsonSerializer.Serialize(e.Contexto);
        foreach (var id in e.Materias.Keys.ToArray()) e.Materias[id] = ConMalla(e.Materias[id]);
        var segundo = await Analizar(caso);
        foreach (var id in e.Materias.Keys.ToArray())
            e.Materias[id] = e.Materias[id] with { Malla = e.Materias[id].Malla! with { Nombre = "Plan renombrado", Vigente = false } };
        var tercero = await Analizar(caso);
        Assert.Equal(1, e.LlamadasAgente);
        Assert.Single(almacen.Filas);
        Assert.True(segundo.Agente!.Reutilizada); Assert.True(tercero.Agente!.Reutilizada);
        Assert.Equal(primero.Agente!.ExplicacionId, tercero.Agente.ExplicacionOriginalId);
        Assert.Equal(JsonSerializer.Serialize(primero.Evaluacion), JsonSerializer.Serialize(tercero.Evaluacion));
        // Forzar una llamada permite inspeccionar el contexto actual realmente enviado.
        await caso.EjecutarAsync(EscenarioInterno.SolicitudId, EscenarioInterno.AprobadaId, EscenarioInterno.ReceptoraId,
            ForzarAnalisis.Crear(Guid.NewGuid(), "Verificar contexto"), default);
        Assert.Equal(contexto, JsonSerializer.Serialize(e.Contexto));
        Assert.Equal(almacen.Filas[0].Clave, almacen.Filas[1].Clave);
        Assert.DoesNotContain("malla", contexto, StringComparison.OrdinalIgnoreCase);
        Assert.Null(Assert.Single(primero.Mallas!.Origenes).Malla);
        Assert.True(Assert.Single(segundo.Mallas!.Origenes).Malla!.VigenteAlAnalizar);
        Assert.False(Assert.Single(tercero.Mallas!.Origenes).Malla!.VigenteAlAnalizar);
    }

    [Theory]
    [InlineData("id")]
    [InlineData("carrera")]
    [InlineData("nombre")]
    public async Task Rechaza_malla_presente_incoherente_antes_de_llamar_al_agente(string caso)
    {
        var e = new EscenarioInterno(); var a = ConMalla(e.Materias[EscenarioInterno.OrigenId]);
        a = a with
        {
            Malla = a.Malla! with
            {
                Id = caso == "id" ? Guid.Empty : a.Malla.Id,
                CarreraId = caso == "carrera" ? Guid.NewGuid() : a.CarreraId,
                Nombre = caso == "nombre" ? " " : a.Malla.Nombre
            }
        };
        e.Materias[a.Id] = a;
        await Assert.ThrowsAsync<FuenteAnalisisNoDisponibleException>(() => Analizar(Caso(e)));
        Assert.Equal(0, e.LlamadasAgente);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Externo_solo_captura_malla_receptora_sin_inventar_origen(bool excluida)
    {
        var e = new EscenarioExterno(); e.Materias[EscenarioExterno.ReceptoraId] = ConMalla(e.Materias[EscenarioExterno.ReceptoraId]);
        if (excluida) e.Aprobada(a => a with { IntentoMatricula = 3 });
        var r = await new AnalizarSolicitudExterna(e, e, new(new UmbralAnalisis()), new RelojAnalisis())
            .EjecutarAsync(EscenarioExterno.SolicitudId, EscenarioExterno.AprobadaId, EscenarioExterno.ReceptoraId, default);
        var o = Assert.Single(r.Mallas!.Origenes);
        Assert.Null(o.AsignaturaCatalogoId); Assert.Null(o.Malla);
        Assert.Equal(EscenarioExterno.AprobadaId, o.AsignaturaAprobadaId);
        Assert.NotNull(Assert.Single(r.Mallas.Receptoras).Malla);
    }

    [Theory]
    [InlineData("Interno")]
    [InlineData("Externo")]
    public async Task Grupo_Na1_captura_cada_asignatura_sin_confundir_UUID(string tipo)
    {
        var e = new EscenarioInterno();
        foreach (var id in e.Materias.Keys.ToArray()) e.Materias[id] = ConMalla(e.Materias[id]);
        var b = Guid.NewGuid(); var o2 = Guid.NewGuid();
        e.Materias[o2] = ConMalla(e.Materias[EscenarioInterno.OrigenId] with { Id = o2 });
        var s = e.Solicitud! with
        {
            TipoCambio = tipo,
            AsignaturasAprobadas = [e.Solicitud!.AsignaturasAprobadas[0],
                e.Solicitud.AsignaturasAprobadas[0] with { Id = b, AsignaturaOrigenId = o2 }]
        };
        var g = GrupoAgrupacion.Crear(s.Id, [EscenarioInterno.AprobadaId, b], [EscenarioInterno.ReceptoraId],
            [new(EscenarioInterno.AprobadaId, EscenarioInterno.ReceptoraId, 40, 4), new(b, EscenarioInterno.ReceptoraId, 40, 4)]);
        var entrada = await new PrepararAgrupacion(e, new RelojAnalisis()).ConstruirAsync(s, g, ParametrosHomologacion.Predeterminados(), default);
        Assert.Equal(2, entrada.Mallas!.Origenes.Count);
        var segundo = entrada.Mallas.Origenes.Single(o => o.AsignaturaAprobadaId == b);
        if (tipo == "Interno") { Assert.Equal(o2, segundo.AsignaturaCatalogoId); Assert.Equal(e.Materias[o2].Malla!.Id, segundo.Malla!.Id); }
        else { Assert.Null(segundo.AsignaturaCatalogoId); Assert.Null(segundo.Malla); }
        Assert.Equal(EscenarioInterno.ReceptoraId, Assert.Single(entrada.Mallas.Receptoras).AsignaturaCatalogoId);
    }

}
