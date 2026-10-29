using Solicitudes.Domain;
using Xunit;
namespace Solicitudes.Tests;
public class SolicitudTests
{
 [Fact] public void NormalizaQuipuxParaUnicidad() { var s=new Solicitud("  uta-2026-001  ","EST-01","Estudiante sintético","Interno",Guid.NewGuid(),null,null,Guid.NewGuid());Assert.Equal("UTA-2026-001",s.QuipuxNormalizado); }
 [Theory][InlineData("")][InlineData("  ")][InlineData("\t")]
 public void RechazaQuipuxVacio(string q) => Assert.Throws<SolicitudException>(()=>new Solicitud(q,"EST-01","Nombre","Interno",Guid.NewGuid(),null,null,Guid.NewGuid()));
 [Fact] public void ExternoRequiereInstitucionYOriginal() => Assert.Throws<SolicitudException>(()=>new Solicitud("Q1","EST-01","Nombre","Externo",null,null,null,Guid.NewGuid()));
 [Fact] public void InternoNoPermiteMismaCarrera() {var id=Guid.NewGuid(); Assert.Throws<SolicitudException>(()=>new Solicitud("Q1","EST-01","Nombre","Interno",id,null,null,id));}
 [Fact] public void RechazaTipoDesconocido() => Assert.Throws<SolicitudException>(()=>new Solicitud("Q1","EST-01","Nombre","Otro",Guid.NewGuid(),null,null,Guid.NewGuid()));
}
