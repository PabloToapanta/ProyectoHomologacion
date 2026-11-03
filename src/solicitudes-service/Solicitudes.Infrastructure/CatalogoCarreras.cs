using System.Net.Http.Json;
using System.Text.Json;
using Solicitudes.Application;
namespace Solicitudes.Infrastructure;
public sealed class CatalogoCarreras(HttpClient http) : ICatalogoCarreras {
 public async Task<IReadOnlyList<CarreraReferencia>> Listar(CancellationToken ct)=>await Leer<CarreraReferencia>("api/carreras-solicitudes",ct);
 public async Task<IReadOnlyList<AsignaturaCatalogo>> AsignaturasDe(Guid carreraId,CancellationToken ct)=>await Leer<AsignaturaCatalogo>($"api/carreras-solicitudes/{carreraId}/asignaturas",ct);
 async Task<List<T>> Leer<T>(string ruta,CancellationToken ct) {
  try {
   using var response=await http.GetAsync(ruta,ct);
   if(!response.IsSuccessStatusCode) throw new CatalogoNoDisponibleException();
   return await response.Content.ReadFromJsonAsync<List<T>>(cancellationToken:ct) ?? throw new CatalogoNoDisponibleException();
  } catch(HttpRequestException) {throw new CatalogoNoDisponibleException();}
   catch(JsonException) {throw new CatalogoNoDisponibleException();}
   catch(OperationCanceledException) when(!ct.IsCancellationRequested) {throw new CatalogoNoDisponibleException();}
 }
}
