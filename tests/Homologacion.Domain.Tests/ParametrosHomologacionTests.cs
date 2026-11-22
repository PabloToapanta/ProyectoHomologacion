using Homologacion.Domain.Configuracion;
using Xunit;

namespace Homologacion.Domain.Tests;

public sealed class ParametrosHomologacionTests
{
    [Fact]
    public void Predeterminados_conservan_valores_acordados()
    {
        var parametros = ParametrosHomologacion.Predeterminados();

        Assert.NotEqual(Guid.Empty, parametros.VersionId);
        Assert.Equal(80, parametros.UmbralPct);
        Assert.Equal(10, parametros.AntiguedadMaximaAnios);
        Assert.Equal((1, 1, 2, 2), (parametros.PeriodosMinimosInterno,
            parametros.AsignaturasHomologablesMinimasInterno, parametros.PeriodosMinimosExterno,
            parametros.AsignaturasHomologablesMinimasExterno));
    }

    [Theory]
    [InlineData(0, 10, 1, 1, 2, 2)]
    [InlineData(101, 10, 1, 1, 2, 2)]
    [InlineData(80, 51, 1, 1, 2, 2)]
    [InlineData(80, 10, 0, 1, 2, 2)]
    [InlineData(80, 10, 1, 1, 2, 51)]
    public void Rechaza_valores_fuera_de_rango(int umbral, int anios, int periodosInternos,
        int materiasInternas, int periodosExternos, int materiasExternas)
        => Assert.ThrowsAny<ArgumentOutOfRangeException>(() => new ParametrosHomologacion(Guid.NewGuid(),
            umbral, anios, periodosInternos, materiasInternas, periodosExternos, materiasExternas));

    [Fact]
    public void Rechaza_version_vacia()
        => Assert.Throws<ArgumentException>(() => new ParametrosHomologacion(Guid.Empty, 80, 10, 1, 1, 2, 2));
}
