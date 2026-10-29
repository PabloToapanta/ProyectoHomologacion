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
 public sealed record SolicitudView(Guid Id,string Estado);
}
public sealed class FakeCatalogo : ICatalogoCarreras {
 public Guid Origen {get;}=Guid.NewGuid();public Guid Receptora {get;}=Guid.NewGuid();public bool Falla {get;set;}
 public Task<IReadOnlyList<CarreraReferencia>> Listar(CancellationToken ct)=>Falla?throw new CatalogoNoDisponibleException():Task.FromResult<IReadOnlyList<CarreraReferencia>>([new(Origen,"TI sintética"),new(Receptora,"Software sintética")]);
}
public sealed class SolicitudesFactory : WebApplicationFactory<Program> {
 const string Key="clave-sintetica-pruebas-hu10-32-bytes-minimo";
 readonly SqliteConnection db=new("Data Source=:memory:");
 public FakeCatalogo Catalogo {get;}=new();
 public SolicitudesFactory(){db.Open();}
 protected override void ConfigureWebHost(IWebHostBuilder b) {
  b.UseEnvironment("Testing");foreach(var setting in new Dictionary<string,string?> {
   ["ConnectionStrings:Solicitudes"]="Server=unused;Database=unused",["Catalogo:BaseUrl"]="http://catalogo.test/",["Jwt:Issuer"]="test",["Jwt:Audience"]="test",["Jwt:SigningKey"]=Key
  }) b.UseSetting(setting.Key,setting.Value);
  b.ConfigureServices(s=>{s.RemoveAll<DbContextOptions<SolicitudesDbContext>>();s.AddDbContext<SolicitudesDbContext>(o=>o.UseSqlite(db));s.RemoveAll<ICatalogoCarreras>();s.AddSingleton<ICatalogoCarreras>(Catalogo);});
 }
 public HttpClient Client(string? role="Coordinador") {
  var client=CreateClient();using var scope=Services.CreateScope();scope.ServiceProvider.GetRequiredService<SolicitudesDbContext>().Database.EnsureCreated();
  if(role is not null) {var jwt=new JwtSecurityToken("test","test",[new Claim("sub","usuario-sintetico"),new Claim("role",role)],DateTime.UtcNow.AddMinutes(-1),DateTime.UtcNow.AddMinutes(5),new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key)),SecurityAlgorithms.HmacSha256));client.DefaultRequestHeaders.Authorization=new("Bearer",new JwtSecurityTokenHandler().WriteToken(jwt));}
  return client;
 }
 public SolicitudInput Input()=>new("Q-001","EST-SINTETICO","Estudiante sintético","Interno",Catalogo.Origen,null,null,Catalogo.Receptora);
 protected override void Dispose(bool disposing){base.Dispose(disposing);if(disposing)db.Dispose();}
}
