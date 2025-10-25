using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniMonitor.Server.Migrations
{
    /// <inheritdoc />
    public partial class Datasets2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DatasetAM_Datasets_DatasetsId",
                table: "DatasetAM");

            migrationBuilder.DropForeignKey(
                name: "FK_DatasetsEM_Datasets_DatasetsId",
                table: "DatasetsEM");

            migrationBuilder.DropForeignKey(
                name: "FK_DatasetsIM_Datasets_DatasetsId",
                table: "DatasetsIM");

            migrationBuilder.DropForeignKey(
                name: "FK_DatasetsUM_Datasets_DatasetsId",
                table: "DatasetsUM");

            migrationBuilder.DropIndex(
                name: "IX_DatasetsUM_DatasetsId",
                table: "DatasetsUM");

            migrationBuilder.DropIndex(
                name: "IX_DatasetsIM_DatasetsId",
                table: "DatasetsIM");

            migrationBuilder.DropIndex(
                name: "IX_DatasetsEM_DatasetsId",
                table: "DatasetsEM");

            migrationBuilder.DropIndex(
                name: "IX_DatasetAM_DatasetsId",
                table: "DatasetAM");

            migrationBuilder.DropColumn(
                name: "DatasetsId",
                table: "DatasetsUM");

            migrationBuilder.DropColumn(
                name: "DatasetsId",
                table: "DatasetsIM");

            migrationBuilder.DropColumn(
                name: "DatasetsId",
                table: "DatasetsEM");

            migrationBuilder.DropColumn(
                name: "DatasetsId",
                table: "DatasetAM");

            migrationBuilder.AddColumn<int>(
                name: "DatasetId",
                table: "DatasetsUM",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DatasetId",
                table: "DatasetsIM",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DatasetId",
                table: "DatasetsEM",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DatasetId",
                table: "DatasetAM",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_DatasetsUM_DatasetId",
                table: "DatasetsUM",
                column: "DatasetId");

            migrationBuilder.CreateIndex(
                name: "IX_DatasetsIM_DatasetId",
                table: "DatasetsIM",
                column: "DatasetId");

            migrationBuilder.CreateIndex(
                name: "IX_DatasetsEM_DatasetId",
                table: "DatasetsEM",
                column: "DatasetId");

            migrationBuilder.CreateIndex(
                name: "IX_DatasetAM_DatasetId",
                table: "DatasetAM",
                column: "DatasetId");

            migrationBuilder.AddForeignKey(
                name: "FK_DatasetAM_Datasets_DatasetId",
                table: "DatasetAM",
                column: "DatasetId",
                principalTable: "Datasets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DatasetsEM_Datasets_DatasetId",
                table: "DatasetsEM",
                column: "DatasetId",
                principalTable: "Datasets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DatasetsIM_Datasets_DatasetId",
                table: "DatasetsIM",
                column: "DatasetId",
                principalTable: "Datasets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DatasetsUM_Datasets_DatasetId",
                table: "DatasetsUM",
                column: "DatasetId",
                principalTable: "Datasets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DatasetAM_Datasets_DatasetId",
                table: "DatasetAM");

            migrationBuilder.DropForeignKey(
                name: "FK_DatasetsEM_Datasets_DatasetId",
                table: "DatasetsEM");

            migrationBuilder.DropForeignKey(
                name: "FK_DatasetsIM_Datasets_DatasetId",
                table: "DatasetsIM");

            migrationBuilder.DropForeignKey(
                name: "FK_DatasetsUM_Datasets_DatasetId",
                table: "DatasetsUM");

            migrationBuilder.DropIndex(
                name: "IX_DatasetsUM_DatasetId",
                table: "DatasetsUM");

            migrationBuilder.DropIndex(
                name: "IX_DatasetsIM_DatasetId",
                table: "DatasetsIM");

            migrationBuilder.DropIndex(
                name: "IX_DatasetsEM_DatasetId",
                table: "DatasetsEM");

            migrationBuilder.DropIndex(
                name: "IX_DatasetAM_DatasetId",
                table: "DatasetAM");

            migrationBuilder.DropColumn(
                name: "DatasetId",
                table: "DatasetsUM");

            migrationBuilder.DropColumn(
                name: "DatasetId",
                table: "DatasetsIM");

            migrationBuilder.DropColumn(
                name: "DatasetId",
                table: "DatasetsEM");

            migrationBuilder.DropColumn(
                name: "DatasetId",
                table: "DatasetAM");

            migrationBuilder.AddColumn<int>(
                name: "DatasetsId",
                table: "DatasetsUM",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DatasetsId",
                table: "DatasetsIM",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DatasetsId",
                table: "DatasetsEM",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DatasetsId",
                table: "DatasetAM",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DatasetsUM_DatasetsId",
                table: "DatasetsUM",
                column: "DatasetsId");

            migrationBuilder.CreateIndex(
                name: "IX_DatasetsIM_DatasetsId",
                table: "DatasetsIM",
                column: "DatasetsId");

            migrationBuilder.CreateIndex(
                name: "IX_DatasetsEM_DatasetsId",
                table: "DatasetsEM",
                column: "DatasetsId");

            migrationBuilder.CreateIndex(
                name: "IX_DatasetAM_DatasetsId",
                table: "DatasetAM",
                column: "DatasetsId");

            migrationBuilder.AddForeignKey(
                name: "FK_DatasetAM_Datasets_DatasetsId",
                table: "DatasetAM",
                column: "DatasetsId",
                principalTable: "Datasets",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DatasetsEM_Datasets_DatasetsId",
                table: "DatasetsEM",
                column: "DatasetsId",
                principalTable: "Datasets",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DatasetsIM_Datasets_DatasetsId",
                table: "DatasetsIM",
                column: "DatasetsId",
                principalTable: "Datasets",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DatasetsUM_Datasets_DatasetsId",
                table: "DatasetsUM",
                column: "DatasetsId",
                principalTable: "Datasets",
                principalColumn: "Id");
        }
    }
}
