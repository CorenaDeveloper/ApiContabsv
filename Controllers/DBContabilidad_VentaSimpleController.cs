using ApiContabsv.DTO.DB_ContabilidadDTO;
using ApiContabsv.Models.Contabilidad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace ApiContabsv.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DBContabilidad_VentaSimpleController : Controller
    {
        private readonly ContabilidadContext _context;

        public DBContabilidad_VentaSimpleController(ContabilidadContext context)
        {
            _context = context;
        }

        [HttpPost("Venta_Simple")]
        public async Task<IActionResult> AgregarVentaAsync([FromBody] object json)
        {
            try
            {
                var jsonInput = new SqlParameter
                {
                    ParameterName = "@jsonInput",
                    SqlDbType = SqlDbType.NVarChar,
                    Size = -1,
                    Value = json.ToString()
                };

                var jsonOutput = new SqlParameter
                {
                    ParameterName = "@jsonOutput",
                    SqlDbType = SqlDbType.NVarChar,
                    Size = -1,
                    Direction = ParameterDirection.Output
                };

                await _context.Database.ExecuteSqlRawAsync(
                   "EXEC [dbo].[sp_RegistrarVentaSimple] @jsonInput, @jsonOutput OUTPUT",
                   jsonInput, jsonOutput
                );

                var jsonResult = jsonOutput.Value?.ToString();

                if (!string.IsNullOrEmpty(jsonResult))
                {
                    return Content(jsonResult, "application/json");
                }
                else
                {
                    return StatusCode(500, new { message = "Error al procesar la solicitud" });
                }

            }
            catch (Exception ex)
            {

                return StatusCode(400, new { message = $"Error al agregar empleado: {ex.Message}" });
            }

        }

        [HttpPost("Venta_Simple/Revertir")]
        public async Task<IActionResult> RevertirVentaAsync([FromBody] object json)
        {
            try
            {
                var jsonInput = new SqlParameter
                {
                    ParameterName = "@jsonInput",
                    SqlDbType = SqlDbType.NVarChar,
                    Size = -1,
                    Value = json.ToString()
                };

                var jsonOutput = new SqlParameter
                {
                    ParameterName = "@jsonOutput",
                    SqlDbType = SqlDbType.NVarChar,
                    Size = -1,
                    Direction = ParameterDirection.Output
                };

                await _context.Database.ExecuteSqlRawAsync(
                   "EXEC [dbo].[sp_AnularVentaSimple] @jsonInput, @jsonOutput OUTPUT",
                   jsonInput, jsonOutput
                );

                var jsonResult = jsonOutput.Value?.ToString();

                if (!string.IsNullOrEmpty(jsonResult))
                {
                    return Content(jsonResult, "application/json");
                }
                else
                {
                    return StatusCode(500, new { message = "Error al procesar la solicitud" });
                }

            }
            catch (Exception ex)
            {

                return StatusCode(400, new { message = $"Error al agregar empleado: {ex.Message}" });
            }

        }

        [HttpPost("Venta_Simple/Listar")]
        public async Task<IActionResult> ListarVentasAsync([FromBody] object json)
        {
            try
            {
                var jsonInput = new SqlParameter
                {
                    ParameterName = "@jsonInput",
                    SqlDbType = SqlDbType.NVarChar,
                    Size = -1,
                    Value = json.ToString()
                };

                var jsonOutput = new SqlParameter
                {
                    ParameterName = "@jsonOutput",
                    SqlDbType = SqlDbType.NVarChar,
                    Size = -1,
                    Direction = ParameterDirection.Output
                };

                await _context.Database.ExecuteSqlRawAsync(
                   "EXEC [dbo].[sp_ListarVentas] @jsonInput, @jsonOutput OUTPUT",
                   jsonInput, jsonOutput
                );

                var jsonResult = jsonOutput.Value?.ToString();

                if (!string.IsNullOrEmpty(jsonResult))
                {
                    return Content(jsonResult, "application/json");
                }
                else
                {
                    return StatusCode(500, new { message = "Error al procesar la solicitud" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = $"Error al listar ventas: {ex.Message}" });
            }
        }
    }
}
//{
//   "idCliente": 1,
//  "numeroDocumento": "TK-00001",
//  "nombreCliente": "Cliente General",
//  "formaPago": "Efectivo",
//  "usuarioRegistro": "admin@contabsv.com",
//  "observaciones": "Venta de mostrador",
//  "productos": [
//    {
//        "idProducto": 1,
//      "codigoProducto": "LAV-001",
//      "sku": "LAV-SAM-18",
//      "descripcion": "Lavadora Samsung 18Kg",
//      "cantidad": 2,
//      "precioUnitario": 550.00,
//      "costoUnitario": 400.00
//    },
//    {
//        "idProducto": 2,
//      "codigoProducto": "REF-001",
//      "sku": "REF-LG-20",
//      "descripcion": "Refrigerador LG 20 pies",
//      "cantidad": 1,
//      "precioUnitario": 850.00,
//      "costoUnitario": 650.00
//    }
//  ]
//}