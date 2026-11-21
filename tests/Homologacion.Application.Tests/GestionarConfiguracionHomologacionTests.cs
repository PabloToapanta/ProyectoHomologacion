using Homologacion.Application.Configuracion;
using Homologacion.Domain.Configuracion;
using Xunit;

namespace Homologacion.Application.Tests;

public class GestionarConfiguracionHomologacionTests
{
    private sealed class Store : IConfiguracionHomologacionStore
    {
        public int Escrituras { get; private set; }
        public ParametrosHomologacion? Nueva { get; private set; }
        public string? Motivo { get; private set; }
        public string? Respaldo { get; private set; }
        public Task<ParametrosHomologacion> ObtenerVigentesAsync(CancellationToken ct) =>
            Task.FromResult(ParametrosHomologacion.Predeterminados(Guid.NewGuid()));
        public Task<ParametrosHomologacion> ActualizarAsync(Guid esperada, ParametrosHomologacion nueva,
            Guid actor, DateTimeOffset fecha, string motivo, string respaldo, CancellationToken ct)
        {
            Escrituras++; Nueva = nueva; Motivo = motivo; Respaldo = respaldo;
            return Task.FromResult(nueva);
        }
        public Task<PaginaHistorialConfiguracion> HistorialAsync(Guid? cursor, int limit, CancellationToken ct) =>
            Task.FromResult(new PaginaHistorialConfiguracion([], null));
    }

    private static CambioConfiguracion Cambio(int umbral = 80, string? motivo = " Acta ", string? respaldo = " Expediente ") =>
        new(umbral, 10, 1, 1, 2, 2, motivo, respaldo);

    [Theory]
    [InlineData(0, "Acta", "Expediente")]
    [InlineData(101, "Acta", "Expediente")]
    [InlineData(80, " ", "Expediente")]
    [InlineData(80, "Acta", " ")]
    public async Task Cambio_invalido_no_llega_al_almacen(int umbral, string motivo, string respaldo)
    {
        var store = new Store(); var caso = new GestionarConfiguracionHomologacion(store, new RelojAnalisis());
        await Assert.ThrowsAsync<ConfiguracionInvalidaException>(() =>
            caso.ActualizarAsync(Guid.NewGuid(), Cambio(umbral, motivo, respaldo), Guid.NewGuid(), default));
        Assert.Equal(0, store.Escrituras);
    }

    [Fact]
    public async Task Cambio_valido_crea_version_nueva_y_limpia_textos()
    {
        var store = new Store(); var caso = new GestionarConfiguracionHomologacion(store, new RelojAnalisis());
        var anterior = Guid.NewGuid();
        var nuevo = await caso.ActualizarAsync(anterior, Cambio(85), Guid.NewGuid(), default);
        Assert.NotEqual(anterior, nuevo.VersionId);
        Assert.Equal(85, store.Nueva!.UmbralPct);
        Assert.Equal("Acta", store.Motivo);
        Assert.Equal("Expediente", store.Respaldo);
        Assert.Equal(1, store.Escrituras);
    }

    [Theory]
    [InlineData("antiguedad", 0)]
    [InlineData("antiguedad", 51)]
    [InlineData("internos", 0)]
    [InlineData("internos", 51)]
    [InlineData("homologablesInterno", 0)]
    [InlineData("homologablesInterno", 51)]
    [InlineData("externos", 0)]
    [InlineData("externos", 51)]
    [InlineData("homologablesExterno", 0)]
    [InlineData("homologablesExterno", 51)]
    public async Task Cada_minimo_y_antiguedad_respeta_limites(string campo, int valor)
    {
        var cambio = Cambio();
        cambio = campo switch
        {
            "antiguedad" => cambio with { AntiguedadMaximaAnios = valor },
            "internos" => cambio with { PeriodosMinimosInterno = valor },
            "homologablesInterno" => cambio with { AsignaturasHomologablesMinimasInterno = valor },
            "externos" => cambio with { PeriodosMinimosExterno = valor },
            _ => cambio with { AsignaturasHomologablesMinimasExterno = valor }
        };
        var store = new Store(); var caso = new GestionarConfiguracionHomologacion(store, new RelojAnalisis());
        await Assert.ThrowsAsync<ConfiguracionInvalidaException>(() =>
            caso.ActualizarAsync(Guid.NewGuid(), cambio, Guid.NewGuid(), default));
        Assert.Equal(0, store.Escrituras);
    }

    [Fact]
    public async Task Motivo_y_respaldo_rechazan_longitud_excesiva()
    {
        var store = new Store(); var caso = new GestionarConfiguracionHomologacion(store, new RelojAnalisis());
        await Assert.ThrowsAsync<ConfiguracionInvalidaException>(() => caso.ActualizarAsync(Guid.NewGuid(),
            Cambio(motivo: new string('m', 501)), Guid.NewGuid(), default));
        await Assert.ThrowsAsync<ConfiguracionInvalidaException>(() => caso.ActualizarAsync(Guid.NewGuid(),
            Cambio(respaldo: new string('r', 2001)), Guid.NewGuid(), default));
        Assert.Equal(0, store.Escrituras);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task Historial_rechaza_limite_fuera_de_rango(int limit)
    {
        var caso = new GestionarConfiguracionHomologacion(new Store(), new RelojAnalisis());
        await Assert.ThrowsAsync<ConfiguracionInvalidaException>(() => caso.HistorialAsync(null, limit, default));
    }
}
