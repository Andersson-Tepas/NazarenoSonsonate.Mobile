using NazarenoSonsonate.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace NazarenoSonsonate.Mobile.Services
{
    public class RecorridoService
    {
        private readonly HttpClient _httpClient;

        public RecorridoService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<RecorridoDto>> ObtenerRecorridosAsync()
        {
            var result = await _httpClient.GetFromJsonAsync<List<RecorridoDto>>("api/recorridos");
            return result ?? new List<RecorridoDto>();
        }

        public async Task<RecorridoDto?> ObtenerRecorridoPorIdAsync(int id)
        {
            return await _httpClient.GetFromJsonAsync<RecorridoDto>($"api/recorridos/{id}");
        }
    }
}
