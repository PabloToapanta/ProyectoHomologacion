using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Homologacion.Infrastructure.AnalisisInterno;

[DbContext(typeof(ResultadosDbContext))]
[Migration("20261104000100_AnalisisInternosHu20")]
public sealed class AnalisisInternosHu20 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.CreateTable(
        name: "AnalisisInternos",
        columns: table => new
        {
            SolicitudId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            AprobadaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            ReceptoraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            Fecha = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
            Contenido = table.Column<string>(type: "nvarchar(max)", nullable: false)
        },
        constraints: table => table.PrimaryKey("PK_AnalisisInternos", x => new { x.SolicitudId, x.AprobadaId, x.ReceptoraId }));
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("AnalisisInternos");
    protected override void BuildTargetModel(ModelBuilder modelBuilder) => ModeloResultadosInicial.Construir(modelBuilder);
}

// Snapshot de la primera migración; futuras migraciones conservan esta definición.
internal static class ModeloResultadosInicial
{
    public static void Construir(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "8.0.30");
        modelBuilder.Entity("Homologacion.Infrastructure.AnalisisInterno.RegistroResultado", b =>
        {
            b.Property<Guid>("SolicitudId").HasColumnType("uniqueidentifier");
            b.Property<Guid>("AprobadaId").HasColumnType("uniqueidentifier");
            b.Property<Guid>("ReceptoraId").HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("Fecha").HasColumnType("datetimeoffset");
            b.Property<string>("Contenido").IsRequired().HasColumnType("nvarchar(max)");
            b.HasKey("SolicitudId", "AprobadaId", "ReceptoraId");
            b.ToTable("AnalisisInternos");
        });
    }
}
[DbContext(typeof(ResultadosDbContext))]
public sealed class ResultadosDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder) => ModeloResultadosInicial.Construir(modelBuilder);
}
