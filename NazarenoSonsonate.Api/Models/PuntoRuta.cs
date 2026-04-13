using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NazarenoSonsonate.Api.Models
{
    public class PuntoRuta
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Recorrido")]
        public int RecorridoId { get; set; }

        public double Latitud { get; set; }
        public double Longitud { get; set; }

        public int Orden { get; set; }

        public string? Referencia { get; set; }
        public string? Grupo { get; set; }
        public string? Tipo { get; set; }

        // Navegación 🔥
        public Recorrido Recorrido { get; set; }
    }
}