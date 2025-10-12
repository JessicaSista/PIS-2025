using Microsoft.EntityFrameworkCore;
using OmniMonitor.Shared.Dtos;

namespace OmniMonitor.Server.Context
{
    /// <summary>
    /// This DbContext is configured to read the connection string directly from IConfiguration.
    /// </summary>
    // Using a primary constructor to inject IConfiguration.
    public class ApplicationDbContext(IConfiguration configuration) : DbContext
    {
        // No longer need a private field, the 'configuration' parameter is available throughout the class.

        // Add this line inside your ApplicationDbContext.cs

        // Entidades del sistema de roles y permisos
        public DbSet<User> Users { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<DatasetIM> DatasetsIM { get; set; }
        public DbSet<DatasetDevice> DatasetDevices { get; set; }
        public DbSet<DatasetUM> DatasetsUM { get; set; }
        public DbSet<DatasetEvent> DatasetEvents { get; set; }
        public DbSet<DatasetNews> DatasetNews { get; set; }
        public DbSet<DatasetEM> DatasetsEM { get; set; }
        public DbSet<DatasetAlert> DatasetAlerts { get; set; }
        public DbSet<DatasetEventEM> DatasetEventsEM { get; set; }
        public DbSet<DatasetExtension> DatasetExtensions { get; set; }
        public DbSet<DatasetResource> DatasetResources { get; set; }
        public DbSet<Visualizacion> Visualizaciones { get; set; }
        public DbSet<GrupoDataset> GrupoDatasets { get; set; }
        public DbSet<CrossModuleJoin> CrossModuleJoins { get; set; }
        public DbSet<JoinOperand> JoinOperands { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<ReportJoin> ReportJoins { get; set; }
        /// <summary>
        /// Configuration step using the injected IConfiguration.
        /// </summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Check if the options are not already configured (e.g., by a unit test).
            if (!optionsBuilder.IsConfigured)
            {
                // Use the connection string named "DatabaseConnection" from your appsettings.json
                optionsBuilder.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            }
        }

        /// <summary>
        /// Model creation step
        /// </summary>
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            
            // Configurar relaciones del sistema de roles y permisos
            ConfigureRolePermissionRelationships(builder);
            ConfigureCrossModuleJoins(builder);
            ConfigureReports(builder);

            // Seed default data
            Seed(builder);
        }

        private void ConfigureReports(ModelBuilder builder)
        {
            builder.Entity<ReportJoin>(entity =>
            {
                // 1. Define the composite primary key for the linking table.
                // This is necessary and ensures a join can only be added to a report once.
                entity.HasKey(rj => new { rj.ReportId, rj.CrossModuleJoinId });

                // 2. Configure the relationship to the Report entity.
                // This sets up the foreign key from ReportJoin -> Report.
                entity.HasOne(rj => rj.Report)
                      .WithMany(r => r.ReportJoins) // A Report has many ReportJoin entries
                      .HasForeignKey(rj => rj.ReportId);

                // 3. Configure the relationship to the CrossModuleJoin entity.
                // This sets up the foreign key from ReportJoin -> CrossModuleJoin.
                entity.HasOne(rj => rj.CrossModuleJoin)
                      .WithMany() // A CrossModuleJoin can be part of many reports
                      .HasForeignKey(rj => rj.CrossModuleJoinId);
            });
        }

        private void ConfigureCrossModuleJoins(ModelBuilder builder)
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

        /// <summary>
        /// Configura las relaciones entre las entidades de roles y permisos
        /// </summary>
        private void ConfigureRolePermissionRelationships(ModelBuilder builder)
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

            // Índices únicos para evitar duplicados
            builder.Entity<UserRole>()
                .HasIndex(ur => new { ur.UserId, ur.RoleId })
                .IsUnique();

            builder.Entity<RolePermission>()
                .HasIndex(rp => new { rp.RoleId, rp.PermissionId })
                .IsUnique();
        }

        /// <summary>
        /// Method to seed default data to the database.
        /// </summary>
        protected void Seed(ModelBuilder builder)
        {

            // Datos de roles
            builder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "Administrador", Description = "Rol con acceso completo al sistema" },
                new Role { Id = 2, Name = "Visitante", Description = "Rol con acceso limitado de solo lectura" }
            );

            // Datos de permisos
            builder.Entity<Permission>().HasData(
                // Permisos de usuarios
                new Permission { Id = 1, Name = "Ver Usuarios", Description = "Permite ver la lista de usuarios"},
                new Permission { Id = 2, Name = "Crear Usuarios", Description = "Permite crear nuevos usuarios"},
                new Permission { Id = 3, Name = "Editar Usuarios", Description = "Permite editar usuarios existentes"},
                new Permission { Id = 4, Name = "Eliminar Usuarios", Description = "Permite eliminar usuarios"},
                
                // Permisos de sensores
                new Permission { Id = 5, Name = "Ver Sensores", Description = "Permite ver datos de sensores"},
                new Permission { Id = 6, Name = "Configurar Sensores", Description = "Permite configurar sensores"},
                
                // Permisos de empleados
                new Permission { Id = 7, Name = "Ver Empleados", Description = "Permite ver la lista de empleados"},
                new Permission { Id = 8, Name = "Gestionar Empleados", Description = "Permite crear, editar y eliminar empleados"},
                
                // Permisos de items
                new Permission { Id = 9, Name = "Ver Items", Description = "Permite ver la lista de items"},
                new Permission { Id = 10, Name = "Gestionar Items", Description = "Permite crear, editar y eliminar items"},
                
                // Permisos de datasets UM
                new Permission { Id = 11, Name = "Ver Datasets UM", Description = "Permite ver datasets del módulo UM (Zonas, Eventos, Noticias)"},
                new Permission { Id = 12, Name = "Crear Datasets UM", Description = "Permite crear nuevos datasets del módulo UM"},
                new Permission { Id = 13, Name = "Eliminar Datasets UM", Description = "Permite eliminar datasets del módulo UM"},
                
                // Permisos de datasets EM
                new Permission { Id = 14, Name = "Ver Datasets EM", Description = "Permite ver datasets del módulo EM (Alertas, Eventos, Extensiones, Recursos)"},
                new Permission { Id = 15, Name = "Crear Datasets EM", Description = "Permite crear nuevos datasets del módulo EM"},
                new Permission { Id = 16, Name = "Eliminar Datasets EM", Description = "Permite eliminar datasets del módulo EM"}
            );

            // Asignar permisos a roles
            builder.Entity<RolePermission>().HasData(
                // Administrador tiene todos los permisos
                new RolePermission { Id = 1, RoleId = 1, PermissionId = 1 },
                new RolePermission { Id = 2, RoleId = 1, PermissionId = 2 },
                new RolePermission { Id = 3, RoleId = 1, PermissionId = 3 },
                new RolePermission { Id = 4, RoleId = 1, PermissionId = 4 },
                new RolePermission { Id = 5, RoleId = 1, PermissionId = 5 },
                new RolePermission { Id = 6, RoleId = 1, PermissionId = 6 },
                new RolePermission { Id = 7, RoleId = 1, PermissionId = 7 },
                new RolePermission { Id = 8, RoleId = 1, PermissionId = 8 },
                new RolePermission { Id = 9, RoleId = 1, PermissionId = 9 },
                new RolePermission { Id = 10, RoleId = 1, PermissionId = 10 },
                new RolePermission { Id = 15, RoleId = 1, PermissionId = 11 },
                new RolePermission { Id = 16, RoleId = 1, PermissionId = 12 },
                new RolePermission { Id = 17, RoleId = 1, PermissionId = 13 },
                new RolePermission { Id = 18, RoleId = 1, PermissionId = 14 },
                new RolePermission { Id = 19, RoleId = 1, PermissionId = 15 },
                new RolePermission { Id = 20, RoleId = 1, PermissionId = 16 },
                
                // Visitante solo tiene permisos de lectura
                new RolePermission { Id = 21, RoleId = 2, PermissionId = 1 },
                new RolePermission { Id = 22, RoleId = 2, PermissionId = 5 },
                new RolePermission { Id = 23, RoleId = 2, PermissionId = 7 },
                new RolePermission { Id = 24, RoleId = 2, PermissionId = 9 },
                new RolePermission { Id = 25, RoleId = 2, PermissionId = 11 },
                new RolePermission { Id = 26, RoleId = 2, PermissionId = 14 }
            );

            // Usuarios de prueba
            builder.Entity<User>().HasData(
                new User { Id = 1, Username = "admin", Password = "admin" },
                new User { Id = 2, Username = "visitante", Password = "visitante" }
            );

            // Asignar roles a usuarios
            builder.Entity<UserRole>().HasData(
                new UserRole { Id = 1, UserId = 1, RoleId = 1 }, // admin -> Administrador
                new UserRole { Id = 2, UserId = 2, RoleId = 2 }  // visitante -> Visitante
            );
        }
    }
}
