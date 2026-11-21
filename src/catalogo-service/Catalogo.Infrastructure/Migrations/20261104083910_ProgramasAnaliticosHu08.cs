using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalogo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ProgramasAnaliticosHu08 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProgramasAnaliticos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AsignaturaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NombreArchivo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Sha256 = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    Tamano = table.Column<int>(type: "int", nullable: false),
                    Contenido = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    FechaCarga = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CargadoPor = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgramasAnaliticos", x => x.Id);
                    table.CheckConstraint("CK_ProgramasAnaliticos_Tamano", "[Tamano] > 0");
                    table.ForeignKey(
                        name: "FK_ProgramasAnaliticos_Asignaturas_AsignaturaId",
                        column: x => x.AsignaturaId,
                        principalTable: "Asignaturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProgramasAnaliticos_AsignaturaId",
                table: "ProgramasAnaliticos",
                column: "AsignaturaId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProgramasAnaliticos");
        }
    }
}
