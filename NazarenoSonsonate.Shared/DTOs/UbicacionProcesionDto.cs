using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NazarenoSonsonate.Shared.DTOs
{
    public class UbicacionProcesionDto
    {
        public int RecorridoId { get; set; }
        public double Latitud { get; set; }
        public double Longitud { get; set; }
        public DateTime FechaHora { get; set; }
        public string? GrupoActual { get; set; }
        public string? Mensaje { get; set; }
    }
}
