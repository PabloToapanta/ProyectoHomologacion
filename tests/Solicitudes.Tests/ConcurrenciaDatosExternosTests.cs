using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Solicitudes.Domain;
using Solicitudes.Infrastructure;
using Xunit;

namespace Solicitudes.Tests;

/// <summary>HU-19: la corrección de datos de RN-05 no puede guardarse después de asignar docentes, ni siquiera en concurrencia.</summary>
public class ConcurrenciaDatosExternosTests
{
    private static readonly DocenteDisponible[] Docentes = [new(Guid.NewGuid(), "docente.ana"), new(Guid.NewGuid(), "docente.luis")];
    private static Solicitud Externa(string quipux) => new(quipux, "EST-SINTETICO", "Estudiante sintético", "Externo", null,
        "Universidad sintética Ecuador", "Carrera externa sintética", Guid.NewGuid(), 1, 0);

    /// <summary>Intercalado: la edición lee «Registrada», la asignación se confirma y solo después la edición intenta guardar.</summary>
    private static async Task EdicionLeidaAntesDeAsignarNoSeGuarda(DbContextOptions<SolicitudesDbContext> options)
    {
        var solicitud = Externa("Q-HU19-INTERCALADO");
        await using (var db = new SolicitudesDbContext(options)) { db.Solicitudes.Add(solicitud); await db.SaveChangesAsync(); }
        await using var edicion = new SolicitudesDbContext(options);
        var repoEdicion = new SolicitudRepository(edicion);
        var leida = (await repoEdicion.ObtenerParaActualizar(solicitud.Id, default))!;
        leida.DeclararDatosCambioExterno(9, 0);
        await using (var asignacion = new SolicitudesDbContext(options))
        {
            var repo = new SolicitudRepository(asignacion);
            var s = (await repo.ObtenerParaActualizar(solicitud.Id, default))!;
            await repo.GuardarAsignacion(DocenteAsignado.Asignar(s, Docentes.Select(d => d.Id).ToArray(), Docentes), default);
        }
        await Assert.ThrowsAsync<DatosExternosNoEditablesException>(() => repoEdicion.GuardarCambios(default));
        await using var lectura = new SolicitudesDbContext(options);
        var final = await lectura.Solicitudes.AsNoTracking().SingleAsync(x => x.Id == solicitud.Id);
        Assert.Equal(DocenteAsignado.EstadoAsignada, final.Estado);
        Assert.Equal(1, final.PeriodosCursados);
    }

    [Fact]
    public async Task Intercalado_con_SQLite_responde_conflicto_y_conserva_los_datos()
    {
        using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<SolicitudesDbContext>().UseSqlite(connection).Options;
        await using (var db = new SolicitudesDbContext(options)) await db.Database.EnsureCreatedAsync();
        await EdicionLeidaAntesDeAsignarNoSeGuarda(options);
    }

    [Fact]
    public async Task Edicion_confirmada_antes_de_asignar_se_conserva()
    {
        using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<SolicitudesDbContext>().UseSqlite(connection).Options;
        var solicitud = Externa("Q-HU19-ORDEN");
        await using (var db = new SolicitudesDbContext(options)) { await db.Database.EnsureCreatedAsync(); db.Solicitudes.Add(solicitud); await db.SaveChangesAsync(); }
        await using (var db = new SolicitudesDbContext(options))
        {
            var repo = new SolicitudRepository(db);
            (await repo.ObtenerParaActualizar(solicitud.Id, default))!.DeclararDatosCambioExterno(4, 0);
            await repo.GuardarCambios(default);
        }
        await using (var db = new SolicitudesDbContext(options))
        {
            var repo = new SolicitudRepository(db);
            var s = (await repo.ObtenerParaActualizar(solicitud.Id, default))!;
            await repo.GuardarAsignacion(DocenteAsignado.Asignar(s, Docentes.Select(d => d.Id).ToArray(), Docentes), default);
        }
        await using var lectura = new SolicitudesDbContext(options);
        Assert.Equal(4, (await lectura.Solicitudes.AsNoTracking().SingleAsync()).PeriodosCursados);
    }

    private static async Task EnBaseSqlServer(Func<DbContextOptions<SolicitudesDbContext>, Task> prueba)
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("HU10_TEST_SQL")) { InitialCatalog = "Hu19Tests_" + Guid.NewGuid().ToString("N") };
        var options = new DbContextOptionsBuilder<SolicitudesDbContext>().UseSqlServer(connection.ConnectionString).Options;
        await using var admin = new SolicitudesDbContext(options);
        try { await admin.Database.MigrateAsync(); await prueba(options); }
        finally { if (connection.InitialCatalog.StartsWith("Hu19Tests_", StringComparison.Ordinal)) await admin.Database.EnsureDeletedAsync(); }
    }

    [SqlServerFact]
    public Task Intercalado_con_SQL_Server_real_responde_conflicto() => EnBaseSqlServer(EdicionLeidaAntesDeAsignarNoSeGuarda);

    [SqlServerFact]
    public Task Edicion_y_asignacion_en_paralelo_solo_fallan_de_forma_controlada() => EnBaseSqlServer(async options =>
    {
        for (var ronda = 0; ronda < 10; ronda++)
        {
            var solicitud = Externa($"Q-HU19-PARALELO-{ronda}");
            await using (var db = new SolicitudesDbContext(options)) { db.Solicitudes.Add(solicitud); await db.SaveChangesAsync(); }
            async Task<Exception?> Editar()
            {
                await using var db = new SolicitudesDbContext(options); var repo = new SolicitudRepository(db);
                try { (await repo.ObtenerParaActualizar(solicitud.Id, default))!.DeclararDatosCambioExterno(7, 0); await repo.GuardarCambios(default); return null; }
                catch (Exception e) { return e; }
            }
            async Task<Exception?> Asignar()
            {
                await using var db = new SolicitudesDbContext(options); var repo = new SolicitudRepository(db);
                try
                {
                    var s = (await repo.ObtenerParaActualizar(solicitud.Id, default))!;
                    await repo.GuardarAsignacion(DocenteAsignado.Asignar(s, Docentes.Select(d => d.Id).ToArray(), Docentes), default);
                    return null;
                }
                catch (Exception e) { return e; }
            }
            var errores = await Task.WhenAll(Task.Run(Editar), Task.Run(Asignar));
            // La asignación siempre gana o llega después; la edición tardía recibe el 409 controlado, nunca un error de SQL.
            Assert.Null(errores[1]);
            if (errores[0] is { } error) Assert.IsType<DatosExternosNoEditablesException>(error);
            await using var lectura = new SolicitudesDbContext(options);
            var final = await lectura.Solicitudes.AsNoTracking().SingleAsync(x => x.Id == solicitud.Id);
            Assert.Equal(DocenteAsignado.EstadoAsignada, final.Estado);
            Assert.Equal(errores[0] is null ? 7 : 1, final.PeriodosCursados);
        }
    });
}
