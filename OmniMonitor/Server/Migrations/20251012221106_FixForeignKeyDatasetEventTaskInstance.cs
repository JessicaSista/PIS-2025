using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniMonitor.Server.Migrations
{
    /// <inheritdoc />
    public partial class FixForeignKeyDatasetEventTaskInstance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GrupoDataset_Datasets_id_dataset",
                table: "GrupoDataset");

            migrationBuilder.DropForeignKey(
                name: "FK_GrupoDataset_Visualizaciones_Id_visualizacion",
                table: "GrupoDataset");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GrupoDataset",
                table: "GrupoDataset");

            migrationBuilder.RenameTable(
                name: "GrupoDataset",
                newName: "GrupoDatasets");

            migrationBuilder.RenameIndex(
                name: "IX_GrupoDataset_Id_visualizacion",
                table: "GrupoDatasets",
                newName: "IX_GrupoDatasets_Id_visualizacion");

            migrationBuilder.RenameIndex(
                name: "IX_GrupoDataset_id_dataset",
                table: "GrupoDatasets",
                newName: "IX_GrupoDatasets_id_dataset");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GrupoDatasets",
                table: "GrupoDatasets",
                column: "id");

            migrationBuilder.CreateTable(
                name: "DatasetAM",
                columns: table => new
                {
                    Id_Dataset = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Is_Dataset = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
                    Username = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Type_Dataset = table.Column<int>(type: "int", nullable: false),
                    Id_Event_Task = table.Column<int>(type: "int", nullable: true),
                    Id_Asset_Type = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatasetAM", x => x.Id_Dataset);
                });

            migrationBuilder.CreateTable(
                name: "DatasetAsset",
                columns: table => new
                {
                    Grupo_Asset = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Id_Asset = table.Column<int>(type: "int", nullable: false),
                    DatasetAMId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatasetAsset", x => x.Grupo_Asset);
                    table.ForeignKey(
                        name: "FK_DatasetAsset_DatasetAM_DatasetAMId",
                        column: x => x.DatasetAMId,
                        principalTable: "DatasetAM",
                        principalColumn: "Id_Dataset",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DatasetEventTaskInstance",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DatasetAMId = table.Column<int>(type: "int", nullable: false),
                    Id_Event_Task_Instance = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatasetEventTaskInstance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DatasetEventTaskInstance_DatasetAM_DatasetAMId",
                        column: x => x.DatasetAMId,
                        principalTable: "DatasetAM",
                        principalColumn: "Id_Dataset",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DatasetStock",
                columns: table => new
                {
                    Grupo_Stock = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Id_Stock = table.Column<int>(type: "int", nullable: false),
                    DatasetEventTaskInstanceId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatasetStock", x => x.Grupo_Stock);
                    table.ForeignKey(
                        name: "FK_DatasetStock_DatasetEventTaskInstance_DatasetEventTaskInstanceId",
                        column: x => x.DatasetEventTaskInstanceId,
                        principalTable: "DatasetEventTaskInstance",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DatasetAsset_DatasetAMId",
                table: "DatasetAsset",
                column: "DatasetAMId");

            migrationBuilder.CreateIndex(
                name: "IX_DatasetEventTaskInstance_DatasetAMId",
                table: "DatasetEventTaskInstance",
                column: "DatasetAMId");

            migrationBuilder.CreateIndex(
                name: "IX_DatasetStock_DatasetEventTaskInstanceId",
                table: "DatasetStock",
                column: "DatasetEventTaskInstanceId");

            migrationBuilder.AddForeignKey(
                name: "FK_GrupoDatasets_Datasets_id_dataset",
                table: "GrupoDatasets",
                column: "id_dataset",
                principalTable: "Datasets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GrupoDatasets_Visualizaciones_Id_visualizacion",
                table: "GrupoDatasets",
                column: "Id_visualizacion",
                principalTable: "Visualizaciones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GrupoDatasets_Datasets_id_dataset",
                table: "GrupoDatasets");

            migrationBuilder.DropForeignKey(
                name: "FK_GrupoDatasets_Visualizaciones_Id_visualizacion",
                table: "GrupoDatasets");

            migrationBuilder.DropTable(
                name: "DatasetAsset");

            migrationBuilder.DropTable(
                name: "DatasetStock");

            migrationBuilder.DropTable(
                name: "DatasetEventTaskInstance");

            migrationBuilder.DropTable(
                name: "DatasetAM");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GrupoDatasets",
                table: "GrupoDatasets");

            migrationBuilder.RenameTable(
                name: "GrupoDatasets",
                newName: "GrupoDataset");

            migrationBuilder.RenameIndex(
                name: "IX_GrupoDatasets_Id_visualizacion",
                table: "GrupoDataset",
                newName: "IX_GrupoDataset_Id_visualizacion");

            migrationBuilder.RenameIndex(
                name: "IX_GrupoDatasets_id_dataset",
                table: "GrupoDataset",
                newName: "IX_GrupoDataset_id_dataset");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GrupoDataset",
                table: "GrupoDataset",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_GrupoDataset_Datasets_id_dataset",
                table: "GrupoDataset",
                column: "id_dataset",
                principalTable: "Datasets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GrupoDataset_Visualizaciones_Id_visualizacion",
                table: "GrupoDataset",
                column: "Id_visualizacion",
                principalTable: "Visualizaciones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
