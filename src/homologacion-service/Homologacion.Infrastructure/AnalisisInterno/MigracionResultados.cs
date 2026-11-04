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
[Migration("20261104000200_AnalisisExternosHu19")]
public sealed class AnalisisExternosHu19 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.CreateTable(
        name: "AnalisisExternos",
        columns: table => new
        {
            SolicitudId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            AprobadaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            ReceptoraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            Fecha = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
            Contenido = table.Column<string>(type: "nvarchar(max)", nullable: false)
        },
        constraints: table => table.PrimaryKey("PK_AnalisisExternos", x => new { x.SolicitudId, x.AprobadaId, x.ReceptoraId }));
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("AnalisisExternos");
    protected override void BuildTargetModel(ModelBuilder modelBuilder) => ModeloResultadosExternos.Construir(modelBuilder);
}

// Snapshot vigente: modelo inicial más la tabla de HU-19.
internal static class ModeloResultadosExternos
{
    public static void Construir(ModelBuilder modelBuilder)
    {
        ModeloResultadosInicial.Construir(modelBuilder);
        modelBuilder.Entity("Homologacion.Infrastructure.AnalisisExterno.RegistroResultadoExterno", b =>
        {
            b.Property<Guid>("SolicitudId").HasColumnType("uniqueidentifier");
            b.Property<Guid>("AprobadaId").HasColumnType("uniqueidentifier");
            b.Property<Guid>("ReceptoraId").HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("Fecha").HasColumnType("datetimeoffset");
            b.Property<string>("Contenido").IsRequired().HasColumnType("nvarchar(max)");
            b.HasKey("SolicitudId", "AprobadaId", "ReceptoraId");
            b.ToTable("AnalisisExternos");
        });
    }
}
[DbContext(typeof(ResultadosDbContext))]
[Migration("20261104000300_RevisionesDocentesHu31")]
public sealed class RevisionesDocentesHu31 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "RevisionesDocentes",
            columns: table => new
            {
                SolicitudId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                AprobadaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ReceptoraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DocenteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Docente = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                Postura = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                Decision = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                Observacion = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                EstadoAgente = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                SugerenciaAgente = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                Fecha = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_RevisionesDocentes", x => new { x.SolicitudId, x.AprobadaId, x.ReceptoraId, x.DocenteId }));
        migrationBuilder.CreateIndex(name: "IX_RevisionesDocentes_SolicitudId", table: "RevisionesDocentes", column: "SolicitudId");
    }
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("RevisionesDocentes");
    protected override void BuildTargetModel(ModelBuilder modelBuilder) => ModeloRevisionesDocentes.Construir(modelBuilder);
}

// Snapshot vigente: modelo de HU-19 más la tabla de HU-31.
internal static class ModeloRevisionesDocentes
{
    public static void Construir(ModelBuilder modelBuilder)
    {
        ModeloResultadosExternos.Construir(modelBuilder);
        modelBuilder.Entity("Homologacion.Infrastructure.RevisionDocente.RegistroRevisionDocente", b =>
        {
            b.Property<Guid>("SolicitudId").HasColumnType("uniqueidentifier");
            b.Property<Guid>("AprobadaId").HasColumnType("uniqueidentifier");
            b.Property<Guid>("ReceptoraId").HasColumnType("uniqueidentifier");
            b.Property<Guid>("DocenteId").HasColumnType("uniqueidentifier");
            b.Property<string>("Decision").IsRequired().HasMaxLength(20).HasColumnType("nvarchar(20)");
            b.Property<string>("Docente").IsRequired().HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.Property<string>("EstadoAgente").IsRequired().HasMaxLength(20).HasColumnType("nvarchar(20)");
            b.Property<DateTimeOffset>("Fecha").HasColumnType("datetimeoffset");
            b.Property<string>("Observacion").HasMaxLength(2000).HasColumnType("nvarchar(2000)");
            b.Property<string>("Postura").IsRequired().HasMaxLength(20).HasColumnType("nvarchar(20)");
            b.Property<string>("SugerenciaAgente").HasMaxLength(50).HasColumnType("nvarchar(50)");
            b.HasKey("SolicitudId", "AprobadaId", "ReceptoraId", "DocenteId");
            b.HasIndex("SolicitudId");
            b.ToTable("RevisionesDocentes");
        });
    }
}
[DbContext(typeof(ResultadosDbContext))]
[Migration("20261104000400_InformesHomologacionHu23")]
public sealed class InformesHomologacionHu23 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.CreateTable(
        name: "InformesHomologacion",
        columns: table => new
        {
            SolicitudId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            Fecha = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
            GeneradoPorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            Contenido = table.Column<string>(type: "nvarchar(max)", nullable: false)
        },
        constraints: table => table.PrimaryKey("PK_InformesHomologacion", x => x.SolicitudId));
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("InformesHomologacion");
    protected override void BuildTargetModel(ModelBuilder modelBuilder) => ModeloInformes.Construir(modelBuilder);
}

// Snapshot vigente: modelo de HU-31 más la tabla de HU-23.
internal static class ModeloInformes
{
    public static void Construir(ModelBuilder modelBuilder)
    {
        ModeloRevisionesDocentes.Construir(modelBuilder);
        modelBuilder.Entity("Homologacion.Infrastructure.Informe.RegistroInforme", b =>
        {
            b.Property<Guid>("SolicitudId").HasColumnType("uniqueidentifier");
            b.Property<string>("Contenido").IsRequired().HasColumnType("nvarchar(max)");
            b.Property<DateTimeOffset>("Fecha").HasColumnType("datetimeoffset");
            b.Property<Guid>("GeneradoPorId").HasColumnType("uniqueidentifier");
            b.HasKey("SolicitudId");
            b.ToTable("InformesHomologacion");
        });
    }
}
[DbContext(typeof(ResultadosDbContext))]
public sealed class ResultadosDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder) => ModeloInformes.Construir(modelBuilder);
}
