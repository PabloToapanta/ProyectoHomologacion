using System.Net;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Solicitudes.Application;
using Solicitudes.Domain;
using Solicitudes.Infrastructure;
using Xunit;
namespace Solicitudes.Tests;
public sealed class SolicitudesApiTests {
 [Fact] public async Task RegistraConsultaYRechazaQuipuxDuplicado() {
  using var f=new SolicitudesFactory();using var c=f.Client();
  var response=await c.PostAsJsonAsync("/api/solicitudes",f.Input());Assert.Equal(HttpStatusCode.Created,response.StatusCode);
  var s=await response.Content.ReadFromJsonAsync<SolicitudView>();Assert.NotNull(s);Assert.Equal("Registrada",s.Estado);
  using var otro=f.Client();var detail=await otro.GetFromJsonAsync<SolicitudView>(response.Headers.Location);Assert.Equal(s.Id,detail!.Id);
  using var document=System.Text.Json.JsonDocument.Parse(await otro.GetStringAsync(response.Headers.Location));Assert.EndsWith("Z",document.RootElement.GetProperty("fechaRegistro").GetString());
  Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync("/api/solicitudes",f.Input() with {NumeroQuipux=" q-001 "})).StatusCode);
  var list=await c.GetFromJsonAsync<List<SolicitudView>>("/api/solicitudes");Assert.Single(list!);
 }
 [Theory][InlineData(null,401)][InlineData("Docente",403)][InlineData("Administrador",403)]
 public async Task ProtegeCreacionConsultaYCarreras(string? role,int status) {
  using var f=new SolicitudesFactory();using var c=f.Client(role);
  Assert.Equal(status,(int)(await c.PostAsJsonAsync("/api/solicitudes",f.Input())).StatusCode);
  foreach(var path in new[]{"/api/solicitudes","/api/solicitudes/carreras",$"/api/solicitudes/{Guid.NewGuid()}"}) Assert.Equal(status,(int)(await c.GetAsync(path)).StatusCode);
 }
 [Fact] public async Task RechazaReferenciasInvalidasYCamposFaltantesSinGuardar() {
  using var f=new SolicitudesFactory();using var c=f.Client();
  foreach(var i in new[]{f.Input() with{NumeroQuipux=""},f.Input() with{IdentificacionEstudiante=" "},f.Input() with{NombreEstudiante=" "},f.Input() with{TipoCambio="Otro"},f.Input() with{CarreraReceptoraId=Guid.NewGuid()},f.Input() with{CarreraOrigenId=Guid.NewGuid()}})
   Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync("/api/solicitudes",i)).StatusCode);
  Assert.Empty((await c.GetFromJsonAsync<List<SolicitudView>>("/api/solicitudes"))!);
  Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync($"/api/solicitudes/{Guid.NewGuid()}")).StatusCode);
 }
 [Fact] public async Task ExternoSeGuardaSeparadoDelCatalogo() {
  using var f=new SolicitudesFactory();using var c=f.Client();
  var input=f.Input() with{TipoCambio="Externo",CarreraOrigenId=null,InstitucionOrigen="Universidad sintética Ecuador",CarreraOrigenExterna="Carrera externa sintética"};
  var response=await c.PostAsJsonAsync("/api/solicitudes",input);Assert.Equal(HttpStatusCode.Created,response.StatusCode);
  var text=await c.GetStringAsync(response.Headers.Location);Assert.Contains("Universidad sint",text);Assert.Contains("Externo",text);
 }
 [Fact] public async Task CatalogoCaidoNoGuardaYNoMuestraExito() {
  using var f=new SolicitudesFactory();f.Catalogo.Falla=true;using var c=f.Client();
  Assert.Equal(HttpStatusCode.ServiceUnavailable,(await c.PostAsJsonAsync("/api/solicitudes",f.Input())).StatusCode);
  Assert.Empty((await c.GetFromJsonAsync<List<SolicitudView>>("/api/solicitudes"))!);
 }
 [Fact] public async Task PaginaInvalidaNoSeAcepta() {using var f=new SolicitudesFactory();using var c=f.Client();Assert.Equal(HttpStatusCode.BadRequest,(await c.GetAsync("/api/solicitudes?pagina=0")).StatusCode);}
 [Fact] public async Task LocationEsRelativaYNoExponeHostInterno() {
  using var f=new SolicitudesFactory();using var c=f.Client();
  var response=await c.PostAsJsonAsync("/api/solicitudes",f.Input());var s=await response.Content.ReadFromJsonAsync<SolicitudView>();
  Assert.False(response.Headers.Location!.IsAbsoluteUri);Assert.Equal($"/api/solicitudes/{s!.Id}",response.Headers.Location.OriginalString);
 }
 [Fact] public async Task ErroresEsperadosNoSeRegistranComoFallos() {
  using var f=new SolicitudesFactory();using var c=f.Client();
  await c.PostAsJsonAsync("/api/solicitudes",f.Input());
  var duplicado=await c.PostAsJsonAsync("/api/solicitudes",f.Input());
  var invalido=await c.PostAsJsonAsync("/api/solicitudes",f.Input() with{TipoCambio="Otro",NumeroQuipux="Q-002"});
  var pagina=await c.GetAsync("/api/solicitudes?pagina=0");
  await Conserva(duplicado,HttpStatusCode.Conflict,"Ya existe una solicitud con ese número de trámite Quipux.");
  await Conserva(invalido,HttpStatusCode.BadRequest,"Seleccione cambio Interno o Externo.");
  Assert.Equal(HttpStatusCode.BadRequest,pagina.StatusCode);
  Assert.Empty(f.Logs.Errores);
 }
 [Fact] public async Task CatalogoCaidoSiSeRegistraComoFallo() {
  using var f=new SolicitudesFactory();f.Catalogo.Falla=true;using var c=f.Client();
  Assert.Equal(HttpStatusCode.ServiceUnavailable,(await c.PostAsJsonAsync("/api/solicitudes",f.Input())).StatusCode);
  Assert.NotEmpty(f.Logs.Errores);
 }
 static async Task Conserva(HttpResponseMessage response,HttpStatusCode status,string title) {
  Assert.Equal(status,response.StatusCode);Assert.Equal("application/problem+json",response.Content.Headers.ContentType?.MediaType);
  using var document=System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
  Assert.Equal(title,document.RootElement.GetProperty("title").GetString());Assert.Equal((int)status,document.RootElement.GetProperty("status").GetInt32());
 }
 [Theory][InlineData("plain")][InlineData("other-key")][InlineData("tampered")]
 public async Task TokenSinCifradoOAlteradoSeRechaza(string scenario) {
  using var f=new SolicitudesFactory();using var c=f.Client(scenario:scenario);
  Assert.Equal(HttpStatusCode.Unauthorized,(await c.GetAsync("/api/solicitudes")).StatusCode);
 }
 [Theory][InlineData("")][InlineData("corta")]
 public void ClaveDeCifradoInvalidaImpideArranque(string value) {
  using var f=new SolicitudesFactory();using var configured=f.WithWebHostBuilder(b=>b.UseSetting("Jwt:EncryptionKey",value));
  var error=Assert.Throws<InvalidOperationException>(()=>configured.CreateClient());
  if(value.Length>0) Assert.DoesNotContain(value,error.Message);
 }
 public sealed record SolicitudView(Guid Id,string Estado);
}
public sealed class LogsCapturados : ILoggerProvider, ILogger {
 readonly System.Collections.Concurrent.ConcurrentQueue<string> errores=new();
 public IReadOnlyCollection<string> Errores=>errores;
 public ILogger CreateLogger(string category)=>this;
 public IDisposable? BeginScope<TState>(TState state) where TState:notnull=>null;
 public bool IsEnabled(LogLevel level)=>level>=LogLevel.Error;
 public void Log<TState>(LogLevel level,EventId id,TState state,Exception? exception,Func<TState,Exception?,string> formatter) {if(IsEnabled(level)) errores.Enqueue(formatter(state,exception));}
 public void Dispose() { }
}
public sealed class FakeCatalogo : ICatalogoCarreras {
 public Guid Origen {get;}=Guid.NewGuid();public Guid Receptora {get;}=Guid.NewGuid();public bool Falla {get;set;}
 public AsignaturaCatalogo Algebra {get;}=new(Guid.NewGuid(),"COD-1","Álgebra sintética",1,3m,144);
 public Task<IReadOnlyList<CarreraReferencia>> Listar(CancellationToken ct)=>Falla?throw new CatalogoNoDisponibleException():Task.FromResult<IReadOnlyList<CarreraReferencia>>([new(Origen,"TI sintética"),new(Receptora,"Software sintética")]);
 public Task<IReadOnlyList<AsignaturaCatalogo>> AsignaturasDe(Guid carreraId,CancellationToken ct)=>Falla?throw new CatalogoNoDisponibleException():Task.FromResult<IReadOnlyList<AsignaturaCatalogo>>(carreraId==Origen?[Algebra]:[]);
}
public sealed class FakeDirectorio : IDirectorioDocentes {
 public (DocenteDisponible Ana,DocenteDisponible Luis,DocenteDisponible Rosa) Docentes {get;}=(new(Guid.NewGuid(),"docente.ana"),new(Guid.NewGuid(),"docente.luis"),new(Guid.NewGuid(),"docente.rosa"));
 public bool Falla {get;set;}
 public Task<IReadOnlyList<DocenteDisponible>> Listar(CancellationToken ct)=>Falla?throw new DirectorioNoDisponibleException():Task.FromResult<IReadOnlyList<DocenteDisponible>>([Docentes.Ana,Docentes.Luis,Docentes.Rosa]);
}
public sealed class SolicitudesFactory : WebApplicationFactory<Program> {
 const string Key="clave-sintetica-pruebas-hu10-32-bytes-minimo";
 // Clave de cifrado sintética por ejecución, nunca usada en despliegues (HU-35).
 static readonly string EncryptionKey=Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
 readonly SqliteConnection db=new("Data Source=:memory:");
 public FakeCatalogo Catalogo {get;}=new();
 public FakeDirectorio Directorio {get;}=new();
 public LogsCapturados Logs {get;}=new();
 public RelojFijo Reloj {get;}=new(new DateTimeOffset(2026,6,15,17,0,0,TimeSpan.Zero));
 public SolicitudesFactory(){db.Open();}
 protected override void ConfigureWebHost(IWebHostBuilder b) {
  b.UseEnvironment("Testing");b.ConfigureLogging(l=>l.AddProvider(Logs));foreach(var setting in new Dictionary<string,string?> {
   ["ConnectionStrings:Solicitudes"]="Server=unused;Database=unused",["Catalogo:BaseUrl"]="http://catalogo.test/",["Auth:BaseUrl"]="http://auth.test/",["Jwt:Issuer"]="test",["Jwt:Audience"]="test",["Jwt:SigningKey"]=Key,["Jwt:EncryptionKey"]=EncryptionKey
  }) b.UseSetting(setting.Key,setting.Value);
  b.ConfigureServices(s=>{s.RemoveAll<DbContextOptions<SolicitudesDbContext>>();s.AddDbContext<SolicitudesDbContext>(o=>o.UseSqlite(db));s.RemoveAll<ICatalogoCarreras>();s.AddSingleton<ICatalogoCarreras>(Catalogo);s.RemoveAll<IDirectorioDocentes>();s.AddSingleton<IDirectorioDocentes>(Directorio);s.RemoveAll<TimeProvider>();s.AddSingleton<TimeProvider>(Reloj);});
 }
 public HttpClient Client(string? role="Coordinador",string sub="usuario-sintetico",string scenario="valid") {
  var client=CreateClient();using var scope=Services.CreateScope();scope.ServiceProvider.GetRequiredService<SolicitudesDbContext>().Database.EnsureCreated();
  if(role is not null) client.DefaultRequestHeaders.Authorization=new("Bearer",Token(role,sub,scenario));
  return client;
 }
 // Igual que Auth en HU-35: firmado con HS256 y cifrado con A256KW + A256CBC-HS512.
 static string Token(string role,string sub,string scenario) {
  var descriptor=new SecurityTokenDescriptor {
   Issuer="test",Audience="test",Subject=new ClaimsIdentity([new Claim("sub",sub),new Claim("role",role)]),
   NotBefore=DateTime.UtcNow.AddMinutes(-1),Expires=DateTime.UtcNow.AddMinutes(5),
   SigningCredentials=new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key)),SecurityAlgorithms.HmacSha256),
   EncryptingCredentials=scenario=="plain"?null:new EncryptingCredentials(new SymmetricSecurityKey(scenario=="other-key"?System.Security.Cryptography.RandomNumberGenerator.GetBytes(32):Convert.FromBase64String(EncryptionKey)),SecurityAlgorithms.Aes256KW,SecurityAlgorithms.Aes256CbcHmacSha512)
  };
  var token=new JwtSecurityTokenHandler().WriteToken(new JwtSecurityTokenHandler().CreateToken(descriptor));
  if(scenario!="tampered") return token;
  var parts=token.Split('.');parts[3]=(parts[3][0]=='A'?'B':'A')+parts[3][1..];return string.Join('.',parts);
 }
 public SolicitudInput Input()=>new("Q-001","EST-SINTETICO","Estudiante sintético","Interno",Catalogo.Origen,null,null,Catalogo.Receptora);
 protected override void Dispose(bool disposing){base.Dispose(disposing);if(disposing)db.Dispose();}
}
