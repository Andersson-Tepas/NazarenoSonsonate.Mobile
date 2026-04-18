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

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] UbicacionProcesionDto ubicacion)
        {
            if (ubicacion is null)
                return BadRequest("La ubicación es requerida.");

            if (string.IsNullOrWhiteSpace(ubicacion.TipoUnidad))
                return BadRequest("TipoUnidad es requerido.");

            ubicacion.FechaHora = DateTime.Now;

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