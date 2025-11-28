using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

using OmniMonitor.Server.Models;
using OmniMonitor.Shared;
using OmniMonitor.Shared.Dtos;

namespace OmniMonitor.Server.Context
{
    public class ApplicationDbContext : IdentityUserContext<User, int>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // No longer need a private field, the 'configuration' parameter is available throughout the class.

        // Add this line inside your ApplicationDbContext.cs

        public DbSet<UserRole> UserRoles { get; set; }

        public DbSet<Role> Roles { get; set; }

        public DbSet<RolePermission> RolePermissions { get; set; }

        public DbSet<Permission> Permissions { get; set; }

        public DbSet<UserClaim> UserClaims { get; set; }

        public DbSet<Datasets> Datasets { get; set; }

        public DbSet<DatasetIM> DatasetsIM { get; set; }

        public DbSet<DatasetDevice> DatasetDevices { get; set; }

        public DbSet<DatasetSource> DatasetSources { get; set; }

        public DbSet<DatasetSensor> DatasetSensors { get; set; }

        public DbSet<DatasetUM> DatasetsUM { get; set; }

        public DbSet<DatasetEvent> DatasetEvents { get; set; }

        public DbSet<DatasetNews> DatasetNews { get; set; }

        public DbSet<DatasetEM> DatasetsEM { get; set; }

        public DbSet<DatasetAlert> DatasetAlerts { get; set; }

        public DbSet<DatasetEventEM> DatasetEventsEM { get; set; }

        public DbSet<DatasetExtension> DatasetExtensions { get; set; }

        public DbSet<Visualizacion> Visualizaciones { get; set; }

        public DbSet<GrupoDataset> GrupoDatasets { get; set; }

        public DbSet<DashboardDto> Dashboards { get; set; }

        public DbSet<GrupoVisualizacion> GrupoVisualizaciones { get; set; }

        public DbSet<DatasetAM> DatasetAM { get; set; }

        public DbSet<DatasetEventTaskInstance> DatasetEventTaskInstance { get; set; }

        public DbSet<DatasetStock> DatasetStock { get; set; }

        public DbSet<DatasetAsset> DatasetAsset { get; set; }

        public DbSet<CrossModuleJoin> CrossModuleJoins { get; set; }

        public DbSet<JoinOperand> JoinOperands { get; set; }

        public DbSet<Report> Reports { get; set; }

        public DbSet<ReportJoin> ReportJoins { get; set; }

        public DbSet<SharedLink> SharedLinks { get; set; }

        public DbSet<Kpi> Kpi { get; set; }

        public DbSet<ScheduledReport> ScheduledReports { get; set; }

        /// <summary>
        /// Model creation step.
        /// </summary>
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configurar relaciones del sistema de roles y permisos
            ConfigureRolePermissionRelationships(builder);

            // Configurar relaciones de dashboards
            ConfigureDashboardRelationships(builder);

            ConfigureCrossModuleJoins(builder);
            ConfigureReports(builder);

            ConfigureSharedLinks(builder);

            // Seed default data
            this.Seed(builder);
        }

        /// <summary>
        /// Method to seed default data to the database.
        /// </summary>
        protected void Seed(ModelBuilder builder)
        {
            // Datos de roles
            builder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "Admin", Description = "Rol con acceso completo al sistema" });

            // Definir permisos modulares con formato Module.Action
            var permissions = new List<Permission>
            {
                // Módulo Users
                new Permission { Id = 1, Module = "Users", Action = "View", Name = "Users.View", Description = "Ver usuarios" },
                new Permission { Id = 2, Module = "Users", Action = "Create", Name = "Users.Create", Description = "Crear usuarios" },
                new Permission { Id = 3, Module = "Users", Action = "Edit", Name = "Users.Edit", Description = "Editar usuarios" },
                new Permission { Id = 4, Module = "Users", Action = "Delete", Name = "Users.Delete", Description = "Eliminar usuarios" },

                // Módulo Dashboards
                new Permission { Id = 5, Module = "Dashboards", Action = "View", Name = "Dashboards.View", Description = "Ver dashboards" },
                new Permission { Id = 6, Module = "Dashboards", Action = "Create", Name = "Dashboards.Create", Description = "Crear dashboards" },
                new Permission { Id = 7, Module = "Dashboards", Action = "Edit", Name = "Dashboards.Edit", Description = "Editar dashboards" },
                new Permission { Id = 8, Module = "Dashboards", Action = "Delete", Name = "Dashboards.Delete", Description = "Eliminar dashboards" },
                new Permission { Id = 9, Module = "Dashboards", Action = "Share", Name = "Dashboards.Share", Description = "Compartir dashboards" },

                // Módulo Datasets
                new Permission { Id = 10, Module = "Datasets", Action = "View", Name = "Datasets.View", Description = "Ver datasets" },
                new Permission { Id = 11, Module = "Datasets", Action = "Create", Name = "Datasets.Create", Description = "Crear datasets" },
                new Permission { Id = 12, Module = "Datasets", Action = "Edit", Name = "Datasets.Edit", Description = "Editar datasets" },
                new Permission { Id = 13, Module = "Datasets", Action = "Delete", Name = "Datasets.Delete", Description = "Eliminar datasets" },

                // Módulo Visualizations
                new Permission { Id = 14, Module = "Visualizations", Action = "View", Name = "Visualizations.View", Description = "Ver visualizaciones" },
                new Permission { Id = 15, Module = "Visualizations", Action = "Create", Name = "Visualizations.Create", Description = "Crear visualizaciones" },
                new Permission { Id = 16, Module = "Visualizations", Action = "Edit", Name = "Visualizations.Edit", Description = "Editar visualizaciones" },
                new Permission { Id = 17, Module = "Visualizations", Action = "Delete", Name = "Visualizations.Delete", Description = "Eliminar visualizaciones" },

                // Módulo Reports
                new Permission { Id = 18, Module = "Reports", Action = "View", Name = "Reports.View", Description = "Ver reportes" },
                new Permission { Id = 19, Module = "Reports", Action = "Create", Name = "Reports.Create", Description = "Crear reportes" },
                new Permission { Id = 20, Module = "Reports", Action = "Edit", Name = "Reports.Edit", Description = "Editar reportes" },
                new Permission { Id = 21, Module = "Reports", Action = "Delete", Name = "Reports.Delete", Description = "Eliminar reportes" },
                new Permission { Id = 22, Module = "Reports", Action = "Export", Name = "Reports.Export", Description = "Exportar reportes" },
                new Permission { Id = 52, Module = "Reports", Action = "Execute", Name = "Reports.Execute", Description = "Ejecutar reportes" },

                // Módulo Sensors (IM)
                new Permission { Id = 23, Module = "Sensors", Action = "View", Name = "Sensors.View", Description = "Ver datos de sensores" },
                new Permission { Id = 24, Module = "Sensors", Action = "Configure", Name = "Sensors.Configure", Description = "Configurar sensores" },

                // Módulo Devices (IM)
                new Permission { Id = 25, Module = "Devices", Action = "View", Name = "Devices.View", Description = "Ver dispositivos" },
                new Permission { Id = 26, Module = "Devices", Action = "Manage", Name = "Devices.Manage", Description = "Gestionar dispositivos" },

                // Módulo Assets (AM)
                new Permission { Id = 27, Module = "Assets", Action = "View", Name = "Assets.View", Description = "Ver activos" },
                new Permission { Id = 28, Module = "Assets", Action = "Create", Name = "Assets.Create", Description = "Crear activos" },
                new Permission { Id = 29, Module = "Assets", Action = "Edit", Name = "Assets.Edit", Description = "Editar activos" },
                new Permission { Id = 30, Module = "Assets", Action = "Delete", Name = "Assets.Delete", Description = "Eliminar activos" },

                // Módulo Tasks (AM)
                new Permission { Id = 31, Module = "Tasks", Action = "View", Name = "Tasks.View", Description = "Ver tareas" },
                new Permission { Id = 32, Module = "Tasks", Action = "Create", Name = "Tasks.Create", Description = "Crear tareas" },
                new Permission { Id = 33, Module = "Tasks", Action = "Edit", Name = "Tasks.Edit", Description = "Editar tareas" },
                new Permission { Id = 34, Module = "Tasks", Action = "Delete", Name = "Tasks.Delete", Description = "Eliminar tareas" },

                // Módulo Zones (UM)
                new Permission { Id = 35, Module = "Zones", Action = "View", Name = "Zones.View", Description = "Ver zonas" },
                new Permission { Id = 36, Module = "Zones", Action = "Manage", Name = "Zones.Manage", Description = "Gestionar zonas" },

                // Módulo Events (UM/EM)
                new Permission { Id = 37, Module = "Events", Action = "View", Name = "Events.View", Description = "Ver eventos" },
                new Permission { Id = 38, Module = "Events", Action = "Manage", Name = "Events.Manage", Description = "Gestionar eventos" },

                // Módulo Alerts (EM)
                new Permission { Id = 39, Module = "Alerts", Action = "View", Name = "Alerts.View", Description = "Ver alertas" },
                new Permission { Id = 40, Module = "Alerts", Action = "Manage", Name = "Alerts.Manage", Description = "Gestionar alertas" },

                // Módulo System (Administración)
                new Permission { Id = 41, Module = "System", Action = "ViewRoles", Name = "System.ViewRoles", Description = "Ver roles del sistema" },
                new Permission { Id = 42, Module = "System", Action = "ManageRoles", Name = "System.ManageRoles", Description = "Gestionar roles" },
                new Permission { Id = 43, Module = "System", Action = "ViewPermissions", Name = "System.ViewPermissions", Description = "Ver permisos" },
                new Permission { Id = 44, Module = "System", Action = "ManagePermissions", Name = "System.ManagePermissions", Description = "Gestionar permisos" },
                new Permission { Id = 45, Module = "System", Action = "ViewLogs", Name = "System.ViewLogs", Description = "Ver logs del sistema" },
                new Permission { Id = 46, Module = "System", Action = "ViewSettings", Name = "System.ViewSettings", Description = "Ver configuración del sistema" },
                new Permission { Id = 47, Module = "System", Action = "ManageSettings", Name = "System.ManageSettings", Description = "Gestionar configuración del sistema" },

                // Módulo KPIs
                new Permission { Id = 48, Module = "Kpis", Action = "View", Name = "Kpis.View", Description = "Ver KPIs" },
                new Permission { Id = 49, Module = "Kpis", Action = "Create", Name = "Kpis.Create", Description = "Crear KPIs" },
                new Permission { Id = 50, Module = "Kpis", Action = "Edit", Name = "Kpis.Edit", Description = "Editar KPIs" },
                new Permission { Id = 51, Module = "Kpis", Action = "Delete", Name = "Kpis.Delete", Description = "Eliminar KPIs" }
            };

            builder.Entity<Permission>().HasData(permissions);

            // Asignar TODOS los permisos al rol Admin
            var rolePermissions = new List<RolePermission>();
            for (int i = 0; i < permissions.Count; i++)
            {
                rolePermissions.Add(new RolePermission 
                { 
                    Id = i + 1, 
                    RoleId = 1, // Admin
                    PermissionId = permissions[i].Id 
                });
            }

            builder.Entity<RolePermission>().HasData(rolePermissions);
        }

        private static void ConfigureReports(ModelBuilder builder)
        {
            builder.Entity<ReportJoin>(entity =>
            {
                entity.HasKey(rj => new { rj.ReportId, rj.CrossModuleJoinId });

                entity.HasOne(rj => rj.Report)
                      .WithMany(r => r.ReportJoins)
                      .HasForeignKey(rj => rj.ReportId);

                entity.HasOne(rj => rj.CrossModuleJoin)
                      .WithMany()
                      .HasForeignKey(rj => rj.CrossModuleJoinId);
            });
        }

        private static void ConfigureCrossModuleJoins(ModelBuilder builder)
        {
            // Configure the relationships for CrossModuleJoin
            builder.Entity<CrossModuleJoin>(entity =>
            {
                // Define the relationship for the LeftOperand
                entity.HasOne(j => j.LeftOperand)
                      .WithMany() // A JoinOperand can be the left side of many joins
                      .HasForeignKey(j => j.LeftOperandId)
                      .OnDelete(DeleteBehavior.Restrict); // Prevent deleting an operand if it's in use

                // Define the relationship for the RightOperand
                entity.HasOne(j => j.RightOperand)
                      .WithMany() // A JoinOperand can be the right side of many joins
                      .HasForeignKey(j => j.RightOperandId)
                      .OnDelete(DeleteBehavior.Restrict); // Prevent deleting an operand if it's in use

                // Store the JoinType enum as a string (e.g., "Inner", "LeftOuter") in the database
                entity.Property(j => j.JoinType)
                      .HasConversion<string>();
            });

            // Configure the JoinOperand entity
            builder.Entity<JoinOperand>(entity =>
            {
                // Store the ModuleType enum as a string (e.g., "InsightMonitor") in the database
                entity.Property(o => o.ModuleType)
                      .HasConversion<string>();
            });
        }

        private static void ConfigureSharedLinks(ModelBuilder builder)
        {
            builder.Entity<SharedLink>(entity =>
            {
                // Asegura que el 'Slug' (el enlace) sea único en la base de datos
                entity.HasIndex(s => s.Slug).IsUnique();

                // Configura la relación: Un Dashboard puede tener muchos SharedLinks
                entity.HasOne(s => s.Dashboard)
                      .WithMany() // DashboardDto no tiene una ICollection<SharedLink>, lo cual está bien.
                      .HasForeignKey(s => s.DashboardId)
                      .OnDelete(DeleteBehavior.Cascade); // Si se borra el dashboard, se borran sus enlaces.

                // Almacena los Enums como strings legibles (ej: "Public") en lugar de números
                entity.Property(s => s.Visibility)
                      .HasConversion<string>();

                entity.Property(s => s.Status)
                      .HasConversion<string>();
            });
        }

        /// <summary>
        /// Configura las relaciones entre las entidades de roles y permisos.
        /// </summary>
        private static void ConfigureRolePermissionRelationships(ModelBuilder builder)
        {
            // Configurar UserRole
            builder.Entity<UserRole>()
                .HasKey(ur => ur.Id);

            builder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configurar RolePermission
            builder.Entity<RolePermission>()
                .HasKey(rp => rp.Id);

            builder.Entity<RolePermission>()
                .HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<RolePermission>()
                .HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configurar UserClaim
            builder.Entity<UserClaim>()
                .HasKey(uc => uc.Id);

            builder.Entity<UserClaim>()
                .HasOne(uc => uc.User)
                .WithMany(u => u.UserClaims)
                .HasForeignKey(uc => uc.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserClaim>()
                .HasOne(uc => uc.Permission)
                .WithMany()
                .HasForeignKey(uc => uc.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Índices únicos para evitar duplicados
            builder.Entity<UserRole>()
                .HasIndex(ur => new { ur.UserId, ur.RoleId })
                .IsUnique();

            builder.Entity<RolePermission>()
                .HasIndex(rp => new { rp.RoleId, rp.PermissionId })
                .IsUnique();

            builder.Entity<UserClaim>()
                .HasIndex(uc => new { uc.UserId, uc.PermissionId })
                .IsUnique();

            // Índice para búsqueda rápida de permisos por Module.Action
            builder.Entity<Permission>()
                .HasIndex(p => new { p.Module, p.Action })
                .IsUnique();
        }
        private static void ConfigureDashboardRelationships(ModelBuilder builder)
        {

            // Configurar GrupoVisualizacion
            builder.Entity<GrupoVisualizacion>()
                .HasKey(gv => gv.IdGrupoVisualizacion);

            // Relación Dashboard -> GrupoVisualizacion (uno a muchos)
            builder.Entity<GrupoVisualizacion>()
                .HasOne(gv => gv.Dashboard)
                .WithMany(d => d.GrupoVisualizaciones)
                .HasForeignKey(gv => gv.GrupoVisualizacionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relación GrupoVisualizacion -> Visualizacion (muchos a uno, opcional)
            builder.Entity<GrupoVisualizacion>()
                .HasOne(gv => gv.Visualizacion)
                .WithMany()
                .HasForeignKey(gv => gv.IdVisualizacion)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            // Relación GrupoVisualizacion -> Kpi (muchos a uno, opcional)
            builder.Entity<GrupoVisualizacion>()
                .HasOne(gv => gv.Kpi)
                .WithMany()
                .HasForeignKey(gv => gv.KpiId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            // Índices para optimizar consultas
            builder.Entity<DashboardDto>()
                .HasIndex(d => d.Username);

            builder.Entity<GrupoVisualizacion>()
                .HasIndex(gv => gv.GrupoVisualizacionId);

            builder.Entity<GrupoVisualizacion>()
                .HasIndex(gv => gv.IdVisualizacion);

            builder.Entity<GrupoVisualizacion>()
                .HasIndex(gv => gv.KpiId);

        }
    }
}
