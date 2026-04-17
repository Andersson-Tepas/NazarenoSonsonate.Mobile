using NazarenoSonsonate.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace NazarenoSonsonate.MobileNative.Services
{
    public class UbicacionService
    {
        private readonly HttpClient _httpClient;

        public UbicacionService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<bool> EnviarUbicacionActualAsync(int recorridoId, string? grupoActual = null, string? mensaje = null)
        {
            var permiso = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

            if (permiso != PermissionStatus.Granted)
                permiso = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            if (permiso != PermissionStatus.Granted)
                return false;

            var request = new GeolocationRequest(
                GeolocationAccuracy.Best,
                TimeSpan.FromSeconds(10));

            var location = await Geolocation.Default.GetLocationAsync(request);

            if (location is null)
                return false;

            var dto = new UbicacionProcesionDto
            {
                RecorridoId = recorridoId,
                Latitud = location.Latitude,
                Longitud = location.Longitude,
                FechaHora = DateTime.Now,
                GrupoActual = grupoActual ?? "Grupo central",
                Mensaje = mensaje ?? "Ubicación enviada desde dispositivo autorizado"
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/ubicacion", dto);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al enviar ubicación: {ex.Message}");
                return false;
            }
        }
    }
}
