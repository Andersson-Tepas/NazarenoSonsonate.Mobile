using Microsoft.AspNetCore.Mvc;
using NazarenoSonsonate.Shared.DTOs;
using NazarenoSonsonate.Shared.Enums;

namespace NazarenoSonsonate.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecorridosController : ControllerBase
    {
        [HttpGet]
        public ActionResult<List<RecorridoDto>> Get()
        {
            return Ok(ObtenerRecorridosMock());
        }

        [HttpGet("{id:int}")]
        public ActionResult<RecorridoDto> GetById(int id)
        {
            var recorrido = ObtenerRecorridosMock().FirstOrDefault(x => x.Id == id);

            if (recorrido is null)
                return NotFound();

            return Ok(recorrido);
        }

        private static List<RecorridoDto> ObtenerRecorridosMock()
        {
            return new List<RecorridoDto>
        {
            new RecorridoDto
            {
                Id = 1,
                Nombre = "Lunes Santo",
                Descripcion = "Recorrido procesional de Lunes Santo",
                HoraSalida = "3:00 PM",
                Activo = true,
                Tipo = TipoRecorrido.LunesSanto,
                PuntosRuta = new()
            },
            new RecorridoDto
            {
                Id = 2,
                Nombre = "Martes Santo",
                Descripcion = "Recorrido procesional de Martes Santo",
                HoraSalida = "3:00 PM",
                Activo = true,
                Tipo = TipoRecorrido.MartesSanto,
                PuntosRuta = new()
            },
            new RecorridoDto
            {
                Id = 3,
                Nombre = "Miércoles Santo",
                Descripcion = "Recorrido procesional de Miércoles Santo",
                HoraSalida = "2:00 PM",
                Activo = true,
                Tipo = TipoRecorrido.MiercolesSanto,
                PuntosRuta = new()
            },
            new RecorridoDto
            {
                Id = 4,
                Nombre = "Viernes Santo",
                Descripcion = "Recorrido procesional de Viernes Santo",
                HoraSalida = "6:00 AM",
                Activo = true,
                Tipo = TipoRecorrido.ViernesSanto,
                PuntosRuta = new()
            }
        };
        }

        private static List<PuntoRutaDto> CrearPuntosMock(double latBase, double lngBase, params string[] referencias)
        {
            var puntos = new List<PuntoRutaDto>();

            for (int i = 0; i < referencias.Length; i++)
            {
                puntos.Add(new PuntoRutaDto
                {
                    Orden = i + 1,
                    Referencia = referencias[i],
                    Latitud = latBase + (i * 0.0008),
                    Longitud = lngBase + ((i % 2 == 0 ? 1 : -1) * 0.0006)
                });
            }

            return puntos;
        }
    }
}
