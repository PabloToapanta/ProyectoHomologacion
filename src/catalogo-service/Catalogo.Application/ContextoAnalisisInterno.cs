namespace Catalogo.Application;

public sealed record ElementoContextoInterno(Guid Id, string Texto);
public sealed record AsignaturaContextoInterno(Guid Id, Guid CarreraId, string Facultad, bool CarreraVigente,
    decimal Creditos, int Horas, string? PlanAnalitico, string? UnidadHoras, string? SistemaCreditos, bool EsSintetico,
    IReadOnlyList<ElementoContextoInterno> Contenidos, IReadOnlyList<ElementoContextoInterno> Resultados)
{
    public static AsignaturaContextoInterno Desde(AsignaturaDto asignatura, CarreraDto carrera,
        PerfilComparacionInput? perfil, IReadOnlyList<UnidadDto> unidades) => new(
        asignatura.Id, asignatura.CarreraId, carrera.Facultad, carrera.Vigencia, asignatura.Creditos, asignatura.Horas,
        perfil?.PlanAnalitico, perfil?.UnidadHoras, perfil?.SistemaCreditos, perfil?.EsSintetico ?? false,
        unidades.SelectMany(u => u.Contenidos).Select(c => new ElementoContextoInterno(c.Id, c.Tema)).ToArray(),
        unidades.Select(u => new ElementoContextoInterno(u.Id, u.ResultadoAprendizaje)).ToArray());
}
