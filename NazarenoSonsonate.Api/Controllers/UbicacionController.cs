using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using NazarenoSonsonate.Api.Hubs;
using NazarenoSonsonate.Shared.DTOs;

namespace NazarenoSonsonate.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UbicacionController : ControllerBase
    {
        private readonly IHubContext<ProcesionHub> _hubContext;

        public UbicacionController(IHubContext<ProcesionHub> hubContext)
        {
            _hubContext = hubContext;
        }

        // ✅ GET (SIMULADO - LO DEJAMOS COMO LO TENÍAS)
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
                Mensaje = "Ubicación actual simulada"
            };

            return Ok(ubicacion);
        }

        // ✅ POST (ENVÍA A SIGNALR)
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] UbicacionProcesionDto ubicacion)
        {
            if (ubicacion is null)
                return BadRequest();

            ubicacion.FechaHora = DateTime.Now;

            // 🔥 IMPORTANTE: nombre del evento corregido
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