using System.ComponentModel.DataAnnotations;

namespace AuthService.Contracts;

public sealed record LoginRequest(
    [Required, StringLength(256)] string NombreUsuario,
    [Required, StringLength(1024)] string Password);

public sealed record LoginResponse(string AccessToken, string TokenType, DateTimeOffset ExpiresAtUtc);
