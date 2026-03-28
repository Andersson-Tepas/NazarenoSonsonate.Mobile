using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NazarenoSonsonate.Shared.DTOs
{
    public class GuardarMapaRecorridoDto
    {
        public string? RutaGeoJson { get; set; }
        public List<PuntoRutaDto> PuntosRuta { get; set; } = new();
    }
}
