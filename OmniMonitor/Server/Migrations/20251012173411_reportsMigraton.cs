using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniMonitor.Server.Migrations
{
    /// <inheritdoc />
    public partial class reportsMigraton : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JoinOperands",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModuleType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DatasetId = table.Column<int>(type: "int", nullable: false),
                    EntityName = table.Column<int>(type: "int", maxLength: 100, nullable: false),
                    JoinPropertyName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JoinOperands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Reports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Username = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CrossModuleJoins",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Username = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    JoinType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LeftOperandId = table.Column<int>(type: "int", nullable: false),
                    RightOperandId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrossModuleJoins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrossModuleJoins_JoinOperands_LeftOperandId",
                        column: x => x.LeftOperandId,
                        principalTable: "JoinOperands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CrossModuleJoins_JoinOperands_RightOperandId",
                        column: x => x.RightOperandId,
                        principalTable: "JoinOperands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReportJoins",
                columns: table => new
                {
                    ReportId = table.Column<int>(type: "int", nullable: false),
                    CrossModuleJoinId = table.Column<int>(type: "int", nullable: false),
                    ExecutionOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportJoins", x => new { x.ReportId, x.CrossModuleJoinId });
                    table.ForeignKey(
                        name: "FK_ReportJoins_CrossModuleJoins_CrossModuleJoinId",
                        column: x => x.CrossModuleJoinId,
                        principalTable: "CrossModuleJoins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReportJoins_Reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "Reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CrossModuleJoins_LeftOperandId",
                table: "CrossModuleJoins",
                column: "LeftOperandId");

            migrationBuilder.CreateIndex(
                name: "IX_CrossModuleJoins_RightOperandId",
                table: "CrossModuleJoins",
                column: "RightOperandId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportJoins_CrossModuleJoinId",
                table: "ReportJoins",
                column: "CrossModuleJoinId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReportJoins");

            migrationBuilder.DropTable(
                name: "CrossModuleJoins");

            migrationBuilder.DropTable(
                name: "Reports");

            migrationBuilder.DropTable(
                name: "JoinOperands");
        }
    }
}
