using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalogo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NormativasHu38 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventosNormativa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NormativaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CodigoNormativa = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Fecha = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Usuario = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Detalle = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventosNormativa", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Normativas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CodigoNormalizado = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Emisor = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FechaEmision = table.Column<DateOnly>(type: "date", nullable: false),
                    VigenteDesde = table.Column<DateOnly>(type: "date", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReformaAId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReformadaPorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MotivoDerogacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaDerogacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    NombreArchivo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Sha256 = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    Tamano = table.Column<int>(type: "int", nullable: false),
                    Contenido = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    FechaRegistro = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RegistradaPor = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Normativas", x => x.Id);
                    table.CheckConstraint("CK_Normativas_Tamano", "[Tamano] > 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventosNormativa_NormativaId",
                table: "EventosNormativa",
                column: "NormativaId");

            migrationBuilder.CreateIndex(
                name: "IX_Normativas_CodigoNormalizado",
                table: "Normativas",
                column: "CodigoNormalizado",
                unique: true);

            // HU-38: el historial es de solo inserción, también fuera de la aplicación.
            if (ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer")
                migrationBuilder.Sql(@"CREATE TRIGGER TR_EventosNormativa_SoloInsercion ON EventosNormativa INSTEAD OF UPDATE, DELETE AS
BEGIN
    THROW 51038, 'El historial de normativas no admite modificaciones ni eliminaciones.', 1;
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventosNormativa");

            migrationBuilder.DropTable(
                name: "Normativas");
        }
    }
}
