using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniMonitor.Server.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDatasets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DatasetDevices_Datasets_DatasetId",
                table: "DatasetDevices");

            migrationBuilder.DropForeignKey(
                name: "FK_GrupoDatasets_Datasets_id_dataset",
                table: "GrupoDatasets");

            migrationBuilder.DropTable(
                name: "Datasets");

            migrationBuilder.CreateTable(
                name: "DatasetsIM",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Is_Dataset = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Username = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Id_Source = table.Column<int>(type: "int", nullable: true),
                    Id_Group = table.Column<int>(type: "int", nullable: true),
                    SensorName = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatasetsIM", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_DatasetDevices_DatasetsIM_DatasetId",
                table: "DatasetDevices",
                column: "DatasetId",
                principalTable: "DatasetsIM",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GrupoDatasets_DatasetsIM_id_dataset",
                table: "GrupoDatasets",
                column: "id_dataset",
                principalTable: "DatasetsIM",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DatasetDevices_DatasetsIM_DatasetId",
                table: "DatasetDevices");

            migrationBuilder.DropForeignKey(
                name: "FK_GrupoDatasets_DatasetsIM_id_dataset",
                table: "GrupoDatasets");

            migrationBuilder.DropTable(
                name: "DatasetsIM");

            migrationBuilder.CreateTable(
                name: "Datasets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Id_Group = table.Column<int>(type: "int", nullable: true),
                    Id_Source = table.Column<int>(type: "int", nullable: true),
                    Is_Dataset = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SensorName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Username = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Datasets", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_DatasetDevices_Datasets_DatasetId",
                table: "DatasetDevices",
                column: "DatasetId",
                principalTable: "Datasets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GrupoDatasets_Datasets_id_dataset",
                table: "GrupoDatasets",
                column: "id_dataset",
                principalTable: "Datasets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
