using Homologacion.Domain.Equivalencias;
using Xunit;

namespace Homologacion.Domain.Tests;

public class EstadosEquivalenciaTests
{
    [Fact]
    public void Distingue_evaluacion_incompleta_insuficiente_y_suficiente()
    {
        Assert.Equal(EstadoEquivalencia.InformacionIncompleta, MotorEquivalencia.Evaluar(null, 80, 80, 80).Estado);
        Assert.Equal(EstadoEquivalencia.NoHomologable, MotorEquivalencia.Evaluar(79.9m, 80, 80, 80).Estado);
        Assert.Equal(EstadoEquivalencia.Homologable, MotorEquivalencia.Evaluar(80, 80, 80, 80).Estado);
    }

    [Fact]
    public void Cero_conocido_no_es_ausencia_de_informacion()
        => Assert.Equal(EstadoEquivalencia.NoHomologable, MotorEquivalencia.Evaluar(0, 100, 100, 80).Estado);

    [Fact]
    public void Dimension_desconocida_conserva_estado_incompleto()
        => Assert.Equal(EstadoEquivalencia.InformacionIncompleta, MotorEquivalencia.Evaluar(null, 79.9m, 100, 80).Estado);
}
