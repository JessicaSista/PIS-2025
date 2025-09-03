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
        public DbSet<Rapero> Raperos { get; set; }
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
            // Seeding logic goes here
        }
    }
}
