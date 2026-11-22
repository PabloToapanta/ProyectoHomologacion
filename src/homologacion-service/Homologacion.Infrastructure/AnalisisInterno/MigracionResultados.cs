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
[Migration("20261120000100_EventosInformeHu25")]
public sealed class EventosInformeHu25 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "EventosInforme",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SolicitudId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                Fecha = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Usuario = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                Rol = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_EventosInforme", x => x.Id));
        migrationBuilder.CreateIndex(name: "IX_EventosInforme_SolicitudId_Tipo", table: "EventosInforme",
            columns: ["SolicitudId", "Tipo"], unique: true);
        // CA3: la aplicación no modifica ni elimina eventos y, en SQL Server, la base también lo impide.
        if (migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer")
            migrationBuilder.Sql(@"CREATE TRIGGER TR_EventosInforme_SoloInsercion ON EventosInforme INSTEAD OF UPDATE, DELETE AS
BEGIN
    THROW 50025, N'El historial del informe es de solo inserción.', 1;
END");
    }
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("EventosInforme");
    protected override void BuildTargetModel(ModelBuilder modelBuilder) => ModeloEventosInforme.Construir(modelBuilder);
}

internal static class ModeloEventosInforme
{
    public static void Construir(ModelBuilder modelBuilder)
    {
        ModeloInformes.Construir(modelBuilder);
        modelBuilder.Entity("Homologacion.Infrastructure.Informe.RegistroEventoInforme", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("Fecha").HasColumnType("datetimeoffset");
            b.Property<string>("Rol").IsRequired().HasMaxLength(30).HasColumnType("nvarchar(30)");
            b.Property<Guid>("SolicitudId").HasColumnType("uniqueidentifier");
            b.Property<string>("Tipo").IsRequired().HasMaxLength(20).HasColumnType("nvarchar(20)");
            b.Property<string>("Usuario").IsRequired().HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.Property<Guid>("UsuarioId").HasColumnType("uniqueidentifier");
            b.HasKey("Id");
            b.HasIndex("SolicitudId", "Tipo").IsUnique();
            b.ToTable("EventosInforme");
        });
    }
}
[DbContext(typeof(ResultadosDbContext))]
[Migration("20261123000100_DecisionesCoordinadorHu33")]
public sealed class DecisionesCoordinadorHu33 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.CreateTable(
        name: "DecisionesCoordinador",
        columns: table => new
        {
            SolicitudId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            AprobadaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            ReceptoraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            Resultado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
            Justificacion = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
            CoordinadorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            Coordinador = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
            Fecha = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
        },
        constraints: table => table.PrimaryKey("PK_DecisionesCoordinador", x => new { x.SolicitudId, x.AprobadaId, x.ReceptoraId }));
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("DecisionesCoordinador");
    protected override void BuildTargetModel(ModelBuilder modelBuilder) => ModeloDecisionesCoordinador.Construir(modelBuilder);
}

// Snapshot vigente: modelo de HU-25 más la tabla de HU-33.
internal static class ModeloDecisionesCoordinador
{
    public static void Construir(ModelBuilder modelBuilder)
    {
        ModeloEventosInforme.Construir(modelBuilder);
        modelBuilder.Entity("Homologacion.Infrastructure.Informe.RegistroDecisionCoordinador", b =>
        {
            b.Property<Guid>("SolicitudId").HasColumnType("uniqueidentifier");
            b.Property<Guid>("AprobadaId").HasColumnType("uniqueidentifier");
            b.Property<Guid>("ReceptoraId").HasColumnType("uniqueidentifier");
            b.Property<string>("Coordinador").IsRequired().HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.Property<Guid>("CoordinadorId").HasColumnType("uniqueidentifier");
            b.Property<DateTimeOffset>("Fecha").HasColumnType("datetimeoffset");
            b.Property<string>("Justificacion").IsRequired().HasMaxLength(2000).HasColumnType("nvarchar(2000)");
            b.Property<string>("Resultado").IsRequired().HasMaxLength(20).HasColumnType("nvarchar(20)");
            b.HasKey("SolicitudId", "AprobadaId", "ReceptoraId");
            b.ToTable("DecisionesCoordinador");
        });
    }
}
[DbContext(typeof(ResultadosDbContext))]
[Migration("20261121000100_ParametrosHomologacionHu03")]
public sealed class ParametrosHomologacionHu03 : Migration
{
    private static readonly Guid VersionInicial = Guid.Parse("9e0ce28d-e93a-4cad-b650-48ad9b260301");

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ConfiguracionesHomologacion",
            columns: table => new
            {
                VersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UmbralPct = table.Column<int>(type: "int", nullable: false),
                AntiguedadMaximaAnios = table.Column<int>(type: "int", nullable: false),
                PeriodosMinimosInterno = table.Column<int>(type: "int", nullable: false),
                AsignaturasHomologablesMinimasInterno = table.Column<int>(type: "int", nullable: false),
                PeriodosMinimosExterno = table.Column<int>(type: "int", nullable: false),
                AsignaturasHomologablesMinimasExterno = table.Column<int>(type: "int", nullable: false),
                ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                FechaHoraUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                Motivo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                Respaldo = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_ConfiguracionesHomologacion", x => x.VersionId));

        migrationBuilder.CreateTable(
            name: "ConfiguracionHomologacionVigente",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false),
                VersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ConfiguracionHomologacionVigente", x => x.Id);
                table.ForeignKey("FK_ConfiguracionHomologacionVigente_ConfiguracionesHomologacion_VersionId",
                    x => x.VersionId, "ConfiguracionesHomologacion", "VersionId", onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateIndex("IX_ConfiguracionHomologacionVigente_VersionId",
            "ConfiguracionHomologacionVigente", "VersionId");

        migrationBuilder.InsertData("ConfiguracionesHomologacion",
            ["VersionId", "UmbralPct", "AntiguedadMaximaAnios", "PeriodosMinimosInterno",
             "AsignaturasHomologablesMinimasInterno", "PeriodosMinimosExterno",
             "AsignaturasHomologablesMinimasExterno", "ActorId", "FechaHoraUtc", "Motivo", "Respaldo"],
            [VersionInicial, 80, 10, 1, 1, 2, 2, Guid.Empty,
             new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
             "Configuración predeterminada inicial de HU-03", "Inicialización del sistema"]);
        migrationBuilder.InsertData("ConfiguracionHomologacionVigente", ["Id", "VersionId"], [1, VersionInicial]);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("ConfiguracionHomologacionVigente");
        migrationBuilder.DropTable("ConfiguracionesHomologacion");
    }

    protected override void BuildTargetModel(ModelBuilder modelBuilder) => ModeloParametrosHu03.Construir(modelBuilder);
}

internal static class ModeloParametrosHu03
{
    public static void Construir(ModelBuilder modelBuilder)
    {
        ModeloInformes.Construir(modelBuilder);
        modelBuilder.Entity("Homologacion.Infrastructure.Configuracion.RegistroConfiguracionHomologacion", b =>
        {
            b.Property<Guid>("VersionId").HasColumnType("uniqueidentifier");
            b.Property<Guid>("ActorId").HasColumnType("uniqueidentifier");
            b.Property<int>("AntiguedadMaximaAnios").HasColumnType("int");
            b.Property<int>("AsignaturasHomologablesMinimasExterno").HasColumnType("int");
            b.Property<int>("AsignaturasHomologablesMinimasInterno").HasColumnType("int");
            b.Property<DateTimeOffset>("FechaHoraUtc").HasColumnType("datetimeoffset");
            b.Property<string>("Motivo").IsRequired().HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<int>("PeriodosMinimosExterno").HasColumnType("int");
            b.Property<int>("PeriodosMinimosInterno").HasColumnType("int");
            b.Property<string>("Respaldo").IsRequired().HasMaxLength(2000).HasColumnType("nvarchar(2000)");
            b.Property<byte[]>("RowVersion").IsRequired().IsConcurrencyToken().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.Property<int>("UmbralPct").HasColumnType("int");
            b.HasKey("VersionId");
            b.ToTable("ConfiguracionesHomologacion");
        });
        modelBuilder.Entity("Homologacion.Infrastructure.Configuracion.ConfiguracionHomologacionVigente", b =>
        {
            b.Property<int>("Id").ValueGeneratedNever().HasColumnType("int");
            b.Property<Guid>("VersionId").HasColumnType("uniqueidentifier");
            b.HasKey("Id");
            b.HasIndex("VersionId");
            b.ToTable("ConfiguracionHomologacionVigente");
        });
        modelBuilder.Entity("Homologacion.Infrastructure.Configuracion.ConfiguracionHomologacionVigente", b =>
        {
            b.HasOne("Homologacion.Infrastructure.Configuracion.RegistroConfiguracionHomologacion", null)
                .WithMany().HasForeignKey("VersionId").OnDelete(DeleteBehavior.Restrict);
        });
    }
}

[DbContext(typeof(ResultadosDbContext))]
[Migration("20261121000200_AuditoriaConfiguracionHu03")]
public sealed class AuditoriaConfiguracionHu03 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>("VersionAnteriorId", "ConfiguracionesHomologacion", "uniqueidentifier", nullable: true);
        migrationBuilder.CreateIndex("IX_ConfiguracionesHomologacion_VersionAnteriorId", "ConfiguracionesHomologacion", "VersionAnteriorId");
        migrationBuilder.AddForeignKey("FK_ConfiguracionesHomologacion_ConfiguracionesHomologacion_VersionAnteriorId",
            "ConfiguracionesHomologacion", "VersionAnteriorId", "ConfiguracionesHomologacion", principalColumn: "VersionId",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey("FK_ConfiguracionesHomologacion_ConfiguracionesHomologacion_VersionAnteriorId", "ConfiguracionesHomologacion");
        migrationBuilder.DropIndex("IX_ConfiguracionesHomologacion_VersionAnteriorId", "ConfiguracionesHomologacion");
        migrationBuilder.DropColumn("VersionAnteriorId", "ConfiguracionesHomologacion");
    }

    protected override void BuildTargetModel(ModelBuilder modelBuilder) => ModeloAuditoriaHu03.Construir(modelBuilder);
}

internal static class ModeloAuditoriaHu03
{
    public static void Construir(ModelBuilder modelBuilder)
    {
        ModeloParametrosHu03.Construir(modelBuilder);
        modelBuilder.Entity("Homologacion.Infrastructure.Configuracion.RegistroConfiguracionHomologacion", b =>
        {
            b.Property<Guid?>("VersionAnteriorId").HasColumnType("uniqueidentifier");
            b.HasIndex("VersionAnteriorId");
            b.HasOne("Homologacion.Infrastructure.Configuracion.RegistroConfiguracionHomologacion", null)
                .WithMany().HasForeignKey("VersionAnteriorId").OnDelete(DeleteBehavior.Restrict);
        });
    }
}
