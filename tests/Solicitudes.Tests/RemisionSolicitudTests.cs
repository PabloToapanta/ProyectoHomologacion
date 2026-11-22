using System.Net;
using System.Net.Http.Json;
using Solicitudes.Domain;
using Xunit;
namespace Solicitudes.Tests;
// HU-32 / Issue #63: Homologación marca la solicitud «Remitida» al remitir el informe. Ruta interna: no se publica en el Gateway.
public class RemisionSolicitudTests {
 static Solicitud Interna()=>new("Q-HU32","0102030405","Estudiante Sintético","Interno",Guid.NewGuid(),null,null,Guid.NewGuid());
 static Solicitud ConDocentes() {
  var s=Interna();
  DocenteAsignado.Asignar(s,[Guid.Parse("00000000-0000-0000-0000-0000000000a1"),Guid.Parse("00000000-0000-0000-0000-0000000000a2")],
   [new(Guid.Parse("00000000-0000-0000-0000-0000000000a1"),"docente.a"),new(Guid.Parse("00000000-0000-0000-0000-0000000000a2"),"docente.b")]);
  return s;
 }
 [Fact] public void Con_docentes_asignados_pasa_a_remitida() {
  var s=ConDocentes();
  Assert.True(s.MarcarRemitida());
  Assert.Equal(Solicitud.EstadoRemitida,s.Estado);
 }
 [Fact] public void Remitir_de_nuevo_no_cambia_nada() {
  var s=ConDocentes();s.MarcarRemitida();
  Assert.False(s.MarcarRemitida());
  Assert.Equal("Remitida",s.Estado);
 }
 [Fact] public void Sin_docentes_asignados_no_se_remite() {
  var s=Interna();
  Assert.Throws<EstadoSolicitudNoPermiteException>(()=>s.MarcarRemitida());
  Assert.Equal("Registrada",s.Estado);
 }
 [Fact] public void Remitida_ya_no_corrige_datos_externos() {
  var s=new Solicitud("Q-HU32-E","0102030405","Estudiante Sintético","Externo",null,"Universidad Sintética","Carrera Sintética",Guid.NewGuid(),2,0);
  DocenteAsignado.Asignar(s,[Guid.Parse("00000000-0000-0000-0000-0000000000a1"),Guid.Parse("00000000-0000-0000-0000-0000000000a2")],
   [new(Guid.Parse("00000000-0000-0000-0000-0000000000a1"),"docente.a"),new(Guid.Parse("00000000-0000-0000-0000-0000000000a2"),"docente.b")]);
  s.MarcarRemitida();
  Assert.Throws<DatosExternosNoEditablesException>(()=>s.DeclararDatosCambioExterno(3,0));
 }

 static async Task<Guid> Asignada(SolicitudesFactory f,string quipux="Q-HU32-API") {
  using var c=f.Client();var (ana,luis,_)=f.Directorio.Docentes;
  var r=await c.PostAsJsonAsync("/api/solicitudes",f.Input() with {NumeroQuipux=quipux});r.EnsureSuccessStatusCode();
  var id=(await r.Content.ReadFromJsonAsync<SolicitudesApiTests.SolicitudView>())!.Id;
  (await c.PostAsJsonAsync($"/api/solicitudes/{id}/docentes",new {docenteIds=new[]{ana.Id,luis.Id}})).EnsureSuccessStatusCode();
  return id;
 }
 static string Ruta(Guid id)=>$"/api/solicitudes/{id}/remision";
 static async Task<string?> EstadoParaCoordinador(SolicitudesFactory f,Guid id) {
  using var c=f.Client();
  return (await c.GetFromJsonAsync<SolicitudesDocenteApiTests.Vista>($"/api/solicitudes/{id}"))!.Estado;
 }

 [Fact] public async Task El_docente_asignado_marca_la_solicitud_remitida_y_ambos_roles_la_ven() {
  using var f=new SolicitudesFactory();var id=await Asignada(f);
  using var ana=f.Client("Docente",f.Directorio.Docentes.Ana.Id.ToString());
  Assert.Equal(HttpStatusCode.NoContent,(await ana.PostAsync(Ruta(id),null)).StatusCode);
  Assert.Equal("Remitida",await EstadoParaCoordinador(f,id));
  using var luis=f.Client("Docente",f.Directorio.Docentes.Luis.Id.ToString());
  Assert.Equal("Remitida",(await luis.GetFromJsonAsync<List<SolicitudesDocenteApiTests.Vista>>("/api/solicitudes/asignadas"))!.Single().Estado);
 }
 [Fact] public async Task Repetir_la_marca_es_idempotente() {
  using var f=new SolicitudesFactory();var id=await Asignada(f);
  using var ana=f.Client("Docente",f.Directorio.Docentes.Ana.Id.ToString());
  await ana.PostAsync(Ruta(id),null);
  // Permite que Homologación reintente si su propia confirmación falló después de esta marca.
  Assert.Equal(HttpStatusCode.NoContent,(await ana.PostAsync(Ruta(id),null)).StatusCode);
  Assert.Equal("Remitida",await EstadoParaCoordinador(f,id));
 }
 [Fact] public async Task Docente_ajeno_o_solicitud_inexistente_responden_404_sin_cambios() {
  using var f=new SolicitudesFactory();var id=await Asignada(f);
  using var rosa=f.Client("Docente",f.Directorio.Docentes.Rosa.Id.ToString());
  Assert.Equal(HttpStatusCode.NotFound,(await rosa.PostAsync(Ruta(id),null)).StatusCode);
  Assert.Equal(HttpStatusCode.NotFound,(await rosa.PostAsync(Ruta(Guid.NewGuid()),null)).StatusCode);
  Assert.Equal("Docentes asignados",await EstadoParaCoordinador(f,id));
 }
 [Theory][InlineData(null,401)][InlineData("Coordinador",403)][InlineData("Administrador",403)]
 public async Task Solo_el_rol_docente_marca_la_remision(string? role,int status) {
  using var f=new SolicitudesFactory();var id=await Asignada(f);
  using var c=f.Client(role,f.Directorio.Docentes.Ana.Id.ToString());
  Assert.Equal(status,(int)(await c.PostAsync(Ruta(id),null)).StatusCode);
  Assert.Equal("Docentes asignados",await EstadoParaCoordinador(f,id));
 }
}
