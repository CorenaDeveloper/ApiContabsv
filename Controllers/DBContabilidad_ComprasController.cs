using ApiContabsv.DTO.DB_ContabilidadDTO;
using ApiContabsv.Models.Contabilidad;
using ApiContabsv.Models.Contabsv;
using ApiContabsv.Models.Seguridad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;


namespace ApiContabsv.Controllers
{
    [ApiController]
    [Route("[Controller]")]
    public class DBContabilidad_ComprasController : ControllerBase
    {
        private readonly ContabilidadContext contabilidadContext;
        private readonly ContabsvContext contabsv_context;
        public DBContabilidad_ComprasController(ContabilidadContext contabilidadContext, ContabsvContext contabsv_context)
        {
            this.contabilidadContext = contabilidadContext; 
            this.contabsv_context = contabsv_context;
        }

        [HttpGet("Compras")]
        [SwaggerOperation(
         Summary = "LISTA DE COMPRA EN RANGO DE FECHA",
         Description = "Este endpoint permite listar todas la compra en un rango especifico de fechas y tipo 1 es fecha de presentacion y 2 fecha de Emision de documento."
        )]
        [SwaggerResponse(200, "Consulta exitosa")]
        [SwaggerResponse(400, "Solicitud incorrecta (datos inválidos)")]
        [SwaggerResponse(404, "Datos no encontrado")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<ActionResult<IEnumerable<ComprasDTO>>> GetCompras([FromQuery] int? idCliente, [FromQuery]  DateOnly? fechaInicio, [FromQuery] DateOnly? fechaFin, [FromQuery] int tipoFecha) // 1 = FechaPresentacion, 2 = FechaEmision
        {
            try
            {
                IQueryable<Compra> query = contabilidadContext.Compras
                   .Where(a => a.IdCliente == idCliente);

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

                var compras = await query                   
                    .Select(c => new
                {
                    c.IdDocCompra,
                    c.FechaCreacion,
                    c.FechaEmision,
                    c.FechaPresentacion,
                    c.IdclaseDocumento,
                    c.IdtipoDocumento,
                    DescripcionTipoDocumento = c.IdtipoDocumentoNavigation != null ? c.IdtipoDocumentoNavigation.Nombre : null,
                    CodigoTipoDocumento = c.IdtipoDocumentoNavigation != null ? c.IdtipoDocumentoNavigation.Codigo : null,
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
                    NombreComercial = c.IdProveedorNavigation != null ? c.IdProveedorNavigation.NombreComercial : null,
                    NitProveedor = c.IdProveedorNavigation != null ? c.IdProveedorNavigation.NitProveedor : null,
                    NRCProveedor = c.IdProveedorNavigation != null ? c.IdProveedorNavigation.Nrc : null,
                    Juridico = c.IdProveedorNavigation != null ? c.IdProveedorNavigation.PersonaJuridica : null,
                    CodigoClasificacion = c.IdClasificacionNavigation != null ? c.IdClasificacionNavigation.Codigo : null,
                    DescripcionClasificacion = c.IdClasificacionNavigation != null ? c.IdClasificacionNavigation.Descripcion : null,
                    CodigoClaseDocumento = c.IdclaseDocumentoNavigation != null ? c.IdclaseDocumentoNavigation.Codigo : null,
                    DescripcionClaseDocumento = c.IdclaseDocumentoNavigation != null ? c.IdclaseDocumentoNavigation.Nombre : null,
                    CodigoSectors = c.IdSectorNavigation != null ? c.IdSectorNavigation.CodigoSector : null,
                    DescripcionSector = c.IdSectorNavigation != null ? c.IdSectorNavigation.Detalle : null,
                    DescripcionTipOperacion = c.IdTipoOperacionNavigation != null ? c.IdTipoOperacionNavigation.SectorP : null,
                    CodigoOperacion = c.IdTipoOperacionNavigation != null ? c.IdTipoOperacionNavigation.Codigo : null,
                    DescripcionOperacion = c.IdTipoOperacionNavigation != null ? c.IdTipoOperacionNavigation.Descripcion : null,
                    CodigoTipoCostoGasto = c.IdTipoCostoGastoNavigation != null ? c.IdTipoCostoGastoNavigation.Codigo : null,
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


        [HttpGet("Compras/{id}")]
        [SwaggerOperation(
         Summary = "CONSULTA UNA COMPRA ",
         Description = "Este endpoint permite consultar una compra enviando su IdCompra."
        )]
        [SwaggerResponse(200, "Consulta exitosa")]
        [SwaggerResponse(400, "Solicitud incorrecta (datos inválidos)")]
        [SwaggerResponse(404, "Datos no encontrado")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<ActionResult<ComprasDTO>> GetCompra(int id)
        {
            try
            {
                var c = await contabilidadContext.Compras
                     .Include(c => c.IdProveedorNavigation)
                     .Include(c => c.IdClasificacionNavigation)
                     .Include(c => c.IdclaseDocumentoNavigation)
                     .Include(c => c.IdSectorNavigation)
                     .Include(c => c.IdTipoOperacionNavigation)
                     .Include(c => c.IdTipoCostoGastoNavigation)
                     .Include(c => c.IdtipoDocumentoNavigation)
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
                    DescripcionTipoDocumento = c.IdtipoDocumentoNavigation != null ? c.IdtipoDocumentoNavigation.Nombre : null,
                    CodigoTipoDocumento = c.IdtipoDocumentoNavigation != null ? c.IdtipoDocumentoNavigation.Codigo : null,
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
                    NombreComercial = c.IdProveedorNavigation != null ? c.IdProveedorNavigation.NombreComercial : null,
                    NitProveedor = c.IdProveedorNavigation != null ? c.IdProveedorNavigation.NitProveedor : null,
                    NRCProveedor = c.IdProveedorNavigation != null ? c.IdProveedorNavigation.Nrc : null,
                    Juridico = c.IdProveedorNavigation != null ? c.IdProveedorNavigation.PersonaJuridica : null,
                    CodigoClasificacion = c.IdClasificacionNavigation != null ? c.IdClasificacionNavigation.Codigo : null,
                    DescripcionClasificacion = c.IdClasificacionNavigation != null ? c.IdClasificacionNavigation.Descripcion : null,
                    CodigoClaseDocumento = c.IdclaseDocumentoNavigation != null ? c.IdclaseDocumentoNavigation.Codigo : null,
                    DescripcionClaseDocumento = c.IdclaseDocumentoNavigation != null ? c.IdclaseDocumentoNavigation.Nombre : null,
                    CodigoSectors = c.IdSectorNavigation != null ? c.IdSectorNavigation.CodigoSector : null,
                    DescripcionSector = c.IdSectorNavigation != null ? c.IdSectorNavigation.Detalle : null,
                    DescripcionTipOperacion = c.IdTipoOperacionNavigation != null ? c.IdTipoOperacionNavigation.SectorP : null,
                    CodigoOperacion = c.IdTipoOperacionNavigation != null ? c.IdTipoOperacionNavigation.Codigo : null,
                    DescripcionOperacion = c.IdTipoOperacionNavigation != null ? c.IdTipoOperacionNavigation.Descripcion : null,
                    CodigoTipoCostoGasto = c.IdTipoCostoGastoNavigation != null ? c.IdTipoCostoGastoNavigation.Codigo : null,
                    DescripcionCostoGasto = c.IdTipoCostoGastoNavigation != null ? c.IdTipoCostoGastoNavigation.Descripcion : null
                };

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [HttpPost("Compras")]
        [SwaggerOperation(
         Summary = "CREAR UNA COMPRA",
         Description = "Este endpoint permite crear una compra."
        )]
        [SwaggerResponse(200, "Creacion Exitosa")]
        [SwaggerResponse(400, "Solicitud incorrecta (datos inválidos)")]
        [SwaggerResponse(404, "Datos no encontrado")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<ActionResult<CreateComprasDTO>> CreateCompra(CreateComprasDTO add)
        {
            try
            {
                var c = new Compra
                {
                    FechaCreacion = DateTime.Now,
                    FechaEmision = add.FechaEmision,
                    FechaPresentacion = add.FechaPresentacion,
                    IdclaseDocumento = add.IdClaseDocumento,
                    IdtipoDocumento = add.IdTipoDocumento,
                    NumeroDocumento = add.NumeroDocumento,
                    InternasExentas = add.InternasExentas,
                    InternacionalesExentasYONsujetas = add.InternacionalesExentasYONsujetas,
                    ImportacionesYONsujetas = add.ImportacionesYONsujetas,
                    InternasGravadas = add.InternasGravadas,
                    InternacionesGravadasBienes = add.InternacionesGravadasBienes,
                    ImportacionesGravadasBienes = add.ImportacionesGravadasBienes,
                    ImportacionesGravadasServicios = add.ImportacionesGravadasServicios,
                    CreditoFiscal = add.CreditoFiscal,
                    TotalCompras = add.TotalCompras,   
                    IdTipoOperacion = add.IdTipoOperacion,
                    IdClasificacion = add.IdClasificacion,
                    IdTipoCostoGasto = add.IdTipoCostoGasto,
                    IdSector = add.IdSector,
                    NumeroAnexo = add.NumeroAnexo,
                    Posteado = false,
                    Anulado = false,    
                    Eliminado = false,  
                    IdCliente = add.IdCliente,
                    Combustible = add.Combustible,  
                    NumSerie = add.NumSerie,    
                    IvaRetenido = add.IvaRetenido,
                    IdProveedor = add.IdProveedor
                };

                contabilidadContext.Compras.Add(c);
                await contabilidadContext.SaveChangesAsync();

                return CreatedAtAction(nameof(GetCompra), new { id = c.IdDocCompra }, c);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }


       
        [HttpPut("Compras")]
        [SwaggerOperation(
         Summary = "MODIFICA UNA COMPRA",
         Description = "Este endpoint permite modificar una compra mediante su id."
        )]
        [SwaggerResponse(200, "Creacion Exitosa")]
        [SwaggerResponse(400, "Solicitud incorrecta (datos inválidos)")]
        [SwaggerResponse(404, "Datos no encontrado")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<IActionResult> UpdateCompra(UpdateComprasDTO c)
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
                cp.IdclaseDocumento = c.IdClaseDocumento;
                cp.IdtipoDocumento = c.IdTipoDocumento;
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


        [HttpPut("Compras/Postear")]
        [SwaggerOperation(
         Summary = "POSTEA COMPRAS",
         Description = "Este endpoint permite posterar una o lista de  mediante su id."
        )]
        [SwaggerResponse(200, "Operación Exitosa")]
        [SwaggerResponse(400, "Solicitud incorrecta (datos inválidos)")]
        [SwaggerResponse(404, "Datos no encontrado")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<IActionResult> PostearCompra([FromBody] PostearComprasDTO c)
        {
            if (c.IdsCompra == null || !c.IdsCompra.Any())
            {
                return BadRequest("Debe proporcionar al menos un ID de compra.");
            }

            try
            {
                var compras = await contabilidadContext.Compras
                    .Where(x => c.IdsCompra.Contains(x.IdDocCompra))
                    .ToListAsync();

                if (compras.Count == 0)
                {
                    return NotFound("No se encontraron las compras especificadas.");
                }

                foreach (var compra in compras)
                {
                    compra.Posteado = !compra.Posteado;
                }

                await contabilidadContext.SaveChangesAsync();
                return Ok("Cambios realizados correctamente.");
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(500, "Error de concurrencia al actualizar los datos.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [HttpDelete("Compras/{id}")]
        [SwaggerOperation(
         Summary = "ELIMINA UNA COMPRA",
         Description = "Este endpoint permite eliminar un compra en caso sea necesario."
        )]
        [SwaggerResponse(200, "Creacion Exitosa")]
        [SwaggerResponse(400, "Solicitud incorrecta (datos inválidos)")]
        [SwaggerResponse(404, "Datos no encontrado")]
        [SwaggerResponse(500, "Error interno del servidor")]
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