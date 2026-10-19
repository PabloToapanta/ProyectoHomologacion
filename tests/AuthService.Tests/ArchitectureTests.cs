using System.Reflection;
using System.Xml.Linq;
using Auth.Application.Login;
using Auth.Domain.Entities;
using Xunit;

namespace AuthService.Tests;

public sealed class ArchitectureTests
{
    private static string Root
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Homologacion.sln")))
                directory = directory.Parent;
            return directory?.FullName ?? throw new InvalidOperationException("No se encontró la raíz de la solución.");
        }
    }

    [Theory]
    [InlineData("Auth.Domain", "")]
    [InlineData("Auth.Application", "Auth.Domain")]
    [InlineData("Auth.Infrastructure", "Auth.Application,Auth.Domain")]
    [InlineData("Auth.Api", "Auth.Application,Auth.Infrastructure")]
    public void Proyectos_RespetanDireccionDeDependencias(string project, string dependencies)
    {
        var path = Path.Combine(Root, "src", "auth-service", project, project + ".csproj");
        Assert.True(File.Exists(path), $"Falta el proyecto independiente {project}");
        var document = XDocument.Load(path);
        var references = document.Descendants("ProjectReference")
            .Select(x => Path.GetFileNameWithoutExtension(x.Attribute("Include")!.Value)).Order().ToArray();
        Assert.Equal(dependencies.Split(',', StringSplitOptions.RemoveEmptyEntries).Order().ToArray(), references);
        if (project is "Auth.Domain" or "Auth.Application")
        {
            Assert.Empty(document.Descendants("PackageReference"));
            Assert.Empty(document.Descendants("FrameworkReference"));
            Assert.Empty(document.Descendants("Reference"));
            Assert.Equal("Microsoft.NET.Sdk", document.Root!.Attribute("Sdk")!.Value);
        }
    }
    [Theory]
    [InlineData("Domain")]
    [InlineData("Application")]
    public void EnsambladosInternos_NoReferencianFrameworksNiCapasExternas(string layer)
    {
        Assembly assembly = layer == "Domain" ? typeof(Usuario).Assembly : typeof(LoginService).Assembly;
        Assert.All(assembly.GetReferencedAssemblies(), reference => Assert.True(
            reference.Name!.StartsWith("System", StringComparison.Ordinal)
                || reference.Name is "netstandard" or "mscorlib"
                || (layer == "Application" && reference.Name == "Auth.Domain"),
            $"Dependencia no permitida desde {assembly.GetName().Name}: {reference.Name}"));
    }

    [Fact]
    public void LoginService_DependeSoloDeInterfacesDeAplicacion()
    {
        var constructor = Assert.Single(typeof(LoginService).GetConstructors());
        Assert.All(constructor.GetParameters(), parameter =>
        {
            Assert.True(parameter.ParameterType.IsInterface);
            Assert.Equal(typeof(LoginService).Assembly, parameter.ParameterType.Assembly);
        });
    }
}
