using Homologacion.Application.AnalisisInterno;
using Homologacion.Application.Equivalencias;
using Homologacion.Application.Configuracion;
using Homologacion.Domain.Configuracion;
using Xunit;

namespace Homologacion.Application.Tests;

/// <summary>HU-37: reutilización de la explicación del agente; el motor siempre se ejecuta.</summary>
public class ReutilizacionAgenteTests
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 22, 9, 0, 0, TimeSpan.FromHours(-5));
    private sealed class Umbral(decimal valor) : IUmbralEquivalencia { public decimal Valor => valor; }
    private sealed class Reloj : TimeProvider
    {
        public DateTimeOffset Ahora { get; set; } = T0;
        public override DateTimeOffset GetUtcNow() => Ahora;
    }
    private sealed class Agente : IAgenteAnalisisInterno, IDescripcionAgente
    {
        public int Llamadas { get; private set; }
        public string Modelo { get; set; } = "modelo-sintetico";
        public string VersionPlantilla { get; set; } = "hu18-v1";
        public TipoFalloAgente? Fallo { get; set; }
        public ExplicacionAgente Salida { get; set; } = new("Explicacion sintetica.", ["C1"]);
        public Task<ExplicacionAgente> JustificarAsync(ContextoAgente contexto, CancellationToken cancellationToken)
        {
            Llamadas++;
            return Fallo is { } tipo ? Task.FromException<ExplicacionAgente>(new FalloAgenteException(tipo)) : Task.FromResult(Salida);
        }
    }
    private sealed class Almacen : IExplicacionesAgente
    {
        public List<ExplicacionGuardada> Filas { get; } = [];
        public int Busquedas { get; private set; }
        public bool FallaBuscar { get; set; }
        public bool FallaGuardar { get; set; }
        public Task<ExplicacionGuardada?> BuscarAsync(string clave, CancellationToken ct)
        {
            Busquedas++;
            if (FallaBuscar) throw new AlmacenAnalisisNoDisponibleException();
            return Task.FromResult(Filas.Where(f => f.Clave == clave).OrderByDescending(f => f.Fecha).FirstOrDefault());
        }
        public Task GuardarAsync(ExplicacionGuardada explicacion, CancellationToken ct)
        {
            if (FallaGuardar) throw new AlmacenAnalisisNoDisponibleException();
            Filas.Add(explicacion); return Task.CompletedTask;
        }
    }

    private static readonly ParametrosHomologacion Parametros = new(Guid.Parse("37000000-0000-0000-0000-000000000001"), 80, 10, 1, 1, 2, 2);
    private static SolicitudEquivalencia Datos(int horasOrigen = 80)
    {
        static DatosAsignatura Materia(string id, int horas) => new(id, "plan-sintetico-" + id, horas, "reloj", 8, "sistema-sintetico", ["C"], ["R"]);
        return new([new(Materia("O", horasOrigen), true)], Materia("D", 100),
            [new("O", "C", "C", new("plan-O/C", "plan-D/C"))], [new("O", "R", "R", new("plan-O/R", "plan-D/R"))]);
    }
    private static ReutilizacionAgente Pareja(string firma = "pareja-sintetica", ForzarAnalisis? forzar = null) => new(firma, forzar);
    private static (AsistirEvaluacionInterna Caso, Agente Agente, Almacen Almacen, Reloj Reloj) Crear()
    {
        var agente = new Agente(); var almacen = new Almacen(); var reloj = new Reloj();
        return (new(new EvaluarEquivalencia(new Umbral(80)), agente, almacen, reloj), agente, almacen, reloj);
    }
    private static Task<AnalisisAsistido> Ejecutar(AsistirEvaluacionInterna caso, ReutilizacionAgente? reutilizacion,
        SolicitudEquivalencia? datos = null, ParametrosHomologacion? parametros = null) =>
        caso.EjecutarAsync(datos ?? Datos(), [], [], parametros ?? Parametros, reutilizacion, default);

    [Fact]
    public async Task CA1_la_segunda_ejecucion_con_la_misma_clave_reutiliza_sin_llamar_al_agente()
    {
        var (caso, agente, almacen, reloj) = Crear();
        var primero = await Ejecutar(caso, Pareja());
        reloj.Ahora = T0.AddDays(3);
        var segundo = await Ejecutar(caso, Pareja());

        Assert.Equal(1, agente.Llamadas);
        Assert.Single(almacen.Filas);
        Assert.Equal(EstadoAsistencia.Completada, segundo.Estado);
        Assert.False(primero.Agente!.Reutilizada);
        Assert.True(segundo.Agente!.Reutilizada);
        Assert.Equal(primero.Agente.ExplicacionId, segundo.Agente.ExplicacionOriginalId);
        Assert.Equal(T0, segundo.Agente.FechaOriginal);
        Assert.Equal(("modelo-sintetico", "hu18-v1"), (segundo.Agente.Modelo, segundo.Agente.VersionPlantilla));
        Assert.Equal(primero.Asistencia, segundo.Asistencia);
        // El motor se recalcula: los porcentajes salen de la evaluación actual.
        Assert.Equal(primero.Evaluacion.CompatibilidadHorariaPct, segundo.Evaluacion.CompatibilidadHorariaPct);
    }

    [Fact]
    public async Task CA1_la_explicacion_guardada_no_contiene_datos_del_estudiante()
    {
        var (caso, _, almacen, _) = Crear();
        await Ejecutar(caso, Pareja());
        var fila = Assert.Single(almacen.Filas);
        Assert.Matches("^[0-9a-f]{64}$", fila.Clave);
        Assert.Equal((Parametros.VersionId, "modelo-sintetico", "hu18-v1", (Guid?)null, (string?)null),
            (fila.VersionParametros, fila.Modelo, fila.VersionPlantilla, fila.ForzadoPorId, fila.Motivo));
        Assert.Equal(new ExplicacionAgente("Explicacion sintetica.", ["C1"]).Resumen, fila.Explicacion.Resumen);
    }

    public enum Cambio { Modelo, Plantilla, Parametros, Pareja, HorasAprobada }

    [Theory]
    [InlineData(Cambio.Modelo)]
    [InlineData(Cambio.Plantilla)]
    [InlineData(Cambio.Parametros)]
    [InlineData(Cambio.Pareja)]
    [InlineData(Cambio.HorasAprobada)]
    public async Task CA2_cambiar_un_componente_de_la_clave_llama_de_nuevo_al_agente(Cambio cambio)
    {
        var (caso, agente, almacen, _) = Crear();
        await Ejecutar(caso, Pareja());
        if (cambio == Cambio.Modelo) agente.Modelo = "otro-modelo";
        if (cambio == Cambio.Plantilla) agente.VersionPlantilla = "hu18-v2";
        var segundo = await Ejecutar(caso, Pareja(cambio == Cambio.Pareja ? "programa-modificado" : "pareja-sintetica"),
            cambio == Cambio.HorasAprobada ? Datos(90) : null,
            cambio == Cambio.Parametros ? new(Guid.Parse("37000000-0000-0000-0000-000000000002"), 80, 10, 1, 1, 2, 2) : null);

        Assert.Equal(2, agente.Llamadas);
        Assert.False(segundo.Agente!.Reutilizada);
        Assert.Equal(2, almacen.Filas.Select(f => f.Clave).Distinct().Count());
    }

    [Theory]
    [InlineData(TipoFalloAgente.NoDisponible)]
    [InlineData(TipoFalloAgente.TiempoAgotado)]
    [InlineData(TipoFalloAgente.RespuestaInvalida)]
    public async Task CA3_un_analisis_fallido_no_se_guarda_y_el_siguiente_llama_al_agente(TipoFalloAgente fallo)
    {
        var (caso, agente, almacen, _) = Crear();
        agente.Fallo = fallo;
        var fallido = await Ejecutar(caso, Pareja());
        Assert.Equal(EstadoAsistencia.RevisionManual, fallido.Estado);
        Assert.Null(fallido.Agente);
        Assert.Empty(almacen.Filas);

        agente.Fallo = null;
        var siguiente = await Ejecutar(caso, Pareja());
        Assert.Equal(2, agente.Llamadas);
        Assert.False(siguiente.Agente!.Reutilizada);
    }

    [Fact]
    public async Task CA3_un_analisis_incompleto_no_busca_ni_guarda()
    {
        var (caso, agente, almacen, _) = Crear();
        var r = await Ejecutar(caso, Pareja(), Datos() with { Receptora = null });
        Assert.Equal(EstadoAsistencia.InformacionIncompleta, r.Estado);
        Assert.Null(r.Agente);
        Assert.Equal((0, 0, 0), (agente.Llamadas, almacen.Busquedas, almacen.Filas.Count));
    }

    [Fact]
    public async Task CA4_forzar_llama_al_agente_y_conserva_la_explicacion_anterior()
    {
        var (caso, agente, almacen, reloj) = Crear();
        await Ejecutar(caso, Pareja());
        reloj.Ahora = T0.AddHours(2);
        var docente = Guid.NewGuid();
        agente.Salida = new("Explicacion sintetica nueva.", ["C1", "R1"]);
        var forzado = await Ejecutar(caso, Pareja(forzar: ForzarAnalisis.Crear(docente, "Se corrigió el programa en la revisión.")));

        Assert.Equal(2, agente.Llamadas);
        Assert.False(forzado.Agente!.Reutilizada);
        Assert.Equal(2, almacen.Filas.Count);
        var nueva = almacen.Filas.Single(f => f.Id == forzado.Agente.ExplicacionId);
        Assert.Equal((docente, "Se corrigió el programa en la revisión.", T0.AddHours(2)), (nueva.ForzadoPorId, nueva.Motivo, nueva.Fecha));
        Assert.Equal(almacen.Filas[0].Clave, nueva.Clave);

        // Sin forzar, se reutiliza la más reciente.
        var luego = await Ejecutar(caso, Pareja());
        Assert.Equal(2, agente.Llamadas);
        Assert.Equal(nueva.Id, luego.Agente!.ExplicacionOriginalId);
        Assert.Equal("Explicacion sintetica nueva.", luego.Asistencia!.Resumen);
    }

    [Fact]
    public async Task Una_explicacion_guardada_que_no_supera_la_validacion_no_se_reutiliza()
    {
        var (caso, agente, almacen, _) = Crear();
        await Ejecutar(caso, Pareja());
        var alterada = almacen.Filas[0] with { Explicacion = new("Explicacion alterada.", ["X9"]) };
        almacen.Filas[0] = alterada;
        var r = await Ejecutar(caso, Pareja());
        Assert.Equal(2, agente.Llamadas);
        Assert.False(r.Agente!.Reutilizada);
        Assert.Equal("Explicacion sintetica.", r.Asistencia!.Resumen);
    }

    [Fact]
    public async Task Si_el_almacen_no_responde_al_buscar_se_llama_al_agente()
    {
        var (caso, agente, almacen, _) = Crear();
        almacen.FallaBuscar = true;
        var r = await Ejecutar(caso, Pareja());
        Assert.Equal(1, agente.Llamadas);
        Assert.Equal(EstadoAsistencia.Completada, r.Estado);
        Assert.Single(almacen.Filas);
    }

    [Fact]
    public async Task Si_el_almacen_falla_al_guardar_no_se_confirma_el_analisis()
    {
        var (caso, _, almacen, _) = Crear();
        almacen.FallaGuardar = true;
        await Assert.ThrowsAsync<AlmacenAnalisisNoDisponibleException>(() => Ejecutar(caso, Pareja()));
    }

    [Fact]
    public async Task Sin_datos_de_reutilizacion_conserva_el_comportamiento_anterior()
    {
        var (caso, agente, almacen, _) = Crear();
        await Ejecutar(caso, null);
        var r = await Ejecutar(caso, null);
        Assert.Equal((2, 0, 0), (agente.Llamadas, almacen.Busquedas, almacen.Filas.Count));
        Assert.Null(r.Agente);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("motivo\u0007con control")]
    public void CA4_el_motivo_es_obligatorio_y_sin_caracteres_de_control(string motivo) =>
        Assert.Throws<DatosAnalisisSolicitudException>(() => ForzarAnalisis.Crear(Guid.NewGuid(), motivo));

    [Fact]
    public void CA4_el_motivo_admite_hasta_500_caracteres()
    {
        Assert.Equal(500, ForzarAnalisis.Crear(Guid.NewGuid(), new string('a', 500)).Motivo.Length);
        Assert.Throws<DatosAnalisisSolicitudException>(() => ForzarAnalisis.Crear(Guid.NewGuid(), new string('a', 501)));
        Assert.Equal("Motivo con\nsalto.", ForzarAnalisis.Crear(Guid.NewGuid(), "  Motivo con\nsalto. ").Motivo);
    }
}

/// <summary>HU-37 en la orquestación de HU-18: firmas de los programas calculadas en Homologación (A1).</summary>
public class ReutilizacionAnalisisSolicitudTests
{
    private sealed class AgenteDescrito(EscenarioInterno e) : IAgenteAnalisisInterno, IDescripcionAgente
    {
        public string Modelo => "modelo-sintetico";
        public string VersionPlantilla => "hu18-v1";
        public Task<ExplicacionAgente> JustificarAsync(ContextoAgente contexto, CancellationToken ct) => e.JustificarAsync(contexto, ct);
    }
    private sealed class Almacen : IExplicacionesAgente
    {
        public List<ExplicacionGuardada> Filas { get; } = [];
        public Task<ExplicacionGuardada?> BuscarAsync(string clave, CancellationToken ct) =>
            Task.FromResult(Filas.Where(f => f.Clave == clave).OrderByDescending(f => f.Fecha).FirstOrDefault());
        public Task GuardarAsync(ExplicacionGuardada explicacion, CancellationToken ct) { Filas.Add(explicacion); return Task.CompletedTask; }
    }
    private sealed class Proveedor : IParametrosHomologacionProvider
    {
        private static readonly ParametrosHomologacion P = new(Guid.Parse("37000000-0000-0000-0000-000000000003"), 80, 10, 1, 1, 2, 2);
        public Task<ParametrosHomologacion> ObtenerVigentesAsync(CancellationToken ct) => Task.FromResult(P);
    }
    private static readonly Guid OtraAprobada = Guid.Parse("37000000-0000-0000-0000-000000000010");
    private static (AnalizarSolicitudInterna Caso, EscenarioInterno E, Almacen Almacen) Crear()
    {
        var e = new EscenarioInterno(); var almacen = new Almacen();
        e.Solicitud = e.Solicitud! with
        {
            AsignaturasAprobadas = [.. e.Solicitud.AsignaturasAprobadas,
                new(OtraAprobada, EscenarioInterno.OrigenId, 8, 80, "Aprobada", new DateOnly(2025, 6, 1)) { IntentoMatricula = 1 }]
        };
        var asistencia = new AsistirEvaluacionInterna(new EvaluarEquivalencia(new UmbralAnalisis()), new AgenteDescrito(e), almacen, new RelojAnalisis());
        return (new(e, e, asistencia, new RelojAnalisis(), new Proveedor()), e, almacen);
    }
    private static Task<ResultadoInternoSolicitud> Analizar(AnalizarSolicitudInterna caso, Guid? aprobada = null, ForzarAnalisis? forzar = null) =>
        caso.EjecutarAsync(EscenarioInterno.SolicitudId, aprobada ?? EscenarioInterno.AprobadaId, EscenarioInterno.ReceptoraId, forzar, default);

    [Fact]
    public async Task CA1_otra_aprobada_con_el_mismo_programa_reutiliza_la_explicacion()
    {
        var (caso, e, _) = Crear();
        var primero = await Analizar(caso);
        var segundo = await Analizar(caso, OtraAprobada);
        Assert.Equal(1, e.LlamadasAgente);
        Assert.Equal(("Completo", "Completo"), (primero.Estado, segundo.Estado));
        Assert.True(segundo.Agente!.Reutilizada);
        Assert.Equal(primero.Agente!.ExplicacionId, segundo.Agente.ExplicacionOriginalId);
        Assert.Contains("Asistencia sintetica", segundo.Resultado!.Justificacion);
    }

    [Fact]
    public async Task CA2_un_cambio_en_el_texto_de_un_programa_llama_de_nuevo_al_agente()
    {
        var (caso, e, _) = Crear();
        await Analizar(caso);
        var origen = e.Materias[EscenarioInterno.OrigenId];
        e.Materias[EscenarioInterno.OrigenId] = origen with { Contenidos = [origen.Contenidos[0] with { Texto = "Contenido origen sintetico revisado" }] };
        var r = await Analizar(caso);
        Assert.Equal(2, e.LlamadasAgente);
        Assert.False(r.Agente!.Reutilizada);
    }

    [Fact]
    public async Task CA2_un_cambio_en_el_perfil_del_programa_receptor_llama_de_nuevo_al_agente()
    {
        var (caso, e, _) = Crear();
        await Analizar(caso);
        e.Materias[EscenarioInterno.ReceptoraId] = e.Materias[EscenarioInterno.ReceptoraId] with { PlanAnalitico = "plan-sintetico-R-v2" };
        await Analizar(caso);
        Assert.Equal(2, e.LlamadasAgente);
    }

    [Fact]
    public async Task CA2_un_cambio_en_una_correspondencia_llama_de_nuevo_al_agente()
    {
        var (caso, e, _) = Crear();
        await Analizar(caso);
        e.Revision = e.Revision with { Contenidos = [e.Revision.Contenidos[0] with { UbicacionReceptora = "plan-R/U2/C1" }] };
        await Analizar(caso);
        Assert.Equal(2, e.LlamadasAgente);
    }

    [Fact]
    public async Task CA3_incompleto_no_lleva_bloque_del_agente_ni_se_guarda()
    {
        var (caso, e, almacen) = Crear();
        e.Revision = new(false, false, [], []);
        var r = await Analizar(caso);
        Assert.Equal("Incompleto", r.Estado);
        Assert.Null(r.Agente);
        Assert.Empty(almacen.Filas);
    }

    [Fact]
    public async Task CA4_forzar_registra_el_docente_y_el_motivo()
    {
        var (caso, e, almacen) = Crear();
        await Analizar(caso);
        var docente = Guid.NewGuid();
        var r = await Analizar(caso, forzar: ForzarAnalisis.Crear(docente, "Revisión del docente"));
        Assert.Equal(2, e.LlamadasAgente);
        Assert.False(r.Agente!.Reutilizada);
        Assert.Equal((docente, "Revisión del docente"), (almacen.Filas[^1].ForzadoPorId, almacen.Filas[^1].Motivo));
    }

    [Fact]
    public async Task CA5_una_solicitud_no_asignada_no_consulta_el_almacen()
    {
        var (caso, e, almacen) = Crear();
        await Analizar(caso);
        e.Solicitud = null;
        await Assert.ThrowsAsync<SolicitudAnalisisNoEncontradaException>(() => Analizar(caso));
        Assert.Equal(1, e.LlamadasAgente);
        Assert.Single(almacen.Filas);
    }
}
