using Microsoft.AspNetCore.Mvc;
using NazarenoSonsonate.Shared.DTOs;
using NazarenoSonsonate.Shared.Enums;
using System.Text.Json;

namespace NazarenoSonsonate.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecorridosController : ControllerBase
    {
        private readonly string _filePath;
        private static readonly object _fileLock = new();

        public RecorridosController(IWebHostEnvironment env)
        {
            var dataFolder = Path.Combine(env.ContentRootPath, "Data");
            Directory.CreateDirectory(dataFolder);

            _filePath = Path.Combine(dataFolder, "recorridos.json");

            if (!System.IO.File.Exists(_filePath))
            {
                var iniciales = ObtenerRecorridosIniciales();
                var json = JsonSerializer.Serialize(iniciales, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                System.IO.File.WriteAllText(_filePath, json);
            }
        }

        [HttpGet]
        public ActionResult<List<RecorridoDto>> Get()
        {
            var recorridos = LeerRecorridos();
            return Ok(recorridos);
        }

        [HttpGet("{id:int}")]
        public ActionResult<RecorridoDto> GetById(int id)
        {
            var recorridos = LeerRecorridos();
            var recorrido = recorridos.FirstOrDefault(x => x.Id == id);

            if (recorrido is null)
                return NotFound();

            return Ok(recorrido);
        }

        [HttpPut("{id:int}/ruta")]
        public IActionResult GuardarRuta(int id, [FromBody] GuardarRutaRequest request)
        {
            var recorridos = LeerRecorridos();
            var recorrido = recorridos.FirstOrDefault(x => x.Id == id);

            if (recorrido is null)
                return NotFound();

            recorrido.RutaGeoJson = request.RutaGeoJson;
            GuardarRecorridos(recorridos);

            return NoContent();
        }

        [HttpPut("{id:int}/mapa")]
        public IActionResult GuardarMapa(int id, [FromBody] GuardarMapaRecorridoDto request)
        {
            var recorridos = LeerRecorridos();
            var recorrido = recorridos.FirstOrDefault(x => x.Id == id);

            if (recorrido is null)
                return NotFound();

            recorrido.RutaGeoJson = request.RutaGeoJson;
            recorrido.PuntosRuta = request.PuntosRuta ?? new List<PuntoRutaDto>();

            for (int i = 0; i < recorrido.PuntosRuta.Count; i++)
            {
                recorrido.PuntosRuta[i].RecorridoId = id;
                recorrido.PuntosRuta[i].Orden = i + 1;
            }

            GuardarRecorridos(recorridos);

            return NoContent();
        }

        private List<RecorridoDto> LeerRecorridos()
        {
            lock (_fileLock)
            {
                if (!System.IO.File.Exists(_filePath))
                    return ObtenerRecorridosIniciales();

                using var stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream);
                var json = reader.ReadToEnd();

                if (string.IsNullOrWhiteSpace(json))
                    return ObtenerRecorridosIniciales();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                return JsonSerializer.Deserialize<List<RecorridoDto>>(json, options)
                       ?? ObtenerRecorridosIniciales();
            }
        }

        private void GuardarRecorridos(List<RecorridoDto> recorridos)
        {
            lock (_fileLock)
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                var json = JsonSerializer.Serialize(recorridos, options);

                using var stream = new FileStream(_filePath, FileMode.Create, FileAccess.Write, FileShare.None);
                using var writer = new StreamWriter(stream);
                writer.Write(json);
            }
        }

        private static List<RecorridoDto> ObtenerRecorridosIniciales()
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
                    PuntosRuta = new(),
                    RutaGeoJson = null
                },
                new RecorridoDto
                {
                    Id = 2,
                    Nombre = "Martes Santo",
                    Descripcion = "Recorrido procesional de Martes Santo",
                    HoraSalida = "3:00 PM",
                    Activo = true,
                    Tipo = TipoRecorrido.MartesSanto,
                    PuntosRuta = new(),
                    RutaGeoJson = null
                },
                new RecorridoDto
                {
                    Id = 3,
                    Nombre = "Miércoles Santo",
                    Descripcion = "Recorrido procesional de Miércoles Santo",
                    HoraSalida = "2:00 PM",
                    Activo = true,
                    Tipo = TipoRecorrido.MiercolesSanto,
                    PuntosRuta = new(),
                    RutaGeoJson = null
                },
                new RecorridoDto
                {
                    Id = 4,
                    Nombre = "Viernes Santo",
                    Descripcion = "Recorrido procesional de Viernes Santo",
                    HoraSalida = "6:00 AM",
                    Activo = true,
                    Tipo = TipoRecorrido.ViernesSanto,
                    PuntosRuta = new(),
                    RutaGeoJson = null
                }
            };
        }

        public class GuardarRutaRequest
        {
            public string RutaGeoJson { get; set; } = string.Empty;
        }
    }
}