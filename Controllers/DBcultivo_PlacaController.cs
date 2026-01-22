using ApiContabsv.Models.Cultivo;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;

namespace ApiContabsv.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DBcultivo_PlacaController : ControllerBase
    {
        private readonly CultivoContext _context;

        public DBcultivo_PlacaController(CultivoContext context)
        {
            _context = context;
        }

        [HttpPost("Datos_Sensores")]
        [SwaggerOperation(
            Summary = "REGISTRO DE SENSORES",
            Description = "Este endpoint registra los datos de todos los sensores"
        )]
        [SwaggerResponse(200, "Operación exitosa")]
        [SwaggerResponse(400, "Solicitud incorrecta (datos inválidos)")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<ActionResult<SensorDatum>> CreateDatosSensores(SensorDatum sensorData)
        {
            try
            {
                // La fecha se asigna automáticamente por la BD
                _context.SensorData.Add(sensorData);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    mensaje = "Datos guardados exitosamente",
                    id = sensorData.Id,
                    timestamp = sensorData.FechaHora
                });
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException != null ? ex.InnerException.Message : "No hay inner exception.";
                return StatusCode(500, $"Error interno: {ex.Message}. Detalles: {inner}");
            }
        }

        [HttpGet("Datos_Sensores")]
        [SwaggerOperation(
            Summary = "OBTENER TODOS LOS DATOS DE SENSORES",
            Description = "Este endpoint obtiene todos los registros de sensores ordenados por fecha"
        )]
        [SwaggerResponse(200, "Operación exitosa")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<ActionResult<IEnumerable<SensorDatum>>> GetDatosSensores()
        {
            try
            {
                return await _context.SensorData
                    .OrderByDescending(x => x.FechaHora)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [HttpGet("Datos_Sensores/Ultimo")]
        [SwaggerOperation(
            Summary = "OBTENER ÚLTIMO REGISTRO DE SENSORES",
            Description = "Este endpoint obtiene el registro más reciente de sensores"
        )]
        public async Task<ActionResult<SensorDatum>> GetUltimoDato()
        {
            try
            {
                var ultimoDato = await _context.SensorData
                    .OrderByDescending(x => x.FechaHora)
                    .FirstOrDefaultAsync();

                if (ultimoDato == null)
                    return NotFound("No hay datos registrados");

                return Ok(ultimoDato);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }
    }
}