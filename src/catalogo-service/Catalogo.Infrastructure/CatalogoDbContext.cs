using Catalogo.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catalogo.Infrastructure;

public sealed class CatalogoDbContext(DbContextOptions<CatalogoDbContext> options) : DbContext(options)
{
    public DbSet<Carrera> Carreras => Set<Carrera>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var carrera = modelBuilder.Entity<Carrera>();
        carrera.ToTable("Carreras");
        carrera.HasKey(c => c.Id);
        carrera.Property(c => c.Id).ValueGeneratedNever();
        carrera.Property(c => c.Nombre).HasMaxLength(200).IsRequired();
        carrera.Property(c => c.Facultad).HasMaxLength(200).IsRequired();
        carrera.Property(c => c.Modalidad).HasMaxLength(100);
        carrera.Property(c => c.NombreNormalizado).HasMaxLength(200).IsRequired();
        carrera.Property(c => c.FacultadNormalizada).HasMaxLength(200).IsRequired();
        if (Database.IsSqlServer())
        {
            carrera.Property(c => c.NombreNormalizado).UseCollation("Latin1_General_100_BIN2");
            carrera.Property(c => c.FacultadNormalizada).UseCollation("Latin1_General_100_BIN2");
        }
        carrera.HasIndex(c => new { c.NombreNormalizado, c.FacultadNormalizada }).IsUnique();

        // Ancla relacional mínima. HU-05 la ampliará con su entidad académica.
        var referencia = modelBuilder.Entity<AsignaturaReferencia>();
        referencia.ToTable("Asignaturas");
        referencia.HasKey(a => a.Id);
        referencia.Property(a => a.Id).ValueGeneratedNever();
        referencia.HasOne<Carrera>().WithMany().HasForeignKey(a => a.CarreraId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class AsignaturaReferencia
{
    public Guid Id { get; set; }
    public Guid CarreraId { get; set; }
}
