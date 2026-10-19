using Auth.Application.Abstractions;
using Auth.Domain.Entities;
using Auth.Infrastructure.Data;
using Auth.Infrastructure.Repositories;
using Auth.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Auth.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AuthDbContext>(options => options.UseSqlServer(configuration.GetConnectionString("Auth")));
        services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ITokenIssuer, JwtTokenIssuer>();
        services.AddScoped<AdminSeeder>();
        services.AddScoped<AuthDatabaseInitializer>();
        return services;
    }
}
