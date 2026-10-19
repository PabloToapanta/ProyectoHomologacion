using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Data;

public sealed class AuthDatabaseInitializer(AuthDbContext db, AdminSeeder seeder)
{
    public async Task InitializeAsync(bool applyMigrations, CancellationToken cancellationToken = default)
    {
        if (applyMigrations)
            await db.Database.MigrateAsync(cancellationToken);
        await seeder.SeedAsync(cancellationToken);
    }
}
