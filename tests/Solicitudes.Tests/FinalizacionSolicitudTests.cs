using System.Net;
using System.Net.Http.Json;
using Solicitudes.Domain;
using Xunit;
namespace Solicitudes.Tests;
// HU-33 / Issue #64: Homologación marca la solicitud «Finalizada» al finalizar el coordinador. Ruta interna: no se publica en el Gateway.
public class FinalizacionSolicitudTests {
 static readonly Guid A1=Guid.Parse("00000000-0000-0000-0000-0000000000a1"),A2=Guid.Parse("00000000-0000-0000-0000-0000000000a2");
 static Solicitud ConDocentes() {
  var s=new Solicitud("Q-HU33","0102030405","Estudiante Sintético","Interno",Guid.NewGuid(),null,null,Guid.NewGuid());
  DocenteAsignado.Asignar(s,[A1,A2],[new(A1,"docente.a"),new(A2,"docente.b")]);
  return s;
 }
 [Fact] public void Remitida_pasa_a_finalizada_y_repetir_no_cambia_nada() {
  var s=ConDocentes();s.MarcarRemitida();
  Assert.True(s.MarcarFinalizada());
  Assert.Equal(Solicitud.EstadoFinalizada,s.Estado);
  Assert.False(s.MarcarFinalizada());
  Assert.Equal("Finalizada",s.Estado);
 }
 [Fact] public void Sin_remision_no_se_finaliza() {
  var s=ConDocentes();
  Assert.Throws<EstadoSolicitudNoPermiteException>(()=>s.MarcarFinalizada());
  Assert.Equal("Docentes asignados",s.Estado);
 }
 [Fact] public void Finalizada_no_vuelve_a_remitirse() {
  var s=ConDocentes();s.MarcarRemitida();s.MarcarFinalizada();
  Assert.Throws<EstadoSolicitudNoPermiteException>(()=>s.MarcarRemitida());
  Assert.Equal("Finalizada",s.Estado);
 }

 static async Task<Guid> Remitida(SolicitudesFactory f) {
  using var c=f.Client();var (ana,luis,_)=f.Directorio.Docentes;
  var r=await c.PostAsJsonAsync("/api/solicitudes",f.Input() with {NumeroQuipux="Q-HU33-API"});r.EnsureSuccessStatusCode();
  var id=(await r.Content.ReadFromJsonAsync<SolicitudesApiTests.SolicitudView>())!.Id;
  (await c.PostAsJsonAsync($"/api/solicitudes/{id}/docentes",new {docenteIds=new[]{ana.Id,luis.Id}})).EnsureSuccessStatusCode();
  using var docente=f.Client("Docente",ana.Id.ToString());
  Assert.Equal(HttpStatusCode.NoContent,(await docente.PostAsync($"/api/solicitudes/{id}/remision",null)).StatusCode);
  return id;
 }
 static string Ruta(Guid id)=>$"/api/solicitudes/{id}/finalizacion";
 static async Task<string?> Estado(SolicitudesFactory f,Guid id) {
  using var c=f.Client();
  return (await c.GetFromJsonAsync<SolicitudesDocenteApiTests.Vista>($"/api/solicitudes/{id}"))!.Estado;
 }

 [Fact] public async Task El_coordinador_finaliza_la_solicitud_remitida_y_los_docentes_ven_el_estado() {
  using var f=new SolicitudesFactory();var id=await Remitida(f);
  using var coordinador=f.Client("Coordinador",Guid.NewGuid().ToString());
  Assert.Equal(HttpStatusCode.NoContent,(await coordinador.PostAsync(Ruta(id),null)).StatusCode);
  Assert.Equal("Finalizada",await Estado(f,id));
  using var luis=f.Client("Docente",f.Directorio.Docentes.Luis.Id.ToString());
  Assert.Equal("Finalizada",(await luis.GetFromJsonAsync<List<SolicitudesDocenteApiTests.Vista>>("/api/solicitudes/asignadas"))!.Single().Estado);
  // Repetir es idempotente para que Homologación pueda reintentar.
  Assert.Equal(HttpStatusCode.NoContent,(await coordinador.PostAsync(Ruta(id),null)).StatusCode);
 }
 [Fact] public async Task Sin_remision_responde_409_e_inexistente_404() {
  using var f=new SolicitudesFactory();using var c=f.Client("Coordinador",Guid.NewGuid().ToString());
  var r=await c.PostAsJsonAsync("/api/solicitudes",f.Input());r.EnsureSuccessStatusCode();
  var id=(await r.Content.ReadFromJsonAsync<SolicitudesApiTests.SolicitudView>())!.Id;
  Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsync(Ruta(id),null)).StatusCode);
  Assert.Equal("Registrada",await Estado(f,id));
  Assert.Equal(HttpStatusCode.NotFound,(await c.PostAsync(Ruta(Guid.NewGuid()),null)).StatusCode);
 }
 [Theory][InlineData(null,401)][InlineData("Docente",403)][InlineData("Administrador",403)]
 public async Task Solo_el_coordinador_marca_la_finalizacion(string? role,int status) {
  using var f=new SolicitudesFactory();var id=await Remitida(f);
  using var c=f.Client(role,f.Directorio.Docentes.Ana.Id.ToString());
  Assert.Equal(status,(int)(await c.PostAsync(Ruta(id),null)).StatusCode);
  Assert.Equal("Remitida",await Estado(f,id));
 }
 [Fact] public async Task CA3_finalizada_no_admite_registrar_asignaturas_aprobadas() {
  using var f=new SolicitudesFactory();var id=await Remitida(f);
  using var c=f.Client("Coordinador",Guid.NewGuid().ToString());
  await c.PostAsync(Ruta(id),null);
  var r=await c.PostAsJsonAsync($"/api/solicitudes/{id}/asignaturas-aprobadas",new {asignaturaOrigenId=f.Catalogo.Algebra.Id,nota=8.5m,fechaAprobacion="2026-06-15",numeroMatricula="MAT-SINTETICA-HU33",intentoMatricula=1});
  Assert.Equal(HttpStatusCode.Conflict,r.StatusCode);
  Assert.Contains("finalizada",await r.Content.ReadAsStringAsync());
 }
}
