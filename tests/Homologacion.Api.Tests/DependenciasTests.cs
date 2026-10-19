using Homologacion.Application.Equivalencias;
using Homologacion.Domain.Equivalencias;
using Xunit;

namespace Homologacion.Api.Tests;

public class DependenciasTests
{
    [Fact]
    public void Domain_solo_referencia_bibliotecas_del_sistema()
        => Assert.All(typeof(MotorEquivalencia).Assembly.GetReferencedAssemblies(), referencia =>
            Assert.StartsWith("System", referencia.Name));

    [Fact]
    public void Application_solo_referencia_Domain_y_el_sistema()
        => Assert.All(typeof(EvaluarEquivalencia).Assembly.GetReferencedAssemblies(), referencia =>
            Assert.True(referencia.Name == "Homologacion.Domain" || referencia.Name!.StartsWith("System", StringComparison.Ordinal), referencia.Name));
}
