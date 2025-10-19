using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniMonitor.Server.Migrations
{
    /// <inheritdoc />
    public partial class cosa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JSON_config",
                table: "Reports",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "DatasetsOfReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModuleType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    id_dataset = table.Column<int>(type: "int", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatasetsOfReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DatasetReports",
                columns: table => new
                {
                    ReportId = table.Column<int>(type: "int", nullable: false),
                    DatasetsOfReportsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatasetReports", x => new { x.ReportId, x.DatasetsOfReportsId });
                    table.ForeignKey(
                        name: "FK_DatasetReports_DatasetsOfReports_DatasetsOfReportsId",
                        column: x => x.DatasetsOfReportsId,
                        principalTable: "DatasetsOfReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DatasetReports_Reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "Reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DatasetReports_DatasetsOfReportsId",
                table: "DatasetReports",
                column: "DatasetsOfReportsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DatasetReports");

            migrationBuilder.DropTable(
                name: "DatasetsOfReports");

            migrationBuilder.DropColumn(
                name: "JSON_config",
                table: "Reports");
        }
    }
}
