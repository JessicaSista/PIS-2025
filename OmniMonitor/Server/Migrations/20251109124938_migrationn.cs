using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniMonitor.Server.Migrations
{
    /// <inheritdoc />
    public partial class migrationn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DatasetStock_DatasetEventTaskInstance_DatasetEventTaskInstanceId",
                table: "DatasetStock");

            migrationBuilder.AlterColumn<int>(
                name: "DatasetEventTaskInstanceId",
                table: "DatasetStock",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "DatasetAMId",
                table: "DatasetStock",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_DatasetStock_DatasetAMId",
                table: "DatasetStock",
                column: "DatasetAMId");

            migrationBuilder.AddForeignKey(
                name: "FK_DatasetStock_DatasetAM_DatasetAMId",
                table: "DatasetStock",
                column: "DatasetAMId",
                principalTable: "DatasetAM",
                principalColumn: "Id_Dataset",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DatasetStock_DatasetEventTaskInstance_DatasetEventTaskInstanceId",
                table: "DatasetStock",
                column: "DatasetEventTaskInstanceId",
                principalTable: "DatasetEventTaskInstance",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DatasetStock_DatasetAM_DatasetAMId",
                table: "DatasetStock");

            migrationBuilder.DropForeignKey(
                name: "FK_DatasetStock_DatasetEventTaskInstance_DatasetEventTaskInstanceId",
                table: "DatasetStock");

            migrationBuilder.DropIndex(
                name: "IX_DatasetStock_DatasetAMId",
                table: "DatasetStock");

            migrationBuilder.DropColumn(
                name: "DatasetAMId",
                table: "DatasetStock");

            migrationBuilder.AlterColumn<int>(
                name: "DatasetEventTaskInstanceId",
                table: "DatasetStock",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DatasetStock_DatasetEventTaskInstance_DatasetEventTaskInstanceId",
                table: "DatasetStock",
                column: "DatasetEventTaskInstanceId",
                principalTable: "DatasetEventTaskInstance",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
