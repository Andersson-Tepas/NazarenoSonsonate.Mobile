using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NazarenoSonsonate.Api.Data;
using NazarenoSonsonate.Api.Hubs;
using NazarenoSonsonate.Api.Models;
using NazarenoSonsonate.Shared.DTOs;

namespace NazarenoSonsonate.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UbicacionController : ControllerBase
    {
        private readonly IHubContext<ProcesionHub> _hubContext;
        private readonly AppDbContext _dbContext;

        public UbicacionController(
            IHubContext<ProcesionHub> hubContext,
            AppDbContext dbContext)
        {
            _hubContext = hubContext;
            _dbContext = dbContext;
        }

        [HttpGet("{recorridoId:int}")]
        public ActionResult<UbicacionProcesionDto> Get(int recorridoId)
        {
            var ubicacion = new UbicacionProcesionDto
            {
                RecorridoId = recorridoId,
                Latitud = 13.7185,
                Longitud = -89.7240,
                FechaHora = DateTime.UtcNow,
                GrupoActual = "Grupo central",
                Mensaje = "Ubicación actual simulada",
                TipoUnidad = "JesusNazareno"
            };

            return Ok(ubicacion);
        }

        [HttpGet("ultimas/{recorridoId:int}")]
        public async Task<ActionResult<List<UbicacionProcesionDto>>> GetUltimas(int recorridoId)
        {
            var resultado = await _dbContext.UltimasUbicacionesProcesion
                .AsNoTracking()
                .Where(x => x.RecorridoId == recorridoId)
                .Where(x => !string.IsNullOrWhiteSpace(x.TipoUnidad))
                .Where(x => x.Latitud != 0 || x.Longitud != 0)
                .OrderBy(x => x.TipoUnidad)
                .Select(x => new UbicacionProcesionDto
                {
                    RecorridoId = x.RecorridoId,
                    Latitud = x.Latitud,
                    Longitud = x.Longitud,
                    FechaHora = DateTime.SpecifyKind(x.FechaHora, DateTimeKind.Utc),
                    GrupoActual = x.GrupoActual,
                    Mensaje = x.Mensaje,
                    TipoUnidad = x.TipoUnidad
                })
                .ToListAsync();

            return Ok(resultado);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] UbicacionProcesionDto ubicacion)
        {
            if (ubicacion is null)
                return BadRequest("La ubicación es requerida.");

            if (string.IsNullOrWhiteSpace(ubicacion.TipoUnidad))
                return BadRequest("TipoUnidad es requerido.");

            ubicacion.FechaHora = DateTime.UtcNow;

            var existente = await _dbContext.UltimasUbicacionesProcesion
                .FirstOrDefaultAsync(x =>
                    x.RecorridoId == ubicacion.RecorridoId &&
                    x.TipoUnidad == ubicacion.TipoUnidad);

            if (existente is null)
            {
                existente = new UltimaUbicacionProcesion
                {
                    RecorridoId = ubicacion.RecorridoId,
                    TipoUnidad = ubicacion.TipoUnidad,
                    Latitud = ubicacion.Latitud,
                    Longitud = ubicacion.Longitud,
                    FechaHora = ubicacion.FechaHora,
                    GrupoActual = ubicacion.GrupoActual,
                    Mensaje = ubicacion.Mensaje
                };

                _dbContext.UltimasUbicacionesProcesion.Add(existente);
            }
            else
            {
                existente.Latitud = ubicacion.Latitud;
                existente.Longitud = ubicacion.Longitud;
                existente.FechaHora = ubicacion.FechaHora;
                existente.GrupoActual = ubicacion.GrupoActual;
                existente.Mensaje = ubicacion.Mensaje;
            }

            await _dbContext.SaveChangesAsync();

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