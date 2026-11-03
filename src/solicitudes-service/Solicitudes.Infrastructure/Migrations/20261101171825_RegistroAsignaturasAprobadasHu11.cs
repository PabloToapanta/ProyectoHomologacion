using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solicitudes.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RegistroAsignaturasAprobadasHu11 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AsignaturasAprobadas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SolicitudId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AsignaturaOrigenId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CodigoAsignatura = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NombreAsignatura = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Creditos = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    SistemaCreditos = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Horas = table.Column<int>(type: "int", nullable: false),
                    UnidadHoras = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReferenciaPrograma = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Nota = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    NotaMaximaEscala = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    NotaMinimaAprobacion = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    FechaAprobacion = table.Column<DateOnly>(type: "date", nullable: false),
                    NumeroMatricula = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IntentoMatricula = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AsignaturasAprobadas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AsignaturasAprobadas_Solicitudes_SolicitudId",
                        column: x => x.SolicitudId,
                        principalTable: "Solicitudes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AsignaturasAprobadas_SolicitudId",
                table: "AsignaturasAprobadas",
                column: "SolicitudId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AsignaturasAprobadas");
        }
    }
}
