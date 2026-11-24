using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalogo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class VersionesMallaHu07 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Asignaturas_CarreraId_CodigoNormalizado",
                table: "Asignaturas");

            migrationBuilder.AddColumn<Guid>(
                name: "MallaId",
                table: "Asignaturas",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Mallas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CarreraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NombreNormalizado = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, collation: "Latin1_General_100_BIN2"),
                    Vigente = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mallas", x => x.Id);
                    table.UniqueConstraint("AK_Mallas_Id_CarreraId", x => new { x.Id, x.CarreraId });
                    table.ForeignKey(
                        name: "FK_Mallas_Carreras_CarreraId",
                        column: x => x.CarreraId,
                        principalTable: "Carreras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Se conservan los UUID y todas las dependencias del catálogo existente.
            migrationBuilder.Sql("""
                INSERT INTO [Mallas] ([Id], [CarreraId], [Nombre], [NombreNormalizado], [Vigente])
                SELECT NEWID(), [Id], N'Inicial migrada', N'INICIAL MIGRADA', 1 FROM [Carreras];
                UPDATE a SET [MallaId] = m.[Id]
                FROM [Asignaturas] a JOIN [Mallas] m ON m.[CarreraId] = a.[CarreraId];
                IF EXISTS (SELECT 1 FROM [Asignaturas] WHERE [MallaId] IS NULL)
                    THROW 51007, 'No se pudo asociar cada asignatura a su malla; no se aplicó HU-07.', 1;
                """);
            migrationBuilder.AlterColumn<Guid>(
                name: "MallaId", table: "Asignaturas", type: "uniqueidentifier", nullable: false,
                oldClrType: typeof(Guid), oldType: "uniqueidentifier", oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Asignaturas_CarreraId",
                table: "Asignaturas",
                column: "CarreraId");

            migrationBuilder.CreateIndex(
                name: "IX_Asignaturas_MallaId_CarreraId",
                table: "Asignaturas",
                columns: new[] { "MallaId", "CarreraId" });

            migrationBuilder.CreateIndex(
                name: "IX_Asignaturas_MallaId_CodigoNormalizado",
                table: "Asignaturas",
                columns: new[] { "MallaId", "CodigoNormalizado" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Mallas_CarreraId",
                table: "Mallas",
                column: "CarreraId",
                unique: true,
                filter: "[Vigente] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Mallas_CarreraId_NombreNormalizado",
                table: "Mallas",
                columns: new[] { "CarreraId", "NombreNormalizado" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Asignaturas_Mallas_MallaId_CarreraId",
                table: "Asignaturas",
                columns: new[] { "MallaId", "CarreraId" },
                principalTable: "Mallas",
                principalColumns: new[] { "Id", "CarreraId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // El esquema anterior no puede representar versiones creadas después de migrar.
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [Mallas] WHERE [NombreNormalizado] <> N'INICIAL MIGRADA' OR [Vigente] = 0)
                    OR EXISTS (SELECT 1 FROM [Mallas] GROUP BY [CarreraId] HAVING COUNT(*) > 1)
                    THROW 51008, 'La reversión perdería versiones de malla. Requiere un plan de restauración revisado.', 1;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_Asignaturas_Mallas_MallaId_CarreraId",
                table: "Asignaturas");

            migrationBuilder.DropTable(
                name: "Mallas");

            migrationBuilder.DropIndex(
                name: "IX_Asignaturas_CarreraId",
                table: "Asignaturas");

            migrationBuilder.DropIndex(
                name: "IX_Asignaturas_MallaId_CarreraId",
                table: "Asignaturas");

            migrationBuilder.DropIndex(
                name: "IX_Asignaturas_MallaId_CodigoNormalizado",
                table: "Asignaturas");

            migrationBuilder.DropColumn(
                name: "MallaId",
                table: "Asignaturas");

            migrationBuilder.CreateIndex(
                name: "IX_Asignaturas_CarreraId_CodigoNormalizado",
                table: "Asignaturas",
                columns: new[] { "CarreraId", "CodigoNormalizado" },
                unique: true);
        }
    }
}
