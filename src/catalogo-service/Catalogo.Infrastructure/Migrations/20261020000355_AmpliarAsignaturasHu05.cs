using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalogo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AmpliarAsignaturasHu05 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // HU-04 solo permitía referencias sin datos académicos. No inventar valores
            // ni borrar registros para completar los nuevos campos obligatorios.
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [Asignaturas])
                    THROW 51000, 'HU-05: existen referencias de asignaturas sin datos academicos. Acordar una migracion de datos antes de continuar; no se eliminaron registros.', 1;
                """);

            migrationBuilder.DropIndex(
                name: "IX_Asignaturas_CarreraId",
                table: "Asignaturas");

            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "Asignaturas",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "CodigoNormalizado",
                table: "Asignaturas",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                collation: "Latin1_General_100_BIN2");

            migrationBuilder.AddColumn<decimal>(
                name: "Creditos",
                table: "Asignaturas",
                type: "decimal(9,2)",
                precision: 9,
                scale: 2,
                nullable: false);

            migrationBuilder.AddColumn<int>(
                name: "Horas",
                table: "Asignaturas",
                type: "int",
                nullable: false);

            migrationBuilder.AddColumn<int>(
                name: "Nivel",
                table: "Asignaturas",
                type: "int",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "Nombre",
                table: "Asignaturas",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "IX_Asignaturas_CarreraId_CodigoNormalizado",
                table: "Asignaturas",
                columns: new[] { "CarreraId", "CodigoNormalizado" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Asignaturas_CreditosPositivos",
                table: "Asignaturas",
                sql: "[Creditos] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Asignaturas_HorasPositivas",
                table: "Asignaturas",
                sql: "[Horas] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Asignaturas_NivelPositivo",
                table: "Asignaturas",
                sql: "[Nivel] > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Asignaturas_CarreraId_CodigoNormalizado",
                table: "Asignaturas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Asignaturas_CreditosPositivos",
                table: "Asignaturas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Asignaturas_HorasPositivas",
                table: "Asignaturas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Asignaturas_NivelPositivo",
                table: "Asignaturas");

            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "Asignaturas");

            migrationBuilder.DropColumn(
                name: "CodigoNormalizado",
                table: "Asignaturas");

            migrationBuilder.DropColumn(
                name: "Creditos",
                table: "Asignaturas");

            migrationBuilder.DropColumn(
                name: "Horas",
                table: "Asignaturas");

            migrationBuilder.DropColumn(
                name: "Nivel",
                table: "Asignaturas");

            migrationBuilder.DropColumn(
                name: "Nombre",
                table: "Asignaturas");

            migrationBuilder.CreateIndex(
                name: "IX_Asignaturas_CarreraId",
                table: "Asignaturas",
                column: "CarreraId");
        }
    }
}
