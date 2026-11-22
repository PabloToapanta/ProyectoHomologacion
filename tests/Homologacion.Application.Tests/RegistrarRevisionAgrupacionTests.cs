using Homologacion.Application.AnalisisInterno;
using Homologacion.Application.Equivalencias;
using Homologacion.Application.Informe;
using Homologacion.Application.RevisionDocente;
using Homologacion.Domain.Equivalencias;
using Xunit;

namespace Homologacion.Application.Tests;

public sealed class RegistrarRevisionAgrupacionTests
{
    private static readonly Guid Solicitud = Guid.NewGuid();
    private static readonly Guid Grupo = Guid.NewGuid();
    private static readonly Guid Docente = Guid.NewGuid();
    private sealed class Fuentes : ISolicitudesAnalisisInterno, IResultadosAgrupacionInforme, IRevisionesAgrupacion, IInformesHomologacion
    {
        public bool Asignada = true;
        public bool AnalisisExiste = true;
        public InformeHomologacion? Informe;
        public List<RevisionAgrupacionRegistrada> Revisiones = [];
        public Task<SolicitudParaAnalisis?> ObtenerSolicitudAsync(Guid id, CancellationToken ct) =>
            Task.FromResult<SolicitudParaAnalisis?>(Asignada && id == Solicitud
                ? new(Solicitud, "Interno", Guid.NewGuid(), Guid.NewGuid(), []) : null);
        public Task<IReadOnlyList<AnalisisAgrupacionGuardado>> ListarAsync(Guid id, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<AnalisisAgrupacionGuardado>>(AnalisisExiste
                ? [new(Solicitud, Grupo, [], new ResultadoAgrupacion([], 80, EstadoEquivalencia.Homologable,
                    "ok", [], [], []), DateTimeOffset.UnixEpoch)] : []);
        Task<IReadOnlyList<RevisionAgrupacionRegistrada>> IRevisionesAgrupacionInforme.ListarAsync(Guid id, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<RevisionAgrupacionRegistrada>>(Revisiones);
        public Task GuardarAsync(RevisionAgrupacionRegistrada revision, CancellationToken ct)
        {
            Revisiones.RemoveAll(r => r.GrupoId == revision.GrupoId && r.DocenteId == revision.DocenteId);
            Revisiones.Add(revision);
            return Task.CompletedTask;
        }
        public Task<InformeHomologacion?> ObtenerAsync(Guid id, CancellationToken ct) => Task.FromResult(Informe);
        public Task GuardarAsync(InformeHomologacion informe, CancellationToken ct) => Task.CompletedTask;
    }

    [Fact]
    public async Task Guarda_una_revision_por_docente_y_permite_corregirla()
    {
        var f = new Fuentes();
        var caso = new RegistrarRevisionAgrupacion(f, f, f, f, TimeProvider.System);

        var primera = await caso.RegistrarAsync(Solicitud, Grupo, Docente, "Ana",
            new("DeAcuerdo", null, null), default);
        var segunda = await caso.RegistrarAsync(Solicitud, Grupo, Docente, "Ana",
            new("Ajuste", "NoHomologable", "Falta contexto"), default);

        Assert.Equal("Homologable", primera.Decision);
        Assert.Equal("NoHomologable", segunda.Decision);
        Assert.Single(f.Revisiones);
    }

    [Fact]
    public async Task Exige_analisis_guardado_y_bloquea_edicion_tras_informe()
    {
        var f = new Fuentes { AnalisisExiste = false };
        var caso = new RegistrarRevisionAgrupacion(f, f, f, f, TimeProvider.System);
        var input = new RevisionAgrupacionInput("Ajuste", "Homologable", "Revisado");

        await Assert.ThrowsAsync<DatosAnalisisSolicitudException>(() => caso.RegistrarAsync(Solicitud, Grupo, Docente, "Ana", input, default));
        f.AnalisisExiste = true;
        f.Informe = new(Solicitud, "Interno", DateTimeOffset.UtcNow, Docente, "Ana", [], [], 0, 0, 0, []);
        await Assert.ThrowsAsync<InformeYaGeneradoException>(() => caso.RegistrarAsync(Solicitud, Grupo, Docente, "Ana", input, default));
        Assert.Empty(f.Revisiones);
    }

    [Fact]
    public async Task Rechaza_docente_no_asignado_y_postura_sin_observacion()
    {
        var f = new Fuentes { Asignada = false };
        var caso = new RegistrarRevisionAgrupacion(f, f, f, f, TimeProvider.System);
        await Assert.ThrowsAsync<SolicitudAnalisisNoEncontradaException>(() => caso.RegistrarAsync(Solicitud, Grupo, Docente, "Ana",
            new("DeAcuerdo", null, null), default));
        f.Asignada = true;
        await Assert.ThrowsAsync<DatosAnalisisSolicitudException>(() => caso.RegistrarAsync(Solicitud, Grupo, Docente, "Ana",
            new("Ajuste", "NoHomologable", null), default));
    }
}
