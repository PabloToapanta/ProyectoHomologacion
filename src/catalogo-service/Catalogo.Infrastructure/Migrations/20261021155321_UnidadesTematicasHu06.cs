using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalogo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UnidadesTematicasHu06 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UnidadTematicaId",
                table: "Contenidos",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UnidadesTematicas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnidadCurricularId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnidadesTematicas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnidadesTematicas_UnidadesCurriculares_UnidadCurricularId",
                        column: x => x.UnidadCurricularId,
                        principalTable: "UnidadesCurriculares",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Contenidos_UnidadTematicaId",
                table: "Contenidos",
                column: "UnidadTematicaId");

            migrationBuilder.CreateIndex(
                name: "IX_UnidadesTematicas_UnidadCurricularId_Orden",
                table: "UnidadesTematicas",
                columns: new[] { "UnidadCurricularId", "Orden" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Contenidos_UnidadesTematicas_UnidadTematicaId",
                table: "Contenidos",
                column: "UnidadTematicaId",
                principalTable: "UnidadesTematicas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contenidos_UnidadesTematicas_UnidadTematicaId",
                table: "Contenidos");

            migrationBuilder.DropTable(
                name: "UnidadesTematicas");

            migrationBuilder.DropIndex(
                name: "IX_Contenidos_UnidadTematicaId",
                table: "Contenidos");

            migrationBuilder.DropColumn(
                name: "UnidadTematicaId",
                table: "Contenidos");
        }
    }
}
