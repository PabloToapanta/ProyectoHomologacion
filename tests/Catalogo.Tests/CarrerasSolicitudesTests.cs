using System.Net;
using System.Net.Http.Json;
using Xunit;
namespace Catalogo.Tests;
public class CarrerasSolicitudesTests {
 [Fact] public async Task CoordinadorLeeCarrerasVigentesSinPoderModificarCatalogo() {
  using var f=new CatalogoFactory();using var admin=f.Admin();
  Assert.Equal(HttpStatusCode.Created,(await admin.PostAsJsonAsync("/api/carreras",new {nombre="TI sintética",facultad="FISEI",vigencia=true})).StatusCode);
  Assert.Equal(HttpStatusCode.Created,(await admin.PostAsJsonAsync("/api/carreras",new {nombre="Inactiva sintética",facultad="FISEI",vigencia=false})).StatusCode);
  using var c=f.CreateClient();Assert.Equal(HttpStatusCode.Unauthorized,(await c.GetAsync("/api/carreras-solicitudes")).StatusCode);
  c.DefaultRequestHeaders.Authorization=new("Bearer",f.Token("Docente"));Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync("/api/carreras-solicitudes")).StatusCode);
  c.DefaultRequestHeaders.Authorization=new("Bearer",f.Token("Coordinador"));var text=await c.GetStringAsync("/api/carreras-solicitudes");Assert.Contains("TI sint",text);Assert.DoesNotContain("Inactiva",text);Assert.DoesNotContain("facultad",text);
  Assert.Equal(HttpStatusCode.Forbidden,(await c.PostAsJsonAsync("/api/carreras",new {nombre="No autorizado",facultad="FISEI",vigencia=true})).StatusCode);
 }
 [Fact] public async Task CoordinadorLeeAsignaturasDeUnaCarreraSinPoderCrearlas() {
  using var f=new CatalogoFactory();using var admin=f.Admin();
  var carrera=await (await admin.PostAsJsonAsync("/api/carreras",new {nombre="TI sintética",facultad="FISEI",vigencia=true})).Content.ReadFromJsonAsync<Referencia>();
  var ruta=$"/api/carreras-solicitudes/{carrera!.Id}/asignaturas";var nueva=new {carreraId=carrera.Id,codigo="COD-1",nombre="Álgebra sintética",nivel=1,creditos=3m,horas=144};
  Assert.Equal(HttpStatusCode.Created,(await admin.PostAsJsonAsync("/api/asignaturas",nueva)).StatusCode);
  using var c=f.CreateClient();Assert.Equal(HttpStatusCode.Unauthorized,(await c.GetAsync(ruta)).StatusCode);
  c.DefaultRequestHeaders.Authorization=new("Bearer",f.Token("Docente"));Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync(ruta)).StatusCode);
  c.DefaultRequestHeaders.Authorization=new("Bearer",f.Token("Coordinador"));var lista=await c.GetFromJsonAsync<List<AsignaturaVista>>(ruta);
  Assert.Equal(new AsignaturaVista("COD-1","Álgebra sintética",1,3m,144),Assert.Single(lista!));
  Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync($"/api/carreras-solicitudes/{Guid.NewGuid()}/asignaturas")).StatusCode);
  Assert.Equal(HttpStatusCode.Forbidden,(await c.PostAsJsonAsync("/api/asignaturas",nueva)).StatusCode);
 }
 sealed record Referencia(Guid Id);
 sealed record AsignaturaVista(string Codigo,string Nombre,int Nivel,decimal Creditos,int Horas);
}
