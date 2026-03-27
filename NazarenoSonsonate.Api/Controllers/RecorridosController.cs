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
                PuntosRuta = CrearPuntosMock(
                    13.7185, -89.7240,
                    "Catedral Santísima Trinidad",
                    "Ermita Barrio Veracruz",
                    "Colonia Landovar",
                    "8A Calle Poniente",
                    "Colonia San Rafael",
                    "Colonia San José",
                    "Megaplaza",
                    "5A. Av Sur",
                    "Colonia Lomas de San Antonio",
                    "Boulevard Acaxual",
                    "Colonia Sensunapan",
                    "C.E Dolores de Brito",
                    "Colonia Aida",
                    "Colonia Monte Rio",
                    "Hotel Plaza",
                    "Iglesia El Angel"
                )
            },
            new RecorridoDto
            {
                Id = 2,
                Nombre = "Martes Santo",
                Descripcion = "Recorrido procesional de Martes Santo",
                HoraSalida = "3:00 PM",
                Activo = true,
                PuntosRuta = CrearPuntosMock(
                    13.7195, -89.7230,
                    "Iglesia El Angel",
                    "Almacenes Bou",
                    "Mercadito El Angel",
                    "Claro",
                    "Ermita Barrio Veracruz",
                    "5A Av Sur",
                    "Colonia San Rafael",
                    "8A Calle Poniente",
                    "C.E. Republica de Haití",
                    "Calle a San Antonio del Monte",
                    "Little Caesars",
                    "BAC",
                    "Calle Obispo Marroquin",
                    "Catedral Santísima Trinidad"
                )
            },
            new RecorridoDto
            {
                Id = 3,
                Nombre = "Miércoles Santo",
                Descripcion = "Recorrido procesional de Miércoles Santo",
                HoraSalida = "2:00 PM",
                Activo = true,
                PuntosRuta = CrearPuntosMock(
                    13.7205, -89.7220,
                    "Catedral Santísima Trinidad",
                    "3 Calle Oriente",
                    "Mercado El Angel",
                    "Iglesia El Angel",
                    "Calle El Angel",
                    "Colonia Belen",
                    "24 Av. Norte",
                    "El Balsamar",
                    "5A Calle Oriente",
                    "Cancha de Fútbol",
                    "Plaza Ferroviaria",
                    "7 Calle Oriente",
                    "Colegio Centro América",
                    "Ermita San Sebastian",
                    "Redondel Col. 14 de Diciembre",
                    "Inst Thomas Jefferson",
                    "Estadio Ana Mercedes Campos",
                    "7 Calle Pte",
                    "Iglesia El Pilar"
                )
            },
            new RecorridoDto
            {
                Id = 4,
                Nombre = "Viernes Santo",
                Descripcion = "Recorrido procesional de Viernes Santo",
                HoraSalida = "6:00 AM",
                Activo = true,
                PuntosRuta = CrearPuntosMock(
                    13.7215, -89.7210,
                    "Iglesia El Pilar",
                    "Casa HJN",
                    "5 Calle Poniente",
                    "3 Calle Poniente",
                    "Hospital Dr Jorge Mazzini",
                    "Alcaldía Municipal",
                    "Claro",
                    "Ermita Veracruz",
                    "Catedral Santísima Trinidad"
                )
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
