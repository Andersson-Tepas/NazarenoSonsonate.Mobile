namespace NazarenoSonsonate.Api.Models
{
    public class UltimaUbicacionProcesion
    {
        public int Id { get; set; }
        public int RecorridoId { get; set; }
        public string TipoUnidad { get; set; } = string.Empty;
        public double Latitud { get; set; }
        public double Longitud { get; set; }
        public DateTime FechaHora { get; set; }
        public string? GrupoActual { get; set; }
        public string? Mensaje { get; set; }
    }
}