using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniMonitor.Server.Migrations
{
    /// <inheritdoc />
    public partial class CorrecionesTablas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_grupoDatasets_Datasets_id_dataset",
                table: "grupoDatasets");

            migrationBuilder.DropForeignKey(
                name: "FK_grupoDatasets_visualizacions_Id_visualizacion",
                table: "grupoDatasets");

            migrationBuilder.DropPrimaryKey(
                name: "PK_grupoDatasets",
                table: "grupoDatasets");

            migrationBuilder.DropPrimaryKey(
                name: "PK_visualizacions",
                table: "visualizacions");

            migrationBuilder.RenameTable(
                name: "grupoDatasets",
                newName: "GrupoDatasets");

            migrationBuilder.RenameTable(
                name: "visualizacions",
                newName: "Visualizaciones");

            migrationBuilder.RenameIndex(
                name: "IX_grupoDatasets_Id_visualizacion",
                table: "GrupoDatasets",
                newName: "IX_GrupoDatasets_Id_visualizacion");

            migrationBuilder.RenameIndex(
                name: "IX_grupoDatasets_id_dataset",
                table: "GrupoDatasets",
                newName: "IX_GrupoDatasets_id_dataset");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GrupoDatasets",
                table: "GrupoDatasets",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Visualizaciones",
                table: "Visualizaciones",
                column: "Id");

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

            migrationBuilder.DropPrimaryKey(
                name: "PK_GrupoDatasets",
                table: "GrupoDatasets");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Visualizaciones",
                table: "Visualizaciones");

            migrationBuilder.RenameTable(
                name: "GrupoDatasets",
                newName: "grupoDatasets");

            migrationBuilder.RenameTable(
                name: "Visualizaciones",
                newName: "visualizacions");

            migrationBuilder.RenameIndex(
                name: "IX_GrupoDatasets_Id_visualizacion",
                table: "grupoDatasets",
                newName: "IX_grupoDatasets_Id_visualizacion");

            migrationBuilder.RenameIndex(
                name: "IX_GrupoDatasets_id_dataset",
                table: "grupoDatasets",
                newName: "IX_grupoDatasets_id_dataset");

            migrationBuilder.AddPrimaryKey(
                name: "PK_grupoDatasets",
                table: "grupoDatasets",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_visualizacions",
                table: "visualizacions",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_grupoDatasets_Datasets_id_dataset",
                table: "grupoDatasets",
                column: "id_dataset",
                principalTable: "Datasets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_grupoDatasets_visualizacions_Id_visualizacion",
                table: "grupoDatasets",
                column: "Id_visualizacion",
                principalTable: "visualizacions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
