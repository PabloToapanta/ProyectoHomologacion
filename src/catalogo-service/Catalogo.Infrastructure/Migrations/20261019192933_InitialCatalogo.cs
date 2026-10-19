using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalogo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCatalogo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Carreras",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Facultad = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Modalidad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Vigencia = table.Column<bool>(type: "bit", nullable: false),
                    NombreNormalizado = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, collation: "Latin1_General_100_BIN2"),
                    FacultadNormalizada = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, collation: "Latin1_General_100_BIN2")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Carreras", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Asignaturas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CarreraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Asignaturas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Asignaturas_Carreras_CarreraId",
                        column: x => x.CarreraId,
                        principalTable: "Carreras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Asignaturas_CarreraId",
                table: "Asignaturas",
                column: "CarreraId");

            migrationBuilder.CreateIndex(
                name: "IX_Carreras_NombreNormalizado_FacultadNormalizada",
                table: "Carreras",
                columns: new[] { "NombreNormalizado", "FacultadNormalizada" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Asignaturas");

            migrationBuilder.DropTable(
                name: "Carreras");
        }
    }
}
