using System.Net;
using System.Net.Http.Json;
using Solicitudes.Application;
using Solicitudes.Domain;
using Xunit;
namespace Solicitudes.Tests;
public class AsignacionDocentesTests {
 static readonly DocenteDisponible Ana=new(Guid.NewGuid(),"docente.ana"),Luis=new(Guid.NewGuid(),"docente.luis"),Rosa=new(Guid.NewGuid(),"docente.rosa");
 static readonly DocenteDisponible[] Disponibles=[Ana,Luis,Rosa];
 static Solicitud Nueva()=>new("Q-HU29","EST-SINTETICO","Estudiante sintético","Interno",Guid.NewGuid(),null,null,Guid.NewGuid());
 [Fact] public void DosDocentesDistintosQuedanAsignadosEnOrden() {
  var s=Nueva();var asignados=DocenteAsignado.Asignar(s,[Luis.Id,Ana.Id],Disponibles);
  Assert.Equal("Docentes asignados",s.Estado);
  Assert.Equal([(Luis.Id,"docente.luis",1),(Ana.Id,"docente.ana",2)],asignados.Select(a=>(a.DocenteId,a.NombreUsuario,a.Orden)));
  Assert.All(asignados,a=>Assert.Equal(s.Id,a.SolicitudId));
 }
 public static TheoryData<Guid[]> CantidadesInvalidas()=>new(){Array.Empty<Guid>(),new[]{Ana.Id},new[]{Ana.Id,Luis.Id,Rosa.Id}};
 [Theory][MemberData(nameof(CantidadesInvalidas))] public void CantidadDistintaDeDosSeRechazaSinCambiarEstado(Guid[] ids) {
  var s=Nueva();Assert.Throws<SolicitudException>(()=>DocenteAsignado.Asignar(s,ids,Disponibles));Assert.Equal("Registrada",s.Estado);
 }
 [Fact] public void MismoDocenteDosVecesSeRechaza() {var s=Nueva();Assert.Throws<SolicitudException>(()=>DocenteAsignado.Asignar(s,[Ana.Id,Ana.Id],Disponibles));Assert.Equal("Registrada",s.Estado);}
 [Fact] public void UsuarioQueNoEsDocenteActivoSeRechaza() {var s=Nueva();Assert.Throws<SolicitudException>(()=>DocenteAsignado.Asignar(s,[Ana.Id,Guid.NewGuid()],Disponibles));Assert.Equal("Registrada",s.Estado);}
 [Fact] public void SolicitudConDocentesNoSeReasigna() {
  var s=Nueva();DocenteAsignado.Asignar(s,[Ana.Id,Luis.Id],Disponibles);
  Assert.Throws<AsignacionNoPermitidaException>(()=>DocenteAsignado.Asignar(s,[Rosa.Id,Luis.Id],Disponibles));
 }
}
public class DocentesAsignadosApiTests {
 static string Ruta(Guid id)=>$"/api/solicitudes/{id}/docentes";
 static async Task<Guid> Solicitud(SolicitudesFactory f,HttpClient c,string quipux="Q-HU29") {
  var response=await c.PostAsJsonAsync("/api/solicitudes",f.Input() with {NumeroQuipux=quipux});response.EnsureSuccessStatusCode();
  return (await response.Content.ReadFromJsonAsync<SolicitudesApiTests.SolicitudView>())!.Id;
 }
 static async Task<Detalle> Leer(HttpClient c,Guid id)=>(await c.GetFromJsonAsync<Detalle>($"/api/solicitudes/{id}"))!;
 [Fact] public async Task AsignaDosDocentesYLaSolicitudCambiaDeEstado() {
  using var f=new SolicitudesFactory();using var c=f.Client();var id=await Solicitud(f,c);var (ana,luis,_)=f.Directorio.Docentes;
  var response=await c.PostAsJsonAsync(Ruta(id),new {docenteIds=new[]{ana.Id,luis.Id}});Assert.Equal(HttpStatusCode.OK,response.StatusCode);
  using var otro=f.Client();var detalle=await Leer(otro,id);
  Assert.Equal("Docentes asignados",detalle.Estado);Assert.Equal([ana.Id,luis.Id],detalle.DocentesAsignados.Select(d=>d.DocenteId));Assert.Equal(["docente.ana","docente.luis"],detalle.DocentesAsignados.Select(d=>d.NombreUsuario));
  Assert.Equal("Docentes asignados",Assert.Single((await otro.GetFromJsonAsync<List<SolicitudesApiTests.SolicitudView>>("/api/solicitudes"))!).Estado);
 }
 [Fact] public async Task DatosInvalidosNoAsignanNadie() {
  using var f=new SolicitudesFactory();using var c=f.Client();var id=await Solicitud(f,c);var (ana,luis,rosa)=f.Directorio.Docentes;
  foreach(var ids in new[]{Array.Empty<Guid>(),[ana.Id],[ana.Id,ana.Id],[ana.Id,luis.Id,rosa.Id],[ana.Id,Guid.NewGuid()]})
   Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync(Ruta(id),new {docenteIds=ids})).StatusCode);
  Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync(Ruta(id),new {})).StatusCode);
  var detalle=await Leer(c,id);Assert.Equal("Registrada",detalle.Estado);Assert.Empty(detalle.DocentesAsignados);Assert.Empty(f.Logs.Errores);
 }
 [Fact] public async Task SegundaAsignacionDevuelveConflictoSinCambios() {
  using var f=new SolicitudesFactory();using var c=f.Client();var id=await Solicitud(f,c);var (ana,luis,rosa)=f.Directorio.Docentes;
  await c.PostAsJsonAsync(Ruta(id),new {docenteIds=new[]{ana.Id,luis.Id}});
  Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync(Ruta(id),new {docenteIds=new[]{rosa.Id,luis.Id}})).StatusCode);
  Assert.Equal([ana.Id,luis.Id],(await Leer(c,id)).DocentesAsignados.Select(d=>d.DocenteId));
 }
 [Fact] public async Task DirectorioNoDisponibleNoPresumeRolValido() {
  using var f=new SolicitudesFactory();using var c=f.Client();var id=await Solicitud(f,c);var (ana,luis,_)=f.Directorio.Docentes;f.Directorio.Falla=true;
  Assert.Equal(HttpStatusCode.ServiceUnavailable,(await c.PostAsJsonAsync(Ruta(id),new {docenteIds=new[]{ana.Id,luis.Id}})).StatusCode);
  Assert.Equal(HttpStatusCode.ServiceUnavailable,(await c.GetAsync($"/api/solicitudes/{id}/docentes-disponibles")).StatusCode);
  f.Directorio.Falla=false;var detalle=await Leer(c,id);Assert.Equal("Registrada",detalle.Estado);Assert.Empty(detalle.DocentesAsignados);
 }
 [Fact] public async Task SolicitudInexistenteDevuelve404() {
  using var f=new SolicitudesFactory();using var c=f.Client();var (ana,luis,_)=f.Directorio.Docentes;
  Assert.Equal(HttpStatusCode.NotFound,(await c.PostAsJsonAsync(Ruta(Guid.NewGuid()),new {docenteIds=new[]{ana.Id,luis.Id}})).StatusCode);
  Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync($"/api/solicitudes/{Guid.NewGuid()}/docentes-disponibles")).StatusCode);
 }
 [Fact] public async Task DocentesDisponiblesVienenDelDirectorio() {
  using var f=new SolicitudesFactory();using var c=f.Client();var id=await Solicitud(f,c);
  var lista=await c.GetFromJsonAsync<List<DocenteDisponible>>($"/api/solicitudes/{id}/docentes-disponibles");
  Assert.Equal(["docente.ana","docente.luis","docente.rosa"],lista!.Select(d=>d.NombreUsuario));
 }
 [Theory][InlineData(null,401)][InlineData("Docente",403)][InlineData("Administrador",403)]
 public async Task AsignacionExigeCoordinador(string? role,int status) {
  using var f=new SolicitudesFactory();using var coordinador=f.Client();var id=await Solicitud(f,coordinador);var (ana,luis,_)=f.Directorio.Docentes;using var c=f.Client(role);
  Assert.Equal(status,(int)(await c.PostAsJsonAsync(Ruta(id),new {docenteIds=new[]{ana.Id,luis.Id}})).StatusCode);
  Assert.Equal(status,(int)(await c.GetAsync($"/api/solicitudes/{id}/docentes-disponibles")).StatusCode);
  Assert.Empty((await Leer(coordinador,id)).DocentesAsignados);
 }
 public sealed record Detalle(Guid Id,string Estado,List<Asignado> DocentesAsignados);
 public sealed record Asignado(Guid DocenteId,string NombreUsuario,int Orden);
}
