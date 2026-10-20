using Homologacion.Application.Antiguedad;
using Homologacion.Domain.Antiguedad;
using Xunit;

namespace Homologacion.Application.Tests;

public class ValidarAntiguedadTests
{
    [Fact]
    public void Caso_de_uso_conserva_resultado_y_fechas_explicitas()
    {
        var resultado = new ValidarAntiguedad().Ejecutar(new SolicitudAntiguedad(
            EstadoAsignatura.Aprobada, new DateOnly(2016, 10, 20), new DateOnly(2026, 10, 20)));
        Assert.Equal(EstadoAntiguedad.Elegible, resultado.Estado);
        Assert.NotEmpty(resultado.Motivo);
    }
}
