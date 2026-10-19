using AuthService.Contracts;
using AuthService.Data;
using AuthService.Models;
using AuthService.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Services;

public sealed class LoginService(AuthDbContext db, IPasswordHasher<Usuario> hasher, JwtTokenIssuer tokens)
{
    // Hash sintético para realizar una verificación costosa también cuando la cuenta no existe.
    private static readonly Usuario DummyUser = new();
    private static readonly string DummyHash = new PasswordHasher<Usuario>().HashPassword(
        DummyUser, Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)));

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.NombreUsuario) || string.IsNullOrWhiteSpace(request.Password)
            || request.NombreUsuario.Length > 256 || request.Password.Length > 1024)
            return null;

        var normalized = request.NombreUsuario.Trim().ToUpperInvariant();
        var usuario = await db.Usuarios.Include(x => x.Rol)
            .SingleOrDefaultAsync(x => x.NombreNormalizado == normalized, cancellationToken);
        var verification = hasher.VerifyHashedPassword(usuario ?? DummyUser,
            usuario?.PasswordHash ?? DummyHash, request.Password);
        if (usuario is null || !usuario.Activo || verification == PasswordVerificationResult.Failed)
            return null;

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            usuario.PasswordHash = hasher.HashPassword(usuario, request.Password);
            await db.SaveChangesAsync(cancellationToken);
        }
        return tokens.Create(usuario);
    }
}
