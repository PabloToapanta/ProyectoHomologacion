using Microsoft.Extensions.Options;

namespace Gateway;

public static class GatewayConfiguration
{
    public static void ValidateDestinations(IConfiguration configuration)
    {
        foreach (var cluster in new[] { "auth", "catalogo", "homologacion" })
        {
            var section = $"ReverseProxy:Clusters:{cluster}:Destinations";
            var destinations = configuration.GetSection(section).GetChildren().ToArray();
            if (destinations.Length == 0)
                throw Invalid($"Configure al menos un destino en {section}.");
            foreach (var destination in destinations)
            {
                var key = $"{destination.Path}:Address";
                if (!Uri.TryCreate(destination["Address"], UriKind.Absolute, out var uri)
                    || uri.Scheme is not ("http" or "https") || string.IsNullOrEmpty(uri.Host)
                    || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
                    throw Invalid($"{key} debe ser una URL absoluta HTTP(S), sin credenciales, query ni fragmento.");
            }
        }
    }

    private static OptionsValidationException Invalid(string message)
        => new("ReverseProxy", typeof(GatewayConfiguration), [message]);
}
