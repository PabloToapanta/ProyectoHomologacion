using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Solicitudes.Domain;
using Solicitudes.Infrastructure;
using Xunit;
namespace Solicitudes.Tests;
public sealed class SqlServerFactAttribute : FactAttribute {
 public SqlServerFactAttribute(){if(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("HU10_TEST_SQL")))Skip="Requiere HU10_TEST_SQL y SQL Server aislado; se ejecuta en CI.";}
}
public class SqlServerTests {
 [SqlServerFact] public async Task MigracionDurabilidadYQuipuxUnicoInclusoConcurrente() {
  var connection=new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("HU10_TEST_SQL")) {InitialCatalog="Hu10Tests_"+Guid.NewGuid().ToString("N")};
  var options=new DbContextOptionsBuilder<SolicitudesDbContext>().UseSqlServer(connection.ConnectionString).Options;
  await using var admin=new SolicitudesDbContext(options);
  try {
   await admin.Database.MigrateAsync();
   var receptora=Guid.NewGuid();var origen=Guid.NewGuid();
   Solicitud Nuevo()=>new("Q-CONCURRENTE","EST-SINTETICO","Estudiante sintético","Interno",origen,null,null,receptora);
   async Task<bool> Insertar(){await using var db=new SolicitudesDbContext(options);try{await new SolicitudRepository(db).Agregar(Nuevo(),default);return true;}catch(QuipuxDuplicadoException){return false;}}
   var results=await Task.WhenAll(Insertar(),Insertar());Assert.Single(results,x=>x);
   await using var lectura=new SolicitudesDbContext(options);Assert.Equal(1,await lectura.Solicitudes.CountAsync());
   var saved=await lectura.Solicitudes.AsNoTracking().SingleAsync();Assert.Equal("Registrada",saved.Estado);Assert.Equal(receptora,saved.CarreraReceptoraId);
   // Saltar la consulta previa demuestra que el índice SQL también rechaza duplicados.
   await using var directo=new SolicitudesDbContext(options);directo.Solicitudes.Add(Nuevo());
   var error=await Assert.ThrowsAsync<DbUpdateException>(()=>directo.SaveChangesAsync());Assert.True(error.InnerException is SqlException {Number:2601 or 2627});
  } finally {if(connection.InitialCatalog.StartsWith("Hu10Tests_",StringComparison.Ordinal)) await admin.Database.EnsureDeletedAsync();}
 }
 [SqlServerFact] public async Task AsignaturaAprobadaPersisteYNoQuedaHuerfana() {
  var connection=new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("HU10_TEST_SQL")) {InitialCatalog="Hu10Tests_"+Guid.NewGuid().ToString("N")};
  var options=new DbContextOptionsBuilder<SolicitudesDbContext>().UseSqlServer(connection.ConnectionString).Options;
  await using var admin=new SolicitudesDbContext(options);
  try {
   await admin.Database.MigrateAsync();
   var solicitud=new Solicitud("Q-HU11","EST-SINTETICO","Estudiante sintético","Interno",Guid.NewGuid(),null,null,Guid.NewGuid());
   var hoy=new DateOnly(2026,6,15);
   await using(var db=new SolicitudesDbContext(options)) {db.Solicitudes.Add(solicitud);db.AsignaturasAprobadas.Add(AsignaturaAprobada.Interna(solicitud.Id,Guid.NewGuid(),"COD-1","Álgebra sintética",3.5m,144,8.55m,hoy,"MAT-SINTETICA-1",2,hoy));db.AsignaturasAprobadas.Add(AsignaturaAprobada.Externa(solicitud.Id,"Álgebra externa",4m,"Créditos sintéticos",64,"Horas sintéticas","PROGRAMA-SINTETICO-1",14m,20m,12m,hoy,"MAT-SINTETICA-2",1,hoy));await db.SaveChangesAsync();}
   await using var lectura=new SolicitudesDbContext(options);var saved=await lectura.AsignaturasAprobadas.AsNoTracking().SingleAsync(x=>x.AsignaturaOrigenId!=null);
   Assert.Equal(8.55m,saved.Nota);Assert.Equal(3.5m,saved.Creditos);Assert.Equal(hoy,saved.FechaAprobacion);Assert.Equal(solicitud.Id,saved.SolicitudId);Assert.Equal("MAT-SINTETICA-1",saved.NumeroMatricula);Assert.Equal(2,saved.IntentoMatricula);
   Assert.Equal("Horas sintéticas",(await lectura.AsignaturasAprobadas.AsNoTracking().SingleAsync(x=>x.AsignaturaOrigenId==null)).UnidadHoras);
   await using var huerfana=new SolicitudesDbContext(options);huerfana.AsignaturasAprobadas.Add(AsignaturaAprobada.Interna(Guid.NewGuid(),Guid.NewGuid(),"COD-1","Sin solicitud",3m,144,8m,hoy,"MAT-SINTETICA-3",1,hoy));
   await Assert.ThrowsAsync<DbUpdateException>(()=>huerfana.SaveChangesAsync());
  } finally {if(connection.InitialCatalog.StartsWith("Hu10Tests_",StringComparison.Ordinal)) await admin.Database.EnsureDeletedAsync();}
 }
 [SqlServerFact] public async Task AsignacionConcurrenteSoloAdmiteUnPar() {
  var connection=new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("HU10_TEST_SQL")) {InitialCatalog="Hu10Tests_"+Guid.NewGuid().ToString("N")};
  var options=new DbContextOptionsBuilder<SolicitudesDbContext>().UseSqlServer(connection.ConnectionString).Options;
  await using var admin=new SolicitudesDbContext(options);
  try {
   await admin.Database.MigrateAsync();
   var solicitud=new Solicitud("Q-HU29","EST-SINTETICO","Estudiante sintético","Interno",Guid.NewGuid(),null,null,Guid.NewGuid());
   await using(var db=new SolicitudesDbContext(options)) {db.Solicitudes.Add(solicitud);await db.SaveChangesAsync();}
   DocenteDisponible[] docentes=[new(Guid.NewGuid(),"docente.ana"),new(Guid.NewGuid(),"docente.luis"),new(Guid.NewGuid(),"docente.rosa")];
   await using var a=new SolicitudesDbContext(options);await using var b=new SolicitudesDbContext(options);
   var repoA=new SolicitudRepository(a);var repoB=new SolicitudRepository(b);
   var sa=(await repoA.ObtenerParaActualizar(solicitud.Id,default))!;var sb=(await repoB.ObtenerParaActualizar(solicitud.Id,default))!;
   var pa=DocenteAsignado.Asignar(sa,[docentes[0].Id,docentes[1].Id],docentes);var pb=DocenteAsignado.Asignar(sb,[docentes[2].Id,docentes[1].Id],docentes);
   await repoA.GuardarAsignacion(pa,default);
   await Assert.ThrowsAsync<AsignacionNoPermitidaException>(()=>repoB.GuardarAsignacion(pb,default));
   await using var lectura=new SolicitudesDbContext(options);
   Assert.Equal([docentes[0].Id,docentes[1].Id],await lectura.DocentesAsignados.AsNoTracking().OrderBy(x=>x.Orden).Select(x=>x.DocenteId).ToListAsync());
   Assert.Equal("Docentes asignados",(await lectura.Solicitudes.AsNoTracking().SingleAsync()).Estado);
  } finally {if(connection.InitialCatalog.StartsWith("Hu10Tests_",StringComparison.Ordinal)) await admin.Database.EnsureDeletedAsync();}
 }
}
