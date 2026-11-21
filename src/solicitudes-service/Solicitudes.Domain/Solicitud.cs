using System.Text;
namespace Solicitudes.Domain;
public sealed class SolicitudException(string message) : Exception(message);
public sealed class DatosExternosNoEditablesException() : Exception("Los datos del cambio externo solo se corrigen antes de asignar docentes.");
public sealed class QuipuxDuplicadoException() : Exception("Ya existe una solicitud con ese número de trámite Quipux.");
public sealed class Solicitud
{
 public Guid Id {get; private set;}
 public string NumeroQuipux {get; private set;} = "";
 public string QuipuxNormalizado {get; private set;} = "";
 public string IdentificacionEstudiante {get; private set;} = "";
 public string NombreEstudiante {get; private set;} = "";
 public string TipoCambio {get; private set;} = "";
 public Guid? CarreraOrigenId {get; private set;}
 public string? InstitucionOrigen {get; private set;}
 public string? CarreraOrigenExterna {get; private set;}
 public Guid CarreraReceptoraId {get; private set;}
 public string Estado {get; private set;} = "Registrada";
 public DateTime FechaRegistro {get; private set;}
 /// <summary>HU-19 (D4): declarados por el Coordinador desde el expediente; null = sin dato, nunca se presume.</summary>
 public int? PeriodosCursados {get; private set;}
 public int? CambiosPreviosCarrera {get; private set;}
 // Límite técnico de validación de entrada; no es una regla institucional.
 const int LimiteTecnicoConteo=100;
 private Solicitud() { }
 public Solicitud(string quipux, string estudiante, string nombre, string tipo, Guid? origenId, string? institucion, string? carreraExterna, Guid receptoraId, int? periodosCursados=null, int? cambiosPreviosCarrera=null)
 {
  NumeroQuipux=Limpiar(quipux,"Número de trámite Quipux",100);
  QuipuxNormalizado=Limpiar(NumeroQuipux.ToUpperInvariant(),"Número de trámite Quipux",100);
  IdentificacionEstudiante=Limpiar(estudiante,"Identificación del estudiante",50);
  NombreEstudiante=Limpiar(nombre,"Nombre del estudiante",200);
  if(tipo is not ("Interno" or "Externo")) throw new SolicitudException("Seleccione cambio Interno o Externo.");
  if(receptoraId==Guid.Empty) throw new SolicitudException("La carrera receptora es obligatoria.");
  if(tipo=="Interno") {
   if(origenId is null || origenId==Guid.Empty || origenId==receptoraId) throw new SolicitudException("Seleccione una carrera de origen distinta de la receptora.");
   if(!string.IsNullOrWhiteSpace(institucion)||!string.IsNullOrWhiteSpace(carreraExterna)) throw new SolicitudException("El cambio interno utiliza carreras del catálogo FISEI.");
   if(periodosCursados is not null||cambiosPreviosCarrera is not null) throw new SolicitudException("Períodos cursados y cambios previos solo se registran en cambios externos.");
   CarreraOrigenId=origenId;
  } else {
   if(origenId is not null) throw new SolicitudException("El origen externo debe registrarse separado del catálogo FISEI.");
   InstitucionOrigen=Limpiar(institucion,"Universidad de origen",200);
   CarreraOrigenExterna=Limpiar(carreraExterna,"Carrera de origen",200);
   AsignarDatosExternos(periodosCursados,cambiosPreviosCarrera);
  }
  TipoCambio=tipo;CarreraReceptoraId=receptoraId;Id=Guid.NewGuid();FechaRegistro=DateTime.UtcNow;
 }
 /// <summary>Corrige los datos de RN-05 mientras no haya docentes asignados, y por tanto ningún análisis.</summary>
 public void DeclararDatosCambioExterno(int? periodosCursados,int? cambiosPreviosCarrera) {
  if(TipoCambio!="Externo") throw new SolicitudException("Períodos cursados y cambios previos solo se registran en cambios externos.");
  if(Estado!="Registrada") throw new DatosExternosNoEditablesException();
  AsignarDatosExternos(periodosCursados,cambiosPreviosCarrera);
 }
 void AsignarDatosExternos(int? periodos,int? cambios) {
  if(periodos is <0 or >LimiteTecnicoConteo) throw new SolicitudException($"Los períodos cursados deben estar entre 0 y {LimiteTecnicoConteo}.");
  if(cambios is <0 or >LimiteTecnicoConteo) throw new SolicitudException($"Los cambios previos de carrera deben estar entre 0 y {LimiteTecnicoConteo}.");
  PeriodosCursados=periodos;CambiosPreviosCarrera=cambios;
 }
 internal void MarcarDocentesAsignados() {Estado=DocenteAsignado.EstadoAsignada;}
 internal static string Limpiar(string? value,string campo,int max) {
  if(string.IsNullOrWhiteSpace(value)) throw new SolicitudException($"{campo} es obligatorio.");
  string text;
  try {text=value.Normalize(NormalizationForm.FormKC).Trim();} catch(ArgumentException) {throw new SolicitudException($"{campo} contiene caracteres inválidos.");}
  if(text.Length>max || text.Any(char.IsControl)) throw new SolicitudException($"{campo} admite hasta {max} caracteres y no admite caracteres de control.");
  return text;
 }
}
