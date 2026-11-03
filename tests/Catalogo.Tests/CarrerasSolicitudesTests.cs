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
}
