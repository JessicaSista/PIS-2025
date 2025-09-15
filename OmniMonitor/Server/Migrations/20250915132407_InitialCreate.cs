using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace OmniMonitor.Server.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FirstName = table.Column<string>(type: "TEXT", nullable: false),
                    LastName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false),
                    Cantidad = table.Column<int>(type: "INTEGER", nullable: false),
                    Precio = table.Column<decimal>(type: "TEXT", nullable: false),
                    Categoria = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SensorClimaxs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Temperatura = table.Column<int>(type: "INTEGER", nullable: false),
                    Humedad = table.Column<int>(type: "INTEGER", nullable: false),
                    Co2 = table.Column<int>(type: "INTEGER", nullable: false),
                    Potencia = table.Column<int>(type: "INTEGER", nullable: false),
                    NivleDeBrillo = table.Column<int>(type: "INTEGER", nullable: false),
                    NivelDeRuido = table.Column<int>(type: "INTEGER", nullable: false),
                    HumedadDelSuelo = table.Column<int>(type: "INTEGER", nullable: false),
                    TemperaturaDelSuelo = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SensorClimaxs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    Password = table.Column<string>(type: "TEXT", nullable: false),
                    SondaToken = table.Column<string>(type: "TEXT", nullable: true),
                    TokenExpiration = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "SensorClimaxs",
                columns: new[] { "Id", "Co2", "Humedad", "HumedadDelSuelo", "NivelDeRuido", "NivleDeBrillo", "Potencia", "Temperatura", "TemperaturaDelSuelo" },
                values: new object[,]
                {
                    { 1, 400, 55, 20, 50, 300, 150, 24, 18 },
                    { 2, 420, 60, 22, 55, 320, 160, 25, 19 }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Password", "SondaToken", "TokenExpiration", "Username" },
                values: new object[,]
                {
                    { 1, "admin123", null, null, "admin" },
                    { 2, "password123", null, null, "usuario1" },
                    { 3, "test123", null, null, "test" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "SensorClimaxs");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
