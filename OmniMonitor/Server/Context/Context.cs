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

        public DbSet<Employee> Employees { get; set; }
        public DbSet<Item> Items { get; set; }
        // Add this line inside your ApplicationDbContext.cs
        public DbSet<SensorClimax> SensorClimaxs { get; set; }

        // Entidades del sistema de roles y permisos
        public DbSet<User> Users { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<Permission> Permissions { get; set; }

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
            
            // Seed default data
            Seed(builder);
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
            // Datos de sensores
            builder.Entity<SensorClimax>().HasData(
                new SensorClimax { Id = 1, Temperatura = 24, Humedad = 55, Co2 = 400, Potencia = 150, NivleDeBrillo = 300, NivelDeRuido = 50, HumedadDelSuelo = 20, TemperaturaDelSuelo = 18 },
                new SensorClimax { Id = 2, Temperatura = 25, Humedad = 60, Co2 = 420, Potencia = 160, NivleDeBrillo = 320, NivelDeRuido = 55, HumedadDelSuelo = 22, TemperaturaDelSuelo = 19 }
            );

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
                new Permission { Id = 10, Name = "Gestionar Items", Description = "Permite crear, editar y eliminar items"}
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
                
                // Visitante solo tiene permisos de lectura
                new RolePermission { Id = 11, RoleId = 2, PermissionId = 1 },
                new RolePermission { Id = 12, RoleId = 2, PermissionId = 5 },
                new RolePermission { Id = 13, RoleId = 2, PermissionId = 7 },
                new RolePermission { Id = 14, RoleId = 2, PermissionId = 9 }
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
