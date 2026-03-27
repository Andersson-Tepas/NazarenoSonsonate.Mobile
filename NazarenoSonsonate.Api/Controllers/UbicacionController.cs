using Microsoft.AspNetCore.Mvc;
using NazarenoSonsonate.Shared.DTOs;

namespace NazarenoSonsonate.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UbicacionController : ControllerBase
    {
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
    }
}
