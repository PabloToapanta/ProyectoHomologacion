using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solicitudes.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DatosCambioExternoHu19 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CambiosPreviosCarrera",
                table: "Solicitudes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PeriodosCursados",
                table: "Solicitudes",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CambiosPreviosCarrera",
                table: "Solicitudes");

            migrationBuilder.DropColumn(
                name: "PeriodosCursados",
                table: "Solicitudes");
        }
    }
}
