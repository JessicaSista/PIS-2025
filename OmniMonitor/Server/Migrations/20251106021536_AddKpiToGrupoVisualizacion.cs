using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniMonitor.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddKpiToGrupoVisualizacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Dashboards_username_nombre",
                table: "Dashboards");

            migrationBuilder.AlterColumn<int>(
                name: "id_visualizacion",
                table: "GrupoVisualizaciones",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "id_kpi",
                table: "GrupoVisualizaciones",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GrupoVisualizaciones_id_kpi",
                table: "GrupoVisualizaciones",
                column: "id_kpi");

            migrationBuilder.AddForeignKey(
                name: "FK_GrupoVisualizaciones_Kpi_id_kpi",
                table: "GrupoVisualizaciones",
                column: "id_kpi",
                principalTable: "Kpi",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GrupoVisualizaciones_Kpi_id_kpi",
                table: "GrupoVisualizaciones");

            migrationBuilder.DropIndex(
                name: "IX_GrupoVisualizaciones_id_kpi",
                table: "GrupoVisualizaciones");

            migrationBuilder.DropColumn(
                name: "id_kpi",
                table: "GrupoVisualizaciones");

            migrationBuilder.AlterColumn<int>(
                name: "id_visualizacion",
                table: "GrupoVisualizaciones",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dashboards_username_nombre",
                table: "Dashboards",
                columns: new[] { "username", "nombre" },
                unique: true);
        }
    }
}
