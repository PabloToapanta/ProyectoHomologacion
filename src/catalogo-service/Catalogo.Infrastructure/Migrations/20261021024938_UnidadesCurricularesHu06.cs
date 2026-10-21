using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalogo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UnidadesCurricularesHu06 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UnidadesCurriculares",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AsignaturaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Horas = table.Column<int>(type: "int", nullable: false),
                    ResultadoAprendizaje = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnidadesCurriculares", x => x.Id);
                    table.CheckConstraint("CK_Unidades_Horas", "[Horas] > 0");
                    table.CheckConstraint("CK_Unidades_Numero", "[Numero] > 0");
                    table.ForeignKey(
                        name: "FK_UnidadesCurriculares_Asignaturas_AsignaturaId",
                        column: x => x.AsignaturaId,
                        principalTable: "Asignaturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Contenidos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnidadCurricularId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    Tema = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contenidos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contenidos_UnidadesCurriculares_UnidadCurricularId",
                        column: x => x.UnidadCurricularId,
                        principalTable: "UnidadesCurriculares",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Contenidos_UnidadCurricularId_Orden",
                table: "Contenidos",
                columns: new[] { "UnidadCurricularId", "Orden" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnidadesCurriculares_AsignaturaId_Numero",
                table: "UnidadesCurriculares",
                columns: new[] { "AsignaturaId", "Numero" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Contenidos");

            migrationBuilder.DropTable(
                name: "UnidadesCurriculares");
        }
    }
}
