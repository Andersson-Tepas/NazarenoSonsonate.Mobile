using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using NazarenoSonsonate.Api.Hubs;
using NazarenoSonsonate.Shared.DTOs;
using System.Collections.Concurrent;

namespace NazarenoSonsonate.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UbicacionController : ControllerBase
    {
        private readonly IHubContext<ProcesionHub> _hubContext;

        private static readonly ConcurrentDictionary<string, UbicacionProcesionDto> _ultimasUbicaciones = new();

        public UbicacionController(IHubContext<ProcesionHub> hubContext)
        {
            _hubContext = hubContext;
        }

        [HttpGet("{recorridoId:int}")]
        public ActionResult<UbicacionProcesionDto> Get(int recorridoId)
        {
            var ubicacion = new UbicacionProcesionDto
            {
                RecorridoId = recorridoId,
                Latitud = 13.7185,
                Longitud = -89.7240,
                FechaHora = DateTime.Now,
                GrupoActual = "Grupo central",
                Mensaje = "Ubicación actual simulada",
                TipoUnidad = "JesusNazareno"
            };

            return Ok(ubicacion);
        }

        [HttpGet("ultimas/{recorridoId:int}")]
        public ActionResult<List<UbicacionProcesionDto>> GetUltimas(int recorridoId)
        {
            var resultado = _ultimasUbicaciones.Values
                .Where(x => x.RecorridoId == recorridoId)
                .OrderBy(x => x.TipoUnidad)
                .ToList();

            return Ok(resultado);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] UbicacionProcesionDto ubicacion)
        {
            if (ubicacion is null)
                return BadRequest("La ubicación es requerida.");

            if (string.IsNullOrWhiteSpace(ubicacion.TipoUnidad))
                return BadRequest("TipoUnidad es requerido.");

            ubicacion.FechaHora = DateTime.Now;

            var key = $"{ubicacion.RecorridoId}:{ubicacion.TipoUnidad}";
            _ultimasUbicaciones[key] = new UbicacionProcesionDto
            {
                RecorridoId = ubicacion.RecorridoId,
                Latitud = ubicacion.Latitud,
                Longitud = ubicacion.Longitud,
                FechaHora = ubicacion.FechaHora,
                GrupoActual = ubicacion.GrupoActual,
                Mensaje = ubicacion.Mensaje,
                TipoUnidad = ubicacion.TipoUnidad
            };

            await _hubContext.Clients
                .Group($"recorrido-{ubicacion.RecorridoId}")
                .SendAsync("RecibirUbicacion", ubicacion);

            return Ok(new
            {
                mensaje = "Ubicación enviada correctamente"
            });
        }
    }
}