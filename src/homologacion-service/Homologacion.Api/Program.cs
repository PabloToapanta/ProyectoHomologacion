using System.Text;
using Homologacion.Api.Security;
using Homologacion.Application.Antiguedad;
using Homologacion.Application.Equivalencias;
using Homologacion.Application.RequisitosCambio;
using Homologacion.Infrastructure.Configuration;
using Homologacion.Infrastructure.AnalisisInterno;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace Homologacion.Api;

public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddInternalAccess();
        builder.Services.AddProblemDetails();
        builder.Services.AddHomologacionInfrastructure();
        builder.Services.AddAnalisisInterno();
        builder.Services.AddScoped<EvaluarEquivalencia>();
        builder.Services.AddScoped<ValidarAntiguedad>();
        builder.Services.AddScoped<ValidarRequisitosCambio>();
        builder.Services.AddOptions<JwtOptions>().BindConfiguration("Jwt").ValidateDataAnnotations().ValidateOnStart();
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, configured) =>
            {
                var jwt = configured.Value;
                bearer.MapInboundClaims = false;
                bearer.IncludeErrorDetails = false;
                bearer.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        if (context.SecurityToken is not JsonWebToken { IsEncrypted: true,
                            Alg: SecurityAlgorithms.Aes256KW, Enc: SecurityAlgorithms.Aes256CbcHmacSha512 })
                            context.Fail("Se requiere un token cifrado.");
                        return Task.CompletedTask;
                    }
                };
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true, ValidIssuer = jwt.Issuer,
                    ValidateAudience = true, ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    TokenDecryptionKey = new SymmetricSecurityKey(Convert.FromBase64String(jwt.EncryptionKey)),
                    ValidateLifetime = true, RequireExpirationTime = true, RequireSignedTokens = true,
                    AlgorithmValidator = (algorithm, _, _, _) =>
                        algorithm is SecurityAlgorithms.HmacSha256 or SecurityAlgorithms.Aes256KW
                            or SecurityAlgorithms.Aes256CbcHmacSha512,
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = "name", RoleClaimType = "role"
                };
            });
        builder.Services.AddAuthorization(options =>
        {
            var academic = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser().RequireRole("Administrador", "Docente", "Coordinador").Build();
            options.DefaultPolicy = academic;
            options.FallbackPolicy = academic;
        });
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.OperationFilter<Homologacion.Api.Controllers.AgrupacionesOpenApi>();
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "Homologación FISEI-UTA — HU-14, HU-15, HU-16, HU-21, HU-23 y HU-24", Version = "v1",
                Description = "Equivalencia, antigüedad y requisitos de cambio son evaluaciones independientes, no una resolución oficial. HU-21: las agrupaciones N:M conservan sus aportes y revisión en el informe. HU-23: el docente asignado genera el informe fijo; docentes asignados y Coordinación consultan solo solicitudes FISEI existentes. HU-24: el navegador permite imprimir el informe y guardarlo como PDF; la exportación no está disponible antes de generarlo." });
            options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "Homologacion.Api.xml"), includeControllerXmlComments: true);
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            { Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT" });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = []
            });
        });
        var app = builder.Build();
        app.UseInternalAccess();
        _ = app.Services.GetRequiredService<IOptions<JwtOptions>>().Value;
        if (builder.Configuration.GetValue<bool>("Database:ApplyMigrations"))
        {
            using var scope = app.Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<ResultadosDbContext>().Database.Migrate();
        }
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.Run();
    }
}
