using Solicitudes.Domain;
namespace Solicitudes.Application;
public sealed record SolicitudInput(string NumeroQuipux,string IdentificacionEstudiante,string NombreEstudiante,string TipoCambio,Guid? CarreraOrigenId,string? InstitucionOrigen,string? CarreraOrigenExterna,Guid CarreraReceptoraId);
public sealed record CarreraReferencia(Guid Id,string Nombre);
public interface ICatalogoCarreras { Task<IReadOnlyList<CarreraReferencia>> Listar(CancellationToken ct); }
public sealed class CatalogoNoDisponibleException() : Exception("No se pudo comprobar el catálogo. Intente nuevamente; la solicitud no fue guardada.");
public interface ISolicitudRepository {
 Task Agregar(Solicitud solicitud,CancellationToken ct);
 Task<Solicitud?> Obtener(Guid id,CancellationToken ct);
 Task<IReadOnlyList<Solicitud>> Listar(int pagina,CancellationToken ct);
}
public sealed class SolicitudService(ISolicitudRepository repository,ICatalogoCarreras catalogo) {
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
 public Task<Solicitud?> Obtener(Guid id,CancellationToken ct)=>repository.Obtener(id,ct);
}
