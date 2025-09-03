using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace OmniMonitor.Server.Migrations
{
    /// <inheritdoc />
    public partial class SeedSensorClimaxData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "SensorClimaxs",
                columns: new[] { "Id", "Co2", "Humedad", "HumedadDelSuelo", "NivelDeRuido", "NivleDeBrillo", "Potencia", "Temperatura", "TemperaturaDelSuelo" },
                values: new object[,]
                {
                    { 1, 400, 55, 20, 50, 300, 150, 24, 18 },
                    { 2, 420, 60, 22, 55, 320, 160, 25, 19 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "SensorClimaxs",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "SensorClimaxs",
                keyColumn: "Id",
                keyValue: 2);
        }
    }
}
