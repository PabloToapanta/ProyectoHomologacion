using System.ComponentModel.DataAnnotations;

namespace Homologacion.Infrastructure.AnalisisInterno;

public sealed class ConfiguracionAnalisisInterno : IValidatableObject
{
    public const string Section = "AnalisisInterno";
    public bool Habilitado { get; set; }
    public string SolicitudesUrl { get; set; } = "http://solicitudes-service:8080/";
    public string CatalogoUrl { get; set; } = "http://catalogo-api:8080/";
    public string OllamaUrl { get; set; } = "";
    public string Modelo { get; set; } = "";
    public int TimeoutSegundos { get; set; } = 90;
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!UrlValida(SolicitudesUrl)) yield return new("Configure AnalisisInterno:SolicitudesUrl como URL HTTP(S) interna terminada en /, sin credenciales, query ni fragmento.");
        if (!UrlValida(CatalogoUrl)) yield return new("Configure AnalisisInterno:CatalogoUrl como URL HTTP(S) interna terminada en /, sin credenciales, query ni fragmento.");
        if (TimeoutSegundos is < 1 or > 300) yield return new("AnalisisInterno:TimeoutSegundos debe estar entre 1 y 300.");
        if (Habilitado)
        {
            if (!UrlValida(OllamaUrl)) yield return new("Configure AnalisisInterno:OllamaUrl como URL HTTP(S) terminada en /, sin credenciales, query ni fragmento.");
            if (string.IsNullOrWhiteSpace(Modelo) || Modelo.Length > 128 || Modelo.Any(char.IsControl)) yield return new("Configure AnalisisInterno:Modelo.");
        }
    }
    private static bool UrlValida(string value) => Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && uri.Scheme is "http" or "https" && !string.IsNullOrEmpty(uri.Host) && string.IsNullOrEmpty(uri.UserInfo)
        && string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment) && uri.AbsolutePath.EndsWith('/');
}
