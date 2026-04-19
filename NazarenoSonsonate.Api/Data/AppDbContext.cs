using Microsoft.EntityFrameworkCore;
using NazarenoSonsonate.Api.Models;

namespace NazarenoSonsonate.Api.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Recorrido> Recorridos { get; set; }
        public DbSet<PuntoRuta> PuntosRuta { get; set; }
        public DbSet<UltimaUbicacionProcesion> UltimasUbicacionesProcesion { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UltimaUbicacionProcesion>()
                .HasIndex(x => new { x.RecorridoId, x.TipoUnidad })
                .IsUnique();

            modelBuilder.Entity<UltimaUbicacionProcesion>()
                .Property(x => x.TipoUnidad)
                .HasMaxLength(100);
        }
    }
}