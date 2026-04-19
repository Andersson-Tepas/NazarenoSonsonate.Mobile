using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NazarenoSonsonate.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUltimaUbicacionProcesion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UltimasUbicacionesProcesion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecorridoId = table.Column<int>(type: "int", nullable: false),
                    TipoUnidad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Latitud = table.Column<double>(type: "float", nullable: false),
                    Longitud = table.Column<double>(type: "float", nullable: false),
                    FechaHora = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GrupoActual = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Mensaje = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UltimasUbicacionesProcesion", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UltimasUbicacionesProcesion_RecorridoId_TipoUnidad",
                table: "UltimasUbicacionesProcesion",
                columns: new[] { "RecorridoId", "TipoUnidad" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UltimasUbicacionesProcesion");
        }
    }
}
