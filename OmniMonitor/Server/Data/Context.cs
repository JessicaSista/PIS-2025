using Microsoft.EntityFrameworkCore;
using OmniMonitor.Server.Models; 

namespace OmniMonitor.Server.Data
{
    // Tu DbContext principal
    public class Context : DbContext
    {
        public Context(DbContextOptions<Context> options) : base(options)
        {
        }

        // 🔹 Tablas de la base de datos
        public DbSet<SomeEntity> SomeTable { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configuración extra de entidades si es necesario
            // Por ejemplo:
            // builder.Entity<SomeEntity>().Property(e => e.Name).IsRequired().HasMaxLength(100);
        }
    }
}
