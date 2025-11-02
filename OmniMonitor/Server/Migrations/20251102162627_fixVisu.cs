using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniMonitor.Server.Migrations
{
    /// <inheritdoc />
    public partial class fixVisu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GrupoDatasets_DatasetsIM_id_dataset",
                table: "GrupoDatasets");

            migrationBuilder.AddForeignKey(
                name: "FK_GrupoDatasets_Datasets_id_dataset",
                table: "GrupoDatasets",
                column: "id_dataset",
                principalTable: "Datasets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GrupoDatasets_Datasets_id_dataset",
                table: "GrupoDatasets");

            migrationBuilder.AddForeignKey(
                name: "FK_GrupoDatasets_DatasetsIM_id_dataset",
                table: "GrupoDatasets",
                column: "id_dataset",
                principalTable: "DatasetsIM",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
