namespace Auth.Application.Login;

public sealed record LoginCommand(string NombreUsuario, string Password);

public sealed record LoginResult(string AccessToken, string TokenType, DateTimeOffset ExpiresAtUtc);
