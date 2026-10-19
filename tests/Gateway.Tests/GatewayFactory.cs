using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using Auth.Domain.Entities;
using Auth.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Yarp.ReverseProxy.Forwarder;

namespace Gateway.Tests;

public sealed class GatewayFactory : WebApplicationFactory<Gateway.Program>
{
    public const string Issuer = "hu01-gateway-tests";
    public const string Audience = "hu01-api-tests";
    public const string Password = "Prueba-Gateway-123!";
    public string Key { get; } = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64));
    public ConcurrentQueue<ForwardedRequest> Forwarded { get; } = new();
    private BackendFactory? backend;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = Issuer, ["Jwt:Audience"] = Audience, ["Jwt:SigningKey"] = Key,
            ["ReverseProxy:Clusters:auth:Destinations:primary:Address"] = "http://auth.test/",
            ["ReverseProxy:Clusters:catalogo:Destinations:primary:Address"] = "http://catalogo.test/",
            ["ReverseProxy:Clusters:homologacion:Destinations:primary:Address"] = "http://homologacion.test/"
        }));
        builder.ConfigureServices(services =>
        {
            backend = new BackendFactory(Key);
            services.RemoveAll<IForwarderHttpClientFactory>();
            services.AddSingleton<IForwarderHttpClientFactory>(new TestProxyClients(backend, Forwarded));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) backend?.Dispose();
    }

    private sealed class TestProxyClients(BackendFactory auth, ConcurrentQueue<ForwardedRequest> forwarded) : IForwarderHttpClientFactory
    {
        public HttpMessageInvoker CreateClient(ForwarderHttpClientContext context)
        {
            HttpMessageHandler handler = context.ClusterId == "auth" ? auth.Server.CreateHandler() : new EchoHandler();
            return new HttpMessageInvoker(new RecordingHandler(context.ClusterId, forwarded, handler));
        }
    }

    private sealed class RecordingHandler(string service, ConcurrentQueue<ForwardedRequest> forwarded, HttpMessageHandler inner)
        : DelegatingHandler(inner)
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            forwarded.Enqueue(new ForwardedRequest(service, request.Method.Method, request.RequestUri!.PathAndQuery,
                request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken),
                request.Headers.Authorization?.ToString()));
            return await base.SendAsync(request, cancellationToken);
        }
    }

    private sealed class EchoHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { destinoDePrueba = true }) });
    }

    private sealed class BackendFactory(string key) : WebApplicationFactory<global::Program>
    {
        private readonly SqliteConnection connection = new("Data Source=:memory:");
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            connection.Open();
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Auth"] = "Server=unused;Database=AuthTests;Integrated Security=true",
                ["Jwt:Issuer"] = Issuer, ["Jwt:Audience"] = Audience, ["Jwt:SigningKey"] = key,
                ["SeedAdmin:Enabled"] = "false", ["Database:ApplyMigrations"] = "false"
            }));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AuthDbContext>>();
                services.RemoveAll<AuthDbContext>();
                services.AddDbContext<AuthDbContext>(options => options.UseSqlite(connection));
                using var db = new AuthDbContext(new DbContextOptionsBuilder<AuthDbContext>().UseSqlite(connection).Options);
                db.Database.EnsureCreated();
                var user = new Usuario { NombreUsuario = "admin-gateway", NombreNormalizado = "ADMIN-GATEWAY",
                    Rol = db.Roles.Single(x => x.Nombre == "Administrador") };
                user.PasswordHash = new PasswordHasher<Usuario>().HashPassword(user, Password);
                db.Usuarios.Add(user);
                db.SaveChanges();
            });
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing) connection.Dispose();
        }
    }
}

public sealed record ForwardedRequest(string Service, string Method, string PathAndQuery, string? Body, string? Authorization);
