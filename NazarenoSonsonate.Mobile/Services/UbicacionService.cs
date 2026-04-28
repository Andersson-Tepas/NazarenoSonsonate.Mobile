using Microsoft.Maui.Devices.Sensors;
using NazarenoSonsonate.Shared.DTOs;
using System.Net.Http.Json;

namespace NazarenoSonsonate.Mobile.Services
{
    public class UbicacionService
    {
        private readonly HttpClient _httpClient;

        private const double PrecisionMaximaAceptableMetros = 25.0;

        public UbicacionService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<bool> EnviarUbicacionAsync(
            int recorridoId,
            string tipoUnidad,
            Location location,
            string? grupoActual = null,
            string? mensaje = null)
        {
            if (location is null)
                return false;

            if (!EsUbicacionValida(location))
                return false;

            var dto = new UbicacionProcesionDto
            {
                RecorridoId = recorridoId,
                Latitud = location.Latitude,
                Longitud = location.Longitude,
                FechaHora = DateTime.UtcNow,
                GrupoActual = grupoActual ?? "",
                Mensaje = mensaje ?? "Ubicación enviada desde dispositivo autorizado",
                TipoUnidad = tipoUnidad,
                PrecisionMetros = location.Accuracy
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/ubicacion", dto);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private static bool EsUbicacionValida(Location location)
        {
            if (double.IsNaN(location.Latitude) || double.IsNaN(location.Longitude))
                return false;

            if (double.IsInfinity(location.Latitude) || double.IsInfinity(location.Longitude))
                return false;

            if (location.Latitude == 0 && location.Longitude == 0)
                return false;

            if (location.Latitude < -90 || location.Latitude > 90)
                return false;

            if (location.Longitude < -180 || location.Longitude > 180)
                return false;

            if (!location.Accuracy.HasValue)
                return false;

            if (location.Accuracy.Value > PrecisionMaximaAceptableMetros)
                return false;

            return true;
        }
    }
}