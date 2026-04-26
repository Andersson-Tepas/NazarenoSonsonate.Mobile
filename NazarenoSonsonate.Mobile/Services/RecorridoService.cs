using Microsoft.Maui.Storage;
using NazarenoSonsonate.Shared.DTOs;
using System.Net.Http.Json;
using System.Text.Json;

namespace NazarenoSonsonate.Mobile.Services
{
    public class RecorridoService
    {
        private readonly HttpClient _httpClient;

        private const string CacheVersion = "v2";
        private const string RecorridosCacheKey = $"recorridos_cache_{CacheVersion}";
        private const string RecorridoDetallePrefix = $"recorrido_detalle_{CacheVersion}_";

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

                var listaLocal = LeerListaDesdePreferences();

                if (listaLocal is not null && listaLocal.Count > 0)
                {
                    CargarListaEnMemoria(listaLocal);
                }

                try
                {
                    var result = await _httpClient.GetFromJsonAsync<List<RecorridoDto>>("api/recorridos")
                                 ?? new List<RecorridoDto>();

                    if (result.Count > 0)
                    {
                        CargarListaEnMemoria(result);
                        GuardarListaEnPreferences(result);
                    }
                }
                catch
                {
                    // Sin internet o API caída: se queda con cache local si existe.
                }
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

            var listaLocal = !forzarRecarga ? LeerListaDesdePreferences() : null;

            if (listaLocal is not null && listaLocal.Count > 0)
            {
                CargarListaEnMemoria(listaLocal);

                if (!permitirApi)
                    return _cacheRecorridos!;
            }

            if (!permitirApi)
                return _cacheRecorridos ?? new List<RecorridoDto>();

            try
            {
                var result = await _httpClient.GetFromJsonAsync<List<RecorridoDto>>("api/recorridos")
                             ?? new List<RecorridoDto>();

                if (result.Count > 0)
                {
                    CargarListaEnMemoria(result);
                    GuardarListaEnPreferences(result);
                    return _cacheRecorridos!;
                }
            }
            catch
            {
                // Sin conexión: devolvemos cache si existe.
            }

            return _cacheRecorridos ?? listaLocal ?? new List<RecorridoDto>();
        }

        public async Task<RecorridoDto?> ObtenerRecorridoPorIdAsync(int id, bool forzarRecarga = false, bool permitirApi = true)
        {
            if (!forzarRecarga && _cachePorId.TryGetValue(id, out var cacheado))
                return cacheado;

            RecorridoDto? detalleLocal = null;

            if (!forzarRecarga)
            {
                detalleLocal = LeerDetalleDesdePreferences(id);

                if (detalleLocal is not null)
                {
                    _cachePorId[id] = detalleLocal;
                    ActualizarListaCache(detalleLocal, guardarLista: false);

                    if (!permitirApi)
                        return detalleLocal;
                }

                if (_cacheRecorridos is null || _cacheRecorridos.Count == 0)
                {
                    var lista = LeerListaDesdePreferences();
                    if (lista is not null && lista.Count > 0)
                        CargarListaEnMemoria(lista);
                }

                if (_cachePorId.TryGetValue(id, out cacheado) && !permitirApi)
                    return cacheado;
            }

            if (!permitirApi)
                return detalleLocal ?? (_cachePorId.TryGetValue(id, out cacheado) ? cacheado : null);

            try
            {
                var result = await _httpClient.GetFromJsonAsync<RecorridoDto>($"api/recorridos/{id}");

                if (result is not null)
                {
                    _cachePorId[id] = result;
                    ActualizarListaCache(result);
                    GuardarDetalleEnPreferences(result);
                    return result;
                }
            }
            catch
            {
                // Sin conexión: devolvemos cache si existe.
            }

            if (detalleLocal is not null)
                return detalleLocal;

            if (_cachePorId.TryGetValue(id, out cacheado))
                return cacheado;

            return null;
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
                GuardarDetalleEnPreferences(recorrido);
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
                GuardarDetalleEnPreferences(recorrido);
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

        private static string ObtenerDetalleKey(int id) => $"{RecorridoDetallePrefix}{id}";

        private List<RecorridoDto>? LeerListaDesdePreferences()
        {
            try
            {
                var cacheLocal = Preferences.Default.Get(RecorridosCacheKey, string.Empty);

                if (string.IsNullOrWhiteSpace(cacheLocal))
                    return null;

                return JsonSerializer.Deserialize<List<RecorridoDto>>(cacheLocal);
            }
            catch
            {
                return null;
            }
        }

        private RecorridoDto? LeerDetalleDesdePreferences(int id)
        {
            try
            {
                var cacheLocal = Preferences.Default.Get(ObtenerDetalleKey(id), string.Empty);

                if (string.IsNullOrWhiteSpace(cacheLocal))
                    return null;

                return JsonSerializer.Deserialize<RecorridoDto>(cacheLocal);
            }
            catch
            {
                return null;
            }
        }

        private void CargarListaEnMemoria(List<RecorridoDto> lista)
        {
            _cacheRecorridos = lista;
            _cachePorId.Clear();

            foreach (var recorrido in lista)
            {
                _cachePorId[recorrido.Id] = recorrido;
            }
        }

        private void ActualizarListaCache(RecorridoDto recorrido, bool guardarLista = true)
        {
            _cacheRecorridos ??= new List<RecorridoDto>();

            var index = _cacheRecorridos.FindIndex(x => x.Id == recorrido.Id);

            if (index >= 0)
                _cacheRecorridos[index] = recorrido;
            else
                _cacheRecorridos.Add(recorrido);

            if (guardarLista)
                GuardarListaEnPreferences(_cacheRecorridos);
        }

        private void GuardarDetalleEnPreferences(RecorridoDto recorrido)
        {
            try
            {
                var key = ObtenerDetalleKey(recorrido.Id);
                var jsonNuevo = JsonSerializer.Serialize(recorrido);
                var jsonActual = Preferences.Default.Get(key, string.Empty);

                if (!string.Equals(jsonActual, jsonNuevo, StringComparison.Ordinal))
                {
                    Preferences.Default.Set(key, jsonNuevo);
                }
            }
            catch
            {
            }
        }

        private void GuardarListaEnPreferences(List<RecorridoDto> lista)
        {
            try
            {
                var jsonNuevo = JsonSerializer.Serialize(lista);
                var jsonActual = Preferences.Default.Get(RecorridosCacheKey, string.Empty);

                if (!string.Equals(jsonActual, jsonNuevo, StringComparison.Ordinal))
                {
                    Preferences.Default.Set(RecorridosCacheKey, jsonNuevo);
                }
            }
            catch
            {
            }
        }
    }
}