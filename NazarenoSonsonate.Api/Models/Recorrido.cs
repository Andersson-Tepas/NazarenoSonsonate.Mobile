using System.ComponentModel.DataAnnotations;

namespace NazarenoSonsonate.Api.Models
{
    public class Recorrido
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Nombre { get; set; } = string.Empty;

        public string Descripcion { get; set; } = string.Empty;

        public int Tipo { get; set; } // guardamos como int

        public string HoraSalida { get; set; } = string.Empty;

        public bool Activo { get; set; }

        public string? RutaGeoJson { get; set; }

        // Relación 🔥
        public List<PuntoRuta> PuntosRuta { get; set; } = new();
    }
}