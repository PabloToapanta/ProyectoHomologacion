using Auth.Application.Abstractions;
using Auth.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Auth.Infrastructure.Security;

public sealed class PasswordService(IPasswordHasher<Usuario> hasher) : IPasswordService
{
    private static readonly Usuario DummyUser = new();
    private static readonly string DummyHash = new PasswordHasher<Usuario>().HashPassword(
        DummyUser, Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)));

    public PasswordVerification Verify(Usuario? usuario, string password)
        => hasher.VerifyHashedPassword(usuario ?? DummyUser, usuario?.PasswordHash ?? DummyHash, password) switch
        {
            PasswordVerificationResult.Success => PasswordVerification.Success,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordVerification.SuccessRehashNeeded,
            _ => PasswordVerification.Failed
        };

    public string Hash(Usuario usuario, string password) => hasher.HashPassword(usuario, password);
}
