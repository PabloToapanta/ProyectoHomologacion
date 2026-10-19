using System.ComponentModel.DataAnnotations;
using System.Text;

namespace AuthService.Security;

public sealed class JwtOptions : IValidatableObject
{
    public const string Section = "Jwt";
    [Required] public string Issuer { get; set; } = "";
    [Required] public string Audience { get; set; } = "";
    [Required] public string SigningKey { get; set; } = "";
    [Range(1, 60)] public int ExpirationMinutes { get; set; } = 15;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Issuer) || string.IsNullOrWhiteSpace(Audience))
            yield return new ValidationResult("Jwt:Issuer y Jwt:Audience son obligatorios.");
        if (string.IsNullOrWhiteSpace(SigningKey) || Encoding.UTF8.GetByteCount(SigningKey) < 32)
            yield return new ValidationResult("Jwt:SigningKey requiere al menos 32 bytes UTF-8.");
    }
}
