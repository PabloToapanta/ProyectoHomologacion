using System.ComponentModel.DataAnnotations;

namespace Gateway.Security;

public sealed class GatewayHttpsOptions : IValidatableObject
{
    public const string Section = "Https";
    public bool Enabled { get; set; }
    public string PublicOrigin { get; set; } = "";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Enabled) yield break;
        if (!Uri.TryCreate(PublicOrigin, UriKind.Absolute, out var origin)
            || origin.Scheme != Uri.UriSchemeHttps
            || string.IsNullOrEmpty(origin.Host)
            || !string.IsNullOrEmpty(origin.UserInfo)
            || origin.AbsolutePath != "/"
            || !string.IsNullOrEmpty(origin.Query)
            || !string.IsNullOrEmpty(origin.Fragment))
            yield return new ValidationResult("Https:PublicOrigin debe ser un origen HTTPS sin ruta, credenciales, query ni fragmento.");
    }
}
