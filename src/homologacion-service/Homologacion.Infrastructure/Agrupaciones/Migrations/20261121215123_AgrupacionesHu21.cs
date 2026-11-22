using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Homologacion.Infrastructure.Agrupaciones.Migrations
{
    /// <inheritdoc />
    public partial class AgrupacionesHu21 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Agrupaciones",
                columns: table => new
                {
                    GrupoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SolicitudId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Congelado = table.Column<bool>(type: "bit", nullable: false),
                    Analisis = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agrupaciones", x => x.GrupoId);
                });

            migrationBuilder.CreateTable(
                name: "ReservasOrigen",
                columns: table => new
                {
                    SolicitudId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AprobadaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GrupoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReceptoraParejaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservasOrigen", x => new { x.SolicitudId, x.AprobadaId });
                });

            migrationBuilder.CreateTable(
                name: "OrigenesAgrupacion",
                columns: table => new
                {
                    GrupoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AprobadaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrigenesAgrupacion", x => new { x.GrupoId, x.AprobadaId });
                    table.ForeignKey(
                        name: "FK_OrigenesAgrupacion_Agrupaciones_GrupoId",
                        column: x => x.GrupoId,
                        principalTable: "Agrupaciones",
                        principalColumn: "GrupoId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReceptorasAgrupacion",
                columns: table => new
                {
                    GrupoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceptoraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceptorasAgrupacion", x => new { x.GrupoId, x.ReceptoraId });
                    table.ForeignKey(
                        name: "FK_ReceptorasAgrupacion_Agrupaciones_GrupoId",
                        column: x => x.GrupoId,
                        principalTable: "Agrupaciones",
                        principalColumn: "GrupoId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RevisionesAgrupacion",
                columns: table => new
                {
                    GrupoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocenteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SolicitudId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Contenido = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RevisionesAgrupacion", x => new { x.GrupoId, x.DocenteId });
                    table.ForeignKey(
                        name: "FK_RevisionesAgrupacion_Agrupaciones_GrupoId",
                        column: x => x.GrupoId,
                        principalTable: "Agrupaciones",
                        principalColumn: "GrupoId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AportesAgrupacion",
                columns: table => new
                {
                    GrupoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AprobadaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceptoraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Horas = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    Creditos = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AportesAgrupacion", x => new { x.GrupoId, x.AprobadaId, x.ReceptoraId });
                    table.ForeignKey(
                        name: "FK_AportesAgrupacion_OrigenesAgrupacion_GrupoId_AprobadaId",
                        columns: x => new { x.GrupoId, x.AprobadaId },
                        principalTable: "OrigenesAgrupacion",
                        principalColumns: new[] { "GrupoId", "AprobadaId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AportesAgrupacion_ReceptorasAgrupacion_GrupoId_ReceptoraId",
                        columns: x => new { x.GrupoId, x.ReceptoraId },
                        principalTable: "ReceptorasAgrupacion",
                        principalColumns: new[] { "GrupoId", "ReceptoraId" });
                });

            migrationBuilder.CreateIndex(
                name: "IX_Agrupaciones_SolicitudId",
                table: "Agrupaciones",
                column: "SolicitudId");

            migrationBuilder.CreateIndex(
                name: "IX_AportesAgrupacion_GrupoId_ReceptoraId",
                table: "AportesAgrupacion",
                columns: new[] { "GrupoId", "ReceptoraId" });

            migrationBuilder.CreateIndex(
                name: "IX_RevisionesAgrupacion_SolicitudId",
                table: "RevisionesAgrupacion",
                column: "SolicitudId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AportesAgrupacion");

            migrationBuilder.DropTable(
                name: "ReservasOrigen");

            migrationBuilder.DropTable(
                name: "RevisionesAgrupacion");

            migrationBuilder.DropTable(
                name: "OrigenesAgrupacion");

            migrationBuilder.DropTable(
                name: "ReceptorasAgrupacion");

            migrationBuilder.DropTable(
                name: "Agrupaciones");
        }
    }
}
