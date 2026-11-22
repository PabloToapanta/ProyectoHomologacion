using Homologacion.Application.AnalisisInterno;
using Homologacion.Application.Informe;
using Homologacion.Domain.Auditoria;
using Homologacion.Infrastructure.AnalisisInterno;
using Homologacion.Infrastructure.Informe;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Homologacion.Infrastructure.Tests;

/// <summary>HU-25: tabla EventosInforme, solo de inserción.</summary>
public class EventosInformeSqlTests
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 20, 10, 0, 0, TimeSpan.FromHours(-5));
    private static EventoInforme Evento(Guid solicitud, TipoEventoInforme tipo, DateTimeOffset fecha) =>
        HistorialInforme.Crear(solicitud, tipo, fecha, Guid.NewGuid(), "docente.sintetico", "Docente");

    private static async Task<DbContextOptions<ResultadosDbContext>> Sqlite(SqliteConnection connection)
    {
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ResultadosDbContext>().UseSqlite(connection).Options;
        await using var db = new ResultadosDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return options;
    }

    [Fact]
    public void Migracion_y_snapshot_incluyen_eventos()
    {
        using var db = new ResultadosDbContext(new DbContextOptionsBuilder<ResultadosDbContext>()
            .UseSqlServer("Server=unused;Database=design_only;Integrated Security=true").Options);
        Assert.Contains("20261120000100_EventosInformeHu25", db.Database.GetMigrations());
        Assert.False(db.Database.HasPendingModelChanges());
    }

    [Fact]
    public void El_contrato_no_ofrece_modificar_ni_eliminar_eventos() =>
        Assert.Equal(["ListarAsync", "RegistrarAsync"], typeof(IAuditoriaInformes).GetMethods().Select(m => m.Name).Order());

    [Fact]
    public async Task Registra_y_lista_solo_los_eventos_de_la_solicitud()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        var options = await Sqlite(connection);
        var solicitud = Guid.NewGuid();
        var generado = Evento(solicitud, TipoEventoInforme.Generado, T0);
        await using (var db = new ResultadosDbContext(options))
        {
            var almacen = new EventosInformeSql(db);
            await almacen.RegistrarAsync(generado, default);
            await almacen.RegistrarAsync(Evento(solicitud, TipoEventoInforme.Remitido, T0.AddHours(1)), default);
            await almacen.RegistrarAsync(Evento(Guid.NewGuid(), TipoEventoInforme.Generado, T0), default);
        }
        await using var lectura = new ResultadosDbContext(options);
        var lista = await new EventosInformeSql(lectura).ListarAsync(solicitud, default);
        Assert.Equal(2, lista.Count);
        var leido = lista.Single(e => e.Tipo == TipoEventoInforme.Generado);
        Assert.Equal((generado.Id, generado.Fecha, generado.UsuarioId, "docente.sintetico", "Docente"),
            (leido.Id, leido.Fecha, leido.UsuarioId, leido.Usuario, leido.Rol));
    }

    [Fact]
    public async Task El_mismo_evento_del_flujo_no_se_registra_dos_veces()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        var options = await Sqlite(connection);
        var solicitud = Guid.NewGuid();
        await using (var db = new ResultadosDbContext(options))
            await new EventosInformeSql(db).RegistrarAsync(Evento(solicitud, TipoEventoInforme.Generado, T0), default);
        await using (var db = new ResultadosDbContext(options))
            await Assert.ThrowsAsync<EventoInformeInvalidoException>(() =>
                new EventosInformeSql(db).RegistrarAsync(Evento(solicitud, TipoEventoInforme.Generado, T0.AddMinutes(1)), default));
        await using var lectura = new ResultadosDbContext(options);
        Assert.Single(await new EventosInformeSql(lectura).ListarAsync(solicitud, default));
    }

    [Fact]
    public async Task Base_no_disponible_no_confirma()
    {
        using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        await using var db = new ResultadosDbContext(new DbContextOptionsBuilder<ResultadosDbContext>().UseSqlite(connection).Options);
        await Assert.ThrowsAsync<AlmacenAnalisisNoDisponibleException>(() =>
            new EventosInformeSql(db).RegistrarAsync(Evento(Guid.NewGuid(), TipoEventoInforme.Generado, T0), default));
        await Assert.ThrowsAsync<AlmacenAnalisisNoDisponibleException>(() => new EventosInformeSql(db).ListarAsync(Guid.NewGuid(), default));
    }

    [SqlServerExternoFact]
    public async Task SQL_Server_real_impide_modificar_o_eliminar_eventos()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("HU20_TEST_SQL"))
            { InitialCatalog = "Hu25Tests_" + Guid.NewGuid().ToString("N") };
        var options = new DbContextOptionsBuilder<ResultadosDbContext>().UseSqlServer(connection.ConnectionString).Options;
        await using var admin = new ResultadosDbContext(options);
        try
        {
            await admin.Database.MigrateAsync();
            await new EventosInformeSql(admin).RegistrarAsync(Evento(Guid.NewGuid(), TipoEventoInforme.Generado, T0), default);
            await Assert.ThrowsAsync<SqlException>(() => admin.Database.ExecuteSqlRawAsync("UPDATE EventosInforme SET Usuario = N'otro'"));
            await Assert.ThrowsAsync<SqlException>(() => admin.Database.ExecuteSqlRawAsync("DELETE FROM EventosInforme"));
            Assert.Equal(1, await admin.EventosInforme.CountAsync(e => e.Usuario == "docente.sintetico"));
        }
        finally { await admin.Database.EnsureDeletedAsync(); }
    }
}
