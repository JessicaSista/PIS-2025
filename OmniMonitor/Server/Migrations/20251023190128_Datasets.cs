using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniMonitor.Server.Migrations
{
    /// <inheritdoc />
    public partial class Datasets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EventName",
                table: "DatasetsUM");

            migrationBuilder.DropColumn(
                name: "AlertState",
                table: "DatasetsEM");

            migrationBuilder.DropColumn(
                name: "CategoryState",
                table: "DatasetsEM");

            migrationBuilder.DropColumn(
                name: "EventState",
                table: "DatasetsEM");

            migrationBuilder.DropColumn(
                name: "ExtensionState",
                table: "DatasetsEM");

            migrationBuilder.DropColumn(
                name: "Id_Alert",
                table: "DatasetsEM");

            migrationBuilder.DropColumn(
                name: "Id_Category",
                table: "DatasetsEM");

            migrationBuilder.DropColumn(
                name: "Id_Event",
                table: "DatasetsEM");

            migrationBuilder.RenameColumn(
                name: "Id_News",
                table: "DatasetsUM",
                newName: "DatasetsId");

            migrationBuilder.RenameColumn(
                name: "Id_Extension",
                table: "DatasetsEM",
                newName: "DatasetsId");

            migrationBuilder.AddColumn<int>(
                name: "DatasetsId",
                table: "DatasetsIM",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DatasetsId",
                table: "DatasetAM",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Datasets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NameDataset = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TipoDataset = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Datasets", x => x.Id);
                });

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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

            migrationBuilder.DropTable(
                name: "Datasets");

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
                table: "DatasetsIM");

            migrationBuilder.DropColumn(
                name: "DatasetsId",
                table: "DatasetAM");

            migrationBuilder.RenameColumn(
                name: "DatasetsId",
                table: "DatasetsUM",
                newName: "Id_News");

            migrationBuilder.RenameColumn(
                name: "DatasetsId",
                table: "DatasetsEM",
                newName: "Id_Extension");

            migrationBuilder.AddColumn<string>(
                name: "EventName",
                table: "DatasetsUM",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AlertState",
                table: "DatasetsEM",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CategoryState",
                table: "DatasetsEM",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EventState",
                table: "DatasetsEM",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExtensionState",
                table: "DatasetsEM",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Id_Alert",
                table: "DatasetsEM",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Id_Category",
                table: "DatasetsEM",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Id_Event",
                table: "DatasetsEM",
                type: "int",
                nullable: true);
        }
    }
}
