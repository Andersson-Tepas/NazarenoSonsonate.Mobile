using Microsoft.Maui.Devices.Sensors;
using NazarenoSonsonate.Shared.DTOs;
using System.Net.Http.Json;

namespace NazarenoSonsonate.Mobile.Services
{
    public class UbicacionService
    {
        private readonly HttpClient _httpClient;

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

            var dto = new UbicacionProcesionDto
            {
                RecorridoId = recorridoId,
                Latitud = location.Latitude,
                Longitud = location.Longitude,
                FechaHora = DateTime.Now,
                GrupoActual = grupoActual ?? "",
                Mensaje = mensaje ?? "Ubicación enviada desde dispositivo autorizado",
                TipoUnidad = tipoUnidad
            };

            var response = await _httpClient.PostAsJsonAsync("api/ubicacion", dto);
            return response.IsSuccessStatusCode;
        }
    }
}