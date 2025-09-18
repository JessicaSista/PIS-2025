using Microsoft.EntityFrameworkCore;
using OmniMonitor.Shared.Dtos;

namespace OmniMonitor.Server.Context
{
    /// <summary>
    /// This DbContext is configured to read the connection string directly from IConfiguration.
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        private readonly IConfiguration? _configuration;

        // Constructor para producción (usando IConfiguration)
        public ApplicationDbContext(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Constructor para pruebas (usando DbContextOptions)
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Employee> Employees { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<SensorClimax> SensorClimaxs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured && _configuration is not null)
            {
                optionsBuilder.UseSqlServer(_configuration.GetConnectionString("DefaultConnection"));
            }
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            Seed(builder);
        }

        protected void Seed(ModelBuilder builder)
        {
            builder.Entity<SensorClimax>().HasData(
                new SensorClimax { Id = 1, Temperatura = 24, Humedad = 55, Co2 = 400, Potencia = 150, NivleDeBrillo = 300, NivelDeRuido = 50, HumedadDelSuelo = 20, TemperaturaDelSuelo = 18 },
                new SensorClimax { Id = 2, Temperatura = 25, Humedad = 60, Co2 = 420, Potencia = 160, NivleDeBrillo = 320, NivelDeRuido = 55, HumedadDelSuelo = 22, TemperaturaDelSuelo = 19 }
            );
        }
    }
}
