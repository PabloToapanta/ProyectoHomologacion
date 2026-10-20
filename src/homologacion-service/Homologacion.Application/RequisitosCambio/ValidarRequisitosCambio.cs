using Homologacion.Domain.RequisitosCambio;

namespace Homologacion.Application.RequisitosCambio;

public sealed record SolicitudRequisitosCambio(TipoCambio TipoCambio, int PeriodosCursados,
    int AsignaturasHomologables, int CambiosPrevios);

public sealed class ValidarRequisitosCambio
{
    public ResultadoRequisitosCambio Ejecutar(SolicitudRequisitosCambio solicitud)
        => ValidadorRequisitosCambio.Validar(solicitud.TipoCambio, solicitud.PeriodosCursados,
            solicitud.AsignaturasHomologables, solicitud.CambiosPrevios);
}
