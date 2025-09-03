using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniMonitor.Server.Migrations
{
    /// <inheritdoc />
    public partial class NewMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Raperos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Edad = table.Column<int>(type: "int", nullable: false),
                    Millones = table.Column<int>(type: "int", nullable: false),
                    NombreRapero = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Raperos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SensorClimaxs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Temperatura = table.Column<int>(type: "int", nullable: false),
                    Humedad = table.Column<int>(type: "int", nullable: false),
                    Co2 = table.Column<int>(type: "int", nullable: false),
                    Potencia = table.Column<int>(type: "int", nullable: false),
                    NivleDeBrillo = table.Column<int>(type: "int", nullable: false),
                    NivelDeRuido = table.Column<int>(type: "int", nullable: false),
                    HumedadDelSuelo = table.Column<int>(type: "int", nullable: false),
                    TemperaturaDelSuelo = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SensorClimaxs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "Raperos");

            migrationBuilder.DropTable(
                name: "SensorClimaxs");
        }
    }
}
