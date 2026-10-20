using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Catalogo.Infrastructure;

public sealed class CatalogoDbContextFactory : IDesignTimeDbContextFactory<CatalogoDbContext>
{
    public CatalogoDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__Catalogo")
            ?? "Server=localhost;Database=Catalogo;Integrated Security=true;TrustServerCertificate=true";
        return new(new DbContextOptionsBuilder<CatalogoDbContext>().UseSqlServer(connection).Options);
    }
}
