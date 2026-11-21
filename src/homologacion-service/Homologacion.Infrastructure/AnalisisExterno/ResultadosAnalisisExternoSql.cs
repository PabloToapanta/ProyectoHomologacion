using Homologacion.Application.AnalisisExterno;
using Homologacion.Application.AnalisisInterno;
using Homologacion.Infrastructure.AnalisisInterno;

namespace Homologacion.Infrastructure.AnalisisExterno;

/// <summary>HU-19: fila de la tabla AnalisisExternos, sin FK ni acceso a bases ajenas.</summary>
public sealed class RegistroResultadoExterno : IRegistroAnalisis
{
    public Guid SolicitudId { get; set; }
    public Guid AprobadaId { get; set; }
    public Guid ReceptoraId { get; set; }
    public DateTimeOffset Fecha { get; set; }
    public string Contenido { get; set; } = "";
}

public sealed class ResultadosAnalisisExternoSql(ResultadosDbContext db) : IResultadosAnalisisExterno
{
    public Task GuardarAsync(AnalisisGuardado resultado, CancellationToken ct) => AlmacenPorPareja.GuardarAsync(db, db.ResultadosExternos, resultado, ct);
    public Task<IReadOnlyList<AnalisisGuardado>> ListarAsync(Guid solicitudId, CancellationToken ct) => AlmacenPorPareja.ListarAsync(db.ResultadosExternos, solicitudId, ct);
}
