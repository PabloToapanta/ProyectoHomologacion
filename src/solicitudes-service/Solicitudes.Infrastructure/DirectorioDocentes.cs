using System.Net.Http.Json;
using System.Text.Json;
using Solicitudes.Application;
using Solicitudes.Domain;
namespace Solicitudes.Infrastructure;
/// <summary>Consulta los docentes activos en Auth por HTTP; no accede a su base de datos.</summary>
public sealed class DirectorioDocentes(HttpClient http) : IDirectorioDocentes {
 public async Task<IReadOnlyList<DocenteDisponible>> Listar(CancellationToken ct) {
  try {
   using var response=await http.GetAsync("api/usuarios/docentes",ct);
   if(!response.IsSuccessStatusCode) throw new DirectorioNoDisponibleException();
   return await response.Content.ReadFromJsonAsync<List<DocenteDisponible>>(cancellationToken:ct) ?? throw new DirectorioNoDisponibleException();
  } catch(HttpRequestException) {throw new DirectorioNoDisponibleException();}
   catch(JsonException) {throw new DirectorioNoDisponibleException();}
   catch(OperationCanceledException) when(!ct.IsCancellationRequested) {throw new DirectorioNoDisponibleException();}
 }
}
