using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniMonitor.Server.Migrations
{
    /// <inheritdoc />
    public partial class new_cmmm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Kpi",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "Kpi");
        }
    }
}
