using Microsoft.AspNetCore.Mvc;
using NazarenoSonsonate.Shared.DTOs;
using NazarenoSonsonate.Shared.Enums;

namespace NazarenoSonsonate.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecorridosController : ControllerBase
    {
        private static readonly List<RecorridoDto> Recorridos = new()
    {
        new RecorridoDto
        {
            Id = 1,
            Nombre = "Lunes Santo",
            Descripcion = "Recorrido procesional de Lunes Santo",
            Tipo = TipoRecorrido.LunesSanto,
            HoraSalida = "06:00 PM",
            PuntosRuta = new List<PuntoRutaDto>
            {
                new() { Id = 1, RecorridoId = 1, Latitud = 13.7185, Longitud = -89.7240, Orden = 1, Referencia = "Punto inicial" },
                new() { Id = 2, RecorridoId = 1, Latitud = 13.7190, Longitud = -89.7230, Orden = 2, Referencia = "Segundo punto" },
                new() { Id = 3, RecorridoId = 1, Latitud = 13.7200, Longitud = -89.7220, Orden = 3, Referencia = "Tercer punto" }
            }
        },
        new RecorridoDto
        {
            Id = 2,
            Nombre = "Martes Santo",
            Descripcion = "Recorrido procesional de Martes Santo",
            Tipo = TipoRecorrido.MartesSanto,
            HoraSalida = "06:00 PM",
            PuntosRuta = new List<PuntoRutaDto>
            {
                new() { Id = 4, RecorridoId = 2, Latitud = 13.7185, Longitud = -89.7240, Orden = 1, Referencia = "Punto inicial" },
                new() { Id = 5, RecorridoId = 2, Latitud = 13.7178, Longitud = -89.7232, Orden = 2, Referencia = "Segundo punto" },
                new() { Id = 6, RecorridoId = 2, Latitud = 13.7170, Longitud = -89.7225, Orden = 3, Referencia = "Tercer punto" }
            }
        },
        new RecorridoDto
        {
            Id = 3,
            Nombre = "Miércoles Santo",
            Descripcion = "Recorrido procesional de Miércoles Santo",
            Tipo = TipoRecorrido.MiercolesSanto,
            HoraSalida = "06:00 PM",
            PuntosRuta = new List<PuntoRutaDto>
            {
                new() { Id = 7, RecorridoId = 3, Latitud = 13.7185, Longitud = -89.7240, Orden = 1, Referencia = "Punto inicial" },
                new() { Id = 8, RecorridoId = 3, Latitud = 13.7192, Longitud = -89.7250, Orden = 2, Referencia = "Segundo punto" },
                new() { Id = 9, RecorridoId = 3, Latitud = 13.7201, Longitud = -89.7260, Orden = 3, Referencia = "Tercer punto" }
            }
        },
        new RecorridoDto
        {
            Id = 4,
            Nombre = "Viernes Santo",
            Descripcion = "Recorrido procesional de Viernes Santo",
            Tipo = TipoRecorrido.ViernesSanto,
            HoraSalida = "03:00 PM",
            PuntosRuta = new List<PuntoRutaDto>
            {
                new() { Id = 10, RecorridoId = 4, Latitud = 13.7185, Longitud = -89.7240, Orden = 1, Referencia = "Punto inicial" },
                new() { Id = 11, RecorridoId = 4, Latitud = 13.7195, Longitud = -89.7235, Orden = 2, Referencia = "Segundo punto" },
                new() { Id = 12, RecorridoId = 4, Latitud = 13.7205, Longitud = -89.7228, Orden = 3, Referencia = "Tercer punto" }
            }
        }
    };

        [HttpGet]
        public ActionResult<List<RecorridoDto>> Get()
        {
            return Ok(Recorridos);
        }

        [HttpGet("{id:int}")]
        public ActionResult<RecorridoDto> GetById(int id)
        {
            var recorrido = Recorridos.FirstOrDefault(x => x.Id == id);

            if (recorrido is null)
                return NotFound();

            return Ok(recorrido);
        }
    }
}
