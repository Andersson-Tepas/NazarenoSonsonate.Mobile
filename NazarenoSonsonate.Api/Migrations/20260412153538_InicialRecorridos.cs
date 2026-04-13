using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NazarenoSonsonate.Api.Migrations
{
    /// <inheritdoc />
    public partial class InicialRecorridos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Recorridos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    HoraSalida = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    RutaGeoJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recorridos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PuntosRuta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecorridoId = table.Column<int>(type: "int", nullable: false),
                    Latitud = table.Column<double>(type: "float", nullable: false),
                    Longitud = table.Column<double>(type: "float", nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    Referencia = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Grupo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tipo = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PuntosRuta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PuntosRuta_Recorridos_RecorridoId",
                        column: x => x.RecorridoId,
                        principalTable: "Recorridos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PuntosRuta_RecorridoId",
                table: "PuntosRuta",
                column: "RecorridoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PuntosRuta");

            migrationBuilder.DropTable(
                name: "Recorridos");
        }
    }
}
