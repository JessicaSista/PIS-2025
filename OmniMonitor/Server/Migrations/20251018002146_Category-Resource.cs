using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniMonitor.Server.Migrations
{
    /// <inheritdoc />
    public partial class CategoryResource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DatasetResources");

            migrationBuilder.RenameColumn(
                name: "Id_Resource",
                table: "DatasetsEM",
                newName: "Id_Category");

            migrationBuilder.RenameColumn(
                name: "ResourceState",
                table: "DatasetsEM",
                newName: "CategoryState");

            migrationBuilder.CreateTable(
                name: "DatasetCategory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Id_category = table.Column<int>(type: "int", nullable: false),
                    DatasetId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatasetCategory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DatasetCategory_DatasetsEM_DatasetId",
                        column: x => x.DatasetId,
                        principalTable: "DatasetsEM",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DatasetCategory_DatasetId",
                table: "DatasetCategory",
                column: "DatasetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DatasetCategory");

            migrationBuilder.RenameColumn(
                name: "CategoryState",
                table: "DatasetsEM",
                newName: "ResourceState");

            migrationBuilder.RenameColumn(
                name: "Id_Category",
                table: "DatasetsEM",
                newName: "Id_Resource");

            migrationBuilder.CreateTable(
                name: "DatasetResources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Id_resource = table.Column<int>(type: "int", nullable: false),
                    DatasetId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatasetResources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DatasetResources_DatasetsEM_DatasetId",
                        column: x => x.DatasetId,
                        principalTable: "DatasetsEM",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DatasetResources_DatasetId",
                table: "DatasetResources",
                column: "DatasetId");
        }
    }
}
