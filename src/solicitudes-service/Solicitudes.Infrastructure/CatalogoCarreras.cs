using System.Net.Http.Json;
using System.Text.Json;
using Solicitudes.Application;
namespace Solicitudes.Infrastructure;
public sealed class CatalogoCarreras(HttpClient http) : ICatalogoCarreras {
 public async Task<IReadOnlyList<CarreraReferencia>> Listar(CancellationToken ct) {
  try {
   using var response=await http.GetAsync("api/carreras-solicitudes",ct);
   if(!response.IsSuccessStatusCode) throw new CatalogoNoDisponibleException();
   return await response.Content.ReadFromJsonAsync<List<CarreraReferencia>>(cancellationToken:ct) ?? throw new CatalogoNoDisponibleException();
  } catch(HttpRequestException) {throw new CatalogoNoDisponibleException();}
   catch(JsonException) {throw new CatalogoNoDisponibleException();}
   catch(OperationCanceledException) when(!ct.IsCancellationRequested) {throw new CatalogoNoDisponibleException();}
 }
}
