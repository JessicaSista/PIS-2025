using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace OmniMonitor.Server.Migrations
{
    /// <inheritdoc />
    public partial class InitialClean : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    Module = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
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
                    JSON_config = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    JSON_filters = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                    JSON_design = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    link = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Visualizaciones", x => x.Id);
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
                    DatasetId = table.Column<int>(type: "int", nullable: false),
                    Filters = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                    DatasetId = table.Column<int>(type: "int", nullable: false),
                    Filters = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                    JsonFilters = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    DatasetId = table.Column<int>(type: "int", nullable: false),
                    Filters = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                name: "UserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false),
                    IsGranted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserClaims_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
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
                name: "DatasetSensors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DatasetId = table.Column<int>(type: "int", nullable: false),
                    SensorName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatasetSensors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DatasetSensors_DatasetsIM_DatasetId",
                        column: x => x.DatasetId,
                        principalTable: "DatasetsIM",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DatasetSources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DatasetId = table.Column<int>(type: "int", nullable: false),
                    Id_source = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatasetSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DatasetSources_DatasetsIM_DatasetId",
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
                    DatasetAMId = table.Column<int>(type: "int", nullable: false),
                    DatasetEventTaskInstanceId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatasetStock", x => x.Grupo_Stock);
                    table.ForeignKey(
                        name: "FK_DatasetStock_DatasetAM_DatasetAMId",
                        column: x => x.DatasetAMId,
                        principalTable: "DatasetAM",
                        principalColumn: "Id_Dataset",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DatasetStock_DatasetEventTaskInstance_DatasetEventTaskInstanceId",
                        column: x => x.DatasetEventTaskInstanceId,
                        principalTable: "DatasetEventTaskInstance",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Action", "Description", "Module", "Name" },
                values: new object[,]
                {
                    { 1, "View", "Ver usuarios", "Users", "Users.View" },
                    { 2, "Create", "Crear usuarios", "Users", "Users.Create" },
                    { 3, "Edit", "Editar usuarios", "Users", "Users.Edit" },
                    { 4, "Delete", "Eliminar usuarios", "Users", "Users.Delete" },
                    { 5, "View", "Ver dashboards", "Dashboards", "Dashboards.View" },
                    { 6, "Create", "Crear dashboards", "Dashboards", "Dashboards.Create" },
                    { 7, "Edit", "Editar dashboards", "Dashboards", "Dashboards.Edit" },
                    { 8, "Delete", "Eliminar dashboards", "Dashboards", "Dashboards.Delete" },
                    { 9, "Share", "Compartir dashboards", "Dashboards", "Dashboards.Share" },
                    { 10, "View", "Ver datasets", "Datasets", "Datasets.View" },
                    { 11, "Create", "Crear datasets", "Datasets", "Datasets.Create" },
                    { 12, "Edit", "Editar datasets", "Datasets", "Datasets.Edit" },
                    { 13, "Delete", "Eliminar datasets", "Datasets", "Datasets.Delete" },
                    { 14, "View", "Ver visualizaciones", "Visualizations", "Visualizations.View" },
                    { 15, "Create", "Crear visualizaciones", "Visualizations", "Visualizations.Create" },
                    { 16, "Edit", "Editar visualizaciones", "Visualizations", "Visualizations.Edit" },
                    { 17, "Delete", "Eliminar visualizaciones", "Visualizations", "Visualizations.Delete" },
                    { 18, "View", "Ver reportes", "Reports", "Reports.View" },
                    { 19, "Create", "Crear reportes", "Reports", "Reports.Create" },
                    { 20, "Edit", "Editar reportes", "Reports", "Reports.Edit" },
                    { 21, "Delete", "Eliminar reportes", "Reports", "Reports.Delete" },
                    { 22, "Export", "Exportar reportes", "Reports", "Reports.Export" },
                    { 23, "View", "Ver datos de sensores", "Sensors", "Sensors.View" },
                    { 24, "Configure", "Configurar sensores", "Sensors", "Sensors.Configure" },
                    { 25, "View", "Ver dispositivos", "Devices", "Devices.View" },
                    { 26, "Manage", "Gestionar dispositivos", "Devices", "Devices.Manage" },
                    { 27, "View", "Ver activos", "Assets", "Assets.View" },
                    { 28, "Create", "Crear activos", "Assets", "Assets.Create" },
                    { 29, "Edit", "Editar activos", "Assets", "Assets.Edit" },
                    { 30, "Delete", "Eliminar activos", "Assets", "Assets.Delete" },
                    { 31, "View", "Ver tareas", "Tasks", "Tasks.View" },
                    { 32, "Create", "Crear tareas", "Tasks", "Tasks.Create" },
                    { 33, "Edit", "Editar tareas", "Tasks", "Tasks.Edit" },
                    { 34, "Delete", "Eliminar tareas", "Tasks", "Tasks.Delete" },
                    { 35, "View", "Ver zonas", "Zones", "Zones.View" },
                    { 36, "Manage", "Gestionar zonas", "Zones", "Zones.Manage" },
                    { 37, "View", "Ver eventos", "Events", "Events.View" },
                    { 38, "Manage", "Gestionar eventos", "Events", "Events.Manage" },
                    { 39, "View", "Ver alertas", "Alerts", "Alerts.View" },
                    { 40, "Manage", "Gestionar alertas", "Alerts", "Alerts.Manage" },
                    { 41, "ViewRoles", "Ver roles del sistema", "System", "System.ViewRoles" },
                    { 42, "ManageRoles", "Gestionar roles", "System", "System.ManageRoles" },
                    { 43, "ViewPermissions", "Ver permisos", "System", "System.ViewPermissions" },
                    { 44, "ManagePermissions", "Gestionar permisos", "System", "System.ManagePermissions" },
                    { 45, "ViewLogs", "Ver logs del sistema", "System", "System.ViewLogs" },
                    { 46, "ViewSettings", "Ver configuración del sistema", "System", "System.ViewSettings" },
                    { 47, "ManageSettings", "Gestionar configuración del sistema", "System", "System.ManageSettings" },
                    { 48, "View", "Ver KPIs", "Kpis", "Kpis.View" },
                    { 49, "Create", "Crear KPIs", "Kpis", "Kpis.Create" },
                    { 50, "Edit", "Editar KPIs", "Kpis", "Kpis.Edit" },
                    { 51, "Delete", "Eliminar KPIs", "Kpis", "Kpis.Delete" }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[] { 1, "Rol con acceso completo al sistema", "Admin" });

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
                    { 11, 11, 1 },
                    { 12, 12, 1 },
                    { 13, 13, 1 },
                    { 14, 14, 1 },
                    { 15, 15, 1 },
                    { 16, 16, 1 },
                    { 17, 17, 1 },
                    { 18, 18, 1 },
                    { 19, 19, 1 },
                    { 20, 20, 1 },
                    { 21, 21, 1 },
                    { 22, 22, 1 },
                    { 23, 23, 1 },
                    { 24, 24, 1 },
                    { 25, 25, 1 },
                    { 26, 26, 1 },
                    { 27, 27, 1 },
                    { 28, 28, 1 },
                    { 29, 29, 1 },
                    { 30, 30, 1 },
                    { 31, 31, 1 },
                    { 32, 32, 1 },
                    { 33, 33, 1 },
                    { 34, 34, 1 },
                    { 35, 35, 1 },
                    { 36, 36, 1 },
                    { 37, 37, 1 },
                    { 38, 38, 1 },
                    { 39, 39, 1 },
                    { 40, 40, 1 },
                    { 41, 41, 1 },
                    { 42, 42, 1 },
                    { 43, 43, 1 },
                    { 44, 44, 1 },
                    { 45, 45, 1 },
                    { 46, 46, 1 },
                    { 47, 47, 1 },
                    { 48, 48, 1 },
                    { 49, 49, 1 },
                    { 50, 50, 1 },
                    { 51, 51, 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

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
                name: "IX_DatasetsEM_DatasetId",
                table: "DatasetsEM",
                column: "DatasetId");

            migrationBuilder.CreateIndex(
                name: "IX_DatasetSensors_DatasetId",
                table: "DatasetSensors",
                column: "DatasetId");

            migrationBuilder.CreateIndex(
                name: "IX_DatasetsIM_DatasetId",
                table: "DatasetsIM",
                column: "DatasetId");

            migrationBuilder.CreateIndex(
                name: "IX_DatasetSources_DatasetId",
                table: "DatasetSources",
                column: "DatasetId");

            migrationBuilder.CreateIndex(
                name: "IX_DatasetStock_DatasetAMId",
                table: "DatasetStock",
                column: "DatasetAMId");

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
                name: "IX_Permissions_Module_Action",
                table: "Permissions",
                columns: new[] { "Module", "Action" },
                unique: true);

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
                name: "IX_UserClaims_PermissionId",
                table: "UserClaims",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserClaims_UserId_PermissionId",
                table: "UserClaims",
                columns: new[] { "UserId", "PermissionId" },
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
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "DatasetAlerts");

            migrationBuilder.DropTable(
                name: "DatasetAsset");

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
                name: "DatasetSensors");

            migrationBuilder.DropTable(
                name: "DatasetSources");

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
                name: "UserClaims");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "DatasetsEM");

            migrationBuilder.DropTable(
                name: "DatasetsUM");

            migrationBuilder.DropTable(
                name: "DatasetsIM");

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
                name: "Dashboards");

            migrationBuilder.DropTable(
                name: "Permissions");

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
