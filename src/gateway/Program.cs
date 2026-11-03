using System.Text;
using Gateway.OpenApi;
using Gateway.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace Gateway;

public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddProblemDetails();
        builder.Services.AddOptions<GatewayJwtOptions>().BindConfiguration(GatewayJwtOptions.Section)
            .ValidateDataAnnotations().ValidateOnStart();
        builder.Services.AddOptions<GatewayHttpsOptions>().BindConfiguration(GatewayHttpsOptions.Section)
            .ValidateDataAnnotations().ValidateOnStart();
        builder.Services.AddHsts(options =>
        {
            options.ExcludedHosts.Clear();
            options.MaxAge = TimeSpan.FromMinutes(1);
        });
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<GatewayJwtOptions>>((bearer, configured) =>
            {
                var jwt = configured.Value;
                bearer.MapInboundClaims = false;
                bearer.IncludeErrorDetails = false;
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
        builder.Services.AddAuthorization(options =>
        {
            var academic = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser().RequireRole("Administrador", "Docente", "Coordinador").Build();
            options.AddPolicy("AcademicAccess", academic);
            options.DefaultPolicy = academic;
            options.FallbackPolicy = academic;
        });
        builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "FISEI-UTA Gateway — HU-01 / T2 / Issue #1", Version = "v1" });
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            { Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT" });
            options.DocumentFilter<ProxyDocumentFilter>();
        });
        var app = builder.Build();
        _ = app.Services.GetRequiredService<IOptions<GatewayJwtOptions>>().Value;
        var https = app.Services.GetRequiredService<IOptions<GatewayHttpsOptions>>().Value;
        GatewayConfiguration.ValidateDestinations(builder.Configuration);
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        if (https.Enabled)
        {
            app.UseHsts();
            app.Use(async (context, next) =>
            {
                if (context.Request.IsHttps)
                {
                    await next();
                    return;
                }

                if (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method))
                {
                    context.Response.StatusCode = StatusCodes.Status308PermanentRedirect;
                    context.Response.Headers.Location = https.PublicOrigin.TrimEnd('/')
                        + context.Request.PathBase + context.Request.Path + context.Request.QueryString;
                    return;
                }

                context.Response.StatusCode = StatusCodes.Status403Forbidden;
            });
        }
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapReverseProxy();
        app.Run();
    }
}
