using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Data.SqlClient;
using Solicitudes.Application;
using Solicitudes.Domain;
namespace Solicitudes.Infrastructure;
public sealed class SolicitudesDbContext(DbContextOptions<SolicitudesDbContext> options) : DbContext(options) {
 public DbSet<Solicitud> Solicitudes=>Set<Solicitud>();
 public DbSet<AsignaturaAprobada> AsignaturasAprobadas=>Set<AsignaturaAprobada>();
 protected override void OnModelCreating(ModelBuilder b) {
  var s=b.Entity<Solicitud>();s.HasKey(x=>x.Id);
  s.Property(x=>x.NumeroQuipux).HasMaxLength(100).IsRequired();s.Property(x=>x.QuipuxNormalizado).HasMaxLength(100).IsRequired();
  s.HasIndex(x=>x.QuipuxNormalizado).IsUnique();
  s.Property(x=>x.IdentificacionEstudiante).HasMaxLength(50).IsRequired();s.Property(x=>x.NombreEstudiante).HasMaxLength(200).IsRequired();
  s.Property(x=>x.TipoCambio).HasMaxLength(20).IsRequired();s.Property(x=>x.Estado).HasMaxLength(30).IsRequired();
  s.Property(x=>x.InstitucionOrigen).HasMaxLength(200);s.Property(x=>x.CarreraOrigenExterna).HasMaxLength(200);
  s.Property(x=>x.FechaRegistro).HasConversion(v=>v,v=>DateTime.SpecifyKind(v,DateTimeKind.Utc));
  s.HasIndex(x=>x.FechaRegistro);
  var a=b.Entity<AsignaturaAprobada>();a.HasKey(x=>x.Id);
  a.Property(x=>x.CodigoAsignatura).HasMaxLength(50);a.Property(x=>x.NombreAsignatura).HasMaxLength(200).IsRequired();
  a.Property(x=>x.SistemaCreditos).HasMaxLength(100);a.Property(x=>x.UnidadHoras).HasMaxLength(100);a.Property(x=>x.ReferenciaPrograma).HasMaxLength(200);a.Property(x=>x.NumeroMatricula).HasMaxLength(50).IsRequired();
  a.Property(x=>x.Creditos).HasPrecision(6,2);a.Property(x=>x.Nota).HasPrecision(5,2);a.Property(x=>x.NotaMaximaEscala).HasPrecision(5,2);a.Property(x=>x.NotaMinimaAprobacion).HasPrecision(5,2);
  a.Ignore(x=>x.Estado);
  a.HasOne<Solicitud>().WithMany().HasForeignKey(x=>x.SolicitudId).OnDelete(DeleteBehavior.Restrict);
 }
}
public sealed class SolicitudesDbContextFactory : IDesignTimeDbContextFactory<SolicitudesDbContext> {
 public SolicitudesDbContext CreateDbContext(string[] args)=>new(new DbContextOptionsBuilder<SolicitudesDbContext>().UseSqlServer(Environment.GetEnvironmentVariable("ConnectionStrings__Solicitudes")??"Server=localhost;Database=HomologacionSolicitudes;Integrated Security=true;TrustServerCertificate=true").Options);
}
public sealed class SolicitudRepository(SolicitudesDbContext db) : ISolicitudRepository {
 public async Task Agregar(Solicitud s,CancellationToken ct) {
  if(await db.Solicitudes.AnyAsync(x=>x.QuipuxNormalizado==s.QuipuxNormalizado,ct)) throw new QuipuxDuplicadoException();
  db.Solicitudes.Add(s);
  try {await db.SaveChangesAsync(ct);} catch(DbUpdateException e) when(e.InnerException is SqlException {Number:2601 or 2627}) {throw new QuipuxDuplicadoException();}
 }
 public Task<Solicitud?> Obtener(Guid id,CancellationToken ct)=>db.Solicitudes.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id,ct);
 public async Task AgregarAsignatura(AsignaturaAprobada a,CancellationToken ct) {db.AsignaturasAprobadas.Add(a);await db.SaveChangesAsync(ct);}
 public async Task<IReadOnlyList<AsignaturaAprobada>> AsignaturasDe(Guid solicitudId,CancellationToken ct)=>await db.AsignaturasAprobadas.AsNoTracking().Where(x=>x.SolicitudId==solicitudId).OrderBy(x=>x.NombreAsignatura).ThenBy(x=>x.Id).ToListAsync(ct);
 public async Task<IReadOnlyList<Solicitud>> Listar(int pagina,CancellationToken ct)=>await db.Solicitudes.AsNoTracking().OrderByDescending(x=>x.FechaRegistro).ThenBy(x=>x.Id).Skip((pagina-1)*50).Take(50).ToListAsync(ct);
}
