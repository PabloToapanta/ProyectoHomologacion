using System.Text;
using AuthService.Data;
using AuthService.Models;
using AuthService.Security;
using AuthService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "FISEI-UTA Auth Service — HU-01 / Issue #1", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    { Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT" });
});
builder.Services.AddOptions<JwtOptions>().BindConfiguration(JwtOptions.Section)
    .ValidateDataAnnotations().ValidateOnStart();
builder.Services.Configure<AdminSeedOptions>(builder.Configuration.GetSection("SeedAdmin"));
builder.Services.AddDbContext<AuthDbContext>(options => options.UseSqlServer(
    builder.Configuration.GetConnectionString("Auth")));
builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<JwtTokenIssuer>();
builder.Services.AddScoped<LoginService>();
builder.Services.AddScoped<AdminSeeder>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((bearer, configured) =>
    {
        var jwt = configured.Value;
        bearer.MapInboundClaims = false;
        bearer.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidIssuer = jwt.Issuer,
            ValidateAudience = true, ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ValidateLifetime = true, RequireExpirationTime = true, RequireSignedTokens = true,
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ClockSkew = TimeSpan.Zero, NameClaimType = "name", RoleClaimType = "role"
        };
    });
builder.Services.AddAuthorization();
var app = builder.Build();
// Validar secretos antes de tocar la base de datos. No incluir sus valores en los errores.
if (string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("Auth")))
    throw new InvalidOperationException("Configure ConnectionStrings:Auth para la base exclusiva de autenticación.");
_ = app.Services.GetRequiredService<IOptions<JwtOptions>>().Value;
using (var scope = app.Services.CreateScope())
{
    if (builder.Configuration.GetValue<bool>("Database:ApplyMigrations"))
        await scope.ServiceProvider.GetRequiredService<AuthDbContext>().Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<AdminSeeder>().SeedAsync();
}
app.UseExceptionHandler();
app.UseStatusCodePages();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program;
