using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Auth.Infrastructure.Security;

public sealed class JwtOptions : IValidatableObject
{
    public const string Section = "Jwt";
    [Required] public string Issuer { get; set; } = "";
    [Required] public string Audience { get; set; } = "";
    [Required] public string SigningKey { get; set; } = "";
    [Required] public string EncryptionKey { get; set; } = "";
    [Range(1, 60)] public int ExpirationMinutes { get; set; } = 15;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Issuer) || string.IsNullOrWhiteSpace(Audience))
            yield return new ValidationResult("Jwt:Issuer y Jwt:Audience son obligatorios.");
        if (string.IsNullOrWhiteSpace(SigningKey) || Encoding.UTF8.GetByteCount(SigningKey) < 32)
            yield return new ValidationResult("Jwt:SigningKey requiere al menos 32 bytes UTF-8.");
        if (!HasValidEncryptionKey(EncryptionKey))
            yield return new ValidationResult("Jwt:EncryptionKey debe contener 32 bytes aleatorios codificados en Base64.");
    }

    public static bool HasValidEncryptionKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        Span<byte> bytes = stackalloc byte[32];
        return Convert.TryFromBase64String(value, bytes, out var count) && count == 32;
    }
}
