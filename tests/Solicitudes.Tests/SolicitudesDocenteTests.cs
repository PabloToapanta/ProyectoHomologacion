using System.Net;
using System.Net.Http.Json;
using Xunit;
namespace Solicitudes.Tests;
// HU-30 / Issue #41: el docente consulta solo las solicitudes donde figura entre los dos asignados.
public class SolicitudesDocenteApiTests {
 const string Asignadas="/api/solicitudes/asignadas";
 // Ana y Luis revisan A; Luis y Rosa revisan B; C sigue sin docentes.
 static async Task<(Guid A,Guid B,Guid C)> Escenario(SolicitudesFactory f) {
  using var c=f.Client();var (ana,luis,rosa)=f.Directorio.Docentes;
  async Task<Guid> Crear(string quipux) {
   var r=await c.PostAsJsonAsync("/api/solicitudes",f.Input() with {NumeroQuipux=quipux});r.EnsureSuccessStatusCode();
   return (await r.Content.ReadFromJsonAsync<SolicitudesApiTests.SolicitudView>())!.Id;
  }
  var a=await Crear("Q-HU30-A");var b=await Crear("Q-HU30-B");var cc=await Crear("Q-HU30-C");
  (await c.PostAsJsonAsync($"/api/solicitudes/{a}/docentes",new {docenteIds=new[]{ana.Id,luis.Id}})).EnsureSuccessStatusCode();
  (await c.PostAsJsonAsync($"/api/solicitudes/{b}/docentes",new {docenteIds=new[]{luis.Id,rosa.Id}})).EnsureSuccessStatusCode();
  return (a,b,cc);
 }
 static HttpClient Docente(SolicitudesFactory f,Guid id)=>f.Client("Docente",id.ToString());
 [Fact] public async Task CadaDocenteListaSoloSusAsignaciones() {
  using var f=new SolicitudesFactory();var (a,b,_)=await Escenario(f);var (ana,luis,rosa)=f.Directorio.Docentes;
  using var cAna=Docente(f,ana.Id);using var cLuis=Docente(f,luis.Id);using var cRosa=Docente(f,rosa.Id);using var cOtro=Docente(f,Guid.NewGuid());
  Assert.Equal([a],(await cAna.GetFromJsonAsync<List<Vista>>(Asignadas))!.Select(s=>s.Id));
  Assert.Equal(new[]{a,b}.Order(),(await cLuis.GetFromJsonAsync<List<Vista>>(Asignadas))!.Select(s=>s.Id).Order());
  Assert.Equal([b],(await cRosa.GetFromJsonAsync<List<Vista>>(Asignadas))!.Select(s=>s.Id));
  Assert.Empty((await cOtro.GetFromJsonAsync<List<Vista>>(Asignadas))!);
 }
 [Fact] public async Task DetalleAsignadoIncluyeSolicitudYAnalisisPendiente() {
  using var f=new SolicitudesFactory();var (a,_,_)=await Escenario(f);using var c=Docente(f,f.Directorio.Docentes.Ana.Id);
  var response=await c.GetAsync($"{Asignadas}/{a}");Assert.Equal(HttpStatusCode.OK,response.StatusCode);
  using var json=System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
  var solicitud=json.RootElement.GetProperty("solicitud");
  Assert.Equal(a,solicitud.GetProperty("id").GetGuid());Assert.Equal("Docentes asignados",solicitud.GetProperty("estado").GetString());
  Assert.Equal(["docente.ana","docente.luis"],solicitud.GetProperty("docentesAsignados").EnumerateArray().Select(d=>d.GetProperty("nombreUsuario").GetString()));
  // Sin HU-18/HU-20 no hay resultado: el estado es explícito y no aparece ninguna recomendación.
  var analisis=json.RootElement.GetProperty("analisis");
  Assert.Equal("Pendiente",analisis.GetProperty("estado").GetString());
  Assert.Equal(System.Text.Json.JsonValueKind.Null,analisis.GetProperty("resultado").ValueKind);
 }
 [Fact] public async Task DetalleNoAsignadoSeRechazaComoNoEncontrado() {
  using var f=new SolicitudesFactory();var (a,b,cc)=await Escenario(f);var (ana,_,rosa)=f.Directorio.Docentes;
  using var cAna=Docente(f,ana.Id);using var cRosa=Docente(f,rosa.Id);
  foreach(var (c,id) in new[]{(cAna,b),(cAna,cc),(cRosa,a),(cAna,Guid.NewGuid())}) {
   var r=await c.GetAsync($"{Asignadas}/{id}");Assert.Equal(HttpStatusCode.NotFound,r.StatusCode);
   Assert.DoesNotContain("Q-HU30",await r.Content.ReadAsStringAsync());
  }
 }
 [Theory][InlineData(null,401)][InlineData("Coordinador",403)][InlineData("Administrador",403)]
 public async Task SoloElRolDocenteConsultaAsignaciones(string? role,int status) {
  using var f=new SolicitudesFactory();var (a,_,_)=await Escenario(f);using var c=f.Client(role,f.Directorio.Docentes.Ana.Id.ToString());
  Assert.Equal(status,(int)(await c.GetAsync(Asignadas)).StatusCode);
  Assert.Equal(status,(int)(await c.GetAsync($"{Asignadas}/{a}")).StatusCode);
 }
 [Fact] public async Task DocenteSinIdentificadorValidoNoConsulta() {
  using var f=new SolicitudesFactory();await Escenario(f);using var c=f.Client("Docente","no-es-guid");
  Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync(Asignadas)).StatusCode);
 }
 [Fact] public async Task DocenteNoUsaLasRutasDelCoordinador() {
  using var f=new SolicitudesFactory();var (a,_,_)=await Escenario(f);using var c=Docente(f,f.Directorio.Docentes.Ana.Id);
  Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync($"/api/solicitudes/{a}")).StatusCode);
  Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync("/api/solicitudes")).StatusCode);
  // CA5: la vista es de solo lectura; HU-31 registrará la revisión propia.
  Assert.Equal(HttpStatusCode.MethodNotAllowed,(await c.PostAsJsonAsync($"{Asignadas}/{a}",new {})).StatusCode);
 }
 public sealed record Vista(Guid Id,string Estado);
}
