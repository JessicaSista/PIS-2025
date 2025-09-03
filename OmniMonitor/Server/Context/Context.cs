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
            // Seed default data
            Seed(builder);
        }

        /// <summary>
        /// Method to seed default data to the database.
        /// </summary>
        protected void Seed(ModelBuilder builder)
        {
            builder.Entity<SensorClimax>().HasData(
            new SensorClimax { Id = 1, Temperatura = 24, Humedad = 55, Co2 = 400, Potencia = 150, NivleDeBrillo = 300, NivelDeRuido = 50, HumedadDelSuelo = 20, TemperaturaDelSuelo = 18 },
            new SensorClimax { Id = 2, Temperatura = 25, Humedad = 60, Co2 = 420, Potencia = 160, NivleDeBrillo = 320, NivelDeRuido = 55, HumedadDelSuelo = 22, TemperaturaDelSuelo = 19 }
    );
        }
    }
}
