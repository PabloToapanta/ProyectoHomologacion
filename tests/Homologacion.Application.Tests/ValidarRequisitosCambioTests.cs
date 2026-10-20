using Homologacion.Application.RequisitosCambio;
using Homologacion.Domain.RequisitosCambio;
using Xunit;

namespace Homologacion.Application.Tests;

public class ValidarRequisitosCambioTests
{
    [Theory]
    [InlineData(TipoCambio.Interno, 1, 1, 0, true)]
    [InlineData(TipoCambio.Interno, 1, 0, 0, false)]
    [InlineData(TipoCambio.Externo, 2, 2, 0, true)]
    [InlineData(TipoCambio.Externo, 2, 1, 0, false)]
    [InlineData(TipoCambio.Externo, 2, 2, 1, false)]
    public void Caso_de_uso_aplica_las_reglas(TipoCambio tipo, int periodos, int homologables, int cambios, bool esperado)
        => Assert.Equal(esperado, new ValidarRequisitosCambio().Ejecutar(
            new SolicitudRequisitosCambio(tipo, periodos, homologables, cambios)).Cumple);
}
