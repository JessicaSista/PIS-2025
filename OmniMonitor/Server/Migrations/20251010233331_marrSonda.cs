using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace OmniMonitor.Server.Migrations
{
    /// <inheritdoc />
    public partial class marrSonda : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.CreateTable(
                name: "DatasetsEM",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Is_Dataset = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Username = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Id_Alert = table.Column<int>(type: "int", nullable: true),
                    Id_Event = table.Column<int>(type: "int", nullable: true),
                    Id_Extension = table.Column<int>(type: "int", nullable: true),
                    Id_Resource = table.Column<int>(type: "int", nullable: true),
                    AlertState = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EventState = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExtensionState = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResourceState = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatasetsEM", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DatasetsUM",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Is_Dataset = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Username = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Id_Zone = table.Column<int>(type: "int", nullable: true),
                    Id_News = table.Column<int>(type: "int", nullable: true),
                    EventName = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatasetsUM", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DatasetAlerts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Id_alert = table.Column<int>(type: "int", nullable: false),
                    DatasetId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatasetAlerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DatasetAlerts_DatasetsEM_DatasetId",
                        column: x => x.DatasetId,
                        principalTable: "DatasetsEM",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DatasetEventsEM",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Id_event = table.Column<int>(type: "int", nullable: false),
                    DatasetId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatasetEventsEM", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DatasetEventsEM_DatasetsEM_DatasetId",
                        column: x => x.DatasetId,
                        principalTable: "DatasetsEM",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DatasetExtensions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Id_extension = table.Column<int>(type: "int", nullable: false),
                    DatasetId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatasetExtensions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DatasetExtensions_DatasetsEM_DatasetId",
                        column: x => x.DatasetId,
                        principalTable: "DatasetsEM",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateTable(
                name: "DatasetEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Id_event = table.Column<int>(type: "int", nullable: false),
                    DatasetId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatasetEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DatasetEvents_DatasetsUM_DatasetId",
                        column: x => x.DatasetId,
                        principalTable: "DatasetsUM",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DatasetNews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Id_news = table.Column<int>(type: "int", nullable: false),
                    DatasetId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatasetNews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DatasetNews_DatasetsUM_DatasetId",
                        column: x => x.DatasetId,
                        principalTable: "DatasetsUM",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[,]
                {
                    { 11, "Permite ver datasets del módulo UM (Zonas, Eventos, Noticias)", "Ver Datasets UM" },
                    { 12, "Permite crear nuevos datasets del módulo UM", "Crear Datasets UM" },
                    { 13, "Permite eliminar datasets del módulo UM", "Eliminar Datasets UM" },
                    { 14, "Permite ver datasets del módulo EM (Alertas, Eventos, Extensiones, Recursos)", "Ver Datasets EM" },
                    { 15, "Permite crear nuevos datasets del módulo EM", "Crear Datasets EM" },
                    { 16, "Permite eliminar datasets del módulo EM", "Eliminar Datasets EM" }
                });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "Id", "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { 21, 1, 2 },
                    { 22, 5, 2 },
                    { 23, 7, 2 },
                    { 24, 9, 2 },
                    { 15, 11, 1 },
                    { 16, 12, 1 },
                    { 17, 13, 1 },
                    { 18, 14, 1 },
                    { 19, 15, 1 },
                    { 20, 16, 1 },
                    { 25, 11, 2 },
                    { 26, 14, 2 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_DatasetAlerts_DatasetId",
                table: "DatasetAlerts",
                column: "DatasetId");

            migrationBuilder.CreateIndex(
                name: "IX_DatasetEvents_DatasetId",
                table: "DatasetEvents",
                column: "DatasetId");

            migrationBuilder.CreateIndex(
                name: "IX_DatasetEventsEM_DatasetId",
                table: "DatasetEventsEM",
                column: "DatasetId");

            migrationBuilder.CreateIndex(
                name: "IX_DatasetExtensions_DatasetId",
                table: "DatasetExtensions",
                column: "DatasetId");

            migrationBuilder.CreateIndex(
                name: "IX_DatasetNews_DatasetId",
                table: "DatasetNews",
                column: "DatasetId");

            migrationBuilder.CreateIndex(
                name: "IX_DatasetResources_DatasetId",
                table: "DatasetResources",
                column: "DatasetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DatasetAlerts");

            migrationBuilder.DropTable(
                name: "DatasetEvents");

            migrationBuilder.DropTable(
                name: "DatasetEventsEM");

            migrationBuilder.DropTable(
                name: "DatasetExtensions");

            migrationBuilder.DropTable(
                name: "DatasetNews");

            migrationBuilder.DropTable(
                name: "DatasetResources");

            migrationBuilder.DropTable(
                name: "DatasetsUM");

            migrationBuilder.DropTable(
                name: "DatasetsEM");

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "Id", "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { 11, 1, 2 },
                    { 12, 5, 2 },
                    { 13, 7, 2 },
                    { 14, 9, 2 }
                });
        }
    }
}
