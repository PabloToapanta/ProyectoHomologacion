using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Catalogo.Domain;

namespace Catalogo.Application;

public record PerfilComparacionInput(string PlanAnalitico, string UnidadHoras, string SistemaCreditos, bool EsSintetico = false);
public record VinculoComparacion(string ElementoOrigen, string ElementoReceptor, string UbicacionOrigen, string UbicacionReceptora);
public record RevisionComparacionInput(IReadOnlyList<VinculoComparacion>? Contenidos, IReadOnlyList<VinculoComparacion>? Resultados);
public record RevisionComparacionDto(bool Registrada, bool Vigente, IReadOnlyList<VinculoComparacion> Contenidos, IReadOnlyList<VinculoComparacion> Resultados);
public interface IComparacionCatalogoRepository
{
    Task<PerfilComparacion?> PerfilAsync(Guid id, CancellationToken ct);
    Task<RevisionComparacion?> RevisionAsync(Guid origen, Guid receptora, CancellationToken ct);
    Task GuardarPerfilAsync(PerfilComparacion perfil, CancellationToken ct);
    Task GuardarRevisionAsync(RevisionComparacion revision, CancellationToken ct);
}
public sealed class ComparacionCatalogoService(IComparacionCatalogoRepository repository, IAsignaturaRepository asignaturas, IUnidadRepository unidades)
{
    private async Task Verificar(Guid id, CancellationToken ct)
    { if (await asignaturas.BuscarAsync(id, ct) is null) throw new CatalogoException("no_encontrada", "No se encontró la asignatura."); }
    public async Task<PerfilComparacionInput?> PerfilAsync(Guid id, CancellationToken ct)
    {
        await Verificar(id, ct); var p = await repository.PerfilAsync(id, ct);
        return p is null ? null : new(p.PlanAnalitico, p.UnidadHoras, p.SistemaCreditos, p.EsSintetico);
    }
    public async Task<PerfilComparacionInput> GuardarPerfilAsync(Guid id, PerfilComparacionInput input, CancellationToken ct)
    {
        await Verificar(id, ct); var p = await repository.PerfilAsync(id, ct);
        if (p is null) p = new(id, input.PlanAnalitico, input.UnidadHoras, input.SistemaCreditos, input.EsSintetico);
        else p.Actualizar(input.PlanAnalitico, input.UnidadHoras, input.SistemaCreditos, input.EsSintetico);
        await repository.GuardarPerfilAsync(p, ct);
        return new(p.PlanAnalitico, p.UnidadHoras, p.SistemaCreditos, p.EsSintetico);
    }
    private async Task<(string Firma, HashSet<string> Contenidos, HashSet<string> Resultados, bool Completo)> Programa(Guid id, CancellationToken ct)
    {
        await Verificar(id, ct); var p = await repository.PerfilAsync(id, ct); var us = await unidades.ListarAsync(id, ct);
        var ordenadas = us.OrderBy(u => u.Numero).Select(UnidadDto.From).ToArray();
        var json = JsonSerializer.Serialize(new { Perfil = p, Unidades = ordenadas });
        return (Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))),
            us.SelectMany(u => u.Contenidos).Select(c => c.Id.ToString()).ToHashSet(),
            us.Select(u => u.Id.ToString()).ToHashSet(), p is not null && us.Count > 0);
    }
    public async Task<RevisionComparacionDto> RevisionAsync(Guid origen, Guid receptora, CancellationToken ct)
    {
        var o = await Programa(origen, ct); var r = await Programa(receptora, ct);
        var revision = await repository.RevisionAsync(origen, receptora, ct);
        if (revision is null) return new(false, false, [], []);
        var datos = JsonSerializer.Deserialize<RevisionComparacionInput>(revision.DatosJson)!;
        return new(true, o.Completo && r.Completo && o.Firma == revision.FirmaOrigen && r.Firma == revision.FirmaReceptora, datos.Contenidos!, datos.Resultados!);
    }
    public async Task<RevisionComparacionDto> GuardarRevisionAsync(Guid origen, Guid receptora, RevisionComparacionInput input, CancellationToken ct)
    {
        if (origen == receptora) throw new CatalogoException("validacion", "Selecciona dos asignaturas distintas.");
        var o = await Programa(origen, ct); var r = await Programa(receptora, ct);
        if (!o.Completo || !r.Completo) throw new CatalogoException("validacion", "Registra perfiles y unidades de ambos programas antes de revisar correspondencias.");
        Validar(input.Contenidos, o.Contenidos, r.Contenidos); Validar(input.Resultados, o.Resultados, r.Resultados);
        var json = JsonSerializer.Serialize(input);
        var revision = await repository.RevisionAsync(origen, receptora, ct);
        if (revision is null) revision = new(origen, receptora, o.Firma, r.Firma, json);
        else revision.Actualizar(o.Firma, r.Firma, json);
        await repository.GuardarRevisionAsync(revision, ct);
        return new(true, true, input.Contenidos!, input.Resultados!);
    }
    private static void Validar(IReadOnlyList<VinculoComparacion>? vinculos, HashSet<string> origen, HashSet<string> receptora)
    {
        if (vinculos is null || vinculos.Count > 1000) throw new CatalogoException("validacion", "Envía las listas explícitas de correspondencias (máximo 1000 por categoría).");
        foreach (var v in vinculos)
            if (v is null || !origen.Contains(v.ElementoOrigen) || !receptora.Contains(v.ElementoReceptor)
                || string.IsNullOrWhiteSpace(v.UbicacionOrigen) || string.IsNullOrWhiteSpace(v.UbicacionReceptora)
                || v.UbicacionOrigen.Length > 500 || v.UbicacionReceptora.Length > 500)
                throw new CatalogoException("validacion", "Cada vínculo debe referir elementos existentes y evidencia de ambos planes (máximo 500 caracteres).");
        if (vinculos.Select(v => (v.ElementoOrigen, v.ElementoReceptor)).Distinct().Count() != vinculos.Count)
            throw new CatalogoException("validacion", "No repitas la misma correspondencia.");
    }
}
