using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Auth.Infrastructure.Repositories;
using Auth.Application.Login;
using Auth.Infrastructure.Data;
using Auth.Domain.Entities;
using Auth.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace AuthService.Tests;

public sealed class AuthBehaviorTests : IDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly AuthDbContext db;
    private readonly PasswordHasher<Usuario> hasher = new();
    private readonly JwtOptions jwt = new()
    {
        Issuer = "auth-test", Audience = "api-test",
        SigningKey = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)),
        ExpirationMinutes = 15
    };
    private readonly DateTimeOffset now = new(2026, 10, 19, 12, 0, 0, TimeSpan.Zero);
    private const string TestPassword = "Solo-Prueba-123!";

    public AuthBehaviorTests()
    {
        connection.Open();
        db = new AuthDbContext(new DbContextOptionsBuilder<AuthDbContext>().UseSqlite(connection).Options);
        db.Database.EnsureCreated();
    }

    private LoginService Service() => new(new UserRepository(db), new PasswordService(hasher), new JwtTokenIssuer(Options.Create(jwt), new FixedClock(now)));

    private async Task<Usuario> AddUser(bool activo = true)
    {
        var user = new Usuario
        {
            NombreUsuario = "admin-prueba", NombreNormalizado = "ADMIN-PRUEBA", Activo = activo,
            Rol = await db.Roles.SingleAsync(x => x.Nombre == "Administrador")
        };
        user.PasswordHash = hasher.HashPassword(user, TestPassword);
        db.Usuarios.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task Correo_como_usuario_de_prueba_se_autentica_con_password_local()
    {
        var correo = "usuario.sintetico@uta.edu.ec";
        await new AdminSeeder(db, hasher, Options.Create(new AdminSeedOptions {
            Enabled = true, NombreUsuario = correo, Password = TestPassword
        })).SeedAsync();
        Assert.NotNull(await Service().LoginAsync(new LoginCommand(correo.ToUpperInvariant(), TestPassword)));
        Assert.Null(await Service().LoginAsync(new LoginCommand(correo, "incorrecta")));
    }

    [Fact]
    public async Task CredencialesCorrectas_EmitenJwtFirmadoConRolYExpiracion()
    {
        var user = await AddUser();
        var result = await Service().LoginAsync(new LoginCommand(" ADMIN-prueba ", TestPassword));
        Assert.NotNull(result);
        Assert.Equal("Bearer", result.TokenType);
        Assert.Equal(now.AddMinutes(15), result.ExpiresAtUtc);
        var principal = new JwtSecurityTokenHandler { MapInboundClaims = false }.ValidateToken(result.AccessToken,
            new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                ValidateIssuer = true, ValidIssuer = jwt.Issuer, ValidateAudience = true, ValidAudience = jwt.Audience,
                ValidateLifetime = true, ClockSkew = TimeSpan.Zero,
                LifetimeValidator = (nbf, exp, _, _) => nbf <= now.UtcDateTime && exp > now.UtcDateTime,
                RoleClaimType = "role"
            }, out var token);
        Assert.Equal(user.Id.ToString(), principal.FindFirst("sub")?.Value);
        Assert.True(principal.IsInRole("Administrador"));
        Assert.Equal(SecurityAlgorithms.HmacSha256, ((JwtSecurityToken)token).Header.Alg);
        Assert.NotNull(principal.FindFirst("jti"));
        Assert.DoesNotContain("Password", result.AccessToken, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("admin-prueba", "incorrecta", true)]
    [InlineData("desconocido", TestPassword, true)]
    [InlineData("admin-prueba", TestPassword, false)]
    [InlineData("", "", true)]
    public async Task CredencialesIncorrectasOUsuarioInactivo_NoEmitenToken(string nombre, string password, bool activo)
    {
        await AddUser(activo);
        Assert.Null(await Service().LoginAsync(new LoginCommand(nombre, password)));
    }

    [Fact]
    public async Task Semilla_CreaAdministradorHasheado_YEsIdempotente()
    {
        var options = new AdminSeedOptions { Enabled = true, NombreUsuario = "admin-prueba", Password = TestPassword };
        var seeder = new AdminSeeder(db, hasher, Options.Create(options));
        await seeder.SeedAsync();
        var originalHash = (await db.Usuarios.SingleAsync()).PasswordHash;
        options.Password = "Otra-Prueba-456!";
        await seeder.SeedAsync();
        var user = await db.Usuarios.Include(x => x.Rol).SingleAsync();
        Assert.Equal("Administrador", user.Rol.Nombre);
        Assert.Equal("ADMIN-PRUEBA", user.NombreNormalizado);
        Assert.NotEqual(TestPassword, user.PasswordHash);
        Assert.Equal(originalHash, user.PasswordHash);
        Assert.NotEqual(PasswordVerificationResult.Failed, hasher.VerifyHashedPassword(user, user.PasswordHash, TestPassword));
        Assert.Equal(3, await db.Roles.CountAsync());
    }

    [Fact]
    public async Task SemillaDeshabilitada_NoCreaUsuarios()
    {
        await new AdminSeeder(db, hasher, Options.Create(new AdminSeedOptions())).SeedAsync();
        Assert.Empty(await db.Usuarios.ToListAsync());
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("admin", "corta")]
    [InlineData(" ", TestPassword)]
    public async Task SemillaSinCredencialesSeguras_FallaSinCrearUsuario(string nombre, string password)
    {
        var seeder = new AdminSeeder(db, hasher, Options.Create(new AdminSeedOptions
        { Enabled = true, NombreUsuario = nombre, Password = password }));
        await Assert.ThrowsAsync<OptionsValidationException>(() => seeder.SeedAsync());
        Assert.Empty(await db.Usuarios.ToListAsync());
    }

    [Fact]
    public async Task NombresNormalizadosDuplicados_SeRechazanEnBaseDeDatos()
    {
        var original = await AddUser();
        db.Usuarios.Add(new Usuario { NombreUsuario = "ADMIN-PRUEBA", NombreNormalizado = original.NombreNormalizado,
            PasswordHash = original.PasswordHash, RolId = original.RolId });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public void Hash_UsaSaltDiferente_YRechazaPasswordIncorrecto()
    {
        var user = new Usuario();
        var first = hasher.HashPassword(user, TestPassword);
        Assert.NotEqual(first, hasher.HashPassword(user, TestPassword));
        Assert.Equal(PasswordVerificationResult.Failed, hasher.VerifyHashedPassword(user, first, "incorrecta"));
    }

    [Fact]
    public async Task Semilla_NoPromueveUsuarioExistenteNiReactivaCuentas()
    {
        var user = await AddUser();
        user.Rol = await db.Roles.SingleAsync(x => x.Nombre == "Docente");
        user.Activo = false;
        await db.SaveChangesAsync();
        var originalHash = user.PasswordHash;
        var seeder = new AdminSeeder(db, hasher, Options.Create(new AdminSeedOptions
        { Enabled = true, NombreUsuario = "admin-prueba", Password = TestPassword }));
        await Assert.ThrowsAsync<InvalidOperationException>(() => seeder.SeedAsync());
        Assert.Equal("Docente", user.Rol.Nombre);
        Assert.False(user.Activo);
        Assert.Equal(originalHash, user.PasswordHash);
    }

    [Fact]
    public async Task Login_ActualizaHashConMenorCostoSinCambiarPassword()
    {
        var user = await AddUser();
        var legacyHasher = new PasswordHasher<Usuario>(Options.Create(new PasswordHasherOptions { IterationCount = 10000 }));
        user.PasswordHash = legacyHasher.HashPassword(user, TestPassword);
        await db.SaveChangesAsync();
        var originalHash = user.PasswordHash;
        Assert.NotNull(await Service().LoginAsync(new LoginCommand(user.NombreUsuario, TestPassword)));
        Assert.NotEqual(originalHash, user.PasswordHash);
        Assert.Equal(PasswordVerificationResult.Success, hasher.VerifyHashedPassword(user, user.PasswordHash, TestPassword));
    }

    public void Dispose() { db.Dispose(); connection.Dispose(); }
    private sealed class FixedClock(DateTimeOffset instant) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => instant;
    }
}
