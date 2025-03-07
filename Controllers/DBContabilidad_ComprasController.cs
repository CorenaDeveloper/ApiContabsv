using ApiContabsv.Models.Contabilidad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using System.Linq.Expressions;


namespace ApiContabsv.Controllers
{
    [ApiController]
    [Route("[Controller]")]
    public class DBContabilidad_ComprasController : ControllerBase
    {
      private readonly ContabilidadContext contabilidadContext;

        public DBContabilidad_ComprasController(ContabilidadContext contabilidadContext)
        {
            this.contabilidadContext = contabilidadContext;
        }

        // 🔵 LISTAR TODAS LAS COMPRAS
        /// <summary>
        /// </summary>
        /// <param name="idCliente"></param>
        /// <param name="fechaInicio"></param>
        /// <param name="fechaFin"></param>
        /// <param name="tipoFecha">En fecha  si : 1 = FechaPresentacion, 2 = FechaEmision </param>
        /// <returns></returns>
        [HttpGet("Compras")]
        [SwaggerOperation(
         Summary = "LISTA DE COMPRA EN RANGO DE FECHA",
         Description = "Este endpoint permite listar todas la compra en un rango especifico de fechas."
        )]
        [SwaggerResponse(200, "Consulta exitosa")]
        [SwaggerResponse(400, "Solicitud incorrecta (datos inválidos)")]
        [SwaggerResponse(404, "Datos no encontrado")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<ActionResult<IEnumerable<Compra>>> GetCompras([FromQuery] int? idCliente, [FromQuery]  DateOnly? fechaInicio, [FromQuery] DateOnly? fechaFin, [FromQuery] int tipoFecha) // 1 = FechaPresentacion, 2 = FechaEmision
        {
            try
            {
                var query = contabilidadContext.Compras
                           .AsQueryable();

                if (idCliente.HasValue)
                {
                    query = query.Where(p => p.IdCliente == idCliente);
                }

                // Determinar qué campo de fecha usar según el tipoFecha
                Expression<Func<Compra, bool>> filtroFecha = _ => true; // Inicialmente sin filtro

                if (fechaInicio.HasValue || fechaFin.HasValue)
                {
                    if (tipoFecha == 1) // FechaPresentacion
                    {
                        filtroFecha = c =>
                            (!fechaInicio.HasValue || c.FechaPresentacion >= fechaInicio.Value) &&
                            (!fechaFin.HasValue || c.FechaPresentacion <= fechaFin.Value);
                    }
                    else if (tipoFecha == 2) // FechaEmision
                    {
                        filtroFecha = c =>
                            (!fechaInicio.HasValue || c.FechaEmision >= fechaInicio.Value) &&
                            (!fechaFin.HasValue || c.FechaEmision <= fechaFin.Value);
                    }

                    query = query.Where(filtroFecha);
                }


                var compras = await query.Select(c => new
                {
                    c.IdDocCompra,
                    c.FechaCreacion,
                    c.FechaEmision,
                    c.FechaPresentacion,
                    c.IdclaseDocumento,
                    c.IdtipoDocumento,
                    c.NumeroDocumento,
                    c.InternasExentas,
                    c.InternacionalesExentasYONsujetas,
                    c.ImportacionesYONsujetas,
                    c.InternasGravadas,
                    c.InternacionesGravadasBienes,
                    c.ImportacionesGravadasBienes,
                    c.ImportacionesGravadasServicios,
                    c.CreditoFiscal,
                    c.TotalCompras,
                    c.IdTipoOperacion,
                    c.IdClasificacion,
                    c.IdTipoCostoGasto,
                    c.IdSector,
                    c.NumeroAnexo,
                    c.Posteado,
                    c.Anulado,
                    c.Eliminado,
                    c.IdCliente,
                    c.Combustible,
                    c.NumSerie,
                    c.IvaRetenido,
                    c.IdProveedor,
                    RazonProveedor = c.IdProveedorNavigation != null ? c.IdProveedorNavigation.NombreRazonSocial : null,
                    NombreProveedor = c.IdProveedorNavigation != null ? c.IdProveedorNavigation.Nombres : null,
                    ApellidopProveedor = c.IdProveedorNavigation != null ? c.IdProveedorNavigation.Apellidos : null,
                    NitProveedor = c.IdProveedorNavigation != null ? c.IdProveedorNavigation.NitProveedor : null,
                    NRCProveedor = c.IdProveedorNavigation != null ? c.IdProveedorNavigation.Nrc : null,
                    CodigoClasificacion = c.IdClasificacionNavigation != null ? c.IdClasificacionNavigation.Codigo : null,
                    DescripcionClasificacion = c.IdClasificacionNavigation != null ? c.IdClasificacionNavigation.Descripcion : null,
                    CodigoClaseDocumento = c.IdclaseDocumentoNavigation != null ? c.IdclaseDocumentoNavigation.Codigo : null,
                    DescripcionClaseDocumento = c.IdclaseDocumentoNavigation != null ? c.IdclaseDocumentoNavigation.Nombre : null,
                    CodigoSectors = c.IdSectorNavigation != null ? c.IdSectorNavigation.CodigoSector : null,
                    DescripcionSector = c.IdSectorNavigation != null ? c.IdSectorNavigation.Detalle : null,
                    DescripcionTipOperacion = c.IdTipoOperacionNavigation != null ? c.IdTipoOperacionNavigation.SectorP : null,
                    CodigoOperacion = c.IdTipoOperacionNavigation != null ? c.IdTipoOperacionNavigation.Codigo : null,
                    DescripcionOperacion = c.IdTipoOperacionNavigation != null ? c.IdTipoOperacionNavigation.Descripcion : null,
                    CodigoTipoCostofasto = c.IdTipoCostoGastoNavigation != null ? c.IdTipoCostoGastoNavigation.Codigo : null,
                    DescripcionCostoGasto = c.IdTipoCostoGastoNavigation != null ? c.IdTipoCostoGastoNavigation.Descripcion : null
                })
                .ToListAsync();

                return Ok(compras);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        // 🔵 OBTENER UNA COMPRA POR ID CON SUS RELACIONES
        [HttpGet("Compras/{id}")]
        public async Task<ActionResult<Compra>> GetCompra(int id)
        {
            try
            {
                var c = await contabilidadContext.Compras
                    .FirstOrDefaultAsync(c => c.IdDocCompra == id);

                if (c == null)
                {
                    return NotFound("Compra no encontrada.");
                }

                var resultado = new
                {
                    c.IdDocCompra,
                    c.FechaCreacion,
                    c.FechaEmision,
                    c.FechaPresentacion,
                    c.IdclaseDocumento,
                    c.IdtipoDocumento,
                    c.NumeroDocumento,
                    c.InternasExentas,
                    c.InternacionalesExentasYONsujetas,
                    c.ImportacionesYONsujetas,
                    c.InternasGravadas,
                    c.InternacionesGravadasBienes,
                    c.ImportacionesGravadasBienes,
                    c.ImportacionesGravadasServicios,
                    c.CreditoFiscal,
                    c.TotalCompras,
                    c.IdTipoOperacion,
                    c.IdClasificacion,
                    c.IdTipoCostoGasto,
                    c.IdSector,
                    c.NumeroAnexo,
                    c.Posteado,
                    c.Anulado,
                    c.Eliminado,
                    c.IdCliente,
                    c.Combustible,
                    c.NumSerie,
                    c.IvaRetenido,
                    c.IdProveedor,
                    RazonProveedor = c.IdProveedorNavigation != null ? c.IdProveedorNavigation.NombreRazonSocial : null,
                    NombreProveedor = c.IdProveedorNavigation != null ? c.IdProveedorNavigation.Nombres : null,
                    ApellidopProveedor = c.IdProveedorNavigation != null ? c.IdProveedorNavigation.Apellidos : null,
                    NitProveedor = c.IdProveedorNavigation != null ? c.IdProveedorNavigation.NitProveedor : null,
                    NRCProveedor = c.IdProveedorNavigation != null ? c.IdProveedorNavigation.Nrc : null,
                    CodigoClasificacion = c.IdClasificacionNavigation != null ? c.IdClasificacionNavigation.Codigo : null,
                    DescripcionClasificacion = c.IdClasificacionNavigation != null ? c.IdClasificacionNavigation.Descripcion : null,
                    CodigoClaseDocumento = c.IdclaseDocumentoNavigation != null ? c.IdclaseDocumentoNavigation.Codigo : null,
                    DescripcionClaseDocumento = c.IdclaseDocumentoNavigation != null ? c.IdclaseDocumentoNavigation.Nombre : null,
                    CodigoSectors = c.IdSectorNavigation != null ? c.IdSectorNavigation.CodigoSector : null,
                    DescripcionSector = c.IdSectorNavigation != null ? c.IdSectorNavigation.Detalle : null,
                    DescripcionTipOperacion = c.IdTipoOperacionNavigation != null ? c.IdTipoOperacionNavigation.SectorP : null,
                    CodigoOperacion = c.IdTipoOperacionNavigation != null ? c.IdTipoOperacionNavigation.Codigo : null,
                    DescripcionOperacion = c.IdTipoOperacionNavigation != null ? c.IdTipoOperacionNavigation.Descripcion : null,
                    CodigoTipoCostofasto = c.IdTipoCostoGastoNavigation != null ? c.IdTipoCostoGastoNavigation.Codigo : null,
                    DescripcionCostoGasto = c.IdTipoCostoGastoNavigation != null ? c.IdTipoCostoGastoNavigation.Descripcion : null
                };

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        // 🔵 CREAR UNA NUEVA COMPRA
        [HttpPost("Compras")]
        public async Task<ActionResult<Compra>> CreateCompra(Compra compra)
        {
            try
            {
                compra.FechaCreacion = DateTime.Now;
                contabilidadContext.Compras.Add(compra);
                await contabilidadContext.SaveChangesAsync();

                return CreatedAtAction(nameof(GetCompra), new { id = compra.IdDocCompra }, compra);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }


        // 🔵 ACTUALIZAR UNA COMPRA
        [HttpPut("Compras")]
        public async Task<IActionResult> UpdateCompra(Compra c)
        {
            if (c.IdDocCompra == 0)
            {
                return BadRequest("El ID de la compra no coincide.");
            }

            try
            {
                // Buscar el proveedor existente en la base de datos
                var cp = await contabilidadContext.Compras.FindAsync(c.IdDocCompra);
                if (cp == null)
                {
                    return NotFound("Proveedor no encontrado.");
                }
                cp.FechaEmision = c.FechaEmision;
                cp.FechaPresentacion = c.FechaPresentacion;
                cp.IdclaseDocumento = c.IdclaseDocumento;
                cp.IdtipoDocumento = c.IdtipoDocumento;
                cp.NumeroDocumento = c.NumeroDocumento;
                cp.InternasExentas = c.InternasExentas;
                cp.InternacionalesExentasYONsujetas = c.InternacionalesExentasYONsujetas;
                cp.ImportacionesYONsujetas = c.ImportacionesYONsujetas;
                cp.InternasGravadas = c.InternasGravadas;
                cp.InternacionesGravadasBienes = c.InternacionesGravadasBienes;
                cp.ImportacionesGravadasBienes = c.ImportacionesGravadasBienes;
                cp.ImportacionesGravadasServicios = c.ImportacionesGravadasServicios;
                cp.CreditoFiscal = c.CreditoFiscal;
                cp.TotalCompras = c.TotalCompras;
                cp.IdTipoOperacion = c.IdTipoOperacion;
                cp.IdClasificacion = c.IdClasificacion; 
                cp.IdTipoCostoGasto = c.IdTipoCostoGasto;
                cp.IdSector = c.IdSector;
                cp.NumeroAnexo = c.NumeroAnexo; 
                cp.Posteado = c.Posteado;   
                cp.Anulado = c.Anulado; 
                cp.Eliminado = c.Eliminado; 
                cp.Combustible = c.Combustible; 
                cp.NumSerie = c.NumSerie;   
                cp.IvaRetenido = c.IvaRetenido; 
                cp.IdProveedor = c.IdProveedor; 
                
                await contabilidadContext.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(500, "Error de concurrencia al actualizar el proveedor.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        // 🔵 ELIMINAR UNA COMPRA
        [HttpDelete("Compras/{id}")]
        public async Task<IActionResult> DeleteCompra(int id)
        {
            try
            {
                var compra = await contabilidadContext.Compras.FindAsync(id);
                if (compra == null)
                {
                    return NotFound("Compra no encontrada.");
                }

                contabilidadContext.Compras.Remove(compra);
                await contabilidadContext.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        private bool CompraExists(int id)
        {
            return contabilidadContext.Compras.Any(e => e.IdDocCompra == id);
        }
    }
}