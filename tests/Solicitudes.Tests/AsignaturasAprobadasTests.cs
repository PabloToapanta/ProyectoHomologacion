using System.Net;
using System.Net.Http.Json;
using Solicitudes.Application;
using Solicitudes.Domain;
using Xunit;
namespace Solicitudes.Tests;
public sealed class RelojFijo(DateTimeOffset ahora) : TimeProvider {
 public DateTimeOffset Ahora {get;set;}=ahora;
 public override DateTimeOffset GetUtcNow()=>Ahora;
}
public class AsignaturaAprobadaTests {
 static readonly DateOnly Hoy=new(2026,6,15);
 static AsignaturaAprobada Interna(decimal nota=8.5m,DateOnly? fecha=null,string matricula="MAT-SINTETICA-1",int intento=1)=>AsignaturaAprobada.Interna(Guid.NewGuid(),Guid.NewGuid(),"COD-1","Álgebra sintética",3m,144,nota,fecha??Hoy,matricula,intento,Hoy);
 static AsignaturaAprobada Externa(string nombre="Álgebra externa",decimal creditos=4m,string sistema="Créditos sintéticos",int horas=64,string unidad="Horas sintéticas",string referencia="PROGRAMA-SINTETICO-1",decimal nota=14m,decimal maxima=20m,decimal minima=12m)=>AsignaturaAprobada.Externa(Guid.NewGuid(),nombre,creditos,sistema,horas,unidad,referencia,nota,maxima,minima,Hoy,"MAT-SINTETICA-1",1,Hoy);
 static decimal D(string valor)=>decimal.Parse(valor,System.Globalization.CultureInfo.InvariantCulture);
 [Fact] public void InternaUsaEscalaFisei() {var a=Interna();Assert.Equal(10m,a.NotaMaximaEscala);Assert.Equal(7m,a.NotaMinimaAprobacion);Assert.Equal("Aprobada",a.Estado);}
 [Theory][InlineData("7.00","Aprobada")][InlineData("6.99","Reprobada")][InlineData("0","Reprobada")][InlineData("10","Aprobada")]
 public void EstadoSeDerivaDelMinimoFisei(string nota,string estado)=>Assert.Equal(estado,Interna(nota:D(nota)).Estado);
 [Theory][InlineData("-0.01")][InlineData("10.01")][InlineData("8.555")] public void NotaFueraDeEscalaFiseiSeRechaza(string nota)=>Assert.Throws<SolicitudException>(()=>Interna(nota:D(nota)));
 [Fact] public void ExternaConservaSuEscalaSinConvertir() {var a=Externa();Assert.Equal(14m,a.Nota);Assert.Equal(20m,a.NotaMaximaEscala);Assert.Equal("Aprobada",a.Estado);}
 [Fact] public void ExternaBajoSuMinimoEsReprobadaAunqueSupereSiete()=>Assert.Equal("Reprobada",Externa(nota:11m).Estado);
 [Theory][InlineData("21","20","12")][InlineData("14","0","0")][InlineData("14","20","0")][InlineData("14","20","21")][InlineData("14","1000","12")] public void EscalaExternaInvalidaSeRechaza(string nota,string maxima,string minima)=>Assert.Throws<SolicitudException>(()=>Externa(nota:D(nota),maxima:D(maxima),minima:D(minima)));
 [Fact] public void FechaDeHoySeAcepta()=>Assert.Equal(Hoy,Interna(fecha:Hoy).FechaAprobacion);
 [Fact] public void FechaDeMananaSeRechaza()=>Assert.Throws<SolicitudException>(()=>Interna(fecha:Hoy.AddDays(1)));
 [Fact] public void FechaVaciaSeRechaza()=>Assert.Throws<SolicitudException>(()=>Interna(fecha:default(DateOnly)));
 [Theory][InlineData("")][InlineData("  ")] public void NumeroMatriculaObligatorio(string matricula)=>Assert.Throws<SolicitudException>(()=>Interna(matricula:matricula));
 [Fact] public void NumeroMatriculaSeConservaComoTexto()=>Assert.Equal("MAT-001/A",Interna(matricula:"  MAT-001/A ").NumeroMatricula);
 [Theory][InlineData(0)][InlineData(-1)][InlineData(4)] public void IntentoFueraDeRangoSeRechaza(int intento)=>Assert.Throws<SolicitudException>(()=>Interna(intento:intento));
 [Theory][InlineData(1)][InlineData(2)][InlineData(3)] public void IntentoValidoSeAcepta(int intento)=>Assert.Equal(intento,Interna(intento:intento).IntentoMatricula);
 [Theory][InlineData("")][InlineData("  ")] public void ExternaExigeNombre(string nombre)=>Assert.Throws<SolicitudException>(()=>Externa(nombre:nombre));
 [Theory][InlineData("")][InlineData("  ")] public void ExternaExigeSistemaDeCreditos(string sistema)=>Assert.Throws<SolicitudException>(()=>Externa(sistema:sistema));
 [Theory][InlineData("")][InlineData("  ")] public void ExternaExigeUnidadDeHoras(string unidad)=>Assert.Throws<SolicitudException>(()=>Externa(unidad:unidad));
 [Theory][InlineData("")][InlineData("  ")] public void ExternaExigeReferenciaAlPrograma(string referencia)=>Assert.Throws<SolicitudException>(()=>Externa(referencia:referencia));
 [Theory][InlineData("0")][InlineData("-1")] public void ExternaExigeCreditosPositivos(string creditos)=>Assert.Throws<SolicitudException>(()=>Externa(creditos:D(creditos)));
 [Theory][InlineData(0)][InlineData(-1)] public void ExternaExigeHorasPositivas(int horas)=>Assert.Throws<SolicitudException>(()=>Externa(horas:horas));
}
public class AsignaturasAprobadasApiTests {
 static string Ruta(Guid id)=>$"/api/solicitudes/{id}/asignaturas-aprobadas";
 static object Interna(SolicitudesFactory f,string fecha="2026-06-15",decimal nota=8.5m,int intento=1,string matricula="MAT-SINTETICA-1",Guid? asignatura=null)=>new {asignaturaOrigenId=asignatura??f.Catalogo.Algebra.Id,nota,fechaAprobacion=fecha,numeroMatricula=matricula,intentoMatricula=intento};
 static object Externa(string nombre="Álgebra externa")=>new {nombreAsignatura=nombre,creditos=4m,sistemaCreditos="Créditos sintéticos",horas=64,unidadHoras="Horas sintéticas",referenciaPrograma="PROGRAMA-SINTETICO-1",nota=14m,notaMaximaEscala=20m,notaMinimaAprobacion=12m,fechaAprobacion="2026-06-15",numeroMatricula="MAT-SINTETICA-1",intentoMatricula=1};
 static async Task<Guid> Solicitud(SolicitudesFactory f,HttpClient c,string quipux,bool externa=false) {
  var input=externa?f.Input() with {NumeroQuipux=quipux,TipoCambio="Externo",CarreraOrigenId=null,InstitucionOrigen="Universidad sintética",CarreraOrigenExterna="Carrera sintética"}:f.Input() with {NumeroQuipux=quipux};
  var response=await c.PostAsJsonAsync("/api/solicitudes",input);response.EnsureSuccessStatusCode();
  return (await response.Content.ReadFromJsonAsync<SolicitudesApiTests.SolicitudView>())!.Id;
 }
 static async Task<List<Asignatura>> Asignaturas(HttpClient c,Guid id)=>(await c.GetFromJsonAsync<Detalle>($"/api/solicitudes/{id}"))!.AsignaturasAprobadas;
 [Fact] public async Task InternaTomaLosDatosDelCatalogo() {
  using var f=new SolicitudesFactory();using var c=f.Client();var id=await Solicitud(f,c,"Q-A");
  var response=await c.PostAsJsonAsync(Ruta(id),Interna(f,intento:2));Assert.Equal(HttpStatusCode.Created,response.StatusCode);Assert.Equal($"/api/solicitudes/{id}",response.Headers.Location!.OriginalString);
  using var otro=f.Client();var a=Assert.Single(await Asignaturas(otro,id));
  Assert.Equal(f.Catalogo.Algebra.Id,a.AsignaturaOrigenId);Assert.Equal("COD-1",a.CodigoAsignatura);Assert.Equal("Álgebra sintética",a.NombreAsignatura);Assert.Equal(3m,a.Creditos);Assert.Equal(144,a.Horas);Assert.Equal(8.5m,a.Nota);Assert.Equal(10m,a.NotaMaximaEscala);Assert.Equal("Aprobada",a.Estado);Assert.Equal("MAT-SINTETICA-1",a.NumeroMatricula);Assert.Equal(2,a.IntentoMatricula);
 }
 [Fact] public async Task InternaBajoSieteSeGuardaComoReprobada() {
  using var f=new SolicitudesFactory();using var c=f.Client();var id=await Solicitud(f,c,"Q-A");
  Assert.Equal(HttpStatusCode.Created,(await c.PostAsJsonAsync(Ruta(id),Interna(f,nota:6.99m))).StatusCode);Assert.Equal("Reprobada",Assert.Single(await Asignaturas(c,id)).Estado);
 }
 [Fact] public async Task InternaRechazaAsignaturaAjenaALaCarreraDeOrigen() {
  using var f=new SolicitudesFactory();using var c=f.Client();var id=await Solicitud(f,c,"Q-A");
  Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync(Ruta(id),Interna(f,asignatura:Guid.NewGuid()))).StatusCode);Assert.Empty(await Asignaturas(c,id));
 }
 [Fact] public async Task InternaNoAceptaDatosManuales() {
  using var f=new SolicitudesFactory();using var c=f.Client();var id=await Solicitud(f,c,"Q-A");
  Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync(Ruta(id),new {asignaturaOrigenId=f.Catalogo.Algebra.Id,nombreAsignatura="Manual",nota=8m,fechaAprobacion="2026-06-15",numeroMatricula="M",intentoMatricula=1})).StatusCode);Assert.Empty(await Asignaturas(c,id));
 }
 [Fact] public async Task CatalogoCaidoNoGuardaAsignatura() {
  using var f=new SolicitudesFactory();using var c=f.Client();var id=await Solicitud(f,c,"Q-A");f.Catalogo.Falla=true;
  Assert.Equal(HttpStatusCode.ServiceUnavailable,(await c.PostAsJsonAsync(Ruta(id),Interna(f))).StatusCode);f.Catalogo.Falla=false;Assert.Empty(await Asignaturas(c,id));
 }
 [Fact] public async Task ExternaGuardaDatosYEscalaDeOrigen() {
  using var f=new SolicitudesFactory();using var c=f.Client();var id=await Solicitud(f,c,"Q-E",externa:true);
  Assert.Equal(HttpStatusCode.Created,(await c.PostAsJsonAsync(Ruta(id),Externa())).StatusCode);
  var a=Assert.Single(await Asignaturas(c,id));Assert.Null(a.AsignaturaOrigenId);Assert.Equal("Álgebra externa",a.NombreAsignatura);Assert.Equal(4m,a.Creditos);Assert.Equal("Créditos sintéticos",a.SistemaCreditos);Assert.Equal(64,a.Horas);Assert.Equal("Horas sintéticas",a.UnidadHoras);Assert.Equal("PROGRAMA-SINTETICO-1",a.ReferenciaPrograma);Assert.Equal(14m,a.Nota);Assert.Equal(20m,a.NotaMaximaEscala);Assert.Equal(12m,a.NotaMinimaAprobacion);Assert.Equal("Aprobada",a.Estado);
 }
 [Fact] public async Task ExternaNoAceptaAsignaturaDelCatalogo() {
  using var f=new SolicitudesFactory();using var c=f.Client();var id=await Solicitud(f,c,"Q-E",externa:true);
  Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync(Ruta(id),Interna(f))).StatusCode);Assert.Empty(await Asignaturas(c,id));
 }
 [Fact] public async Task AsignaturasDeOrigenSegunTipoDeSolicitud() {
  using var f=new SolicitudesFactory();using var c=f.Client();var interna=await Solicitud(f,c,"Q-A");var externa=await Solicitud(f,c,"Q-E",externa:true);
  var lista=await c.GetFromJsonAsync<List<AsignaturaCatalogo>>($"/api/solicitudes/{interna}/asignaturas-origen");Assert.Equal(f.Catalogo.Algebra,Assert.Single(lista!));
  Assert.Empty((await c.GetFromJsonAsync<List<AsignaturaCatalogo>>($"/api/solicitudes/{externa}/asignaturas-origen"))!);
  Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync($"/api/solicitudes/{Guid.NewGuid()}/asignaturas-origen")).StatusCode);
 }
 [Fact] public async Task LasAsignaturasNoSeMezclanEntreSolicitudes() {
  using var f=new SolicitudesFactory();using var c=f.Client();var a=await Solicitud(f,c,"Q-A");var b=await Solicitud(f,c,"Q-B");
  await c.PostAsJsonAsync(Ruta(a),Interna(f));
  Assert.Single(await Asignaturas(c,a));Assert.Empty(await Asignaturas(c,b));
 }
 [Fact] public async Task SolicitudInexistenteNoAdmiteHuerfanos() {
  using var f=new SolicitudesFactory();using var c=f.Client();
  Assert.Equal(HttpStatusCode.NotFound,(await c.PostAsJsonAsync(Ruta(Guid.NewGuid()),Interna(f))).StatusCode);Assert.Empty(f.Logs.Errores);
 }
 [Fact] public async Task FechaUsaElDiaDeEcuadorYNoElDeUtc() {
  using var f=new SolicitudesFactory();using var c=f.Client();var id=await Solicitud(f,c,"Q-A");
  f.Reloj.Ahora=new DateTimeOffset(2026,6,15,3,0,0,TimeSpan.Zero); // 14/06 22:00 en Ecuador
  Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync(Ruta(id),Interna(f,"2026-06-15"))).StatusCode);
  Assert.Equal(HttpStatusCode.Created,(await c.PostAsJsonAsync(Ruta(id),Interna(f,"2026-06-14"))).StatusCode);
 }
 [Fact] public async Task DatosInvalidosNoSeGuardan() {
  using var f=new SolicitudesFactory();using var c=f.Client();var id=await Solicitud(f,c,"Q-A");
  foreach(var cuerpo in new[]{Interna(f,"2026-06-16"),Interna(f,intento:0),Interna(f,intento:4),Interna(f,nota:-1m),Interna(f,nota:10.01m),Interna(f,matricula:" "),new {nota=8m}})
   Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync(Ruta(id),cuerpo)).StatusCode);
  Assert.Empty(await Asignaturas(c,id));Assert.Empty(f.Logs.Errores);
 }
 [Theory][InlineData(null,401)][InlineData("Docente",403)][InlineData("Administrador",403)]
 public async Task RegistroYAsignaturasDeOrigenExigenCoordinador(string? role,int status) {
  using var f=new SolicitudesFactory();using var coordinador=f.Client();var id=await Solicitud(f,coordinador,"Q-A");using var c=f.Client(role);
  Assert.Equal(status,(int)(await c.PostAsJsonAsync(Ruta(id),Interna(f))).StatusCode);Assert.Equal(status,(int)(await c.GetAsync($"/api/solicitudes/{id}/asignaturas-origen")).StatusCode);
  Assert.Empty(await Asignaturas(coordinador,id));
 }
 [Fact] public async Task GuardarNoDeclaraElegibilidad() {
  using var f=new SolicitudesFactory();using var c=f.Client();var id=await Solicitud(f,c,"Q-A");
  var texto=await (await c.PostAsJsonAsync(Ruta(id),Interna(f))).Content.ReadAsStringAsync();
  Assert.DoesNotContain("elegible",texto,StringComparison.OrdinalIgnoreCase);Assert.DoesNotContain("homologable",texto,StringComparison.OrdinalIgnoreCase);
 }
 public sealed record Detalle(Guid Id,string Estado,List<Asignatura> AsignaturasAprobadas);
 public sealed record Asignatura(Guid? AsignaturaOrigenId,string? CodigoAsignatura,string NombreAsignatura,decimal Creditos,string? SistemaCreditos,int Horas,string? UnidadHoras,string? ReferenciaPrograma,decimal Nota,decimal NotaMaximaEscala,decimal NotaMinimaAprobacion,string Estado,DateOnly FechaAprobacion,string NumeroMatricula,int IntentoMatricula);
}
