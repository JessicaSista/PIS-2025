using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniMonitor.Server.Migrations
{
    /// <inheritdoc />
    public partial class ChangeName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_SomeTable",
                table: "SomeTable");

            migrationBuilder.RenameTable(
                name: "SomeTable",
                newName: "SomeTable1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SomeTable1",
                table: "SomeTable1",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_SomeTable1",
                table: "SomeTable1");

            migrationBuilder.RenameTable(
                name: "SomeTable1",
                newName: "SomeTable");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SomeTable",
                table: "SomeTable",
                column: "Id");
        }
    }
}
