using Homologacion.Domain.Antiguedad;

namespace Homologacion.Api.Contracts;

public sealed record ValidarAntiguedadResponse(string Estado, string Motivo)
{
    public static ValidarAntiguedadResponse FromDomain(ResultadoAntiguedad resultado)
        => new(resultado.Estado.ToString(), resultado.Motivo);
}
