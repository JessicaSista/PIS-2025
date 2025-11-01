using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniMonitor.Server.Migrations
{
    /// <inheritdoc />
    public partial class sharedDashboard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SharedLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Slug = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DashboardId = table.Column<int>(type: "int", nullable: false),
                    Visibility = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SharedLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SharedLinks_Dashboards_DashboardId",
                        column: x => x.DashboardId,
                        principalTable: "Dashboards",
                        principalColumn: "id_dashboard",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SharedLinks_DashboardId",
                table: "SharedLinks",
                column: "DashboardId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedLinks_Slug",
                table: "SharedLinks",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SharedLinks");
        }
    }
}
