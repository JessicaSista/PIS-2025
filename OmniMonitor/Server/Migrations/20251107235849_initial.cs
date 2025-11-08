using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace OmniMonitor.Server.Migrations
{
    /// <inheritdoc />
    public partial class initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SondaTokenIM = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TokenExpirationIM = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SondaTokenAM = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TokenExpirationAM = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SondaTokenUM = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TokenExpirationUM = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SondaTokenEM = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TokenExpirationEM = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SondaTokenOM = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TokenExpirationOM = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Dashboards",
                columns: table => new
                {
                    id_dashboard = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    username = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    grupo_visualizacion = table.Column<int>(type: "int", nullable: true),
                    JSON_diseño = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    fecha_creacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    fecha_modificacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dashboards", x => x.id_dashboard);
                });

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

            migrationBuilder.CreateTable(
                name: "DatasetsOfReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModuleType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    id_dataset = table.Column<int>(type: "int", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatasetsOfReports", x => x.Id);
                });

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
                name: "Kpi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SourceModule = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DatasetId = table.Column<int>(type: "int", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Metric = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Multiplier = table.Column<double>(type: "float", nullable: true),
                    DefaultColor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ColorRanges = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Atributo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExtraInfo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kpi", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Reports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Username = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    JSON_config = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Visualizaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Date_from = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Date_to = table.Column<DateTime>(type: "datetime2", nullable: false),
                    JSON_design = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Visualizaciones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateTable(
                name: "DatasetAM",
                columns: table => new
                {
                    Id_Dataset = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Is_Dataset = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
                    Username = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ContentType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Type_Dataset = table.Column<int>(type: "int", nullable: false),
                    Id_Event_Task = table.Column<int>(type: "int", nullable: true),
                    Id_Asset_Type = table.Column<int>(type: "int", nullable: true),
                    DatasetId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatasetAM", x => x.Id_Dataset);
                    table.ForeignKey(
                        name: "FK_DatasetAM_Datasets_DatasetId",
                        column: x => x.DatasetId,
                        principalTable: "Datasets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                    DatasetId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatasetsEM", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DatasetsEM_Datasets_DatasetId",
                        column: x => x.DatasetId,
                        principalTable: "Datasets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DatasetsIM",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Is_Dataset = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Username = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Id_Source = table.Column<int>(type: "int", nullable: true),
                    Id_Group = table.Column<int>(type: "int", nullable: true),
                    SensorName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DatasetId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatasetsIM", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DatasetsIM_Datasets_DatasetId",
                        column: x => x.DatasetId,
                        principalTable: "Datasets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                    DatasetId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatasetsUM", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DatasetsUM_Datasets_DatasetId",
                        column: x => x.DatasetId,
                        principalTable: "Datasets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                name: "DatasetReports",
                columns: table => new
                {
                    ReportId = table.Column<int>(type: "int", nullable: false),
                    DatasetsOfReportsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatasetReports", x => new { x.ReportId, x.DatasetsOfReportsId });
                    table.ForeignKey(
                        name: "FK_DatasetReports_DatasetsOfReports_DatasetsOfReportsId",
                        column: x => x.DatasetsOfReportsId,
                        principalTable: "DatasetsOfReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DatasetReports_Reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "Reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GrupoDatasets",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Id_visualizacion = table.Column<int>(type: "int", nullable: false),
                    id_dataset = table.Column<int>(type: "int", nullable: false),
                    JSON_design = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GrupoDatasets", x => x.id);
                    table.ForeignKey(
                        name: "FK_GrupoDatasets_Datasets_id_dataset",
                        column: x => x.id_dataset,
                        principalTable: "Datasets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GrupoDatasets_Visualizaciones_Id_visualizacion",
                        column: x => x.Id_visualizacion,
                        principalTable: "Visualizaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GrupoVisualizaciones",
                columns: table => new
                {
                    id_grupo_visualizacion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    grupo_visualizacion = table.Column<int>(type: "int", nullable: false),
                    id_visualizacion = table.Column<int>(type: "int", nullable: true),
                    id_kpi = table.Column<int>(type: "int", nullable: true),
                    tipo_card = table.Column<int>(type: "int", nullable: false),
                    props_configuracion = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    fecha_agregado = table.Column<DateTime>(type: "datetime2", nullable: false),
                    orden = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GrupoVisualizaciones", x => x.id_grupo_visualizacion);
                    table.ForeignKey(
                        name: "FK_GrupoVisualizaciones_Dashboards_grupo_visualizacion",
                        column: x => x.grupo_visualizacion,
                        principalTable: "Dashboards",
                        principalColumn: "id_dashboard",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GrupoVisualizaciones_Kpi_id_kpi",
                        column: x => x.id_kpi,
                        principalTable: "Kpi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GrupoVisualizaciones_Visualizaciones_id_visualizacion",
                        column: x => x.id_visualizacion,
                        principalTable: "Visualizaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DatasetAsset",
                columns: table => new
                {
                    Grupo_Asset = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Id_Asset = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DatasetAMId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatasetAsset", x => x.Grupo_Asset);
                    table.ForeignKey(
                        name: "FK_DatasetAsset_DatasetAM_DatasetAMId",
                        column: x => x.DatasetAMId,
                        principalTable: "DatasetAM",
                        principalColumn: "Id_Dataset",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DatasetEventTaskInstance",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DatasetAMId = table.Column<int>(type: "int", nullable: false),
                    Id_Event_Task_Instance = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatasetEventTaskInstance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DatasetEventTaskInstance_DatasetAM_DatasetAMId",
                        column: x => x.DatasetAMId,
                        principalTable: "DatasetAM",
                        principalColumn: "Id_Dataset",
                        onDelete: ReferentialAction.Cascade);
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
                name: "DatasetCategory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Id_Category = table.Column<int>(type: "int", nullable: false),
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
                name: "DatasetDevices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Id_device = table.Column<int>(type: "int", nullable: false),
                    DatasetId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatasetDevices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DatasetDevices_DatasetsIM_DatasetId",
                        column: x => x.DatasetId,
                        principalTable: "DatasetsIM",
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

            migrationBuilder.CreateTable(
                name: "DatasetStock",
                columns: table => new
                {
                    Grupo_Stock = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Id_Stock = table.Column<int>(type: "int", nullable: false),
                    DatasetEventTaskInstanceId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatasetStock", x => x.Grupo_Stock);
                    table.ForeignKey(
                        name: "FK_DatasetStock_DatasetEventTaskInstance_DatasetEventTaskInstanceId",
                        column: x => x.DatasetEventTaskInstanceId,
                        principalTable: "DatasetEventTaskInstance",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[,]
                {
                    { 1, "Permite ver la lista de usuarios", "Ver Usuarios" },
                    { 2, "Permite crear nuevos usuarios", "Crear Usuarios" },
                    { 3, "Permite editar usuarios existentes", "Editar Usuarios" },
                    { 4, "Permite eliminar usuarios", "Eliminar Usuarios" },
                    { 5, "Permite ver datos de sensores", "Ver Sensores" },
                    { 6, "Permite configurar sensores", "Configurar Sensores" },
                    { 7, "Permite ver la lista de empleados", "Ver Empleados" },
                    { 8, "Permite crear, editar y eliminar empleados", "Gestionar Empleados" },
                    { 9, "Permite ver la lista de items", "Ver Items" },
                    { 10, "Permite crear, editar y eliminar items", "Gestionar Items" },
                    { 11, "Permite ver datasets del módulo UM (Zonas, Eventos, Noticias)", "Ver Datasets UM" },
                    { 12, "Permite crear nuevos datasets del módulo UM", "Crear Datasets UM" },
                    { 13, "Permite eliminar datasets del módulo UM", "Eliminar Datasets UM" },
                    { 14, "Permite ver datasets del módulo EM (Alertas, Eventos, Extensiones, Recursos)", "Ver Datasets EM" },
                    { 15, "Permite crear nuevos datasets del módulo EM", "Crear Datasets EM" },
                    { 16, "Permite eliminar datasets del módulo EM", "Eliminar Datasets EM" },
                    { 17, "Permite ver dashboards personalizables", "Ver Dashboards" },
                    { 18, "Permite crear nuevos dashboards personalizables", "Crear Dashboards" },
                    { 19, "Permite editar dashboards existentes", "Editar Dashboards" },
                    { 20, "Permite eliminar dashboards", "Eliminar Dashboards" }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[,]
                {
                    { 1, "Rol con acceso completo al sistema", "Administrador" },
                    { 2, "Rol con acceso limitado de solo lectura", "Visitante" }
                });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "Id", "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { 1, 1, 1 },
                    { 2, 2, 1 },
                    { 3, 3, 1 },
                    { 4, 4, 1 },
                    { 5, 5, 1 },
                    { 6, 6, 1 },
                    { 7, 7, 1 },
                    { 8, 8, 1 },
                    { 9, 9, 1 },
                    { 10, 10, 1 },
                    { 15, 11, 1 },
                    { 16, 12, 1 },
                    { 17, 13, 1 },
                    { 18, 14, 1 },
                    { 19, 15, 1 },
                    { 20, 16, 1 },
                    { 21, 1, 2 },
                    { 22, 5, 2 },
                    { 23, 7, 2 },
                    { 24, 9, 2 },
                    { 25, 11, 2 },
                    { 26, 14, 2 },
                    { 27, 17, 1 },
                    { 28, 18, 1 },
                    { 29, 19, 1 },
                    { 30, 20, 1 },
                    { 31, 17, 2 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CrossModuleJoins_LeftOperandId",
                table: "CrossModuleJoins",
                column: "LeftOperandId");

            migrationBuilder.CreateIndex(
                name: "IX_CrossModuleJoins_RightOperandId",
                table: "CrossModuleJoins",
                column: "RightOperandId");

            migrationBuilder.CreateIndex(
                name: "IX_Dashboards_username",
                table: "Dashboards",
                column: "username");

            migrationBuilder.CreateIndex(
                name: "IX_DatasetAlerts_DatasetId",
                table: "DatasetAlerts",
                column: "DatasetId");

            migrationBuilder.CreateIndex(
                name: "IX_DatasetAM_DatasetId",
                table: "DatasetAM",
                column: "DatasetId");

            migrationBuilder.CreateIndex(
                name: "IX_DatasetAsset_DatasetAMId",
                table: "DatasetAsset",
                column: "DatasetAMId");

            migrationBuilder.CreateIndex(
                name: "IX_DatasetCategory_DatasetId",
                table: "DatasetCategory",
                column: "DatasetId");

            migrationBuilder.CreateIndex(
                name: "IX_DatasetDevices_DatasetId",
                table: "DatasetDevices",
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
                name: "IX_DatasetEventTaskInstance_DatasetAMId",
                table: "DatasetEventTaskInstance",
                column: "DatasetAMId");

            migrationBuilder.CreateIndex(
                name: "IX_DatasetExtensions_DatasetId",
                table: "DatasetExtensions",
                column: "DatasetId");

            migrationBuilder.CreateIndex(
                name: "IX_DatasetNews_DatasetId",
                table: "DatasetNews",
                column: "DatasetId");

            migrationBuilder.CreateIndex(
                name: "IX_DatasetReports_DatasetsOfReportsId",
                table: "DatasetReports",
                column: "DatasetsOfReportsId");

            migrationBuilder.CreateIndex(
                name: "IX_DatasetsEM_DatasetId",
                table: "DatasetsEM",
                column: "DatasetId");

            migrationBuilder.CreateIndex(
                name: "IX_DatasetsIM_DatasetId",
                table: "DatasetsIM",
                column: "DatasetId");

            migrationBuilder.CreateIndex(
                name: "IX_DatasetStock_DatasetEventTaskInstanceId",
                table: "DatasetStock",
                column: "DatasetEventTaskInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_DatasetsUM_DatasetId",
                table: "DatasetsUM",
                column: "DatasetId");

            migrationBuilder.CreateIndex(
                name: "IX_GrupoDatasets_id_dataset",
                table: "GrupoDatasets",
                column: "id_dataset");

            migrationBuilder.CreateIndex(
                name: "IX_GrupoDatasets_Id_visualizacion",
                table: "GrupoDatasets",
                column: "Id_visualizacion");

            migrationBuilder.CreateIndex(
                name: "IX_GrupoVisualizaciones_grupo_visualizacion",
                table: "GrupoVisualizaciones",
                column: "grupo_visualizacion");

            migrationBuilder.CreateIndex(
                name: "IX_GrupoVisualizaciones_id_kpi",
                table: "GrupoVisualizaciones",
                column: "id_kpi");

            migrationBuilder.CreateIndex(
                name: "IX_GrupoVisualizaciones_id_visualizacion",
                table: "GrupoVisualizaciones",
                column: "id_visualizacion");

            migrationBuilder.CreateIndex(
                name: "IX_ReportJoins_CrossModuleJoinId",
                table: "ReportJoins",
                column: "CrossModuleJoinId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId_PermissionId",
                table: "RolePermissions",
                columns: new[] { "RoleId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SharedLinks_DashboardId",
                table: "SharedLinks",
                column: "DashboardId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedLinks_Slug",
                table: "SharedLinks",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserId_RoleId",
                table: "UserRoles",
                columns: new[] { "UserId", "RoleId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "DatasetAlerts");

            migrationBuilder.DropTable(
                name: "DatasetAsset");

            migrationBuilder.DropTable(
                name: "DatasetCategory");

            migrationBuilder.DropTable(
                name: "DatasetDevices");

            migrationBuilder.DropTable(
                name: "DatasetEvents");

            migrationBuilder.DropTable(
                name: "DatasetEventsEM");

            migrationBuilder.DropTable(
                name: "DatasetExtensions");

            migrationBuilder.DropTable(
                name: "DatasetNews");

            migrationBuilder.DropTable(
                name: "DatasetReports");

            migrationBuilder.DropTable(
                name: "DatasetStock");

            migrationBuilder.DropTable(
                name: "GrupoDatasets");

            migrationBuilder.DropTable(
                name: "GrupoVisualizaciones");

            migrationBuilder.DropTable(
                name: "ReportJoins");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "SharedLinks");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "DatasetsIM");

            migrationBuilder.DropTable(
                name: "DatasetsEM");

            migrationBuilder.DropTable(
                name: "DatasetsUM");

            migrationBuilder.DropTable(
                name: "DatasetsOfReports");

            migrationBuilder.DropTable(
                name: "DatasetEventTaskInstance");

            migrationBuilder.DropTable(
                name: "Kpi");

            migrationBuilder.DropTable(
                name: "Visualizaciones");

            migrationBuilder.DropTable(
                name: "CrossModuleJoins");

            migrationBuilder.DropTable(
                name: "Reports");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "Dashboards");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "DatasetAM");

            migrationBuilder.DropTable(
                name: "JoinOperands");

            migrationBuilder.DropTable(
                name: "Datasets");
        }
    }
}
