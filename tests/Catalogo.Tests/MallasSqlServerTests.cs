using Catalogo.Domain;
using Catalogo.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace Catalogo.Tests;

public sealed class SqlHu07FactAttribute : FactAttribute
{
    public SqlHu07FactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("HU07_TEST_SQL")))
            Skip = "Requiere SQL Server aislado mediante HU07_TEST_SQL; no usa bases persistentes.";
    }
}

public class MallasSqlServerTests
{
    private const string Anterior = "20261104083910_ProgramasAnaliticosHu08";

    private sealed class BaseAislada : IAsyncDisposable
    {
        private readonly string connection;
        public BaseAislada()
        {
            var builder = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("HU07_TEST_SQL"));
            if (!builder.DataSource.StartsWith("127.0.0.1,", StringComparison.Ordinal) || builder.ApplicationName != "hu07-aislado")
                throw new InvalidOperationException("HU07_TEST_SQL debe apuntar al contenedor local aislado de HU-07.");
            builder.InitialCatalog = "Hu07_" + Guid.NewGuid().ToString("N");
            connection = builder.ConnectionString;
        }
        public CatalogoDbContext Crear() => new(new DbContextOptionsBuilder<CatalogoDbContext>().UseSqlServer(connection).Options);
        public async ValueTask DisposeAsync()
        {
            await using var db = Crear();
            await db.Database.EnsureDeletedAsync();
        }
    }

    private static async Task<string> Leer(CatalogoDbContext db, string sql)
    {
        await db.Database.OpenConnectionAsync();
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        return (string)(await command.ExecuteScalarAsync())!;
    }

    [SqlHu07Fact]
    public async Task Migracion_desde_develop_conserva_filas_relaciones_y_PDF_y_reversion_segura()
    {
        await using var aislada = new BaseAislada();
        await using var db = aislada.Crear();
        await db.GetService<IMigrator>().MigrateAsync(Anterior);
        var carrera = Guid.NewGuid(); var asignatura = Guid.NewGuid(); var unidad = Guid.NewGuid(); var tematica = Guid.NewGuid();
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Carreras (Id,Nombre,Facultad,Vigencia,NombreNormalizado,FacultadNormalizada) VALUES ({carrera},N'Histórica',N'Otra facultad',0,N'HISTÓRICA',N'OTRA FACULTAD')");
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Asignaturas (Id,CarreraId,Codigo,CodigoNormalizado,Nombre,Nivel,Creditos,Horas) VALUES ({asignatura},{carrera},N'ALG',N'ALG',N'Álgebra',1,3.5,120)");
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO UnidadesCurriculares (Id,AsignaturaId,Numero,Nombre,Horas,ResultadoAprendizaje) VALUES ({unidad},{asignatura},1,N'Unidad',120,N'Resultado sintético')");
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO UnidadesTematicas (Id,UnidadCurricularId,Orden,Nombre) VALUES ({tematica},{unidad},1,N'Temática')");
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Contenidos (Id,UnidadCurricularId,Orden,Tema,UnidadTematicaId) VALUES ({Guid.NewGuid()},{unidad},1,N'Contenido sintético',{tematica})");
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO PerfilesComparacion (AsignaturaId,PlanAnalitico,UnidadHoras,SistemaCreditos,EsSintetico) VALUES ({asignatura},N'plan-sintético',N'horas',N'créditos',1)");
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO RevisionesComparacion (OrigenId,ReceptoraId,FirmaOrigen,FirmaReceptora,DatosJson) VALUES ({asignatura},{asignatura},N'firma-original',N'firma-receptora',N'{{\"contenidos\":[],\"resultados\":[]}}')");
        var bytes = "%PDF-1.7 sintético HU07"u8.ToArray();
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes));
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO ProgramasAnaliticos (Id,AsignaturaId,NombreArchivo,Sha256,Tamano,Contenido,FechaCarga,CargadoPor) VALUES ({Guid.NewGuid()},{asignatura},N'prueba.pdf',{hash},{bytes.Length},{bytes},{DateTimeOffset.UtcNow},N'actor-sintético')");
        var tablas = new[] { "Carreras", "UnidadesCurriculares", "UnidadesTematicas", "Contenidos", "PerfilesComparacion", "RevisionesComparacion", "ProgramasAnaliticos" };
        var antes = new Dictionary<string, string>();
        foreach (var tabla in tablas) antes[tabla] = await Leer(db, $"SELECT * FROM [{tabla}] FOR JSON PATH, INCLUDE_NULL_VALUES");
        const string asignaturasSql = "SELECT Id,CarreraId,Codigo,CodigoNormalizado,Nombre,Nivel,Creditos,Horas FROM Asignaturas FOR JSON PATH, INCLUDE_NULL_VALUES";
        var asignaturasAntes = await Leer(db, asignaturasSql);
        await db.Database.MigrateAsync();
        foreach (var tabla in tablas) Assert.Equal(antes[tabla], await Leer(db, $"SELECT * FROM [{tabla}] FOR JSON PATH, INCLUDE_NULL_VALUES"));
        Assert.Equal(asignaturasAntes, await Leer(db, asignaturasSql));
        var malla = Assert.Single(await db.Mallas.ToListAsync());
        Assert.Equal(carrera, malla.CarreraId); Assert.True(malla.Vigente); Assert.Equal("Inicial migrada", malla.Nombre);
        Assert.Equal(malla.Id, (await db.Asignaturas.SingleAsync()).MallaId);
        Assert.Equal(bytes, (await db.Set<ProgramaAnalitico>().SingleAsync()).Contenido);
        db.ChangeTracker.Clear();
        await db.GetService<IMigrator>().MigrateAsync(Anterior);
        Assert.Equal(asignaturasAntes, await Leer(db, asignaturasSql));
        await db.Database.MigrateAsync();
        await new MallaRepository(db).CrearAsync(new Malla(carrera, "Versión posterior", false), default);
        var historial = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
        await Assert.ThrowsAsync<SqlException>(() => db.GetService<IMigrator>().MigrateAsync(Anterior));
        Assert.Equal(historial, (await db.Database.GetAppliedMigrationsAsync()).ToArray());
        Assert.Equal(2, await db.Mallas.CountAsync());
        Assert.Equal(asignaturasAntes, await Leer(db, asignaturasSql));
    }

    [SqlHu07Fact]
    public async Task Base_vacia_y_activaciones_concurrentes_mantienen_una_vigente()
    {
        await using var aislada = new BaseAislada();
        await using var db = aislada.Crear();
        await db.Database.MigrateAsync();
        var carrera = new Carrera("Concurrente", "FISEI", true);
        await new CarreraRepository(db).GuardarAsync(carrera, true, default);
        var m1 = new Malla(carrera.Id, "Plan 1", false); var m2 = new Malla(carrera.Id, "Plan 2", false);
        await new MallaRepository(db).CrearAsync(m1, default);
        await new MallaRepository(db).CrearAsync(m2, default);
        async Task Activar(Guid id)
        {
            await using var otro = aislada.Crear();
            await new MallaRepository(otro).ActivarAsync(id, default);
        }
        await Task.WhenAll(Activar(m1.Id), Activar(m2.Id));
        db.ChangeTracker.Clear();
        Assert.Equal(3, await db.Mallas.CountAsync());
        Assert.Single(await db.Mallas.Where(m => m.Vigente).ToListAsync());
        var otra = new Carrera("Ajena", "FISEI", true);
        await new CarreraRepository(db).GuardarAsync(otra, true, default);
        var asignatura = new Asignatura(carrera.Id, "C", "Nombre", 1, 1, 60);
        asignatura.AsignarMalla(m1);
        db.Asignaturas.Add(asignatura); await db.SaveChangesAsync();
        await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Asignaturas SET CarreraId={otra.Id} WHERE Id={asignatura.Id}"));
        Assert.Equal(carrera.Id, (await db.Asignaturas.AsNoTracking().SingleAsync()).CarreraId);
    }

    [SqlHu07Fact]
    public async Task Fallo_antes_del_commit_de_migracion_no_deja_esquema_ni_historial_parcial()
    {
        await using var aislada = new BaseAislada();
        await using var db = aislada.Crear();
        await db.GetService<IMigrator>().MigrateAsync(Anterior);
        var historial = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
        var script = db.GetService<IMigrator>().GenerateScript(Anterior)
            .Replace("BEGIN TRANSACTION;", "").Replace("COMMIT;", "");
        await db.Database.OpenConnectionAsync();
        await using (var transaction = await db.Database.BeginTransactionAsync())
        {
            foreach (var batch in System.Text.RegularExpressions.Regex.Split(script, @"(?m)^GO\s*$"))
                if (!string.IsNullOrWhiteSpace(batch)) await db.Database.ExecuteSqlRawAsync(batch);
            // El esquema y el historial llegaron a escribirse, pero aún no se ha confirmado la transacción.
            Assert.Equal(0, await db.Asignaturas.CountAsync());
            await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlRawAsync("THROW 51010, 'Fallo sintético previo al commit', 1;"));
            await transaction.RollbackAsync();
        }
        Assert.Equal(historial, (await db.Database.GetAppliedMigrationsAsync()).ToArray());
        Assert.Equal("no", await Leer(db, "SELECT CASE WHEN OBJECT_ID(N'Mallas') IS NULL AND COL_LENGTH(N'Asignaturas', N'MallaId') IS NULL THEN N'no' ELSE N'parcial' END"));
        await db.Database.MigrateAsync();
        Assert.Empty(await db.Mallas.ToListAsync());
    }

    [SqlHu07Fact]
    public async Task Fallo_despues_de_desactivar_revierte_toda_la_transaccion()
    {
        await using var aislada = new BaseAislada();
        await using var db = aislada.Crear();
        await db.Database.MigrateAsync();
        var carrera = new Carrera("Rollback", "FISEI", true);
        await new CarreraRepository(db).GuardarAsync(carrera, true, default);
        var inicial = (await db.Mallas.AsNoTracking().SingleAsync()).Id;
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER FalloHu07 ON Mallas AFTER INSERT AS THROW 51009, 'Fallo sintético posterior a desactivar', 1;");
        await Assert.ThrowsAsync<DbUpdateException>(() => new MallaRepository(db).CrearAsync(new Malla(carrera.Id, "Nueva fallida", true), default));
        db.ChangeTracker.Clear();
        var unica = Assert.Single(await db.Mallas.AsNoTracking().ToListAsync());
        Assert.Equal(inicial, unica.Id); Assert.True(unica.Vigente);
    }
}
