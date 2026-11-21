namespace Solicitudes.Domain;
public sealed class AsignacionNoPermitidaException() : Exception("La solicitud ya tiene docentes asignados. La reasignación no está definida.");
public sealed record DocenteDisponible(Guid Id,string NombreUsuario);
public sealed class DocenteAsignado
{
 public const string EstadoAsignada="Docentes asignados";
 public Guid Id {get; private set;}
 public Guid SolicitudId {get; private set;}
 public Guid DocenteId {get; private set;}
 public string NombreUsuario {get; private set;} = "";
 public int Orden {get; private set;}
 private DocenteAsignado() { }
 DocenteAsignado(Guid solicitudId,DocenteDisponible docente,int orden) {Id=Guid.NewGuid();SolicitudId=solicitudId;DocenteId=docente.Id;NombreUsuario=docente.NombreUsuario;Orden=orden;}
 /// <summary>Asigna exactamente dos docentes distintos con rol Docente a una solicitud registrada.</summary>
 public static IReadOnlyList<DocenteAsignado> Asignar(Solicitud solicitud,IReadOnlyList<Guid> docenteIds,IReadOnlyCollection<DocenteDisponible> disponibles)
 {
  if(solicitud.Estado!="Registrada") throw new AsignacionNoPermitidaException();
  if(docenteIds.Count!=2) throw new SolicitudException("Seleccione exactamente dos docentes.");
  if(docenteIds[0]==docenteIds[1]) throw new SolicitudException("Seleccione dos docentes distintos.");
  var elegidos=docenteIds.Select(id=>disponibles.FirstOrDefault(d=>d.Id==id)).ToList();
  if(elegidos.Any(d=>d is null)) throw new SolicitudException("Solo pueden asignarse usuarios activos con rol Docente.");
  solicitud.MarcarDocentesAsignados();
  return elegidos.Select((d,i)=>new DocenteAsignado(solicitud.Id,d!,i+1)).ToList();
 }
}
