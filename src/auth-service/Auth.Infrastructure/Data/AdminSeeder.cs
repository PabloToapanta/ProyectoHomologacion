using Auth.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Auth.Infrastructure.Data;

public sealed class AdminSeedOptions
{
    public bool Enabled { get; set; }
    public string NombreUsuario { get; set; } = "";
    public string Password { get; set; } = "";
}

public sealed class AdminSeeder(AuthDbContext db, IPasswordHasher<Usuario> hasher, IOptions<AdminSeedOptions> options)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (!settings.Enabled) return;
        if (string.IsNullOrWhiteSpace(settings.NombreUsuario) || settings.NombreUsuario.Length > 256
            || string.IsNullOrWhiteSpace(settings.Password) || settings.Password.Length is < 12 or > 1024)
            throw new OptionsValidationException("SeedAdmin", typeof(AdminSeedOptions),
                ["La semilla requiere un nombre de usuario (1–256 caracteres) y contraseña de 12–1024 caracteres por configuración externa."]);

        var normalized = settings.NombreUsuario.Trim().ToUpperInvariant();
        var existing = await db.Usuarios.Include(x => x.Rol)
            .SingleOrDefaultAsync(x => x.NombreNormalizado == normalized, cancellationToken);
        if (existing is not null)
        {
            // La semilla nunca reactiva cuentas, cambia roles ni restablece contraseñas.
            if (existing.Rol.Nombre != "Administrador" || !existing.Activo)
                throw new InvalidOperationException("El usuario semilla existente no es un administrador activo. Revise la configuración.");
            return;
        }
        var user = new Usuario
        {
            NombreUsuario = settings.NombreUsuario.Trim(), NombreNormalizado = normalized,
            Rol = await db.Roles.SingleAsync(x => x.Nombre == "Administrador", cancellationToken)
        };
        user.PasswordHash = hasher.HashPassword(user, settings.Password);
        db.Usuarios.Add(user);
        await db.SaveChangesAsync(cancellationToken);
    }
}
