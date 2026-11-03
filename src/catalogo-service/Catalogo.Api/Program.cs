using System.Text;
using Catalogo.Api;
using Catalogo.Application;
using Catalogo.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<CatalogoExceptionHandler>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo { Title = "Catálogo FISEI-UTA — HU-04/HU-05/HU-06", Version = "v1" });
    o.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "Catalogo.Api.xml"));
    o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT" });
    o.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = []
    });
});
var connection = builder.Configuration.GetConnectionString("Catalogo")
    ?? throw new InvalidOperationException("Configure ConnectionStrings:Catalogo para la base exclusiva del catálogo.");
builder.Services.AddDbContext<CatalogoDbContext>(o => o.UseSqlServer(connection));
builder.Services.AddScoped<ICarreraRepository, CarreraRepository>();
builder.Services.AddScoped<CarreraService>();
builder.Services.AddScoped<IAsignaturaRepository, AsignaturaRepository>();
builder.Services.AddScoped<AsignaturaService>();
builder.Services.AddScoped<IUnidadRepository, UnidadRepository>();
builder.Services.AddScoped<UnidadService>();
builder.Services.AddScoped<IComparacionCatalogoRepository, ComparacionCatalogoRepository>();
builder.Services.AddScoped<ComparacionCatalogoService>();

var issuer = builder.Configuration["Jwt:Issuer"];
var audience = builder.Configuration["Jwt:Audience"];
var signingKey = builder.Configuration["Jwt:SigningKey"];
var encryptionKey = builder.Configuration["Jwt:EncryptionKey"];
byte[] encryptionBytes = new byte[32];
if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience)
    || string.IsNullOrWhiteSpace(signingKey) || Encoding.UTF8.GetByteCount(signingKey) < 32)
    throw new InvalidOperationException("Configure Jwt:Issuer, Jwt:Audience y Jwt:SigningKey (mínimo 32 bytes UTF-8).");
if (string.IsNullOrWhiteSpace(encryptionKey)
    || !Convert.TryFromBase64String(encryptionKey, encryptionBytes, out var encryptionCount) || encryptionCount != 32)
    throw new InvalidOperationException("Configure Jwt:EncryptionKey con 32 bytes codificados en Base64.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.MapInboundClaims = false;
    o.IncludeErrorDetails = false;
    o.Events = new JwtBearerEvents
    {
        OnTokenValidated = context =>
        {
            if (context.SecurityToken is not JsonWebToken { IsEncrypted: true,
                Alg: SecurityAlgorithms.Aes256KW, Enc: SecurityAlgorithms.Aes256CbcHmacSha512 })
                context.Fail("Se requiere un token cifrado.");
            return Task.CompletedTask;
        }
    };
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidIssuer = issuer, ValidateAudience = true, ValidAudience = audience,
        ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
        TokenDecryptionKey = new SymmetricSecurityKey(Convert.FromBase64String(encryptionKey)),
        ValidateLifetime = true, RequireExpirationTime = true, RequireSignedTokens = true,
        AlgorithmValidator = (algorithm, _, _, _) =>
            algorithm is SecurityAlgorithms.HmacSha256 or SecurityAlgorithms.Aes256KW
                or SecurityAlgorithms.Aes256CbcHmacSha512,
        ClockSkew = TimeSpan.Zero,
        NameClaimType = "name", RoleClaimType = "role"
    };
});
builder.Services.AddAuthorization();
var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
if (app.Configuration.GetValue<bool>("Database:ApplyMigrations"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<CatalogoDbContext>().Database.MigrateAsync();
}
app.Run();

public partial class Program;
