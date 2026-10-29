using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solicitudes.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RegistroSolicitudesHu10 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Solicitudes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumeroQuipux = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    QuipuxNormalizado = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IdentificacionEstudiante = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NombreEstudiante = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TipoCambio = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CarreraOrigenId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InstitucionOrigen = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CarreraOrigenExterna = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CarreraReceptoraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Solicitudes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Solicitudes_FechaRegistro",
                table: "Solicitudes",
                column: "FechaRegistro");

            migrationBuilder.CreateIndex(
                name: "IX_Solicitudes_QuipuxNormalizado",
                table: "Solicitudes",
                column: "QuipuxNormalizado",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Solicitudes");
        }
    }
}
