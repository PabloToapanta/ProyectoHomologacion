using Homologacion.Infrastructure.AnalisisInterno;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace Homologacion.Infrastructure.Agrupaciones;
// Solo herramientas EF: conexión de diseño sin secretos; no abre ninguna base.
public sealed class ResultadosDesignFactory : IDesignTimeDbContextFactory<ResultadosDbContext>
{
    public ResultadosDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<ResultadosDbContext>()
        .UseSqlServer("Server=localhost;Database=Hu21DesignOnly;Integrated Security=true;Encrypt=true").Options);
}
