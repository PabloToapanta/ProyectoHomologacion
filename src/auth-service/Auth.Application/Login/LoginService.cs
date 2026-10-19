using Auth.Application.Abstractions;

namespace Auth.Application.Login;

public sealed class LoginService(IUserRepository users, IPasswordService passwords, ITokenIssuer tokens)
{
    public async Task<LoginResult?> LoginAsync(LoginCommand request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.NombreUsuario) || string.IsNullOrWhiteSpace(request.Password)
            || request.NombreUsuario.Length > 256 || request.Password.Length > 1024)
            return null;

        var normalized = request.NombreUsuario.Trim().ToUpperInvariant();
        var usuario = await users.FindByNormalizedNameAsync(normalized, cancellationToken);
        var verification = passwords.Verify(usuario, request.Password);
        if (usuario is null || !usuario.Activo || verification == PasswordVerification.Failed)
            return null;

        if (verification == PasswordVerification.SuccessRehashNeeded)
            await users.UpdatePasswordHashAsync(usuario, passwords.Hash(usuario, request.Password), cancellationToken);

        return tokens.Create(usuario);
    }
}
