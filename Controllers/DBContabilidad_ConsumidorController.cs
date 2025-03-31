using ApiContabsv.DTO.DB_ContabilidadDTO;
using ApiContabsv.Models.Contabilidad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;

namespace ApiContabsv.Controllers
{
    [ApiController]
    [Route("[Controller]")]
    public class DBContabilidad_ConsumidorController : ControllerBase
    {
        private readonly ContabilidadContext contabilidadContext;

        public DBContabilidad_ConsumidorController(ContabilidadContext contabilidadContext)
        {
            this.contabilidadContext = contabilidadContext;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fechaInicio"></param>
        /// <param name="fechaFin"></param>
        /// <param name="tipoFecha"></param>
        /// <param name="idCliente"></param>
        /// <returns></returns>
        [HttpGet("Consumidor")]
        [SwaggerOperation(
         Summary = "Consulta FCF en un rango de fechas y por cliente",
         Description = "Permite mostrar las facturas de consumidor final en un rango de fechas. Si tipoFecha es 1 se filtra por FechaEmision, y si es 2 se filtra por FechaPresentacion. Además, se filtra por idCliente."
        )]
        [SwaggerResponse(200, "Consulta exitosa")]
        [SwaggerResponse(400, "Solicitud incorrecta (datos inválidos)")]
        [SwaggerResponse(404, "Usuario no encontrado")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<ActionResult<List<ListConsumidor_FinalDTO>>> GetConsumidorFinal(
    [FromQuery] DateOnly fechaInicio, [FromQuery] DateOnly fechaFin, [FromQuery] int tipoFecha, [FromQuery] int idCliente)
        {
            try
            {
                // Iniciar la consulta y filtrar por idCliente
                IQueryable<VentaConsumidor> query = contabilidadContext.VentaConsumidors
                    .Where(a => a.IdCliente == idCliente);

                // Aplicar el filtro según el tipo de fecha
                if (tipoFecha == 1)
                {
                    query = query.Where(a => a.FechaEmision >= fechaInicio && a.FechaEmision <= fechaFin);
                }
                else if (tipoFecha == 2)
                {
                    query = query.Where(a => a.FechaPresentacion >= fechaInicio && a.FechaPresentacion <= fechaFin);
                }
                else
                {
                    return BadRequest("El valor de tipoFecha no es válido. Debe ser 1 o 2.");
                }

                // Opcional: implementar paginación para limitar la cantidad de registros devueltos
                // query = query.Skip((pageNumber - 1) * pageSize).Take(pageSize);

                var result = await query.Select(c => new
                {
                    c.IdVentaConsumidor,
                    c.FechaCreacion,
                    c.FechaEmision,
                    c.FechaPresentacion,
                    c.IdClaseDocumento,
                    CodigoClaseDocumento = c.IdClaseDocumentoNavigation != null ? c.IdClaseDocumentoNavigation.Codigo : null,
                    DetalleClaseDocumento = c.IdClaseDocumentoNavigation != null ? c.IdClaseDocumentoNavigation.Nombre : null,
                    c.IdTipoDocumento,
                    CodigoTipoDocumento = c.IdTipoDocumentoNavigation != null ? c.IdTipoDocumentoNavigation.Codigo : null,
                    DetalleTipoDocumento = c.IdTipoDocumentoNavigation != null ? c.IdTipoDocumentoNavigation.Nombre : null,
                    c.NumeroResolucion,
                    c.NumeroDocumento,
                    c.SerieDocumento,
                    c.NumeroControlInterno,
                    c.NumeroMaquinaRegistradora,
                    c.VentasExentas,
                    c.VentasInternasExentasNoProporcionalidad,
                    c.VentasGravadasLocales,
                    c.ExportacionesCentroamerica,
                    c.ExportacionesFueraCentroamerica,
                    c.ExportacionesServicio,
                    c.VentasZonasFrancasDpa,
                    c.VentasTercerosNoDomiciliados,
                    c.TotalVentas,
                    c.IdTipoOperacionCg,
                    CodigoTipoOperacionCg = c.IdTipoOperacionCgNavigation != null ? c.IdTipoOperacionCgNavigation.Codigo : null,
                    DetalleTipoOperacionCg = c.IdTipoOperacionCgNavigation != null ? c.IdTipoOperacionCgNavigation.Descripcion : null,
                    c.IdOperacion,
                    CodigoOperacion = c.IdOperacionNavigation != null ? c.IdOperacionNavigation.Codigo : null,
                    DetalleOperacion = c.IdOperacionNavigation != null ? c.IdOperacionNavigation.Descripcion : null,
                    c.NumeroAnexo,
                    c.Posteado,
                    c.Anulado,
                    c.Eliminado,
                    c.IdCliente
                }).ToListAsync();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.ToString()}");
            }
        }

        [HttpGet("Consumidor/{idFcf}")]
        [SwaggerOperation(
         Summary = "CONSULTA LOS DATOS DE UNA FCF EN ESPECIFICO",
         Description = "Este endpoint permite mostrar los datos de una factura de consumidor final en especifico."
        )]
        [SwaggerResponse(200, "Consulta exitosa")]
        [SwaggerResponse(400, "Solicitud incorrecta (datos inválidos)")]
        [SwaggerResponse(404, "Usuario no encontrado")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<ActionResult<ListConsumidor_FinalDTO>> GetConsumidorFinal(int idFcf)
        {
            try
            {
                var c = await contabilidadContext.VentaConsumidors
                        .Where(a => a.IdVentaConsumidor == idFcf)
                        .Select(c => new
                        {
                            c.IdVentaConsumidor,
                            c.FechaCreacion,
                            c.FechaEmision,
                            c.FechaPresentacion,
                            c.IdClaseDocumento,
                            CodigoClaseDocumento = c.IdClaseDocumentoNavigation != null ? c.IdClaseDocumentoNavigation.Codigo : null,
                            DetalleClaseDocumento = c.IdClaseDocumentoNavigation != null ? c.IdClaseDocumentoNavigation.Nombre : null,
                            c.IdTipoDocumento,
                            CodigoTipoDocumento = c.IdTipoDocumentoNavigation != null ? c.IdTipoDocumentoNavigation.Codigo : null,
                            DetalleTipoDocumento = c.IdTipoDocumentoNavigation != null ? c.IdTipoDocumentoNavigation.Nombre : null,
                            c.NumeroResolucion,
                            c.NumeroDocumento,
                            c.SerieDocumento,
                            c.NumeroControlInterno,
                            c.NumeroMaquinaRegistradora,
                            c.VentasExentas,
                            c.VentasInternasExentasNoProporcionalidad,
                            c.VentasGravadasLocales,
                            c.ExportacionesCentroamerica,
                            c.ExportacionesFueraCentroamerica,
                            c.ExportacionesServicio,
                            c.VentasZonasFrancasDpa,
                            c.VentasTercerosNoDomiciliados,
                            c.TotalVentas,
                            c.IdTipoOperacionCg,
                            CodigoTipoOperacionCg = c.IdTipoOperacionCgNavigation != null ? c.IdTipoOperacionCgNavigation.Codigo : null,
                            DetalleTipoOperacionCg = c.IdTipoOperacionCgNavigation != null ? c.IdTipoOperacionCgNavigation.Descripcion : null,
                            c.IdOperacion,
                            CodigoOperacion = c.IdOperacionNavigation != null ? c.IdOperacionNavigation.Codigo : null,
                            DetalleOperacion = c.IdOperacionNavigation != null ? c.IdOperacionNavigation.Descripcion : null,
                            c.NumeroAnexo,
                            c.Posteado,
                            c.Anulado,
                            c.Eliminado,
                            c.IdCliente
                        })
                        .FirstOrDefaultAsync();

                return Ok(c);

            }
            catch (Exception ex)
            {

                return StatusCode(500, $"Error interno: {ex.ToString()}");
            }
        }


        [HttpPost("Consumidor")]
        [SwaggerOperation(
         Summary = "CREA VENTAS A CONSUMIDOR FINAL",
         Description = "Este endpoints registra ventas en consumidor final de un cliente en especifico."
        )]
        [SwaggerResponse(200, "Consulta exitosa")]
        [SwaggerResponse(400, "Solicitud incorrecta (datos inválidos)")]
        [SwaggerResponse(404, "Usuario no encontrado")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<ActionResult<Consumidor_FinalDTO>> CreateConsumidor(Consumidor_FinalDTO c)
        {
            try
            {
                var a = new VentaConsumidor
                {
                    IdVentaConsumidor = 0,
                    FechaCreacion = DateOnly.FromDateTime(DateTime.Now),
                    FechaPresentacion = c.FechaPresentacion,
                    FechaEmision = c.FechaEmision,
                    IdClaseDocumento = c.IdClaseDocumento,
                    IdTipoDocumento = c.IdTipoDocumento,
                    NumeroResolucion = c.NumeroResolucion,
                    NumeroDocumento = c.NumeroDocumento,
                    SerieDocumento = c.SerieDocumento,
                    NumeroControlInterno = c.NumeroControlInterno,
                    NumeroMaquinaRegistradora = c.NumeroMaquinaRegistradora,
                    VentasExentas = c.VentasExentas,
                    VentasInternasExentasNoProporcionalidad = c.VentasInternasExentasNoProporcionalidad,
                    VentasNoSujetas = c.VentasNoSujetas,
                    VentasGravadasLocales = c.VentasGravadasLocales,
                    ExportacionesCentroamerica = c.ExportacionesCentroamerica,
                    ExportacionesFueraCentroamerica = c.ExportacionesFueraCentroamerica,
                    ExportacionesServicio = c.ExportacionesServicio,
                    VentasZonasFrancasDpa = c.VentasZonasFrancasDpa,
                    VentasTercerosNoDomiciliados = c.VentasTercerosNoDomiciliados,
                    TotalVentas = c.TotalVentas,
                    IdTipoOperacionCg = c.IdTipoOperacionCg,
                    IdOperacion = c.IdOperacion,
                    NumeroAnexo = c.NumeroAnexo,
                    Posteado = false,
                    Anulado = false,
                    Eliminado = false,
                    IdCliente = c.IdCliente,    
                    IdClienteCit = c.IdClienteCit
                };

                contabilidadContext.Add(a);
                await contabilidadContext.SaveChangesAsync();

                return CreatedAtAction(nameof(GetConsumidorFinal), new { idFcf = c.IdVentaConsumidor }, c);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException != null ? ex.InnerException.Message : "No hay inner exception.";
                return StatusCode(500, $"Error interno: {ex.Message}. Detalles: {inner}");
            }
        }
    }
}
