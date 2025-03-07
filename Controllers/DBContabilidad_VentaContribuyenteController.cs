using ApiContabsv.DTO.DB_ContabilidadDTO;
using ApiContabsv.Models.Contabilidad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;

namespace ApiContabsv.Controllers
{
    [ApiController]
    [Route("[Controller]")]
    public class DBContabilidad_VentaContribuyenteController : ControllerBase
    {
        private readonly ContabilidadContext contabilidadContext;

        public DBContabilidad_VentaContribuyenteController(ContabilidadContext contabilidadContext)
        {
            this.contabilidadContext = contabilidadContext;
        }

        [HttpGet("Contribuyente/{idCCF}")]
        [SwaggerOperation(
         Summary = "CONSULTA LOS DATOS DE UNA FCF EN ESPECIFICO",
         Description = "Este endpoint permite mostrar los datos de una CCF en especifico."
        )]
        [SwaggerResponse(200, "Consulta exitosa")]
        [SwaggerResponse(400, "Solicitud incorrecta (datos inválidos)")]
        [SwaggerResponse(404, "Usuario no encontrado")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<ActionResult<ListVentaContribuyenteDTO>> GetContribuyente(int idCCF)
        {
            try
            {
                var c = await contabilidadContext.VentaContribuyentes
                        .Where(a => a.IdVentaContribuyentes == idCCF)
                        .Select(c => new
                        {
                            c.IdVentaContribuyentes,
                            c.FechaCreacion,
                            c.FechaEmisionDocumento,
                            c.FechaPresentacion,
                            c.IdClaseDocumento,
                            CodigoClaseDocumento = c.IdClaseDocumentoNavigation != null ? c.IdClaseDocumentoNavigation.Codigo : null,
                            DetalleClaseDocumento = c.IdClaseDocumentoNavigation != null ? c.IdClaseDocumentoNavigation.Nombre : null,
                            c.IdTipoDocumento,
                            CodigoTipoDocumento = c.IdTipoDocumentoNavigation != null ? c.IdTipoDocumentoNavigation.Codigo : null,
                            DetalleTipoDocumento = c.IdTipoDocumentoNavigation != null ? c.IdTipoDocumentoNavigation.Nombre : null,
                            c.NumeroResolucion,
                            c.SerieDocumento,
                            c.NumeroDocumento,
                            c.NumeroControlInterno,
                            c.VentasExentas,
                            c.VentasNoSujetas,
                            c.VentasGravadasLocales,
                            c.DebitoFiscal,
                            c.VentasTercerosNoDomiciliados,
                            c.DebitoFiscalVentasTerceros,
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
                            c.IdCliente,
                            c.IdClienteCit,
                            nit = c.IdClienteCitNavigation != null ? c.IdClienteCitNavigation.NitCliente : null
                        })
                        .FirstOrDefaultAsync();

                return Ok(c);

            }
            catch (Exception ex)
            {

                return StatusCode(500, $"Error interno: {ex.ToString()}");
            }
        }


        [HttpPost("Contribuyente")]
        [SwaggerOperation(
        Summary = "CREA VENTAS A CONTRIBUYENTE FINAL",
        Description = "Este endpoints registra ventas en contribuyente de un cliente en especifico."
       )]
        [SwaggerResponse(200, "Consulta exitosa")]
        [SwaggerResponse(400, "Solicitud incorrecta (datos inválidos)")]
        [SwaggerResponse(404, "Usuario no encontrado")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<ActionResult<VentaContribuyenteDTO>> CreateContribuyente(VentaContribuyenteDTO c)
        {
            try
            {
                var a = new VentaContribuyente
                {
                    IdVentaContribuyentes = 0, 
                    FechaCreacion = DateOnly.FromDateTime(DateTime.Now), 
                    FechaPresentacion = c.FechaPresentacion, 
                    FechaEmisionDocumento = c.FechaEmisionDocumento, 
                    IdClaseDocumento = c.IdClaseDocumento, 
                    IdTipoDocumento = c.IdTipoDocumento, 
                    NumeroResolucion = c.NumeroResolucion, 
                    SerieDocumento = c.SerieDocumento, 
                    NumeroDocumento = c.NumeroDocumento, 
                    NumeroControlInterno = c.NumeroControlInterno, 
                    VentasExentas = c.VentasExentas, 
                    VentasNoSujetas = c.VentasNoSujetas, 
                    VentasGravadasLocales = c.VentasGravadasLocales, 
                    DebitoFiscal = c.DebitoFiscal, 
                    VentasTercerosNoDomiciliados = c.VentasTercerosNoDomiciliados, 
                    DebitoFiscalVentasTerceros = c.DebitoFiscalVentasTerceros, 
                    TotalVentas = c.TotalVentas, 
                    IdTipoOperacionCg = c.IdTipoOperacionCg, 
                    IdOperacion = c.IdOperacion, 
                    NumeroAnexo = 1, 
                    Posteado = false,
                    Anulado = false,
                    Eliminado = false,
                    IdCliente = c.IdCliente, 
                    IdClienteCit = c.IdClienteCit 
                };

                contabilidadContext.Add(a);
                await contabilidadContext.SaveChangesAsync();

                return CreatedAtAction(nameof(GetContribuyente), new { idCCF = c.IdVentaContribuyentes }, c);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException != null ? ex.InnerException.Message : "No hay inner exception.";
                return StatusCode(500, $"Error interno: {ex.Message}. Detalles: {inner}");
            }
        }
    }
}
