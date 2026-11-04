using Solicitudes.Domain;
namespace Solicitudes.Application;
public sealed record SolicitudInput(string NumeroQuipux,string IdentificacionEstudiante,string NombreEstudiante,string TipoCambio,Guid? CarreraOrigenId,string? InstitucionOrigen,string? CarreraOrigenExterna,Guid CarreraReceptoraId);
public sealed record CarreraReferencia(Guid Id,string Nombre);
public sealed record AsignaturaCatalogo(Guid Id,string Codigo,string Nombre,int Nivel,decimal Creditos,int Horas);
public interface ICatalogoCarreras {
 Task<IReadOnlyList<CarreraReferencia>> Listar(CancellationToken ct);
 Task<IReadOnlyList<AsignaturaCatalogo>> AsignaturasDe(Guid carreraId,CancellationToken ct);
}
public sealed class CatalogoNoDisponibleException() : Exception("No se pudo comprobar el catálogo. Intente nuevamente; la solicitud no fue guardada.");
public interface IDirectorioDocentes { Task<IReadOnlyList<DocenteDisponible>> Listar(CancellationToken ct); }
public sealed class DirectorioNoDisponibleException() : Exception("No se pudo comprobar los docentes en el servicio de autenticación. Intente nuevamente; no se asignó a nadie.");
public interface ISolicitudRepository {
 Task Agregar(Solicitud solicitud,CancellationToken ct);
 Task<Solicitud?> Obtener(Guid id,CancellationToken ct);
 Task<IReadOnlyList<Solicitud>> Listar(int pagina,CancellationToken ct);
 Task AgregarAsignatura(AsignaturaAprobada asignatura,CancellationToken ct);
 Task<IReadOnlyList<AsignaturaAprobada>> AsignaturasDe(Guid solicitudId,CancellationToken ct);
 Task<Solicitud?> ObtenerParaActualizar(Guid id,CancellationToken ct);
 Task GuardarAsignacion(IReadOnlyList<DocenteAsignado> docentes,CancellationToken ct);
 Task<IReadOnlyList<DocenteAsignado>> DocentesDe(Guid solicitudId,CancellationToken ct);
}
public sealed record AsignaturaAprobadaInput(Guid? AsignaturaOrigenId,string? NombreAsignatura,decimal? Creditos,string? SistemaCreditos,int? Horas,string? UnidadHoras,string? ReferenciaPrograma,decimal Nota,decimal? NotaMaximaEscala,decimal? NotaMinimaAprobacion,DateOnly FechaAprobacion,string NumeroMatricula,int IntentoMatricula);
public sealed record SolicitudDetalle(Guid Id,string NumeroQuipux,string IdentificacionEstudiante,string NombreEstudiante,string TipoCambio,Guid? CarreraOrigenId,string? InstitucionOrigen,string? CarreraOrigenExterna,Guid CarreraReceptoraId,string Estado,DateTime FechaRegistro,IReadOnlyList<AsignaturaAprobada> AsignaturasAprobadas,IReadOnlyList<DocenteAsignado> DocentesAsignados);
public sealed class SolicitudService(ISolicitudRepository repository,ICatalogoCarreras catalogo,IDirectorioDocentes directorio,TimeProvider reloj) {
 // Ecuador continental no aplica horario de verano; la fecha académica no depende de UTC ni del contenedor.
 static readonly TimeSpan OffsetEcuador=TimeSpan.FromHours(-5);
 public async Task<Solicitud> Crear(SolicitudInput input,CancellationToken ct) {
  var solicitud=new Solicitud(input.NumeroQuipux,input.IdentificacionEstudiante,input.NombreEstudiante,input.TipoCambio,input.CarreraOrigenId,input.InstitucionOrigen,input.CarreraOrigenExterna,input.CarreraReceptoraId);
  var carreras=await catalogo.Listar(ct);
  if(!carreras.Any(c=>c.Id==solicitud.CarreraReceptoraId) || (solicitud.CarreraOrigenId is Guid id && !carreras.Any(c=>c.Id==id))) throw new SolicitudException("Las carreras seleccionadas deben existir y estar vigentes en FISEI. Actualice el catálogo.");
  await repository.Agregar(solicitud,ct);return solicitud;
 }
 public Task<IReadOnlyList<Solicitud>> Listar(int pagina,CancellationToken ct) {
  if(pagina<1 || pagina>100000) throw new SolicitudException("La página debe estar entre 1 y 100000.");
  return repository.Listar(pagina,ct);
 }
 public async Task<SolicitudDetalle?> Obtener(Guid id,CancellationToken ct) {
  if(await repository.Obtener(id,ct) is not {} s) return null;
  return new(s.Id,s.NumeroQuipux,s.IdentificacionEstudiante,s.NombreEstudiante,s.TipoCambio,s.CarreraOrigenId,s.InstitucionOrigen,s.CarreraOrigenExterna,s.CarreraReceptoraId,s.Estado,s.FechaRegistro,await repository.AsignaturasDe(id,ct),await repository.DocentesDe(id,ct));
 }
 public async Task<AsignaturaAprobada> RegistrarAsignatura(Guid solicitudId,AsignaturaAprobadaInput input,CancellationToken ct) {
  var solicitud=await repository.Obtener(solicitudId,ct)??throw new SolicitudNoEncontradaException();
  var hoy=DateOnly.FromDateTime(reloj.GetUtcNow().ToOffset(OffsetEcuador).DateTime);
  var asignatura=solicitud.CarreraOrigenId is Guid carreraOrigen?await Interna(solicitudId,carreraOrigen,input,hoy,ct):Externa(solicitudId,input,hoy);
  await repository.AgregarAsignatura(asignatura,ct);return asignatura;
 }
 public async Task<IReadOnlyList<AsignaturaCatalogo>> AsignaturasOrigen(Guid solicitudId,CancellationToken ct) {
  var solicitud=await repository.Obtener(solicitudId,ct)??throw new SolicitudNoEncontradaException();
  return solicitud.CarreraOrigenId is Guid carreraOrigen?await catalogo.AsignaturasDe(carreraOrigen,ct):[];
 }
 async Task<AsignaturaAprobada> Interna(Guid solicitudId,Guid carreraOrigen,AsignaturaAprobadaInput i,DateOnly hoy,CancellationToken ct) {
  if(i.AsignaturaOrigenId is not Guid id) throw new SolicitudException("Seleccione la asignatura de origen del catálogo FISEI.");
  if(i.NombreAsignatura is not null||i.Creditos is not null||i.SistemaCreditos is not null||i.Horas is not null||i.UnidadHoras is not null||i.ReferenciaPrograma is not null||i.NotaMaximaEscala is not null||i.NotaMinimaAprobacion is not null) throw new SolicitudException("El cambio interno toma los datos del catálogo y la escala FISEI.");
  var origen=(await catalogo.AsignaturasDe(carreraOrigen,ct)).FirstOrDefault(a=>a.Id==id)??throw new SolicitudException("La asignatura seleccionada no pertenece a la carrera de origen.");
  return AsignaturaAprobada.Interna(solicitudId,origen.Id,origen.Codigo,origen.Nombre,origen.Creditos,origen.Horas,i.Nota,i.FechaAprobacion,i.NumeroMatricula,i.IntentoMatricula,hoy);
 }
 static AsignaturaAprobada Externa(Guid solicitudId,AsignaturaAprobadaInput i,DateOnly hoy) {
  if(i.AsignaturaOrigenId is not null) throw new SolicitudException("El cambio externo no utiliza asignaturas del catálogo FISEI.");
  if(i.Creditos is not decimal creditos||i.Horas is not int horas||i.NotaMaximaEscala is not decimal maxima||i.NotaMinimaAprobacion is not decimal minima) throw new SolicitudException("Complete créditos, horas y la escala de notas de la universidad de origen.");
  return AsignaturaAprobada.Externa(solicitudId,i.NombreAsignatura??"",creditos,i.SistemaCreditos??"",horas,i.UnidadHoras??"",i.ReferenciaPrograma??"",i.Nota,maxima,minima,i.FechaAprobacion,i.NumeroMatricula,i.IntentoMatricula,hoy);
 }
 public async Task<SolicitudDetalle> AsignarDocentes(Guid solicitudId,IReadOnlyList<Guid> docenteIds,CancellationToken ct) {
  var solicitud=await repository.ObtenerParaActualizar(solicitudId,ct)??throw new SolicitudNoEncontradaException();
  // Sin respuesta del directorio no se presume que los usuarios tengan rol Docente.
  var asignados=DocenteAsignado.Asignar(solicitud,docenteIds,await directorio.Listar(ct));
  await repository.GuardarAsignacion(asignados,ct);
  return (await Obtener(solicitudId,ct))!;
 }
 public async Task<IReadOnlyList<DocenteDisponible>> DocentesDisponibles(Guid solicitudId,CancellationToken ct) {
  if(await repository.Obtener(solicitudId,ct) is null) throw new SolicitudNoEncontradaException();
  return await directorio.Listar(ct);
 }
}
