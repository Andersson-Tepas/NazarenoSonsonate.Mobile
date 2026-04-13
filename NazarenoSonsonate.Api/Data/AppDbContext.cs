using Microsoft.EntityFrameworkCore;
using NazarenoSonsonate.Api.Models;

namespace NazarenoSonsonate.Api.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Recorrido> Recorridos { get; set; }
        public DbSet<PuntoRuta> PuntosRuta { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }
    }
}
