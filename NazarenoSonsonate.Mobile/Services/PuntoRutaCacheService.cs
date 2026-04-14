using NazarenoSonsonate.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NazarenoSonsonate.Mobile.Services
{
    public class PuntoRutaCacheService
    {
        private readonly HttpClient _httpClient;
        private readonly Dictionary<string, List<PuntoRutaDto>> _memoryCache = new();

        public PuntoRutaCacheService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<PuntoRutaDto>> ObtenerPuntosPorRecorridoAsync(
            int recorridoId,
            bool forzarRecarga = false,
            bool permitirApi = true)
        {
            var cacheKey = $"puntos_recorrido_{recorridoId}";

            if (!forzarRecarga && _memoryCache.TryGetValue(cacheKey, out var cacheado))
                return cacheado;

            if (!forzarRecarga)
            {
                var json = Preferences.Default.Get(cacheKey, string.Empty);

                if (!string.IsNullOrWhiteSpace(json))
                {
                    var data = JsonSerializer.Deserialize<List<PuntoRutaDto>>(json);
                    if (data is not null)
                    {
                        _memoryCache[cacheKey] = data;
                        return data;
                    }
                }
            }

            if (!permitirApi)
                return new List<PuntoRutaDto>();

            var result = await _httpClient.GetFromJsonAsync<List<PuntoRutaDto>>($"api/ubicacion/{recorridoId}")
                         ?? new List<PuntoRutaDto>();

            _memoryCache[cacheKey] = result;
            Preferences.Default.Set(cacheKey, JsonSerializer.Serialize(result));

            return result;
        }

        public async Task<List<PuntoRutaDto>> ObtenerPorTipoAsync(
            int recorridoId,
            string tipo,
            bool forzarRecarga = false)
        {
            var puntos = await ObtenerPuntosPorRecorridoAsync(recorridoId, forzarRecarga);

            return puntos
                .Where(x => !string.IsNullOrWhiteSpace(x.Tipo) &&
                            x.Tipo.Equals(tipo, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public async Task<List<PuntoRutaDto>> ObtenerPorGrupoAsync(
            int recorridoId,
            string tipo,
            string grupo,
            bool forzarRecarga = false)
        {
            var puntos = await ObtenerPuntosPorRecorridoAsync(recorridoId, forzarRecarga);

            return puntos
                .Where(x => !string.IsNullOrWhiteSpace(x.Tipo) &&
                            x.Tipo.Equals(tipo, StringComparison.OrdinalIgnoreCase) &&
                            !string.IsNullOrWhiteSpace(x.Grupo) &&
                            x.Grupo.Equals(grupo, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public async Task<List<string>> ObtenerNumerosGrupoAsync(
            int recorridoId,
            string tipo,
            bool forzarRecarga = false)
        {
            var puntos = await ObtenerPorTipoAsync(recorridoId, tipo, forzarRecarga);

            return puntos
                .Where(x => !string.IsNullOrWhiteSpace(x.Grupo))
                .Select(x => x.Grupo!.Trim())
                .Distinct()
                .OrderBy(x =>
                {
                    if (int.TryParse(x, out var n))
                        return n;
                    return int.MaxValue;
                })
                .ThenBy(x => x)
                .ToList();
        }

        public void LimpiarCacheRecorrido(int recorridoId)
        {
            var cacheKey = $"puntos_recorrido_{recorridoId}";
            _memoryCache.Remove(cacheKey);
            Preferences.Default.Remove(cacheKey);
        }

        public void LimpiarTodo()
        {
            foreach (var key in _memoryCache.Keys.ToList())
            {
                Preferences.Default.Remove(key);
            }

            _memoryCache.Clear();
        }
    }
}
