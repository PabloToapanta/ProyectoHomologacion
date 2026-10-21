using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalogo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PerfilesYCorrespondenciasHu14 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PerfilesComparacion",
                columns: table => new
                {
                    AsignaturaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanAnalitico = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UnidadHoras = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SistemaCreditos = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EsSintetico = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerfilesComparacion", x => x.AsignaturaId);
                    table.ForeignKey(
                        name: "FK_PerfilesComparacion_Asignaturas_AsignaturaId",
                        column: x => x.AsignaturaId,
                        principalTable: "Asignaturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RevisionesComparacion",
                columns: table => new
                {
                    OrigenId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceptoraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirmaOrigen = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FirmaReceptora = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DatosJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RevisionesComparacion", x => new { x.OrigenId, x.ReceptoraId });
                    table.ForeignKey(
                        name: "FK_RevisionesComparacion_Asignaturas_OrigenId",
                        column: x => x.OrigenId,
                        principalTable: "Asignaturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RevisionesComparacion_Asignaturas_ReceptoraId",
                        column: x => x.ReceptoraId,
                        principalTable: "Asignaturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RevisionesComparacion_ReceptoraId",
                table: "RevisionesComparacion",
                column: "ReceptoraId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PerfilesComparacion");

            migrationBuilder.DropTable(
                name: "RevisionesComparacion");
        }
    }
}
