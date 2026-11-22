using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solicitudes.Infrastructure.Migrations;

public sealed partial class AdjuntosSolicitudHu12
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Adjuntos",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SolicitudId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                NombreArchivo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                TamanoBytes = table.Column<long>(type: "bigint", nullable: false),
                HuellaSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                SubidoPor = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                FechaSubidaUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                Contenido = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Adjuntos", x => x.Id);
                table.ForeignKey("FK_Adjuntos_Solicitudes_SolicitudId", x => x.SolicitudId, "Solicitudes", "Id", onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateIndex(name: "IX_Adjuntos_SolicitudId_FechaSubidaUtc", table: "Adjuntos", columns: new[] { "SolicitudId", "FechaSubidaUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Adjuntos");
    }
}
