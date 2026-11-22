using Homologacion.Domain.Configuracion;

namespace Homologacion.Application.Configuracion;

/// <summary>Obtiene una única versión inmutable de configuración al inicio de una evaluación.</summary>
public interface IParametrosHomologacionProvider
{
    Task<ParametrosHomologacion> ObtenerVigentesAsync(CancellationToken cancellationToken);
}
