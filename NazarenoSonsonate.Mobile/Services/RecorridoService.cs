using Microsoft.Maui.Storage;
using NazarenoSonsonate.Shared.DTOs;
using System.Net.Http.Json;
using System.Text.Json;

namespace NazarenoSonsonate.Mobile.Services
{
    public class RecorridoService
    {
        private readonly HttpClient _httpClient;

        private const string RecorridosCacheKey = "recorridos_cache_v1";
        private const string RecorridoDetallePrefix = "recorrido_detalle_";

        private List<RecorridoDto>? _cacheRecorridos;
        private readonly Dictionary<int, RecorridoDto> _cachePorId = new();

        private Task? _preloadTask;
        private readonly SemaphoreSlim _lock = new(1, 1);

        public RecorridoService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task PreloadRecorridosAsync()
        {
            if (_cacheRecorridos is not null && _cacheRecorridos.Count > 0)
                return;

            if (_preloadTask is not null && !_preloadTask.IsCompleted)
            {
                await _preloadTask;
                return;
            }

            _preloadTask = PreloadInternoAsync();
            await _preloadTask;
        }

        private async Task PreloadInternoAsync()
        {
            await _lock.WaitAsync();

            try
            {
                if (_cacheRecorridos is not null && _cacheRecorridos.Count > 0)
                    return;

                var cacheLocal = Preferences.Default.Get(RecorridosCacheKey, string.Empty);

                if (!string.IsNullOrWhiteSpace(cacheLocal))
                {
                    var listaLocal = JsonSerializer.Deserialize<List<RecorridoDto>>(cacheLocal);
                    if (listaLocal is not null && listaLocal.Count > 0)
                    {
                        _cacheRecorridos = listaLocal;
                        SincronizarCachePorId(listaLocal);
                        return;
                    }
                }

                var result = await _httpClient.GetFromJsonAsync<List<RecorridoDto>>("api/recorridos")
                             ?? new List<RecorridoDto>();

                _cacheRecorridos = result;
                SincronizarCachePorId(result);
                GuardarListaEnPreferences(result);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<List<RecorridoDto>> ObtenerRecorridosAsync(bool forzarRecarga = false, bool permitirApi = true)
        {
            if (!forzarRecarga && _cacheRecorridos is not null && _cacheRecorridos.Count > 0)
                return _cacheRecorridos;

            if (!forzarRecarga)
            {
                var cacheLocal = Preferences.Default.Get(RecorridosCacheKey, string.Empty);

                if (!string.IsNullOrWhiteSpace(cacheLocal))
                {
                    var lista = JsonSerializer.Deserialize<List<RecorridoDto>>(cacheLocal);
                    if (lista is not null && lista.Count > 0)
                    {
                        _cacheRecorridos = lista;
                        SincronizarCachePorId(lista);
                        return _cacheRecorridos;
                    }
                }
            }

            if (!permitirApi)
                return new List<RecorridoDto>();

            var result = await _httpClient.GetFromJsonAsync<List<RecorridoDto>>("api/recorridos")
                         ?? new List<RecorridoDto>();

            _cacheRecorridos = result;
            SincronizarCachePorId(result);
            GuardarListaEnPreferences(result);

            return _cacheRecorridos;
        }

        public async Task<RecorridoDto?> ObtenerRecorridoPorIdAsync(int id, bool forzarRecarga = false, bool permitirApi = true)
        {
            if (!forzarRecarga && _cachePorId.TryGetValue(id, out var cacheado))
                return cacheado;

            if (!forzarRecarga)
            {
                var cacheLocal = Preferences.Default.Get($"{RecorridoDetallePrefix}{id}", string.Empty);

                if (!string.IsNullOrWhiteSpace(cacheLocal))
                {
                    var item = JsonSerializer.Deserialize<RecorridoDto>(cacheLocal);
                    if (item is not null)
                    {
                        _cachePorId[id] = item;
                        ActualizarListaCache(item);
                        return item;
                    }
                }
            }

            if (!permitirApi)
                return null;

            var result = await _httpClient.GetFromJsonAsync<RecorridoDto>($"api/recorridos/{id}");

            if (result is not null)
            {
                _cachePorId[id] = result;
                ActualizarListaCache(result);
                Preferences.Default.Set($"{RecorridoDetallePrefix}{id}", JsonSerializer.Serialize(result));
            }

            return result;
        }

        public async Task GuardarRutaAsync(int id, string rutaGeoJson)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/recorridos/{id}/ruta", new
            {
                rutaGeoJson
            });

            response.EnsureSuccessStatusCode();

            if (_cachePorId.TryGetValue(id, out var recorrido))
            {
                recorrido.RutaGeoJson = rutaGeoJson;
                Preferences.Default.Set($"{RecorridoDetallePrefix}{id}", JsonSerializer.Serialize(recorrido));
            }

            if (_cacheRecorridos is not null)
            {
                var item = _cacheRecorridos.FirstOrDefault(x => x.Id == id);
                if (item is not null)
                {
                    item.RutaGeoJson = rutaGeoJson;
                    GuardarListaEnPreferences(_cacheRecorridos);
                }
            }
        }

        public async Task GuardarMapaAsync(int recorridoId, GuardarMapaRecorridoDto dto)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/recorridos/{recorridoId}/mapa", dto);
            response.EnsureSuccessStatusCode();

            if (_cachePorId.TryGetValue(recorridoId, out var recorrido))
            {
                recorrido.RutaGeoJson = dto.RutaGeoJson;
                recorrido.PuntosRuta = dto.PuntosRuta;
                Preferences.Default.Set($"{RecorridoDetallePrefix}{recorridoId}", JsonSerializer.Serialize(recorrido));
            }

            if (_cacheRecorridos is not null)
            {
                var item = _cacheRecorridos.FirstOrDefault(x => x.Id == recorridoId);
                if (item is not null)
                {
                    item.RutaGeoJson = dto.RutaGeoJson;
                    item.PuntosRuta = dto.PuntosRuta;
                    GuardarListaEnPreferences(_cacheRecorridos);
                }
            }
        }

        public void LimpiarCache()
        {
            _cacheRecorridos = null;
            _cachePorId.Clear();
            Preferences.Default.Remove(RecorridosCacheKey);
        }

        private void SincronizarCachePorId(List<RecorridoDto> lista)
        {
            _cachePorId.Clear();

            foreach (var recorrido in lista)
            {
                _cachePorId[recorrido.Id] = recorrido;
                Preferences.Default.Set($"{RecorridoDetallePrefix}{recorrido.Id}", JsonSerializer.Serialize(recorrido));
            }
        }

        private void ActualizarListaCache(RecorridoDto recorrido)
        {
            if (_cacheRecorridos is null)
                return;

            var index = _cacheRecorridos.FindIndex(x => x.Id == recorrido.Id);
            if (index >= 0)
                _cacheRecorridos[index] = recorrido;
            else
                _cacheRecorridos.Add(recorrido);

            GuardarListaEnPreferences(_cacheRecorridos);
        }

        private void GuardarListaEnPreferences(List<RecorridoDto> lista)
        {
            Preferences.Default.Set(RecorridosCacheKey, JsonSerializer.Serialize(lista));
        }
    }
}