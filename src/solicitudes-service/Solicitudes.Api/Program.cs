using System.Text;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Solicitudes.Application;
using Solicitudes.Infrastructure;
using Solicitudes.Api;
using Solicitudes.Api.Security;
var builder=WebApplication.CreateBuilder(args);
builder.Services.AddInternalAccess();
builder.Services.AddControllers(o=>o.Filters.Add<SolicitudesExceptionFilter>()).AddJsonOptions(o=>o.JsonSerializerOptions.UnmappedMemberHandling=System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow);
builder.Services.AddProblemDetails();builder.Services.AddExceptionHandler<SolicitudesExceptionHandler>();
builder.Services.AddEndpointsApiExplorer();builder.Services.AddSwaggerGen(o=>{
 o.SwaggerDoc("v1",new OpenApiInfo {Title="Solicitudes FISEI-UTA — HU-10 / Issue #38",Version="v1"});
 o.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory,"Solicitudes.Api.xml"));
 o.AddSecurityDefinition("Bearer",new OpenApiSecurityScheme {Type=SecuritySchemeType.Http,Scheme="bearer"});
 o.AddSecurityRequirement(new OpenApiSecurityRequirement { [new OpenApiSecurityScheme {Reference=new OpenApiReference {Type=ReferenceType.SecurityScheme,Id="Bearer"}}]=[] });
});
builder.Services.AddDbContext<SolicitudesDbContext>(o=>o.UseSqlServer(builder.Configuration.GetConnectionString("Solicitudes")??throw new InvalidOperationException("Configure ConnectionStrings:Solicitudes.")));
builder.Services.AddSingleton(TimeProvider.System);builder.Services.AddScoped<ISolicitudRepository,SolicitudRepository>();builder.Services.AddScoped<SolicitudService>();
builder.Services.AddHttpContextAccessor();builder.Services.AddTransient<CatalogoAuthorizationHandler>();
var catalogoUrl=builder.Configuration["Catalogo:BaseUrl"];
if(!Uri.TryCreate(catalogoUrl,UriKind.Absolute,out var catalogoUri) || catalogoUri.Scheme is not ("http" or "https")) throw new InvalidOperationException("Configure Catalogo:BaseUrl con la dirección interna del catálogo.");
builder.Services.AddHttpClient<ICatalogoCarreras,CatalogoCarreras>(h=>{h.BaseAddress=catalogoUri;h.Timeout=TimeSpan.FromSeconds(10);}).ConfigurePrimaryHttpMessageHandler(()=>new HttpClientHandler {AllowAutoRedirect=false}).AddHttpMessageHandler<CatalogoAuthorizationHandler>();
var authUrl=builder.Configuration["Auth:BaseUrl"];
if(!Uri.TryCreate(authUrl,UriKind.Absolute,out var authUri) || authUri.Scheme is not ("http" or "https")) throw new InvalidOperationException("Configure Auth:BaseUrl con la dirección interna del servicio de autenticación.");
builder.Services.AddHttpClient<IDirectorioDocentes,DirectorioDocentes>(h=>{h.BaseAddress=authUri;h.Timeout=TimeSpan.FromSeconds(10);}).ConfigurePrimaryHttpMessageHandler(()=>new HttpClientHandler {AllowAutoRedirect=false}).AddHttpMessageHandler<CatalogoAuthorizationHandler>();
var issuer=builder.Configuration["Jwt:Issuer"];var audience=builder.Configuration["Jwt:Audience"];var key=builder.Configuration["Jwt:SigningKey"];var encryptionKey=builder.Configuration["Jwt:EncryptionKey"];
if(string.IsNullOrWhiteSpace(issuer)||string.IsNullOrWhiteSpace(audience)||string.IsNullOrWhiteSpace(key)||Encoding.UTF8.GetByteCount(key)<32) throw new InvalidOperationException("Configure Jwt:Issuer, Jwt:Audience y Jwt:SigningKey de al menos 32 bytes.");
// HU-35: el token de Auth llega firmado (HS256) y cifrado (A256KW + A256CBC-HS512); se rechaza el que no venga cifrado.
var encryptionBytes=new byte[32];
if(string.IsNullOrWhiteSpace(encryptionKey)||!Convert.TryFromBase64String(encryptionKey,encryptionBytes,out var encryptionCount)||encryptionCount!=32) throw new InvalidOperationException("Configure Jwt:EncryptionKey con 32 bytes codificados en Base64.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o=>{o.MapInboundClaims=false;o.IncludeErrorDetails=false;
 o.Events=new JwtBearerEvents {OnTokenValidated=context=>{
  if(context.SecurityToken is not JsonWebToken {IsEncrypted:true,Alg:SecurityAlgorithms.Aes256KW,Enc:SecurityAlgorithms.Aes256CbcHmacSha512}) context.Fail("Se requiere un token cifrado.");
  return Task.CompletedTask;}};
 o.TokenValidationParameters=new TokenValidationParameters {
 ValidateIssuer=true,ValidIssuer=issuer,ValidateAudience=true,ValidAudience=audience,ValidateIssuerSigningKey=true,IssuerSigningKey=new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),TokenDecryptionKey=new SymmetricSecurityKey(encryptionBytes),ValidateLifetime=true,RequireExpirationTime=true,RequireSignedTokens=true,
 AlgorithmValidator=(algorithm,_,_,_)=>algorithm is SecurityAlgorithms.HmacSha256 or SecurityAlgorithms.Aes256KW or SecurityAlgorithms.Aes256CbcHmacSha512,ClockSkew=TimeSpan.Zero,NameClaimType="name",RoleClaimType="role"
};});
builder.Services.AddAuthorization();
var app=builder.Build();app.UseInternalAccess();app.UseExceptionHandler();app.UseStatusCodePages();
if(app.Environment.IsDevelopment()){app.UseSwagger();app.UseSwaggerUI();}
app.UseAuthentication();app.UseAuthorization();app.MapControllers();
if(app.Configuration.GetValue<bool>("Database:ApplyMigrations")){using var scope=app.Services.CreateScope();await scope.ServiceProvider.GetRequiredService<SolicitudesDbContext>().Database.MigrateAsync();}
app.Run();
public partial class Program;
namespace Solicitudes.Api {
 public sealed class CatalogoAuthorizationHandler(IHttpContextAccessor context, IConfiguration configuration) : DelegatingHandler {
  protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct) {
   if(AuthenticationHeaderValue.TryParse(context.HttpContext?.Request.Headers.Authorization.ToString(),out var value)) request.Headers.Authorization=value;
   request.Headers.Remove(InternalAccess.Header);
   request.Headers.TryAddWithoutValidation(InternalAccess.Header, configuration["InternalAccess:Key"]);
   return base.SendAsync(request,ct);
  }
 }
}
