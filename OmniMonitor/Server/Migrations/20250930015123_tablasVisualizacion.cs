using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniMonitor.Server.Migrations
{
    /// <inheritdoc />
    public partial class tablasVisualizacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "visualizacions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Date_from = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Date_to = table.Column<DateTime>(type: "datetime2", nullable: false),
                    JSON_design = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_visualizacions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "grupoDatasets",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Id_visualizacion = table.Column<int>(type: "int", nullable: false),
                    id_dataset = table.Column<int>(type: "int", nullable: false),
                    JSON_design = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_grupoDatasets", x => x.id);
                    table.ForeignKey(
                        name: "FK_grupoDatasets_Datasets_id_dataset",
                        column: x => x.id_dataset,
                        principalTable: "Datasets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_grupoDatasets_visualizacions_Id_visualizacion",
                        column: x => x.Id_visualizacion,
                        principalTable: "visualizacions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_grupoDatasets_id_dataset",
                table: "grupoDatasets",
                column: "id_dataset");

            migrationBuilder.CreateIndex(
                name: "IX_grupoDatasets_Id_visualizacion",
                table: "grupoDatasets",
                column: "Id_visualizacion");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "grupoDatasets");

            migrationBuilder.DropTable(
                name: "visualizacions");
        }
    }
}
