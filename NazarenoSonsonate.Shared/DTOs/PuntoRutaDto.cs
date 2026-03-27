using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NazarenoSonsonate.Shared.DTOs
{
    public class PuntoRutaDto
    {
        public int Id { get; set; }
        public int RecorridoId { get; set; }
        public double Latitud { get; set; }
        public double Longitud { get; set; }
        public int Orden { get; set; }
        public string? Referencia { get; set; }
    }
}
