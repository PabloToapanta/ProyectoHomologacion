using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solicitudes.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AsignacionDocentesHu29 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocentesAsignados",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SolicitudId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocenteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NombreUsuario = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocentesAsignados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocentesAsignados_Solicitudes_SolicitudId",
                        column: x => x.SolicitudId,
                        principalTable: "Solicitudes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocentesAsignados_SolicitudId_DocenteId",
                table: "DocentesAsignados",
                columns: new[] { "SolicitudId", "DocenteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocentesAsignados_SolicitudId_Orden",
                table: "DocentesAsignados",
                columns: new[] { "SolicitudId", "Orden" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocentesAsignados");
        }
    }
}
