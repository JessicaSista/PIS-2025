using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniMonitor.Server.Migrations
{
    /// <inheritdoc />
    public partial class DatasetsAM : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GrupoDatasets_Datasets_id_dataset",
                table: "GrupoDatasets");

            migrationBuilder.DropForeignKey(
                name: "FK_GrupoDatasets_Visualizaciones_Id_visualizacion",
                table: "GrupoDatasets");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
    }
}
