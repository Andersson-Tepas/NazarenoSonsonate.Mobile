using NazarenoSonsonate.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NazarenoSonsonate.Shared.DTOs
{
    public class RecorridoDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public TipoRecorrido Tipo { get; set; }
        public string HoraSalida { get; set; } = string.Empty;
        public List<PuntoRutaDto> PuntosRuta { get; set; } = new();
    }
}
