namespace Homologacion.Infrastructure.AnalisisInterno;

public sealed record OllamaAnalisisOptions(Uri BaseUrl, string Modelo, TimeSpan Timeout)
{
    public void Validar()
    {
        if (BaseUrl is null || !BaseUrl.IsAbsoluteUri || BaseUrl.Scheme is not ("http" or "https")
            || !string.IsNullOrEmpty(BaseUrl.UserInfo) || !string.IsNullOrEmpty(BaseUrl.Query)
            || !string.IsNullOrEmpty(BaseUrl.Fragment) || !BaseUrl.AbsolutePath.EndsWith('/'))
            throw new InvalidOperationException("Configure la URL base HTTP/HTTPS del agente, sin credenciales, query ni fragmento y terminada en /.");
        if (string.IsNullOrWhiteSpace(Modelo) || Modelo.Length > 128 || Modelo.Any(char.IsControl))
            throw new InvalidOperationException("Configure el modelo del agente.");
        if (Timeout <= TimeSpan.Zero || Timeout > TimeSpan.FromMinutes(5))
            throw new InvalidOperationException("Configure un timeout del agente positivo y de hasta cinco minutos.");
    }
}
