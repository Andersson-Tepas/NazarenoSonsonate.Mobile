using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NazarenoSonsonate.Api.Data;
using NazarenoSonsonate.Api.Models;
using NazarenoSonsonate.Shared.DTOs;
using NazarenoSonsonate.Shared.Enums;

namespace NazarenoSonsonate.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecorridosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RecorridosController(AppDbContext context)
        {
            _context = context;
        }

        // ✅ GET: api/recorridos
        [HttpGet]
        public async Task<ActionResult<List<RecorridoDto>>> Get()
        {
            var recorridos = await _context.Recorridos
                .Include(r => r.PuntosRuta)
                .ToListAsync();

            var resultado = recorridos.Select(r => new RecorridoDto
            {
                Id = r.Id,
                Nombre = r.Nombre,
                Descripcion = r.Descripcion,
                HoraSalida = r.HoraSalida,
                Activo = r.Activo,

                // 🔥 CORRECCIÓN AQUÍ
                Tipo = (TipoRecorrido)r.Tipo,

                RutaGeoJson = r.RutaGeoJson,

                PuntosRuta = r.PuntosRuta.Select(p => new PuntoRutaDto
                {
                    Id = p.Id,
                    RecorridoId = p.RecorridoId,
                    Latitud = p.Latitud,
                    Longitud = p.Longitud,
                    Orden = p.Orden,
                    Referencia = p.Referencia,
                    Grupo = p.Grupo,
                    Tipo = p.Tipo
                }).ToList()
            }).ToList();

            return Ok(resultado);
        }

        // ✅ GET: api/recorridos/1
        [HttpGet("{id:int}")]
        public async Task<ActionResult<RecorridoDto>> GetById(int id)
        {
            var r = await _context.Recorridos
                .Include(x => x.PuntosRuta)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (r == null)
                return NotFound();

            var dto = new RecorridoDto
            {
                Id = r.Id,
                Nombre = r.Nombre,
                Descripcion = r.Descripcion,
                HoraSalida = r.HoraSalida,
                Activo = r.Activo,

                // 🔥 CORRECCIÓN AQUÍ TAMBIÉN
                Tipo = (TipoRecorrido)r.Tipo,

                RutaGeoJson = r.RutaGeoJson,

                PuntosRuta = r.PuntosRuta.Select(p => new PuntoRutaDto
                {
                    Id = p.Id,
                    RecorridoId = p.RecorridoId,
                    Latitud = p.Latitud,
                    Longitud = p.Longitud,
                    Orden = p.Orden,
                    Referencia = p.Referencia,
                    Grupo = p.Grupo,
                    Tipo = p.Tipo
                }).ToList()
            };

            return Ok(dto);
        }

        // ✅ PUT: api/recorridos/1/ruta
        [HttpPut("{id:int}/ruta")]
        public async Task<IActionResult> GuardarRuta(int id, [FromBody] GuardarRutaRequest request)
        {
            var recorrido = await _context.Recorridos.FindAsync(id);

            if (recorrido == null)
                return NotFound();

            recorrido.RutaGeoJson = request.RutaGeoJson;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // ✅ PUT: api/recorridos/1/mapa
        [HttpPut("{id:int}/mapa")]
        public async Task<IActionResult> GuardarMapa(int id, [FromBody] GuardarMapaRecorridoDto request)
        {
            var recorrido = await _context.Recorridos
                .Include(r => r.PuntosRuta)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (recorrido == null)
                return NotFound();

            recorrido.RutaGeoJson = request.RutaGeoJson;

            // ❌ eliminar puntos anteriores
            _context.PuntosRuta.RemoveRange(recorrido.PuntosRuta);

            // ✅ agregar nuevos puntos
            var nuevos = request.PuntosRuta.Select((p, index) => new PuntoRuta
            {
                RecorridoId = id,
                Latitud = p.Latitud,
                Longitud = p.Longitud,
                Orden = index + 1,
                Referencia = p.Referencia,
                Grupo = p.Grupo,
                Tipo = NormalizarTipo(p.Tipo)
            });

            await _context.PuntosRuta.AddRangeAsync(nuevos);

            await _context.SaveChangesAsync();

            return NoContent();
        }

        private static string NormalizarTipo(string? tipo)
        {
            var valor = (tipo ?? "").ToLower();

            if (valor.Contains("cargadora"))
                return "Cargadora";

            return "Cargador";
        }

        public class GuardarRutaRequest
        {
            public string RutaGeoJson { get; set; } = string.Empty;
        }
    }
}