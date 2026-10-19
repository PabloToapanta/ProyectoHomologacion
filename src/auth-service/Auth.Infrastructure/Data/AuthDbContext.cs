using Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Data;

public sealed class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Rol> Roles => Set<Rol>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Rol>(entity =>
        {
            entity.Property(x => x.Nombre).HasMaxLength(32).IsRequired();
            entity.HasIndex(x => x.Nombre).IsUnique();
            entity.HasData(
                new Rol { Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), Nombre = "Administrador" },
                new Rol { Id = Guid.Parse("00000000-0000-0000-0000-000000000002"), Nombre = "Docente" },
                new Rol { Id = Guid.Parse("00000000-0000-0000-0000-000000000003"), Nombre = "Coordinador" });
        });
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.Property(x => x.NombreUsuario).HasMaxLength(256).IsRequired();
            entity.Property(x => x.NombreNormalizado).HasMaxLength(256).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(1024).IsRequired();
            entity.HasIndex(x => x.NombreNormalizado).IsUnique();
            entity.HasOne(x => x.Rol).WithMany().HasForeignKey(x => x.RolId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
