using Microsoft.Maui.Storage;
using NazarenoSonsonate.Shared.DTOs;
using System.Net.Http.Json;
using System.Text.Json;

namespace NazarenoSonsonate.Mobile.Services
{
    public class PuntoRutaCacheService
    {
        private readonly HttpClient _httpClient;

        private const string CacheVersion = "v2";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(12);

        private readonly Dictionary<string, List<PuntoRutaDto>> _memoryCache = new();
        private readonly Dictionary<string, DateTime> _memoryCacheTime = new();
        private readonly SemaphoreSlim _lock = new(1, 1);

        public PuntoRutaCacheService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        private static string GetCacheKey(int recorridoId) =>
            $"puntos_recorrido_{CacheVersion}_{recorridoId}";

        private static string GetCacheTimeKey(int recorridoId) =>
            $"puntos_recorrido_time_{CacheVersion}_{recorridoId}";

        public async Task<List<PuntoRutaDto>> ObtenerPuntosPorRecorridoAsync(
            int recorridoId,
            bool forzarRecarga = false,
            bool permitirApi = true)
        {
            var cacheKey = GetCacheKey(recorridoId);

            if (!forzarRecarga &&
                _memoryCache.TryGetValue(cacheKey, out var cacheado) &&
                _memoryCacheTime.TryGetValue(cacheKey, out var fechaMemoria) &&
                !CacheExpirado(fechaMemoria))
            {
                return cacheado;
            }

            await _lock.WaitAsync();
            try
            {
                if (!forzarRecarga &&
                    _memoryCache.TryGetValue(cacheKey, out cacheado) &&
                    _memoryCacheTime.TryGetValue(cacheKey, out fechaMemoria) &&
                    !CacheExpirado(fechaMemoria))
                {
                    return cacheado;
                }

                if (!forzarRecarga)
                {
                    var local = LeerDesdePreferences(recorridoId);
                    if (local is not null)
                    {
                        _memoryCache[cacheKey] = local.Value.Data;
                        _memoryCacheTime[cacheKey] = local.Value.Timestamp;
                        return local.Value.Data;
                    }
                }

                if (!permitirApi)
                    return new List<PuntoRutaDto>();

                // Mantengo tu endpoint actual
                var result = await _httpClient.GetFromJsonAsync<List<PuntoRutaDto>>($"api/ubicacion/{recorridoId}")
                             ?? new List<PuntoRutaDto>();

                await GuardarPuntosPorRecorridoAsync(recorridoId, result);
                return result;
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task GuardarPuntosPorRecorridoAsync(int recorridoId, List<PuntoRutaDto> puntos)
        {
            var cacheKey = GetCacheKey(recorridoId);
            var now = DateTime.UtcNow;

            _memoryCache[cacheKey] = puntos;
            _memoryCacheTime[cacheKey] = now;

            Preferences.Default.Set(cacheKey, JsonSerializer.Serialize(puntos));
            Preferences.Default.Set(GetCacheTimeKey(recorridoId), now.ToString("O"));

            await Task.CompletedTask;
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
            var cacheKey = GetCacheKey(recorridoId);

            _memoryCache.Remove(cacheKey);
            _memoryCacheTime.Remove(cacheKey);

            Preferences.Default.Remove(cacheKey);
            Preferences.Default.Remove(GetCacheTimeKey(recorridoId));
        }

        public void LimpiarTodo()
        {
            foreach (var key in _memoryCache.Keys.ToList())
            {
                Preferences.Default.Remove(key);
            }

            _memoryCache.Clear();
            _memoryCacheTime.Clear();
        }

        private static bool CacheExpirado(DateTime fechaUtc)
        {
            return DateTime.UtcNow - fechaUtc > CacheDuration;
        }

        private (List<PuntoRutaDto> Data, DateTime Timestamp)? LeerDesdePreferences(int recorridoId)
        {
            var cacheKey = GetCacheKey(recorridoId);
            var timeKey = GetCacheTimeKey(recorridoId);

            var json = Preferences.Default.Get(cacheKey, string.Empty);
            var timestampRaw = Preferences.Default.Get(timeKey, string.Empty);

            if (string.IsNullOrWhiteSpace(json) || string.IsNullOrWhiteSpace(timestampRaw))
                return null;

            if (!DateTime.TryParse(
                    timestampRaw,
                    null,
                    System.Globalization.DateTimeStyles.RoundtripKind,
                    out var timestamp))
            {
                return null;
            }

            if (CacheExpirado(timestamp))
                return null;

            var data = JsonSerializer.Deserialize<List<PuntoRutaDto>>(json);
            if (data is null)
                return null;

            return (data, timestamp);
        }
    }
}