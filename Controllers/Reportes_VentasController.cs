using ApiContabsv.Models.Contabilidad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;

namespace ApiContabsv.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class Reportes_VentasController : Controller
    {
        private readonly ContabilidadContext _context;

        public Reportes_VentasController(ContabilidadContext context)
        {
            _context = context;
        }
        [HttpGet("Reporte/Ventas")]
        public async Task<ActionResult<IEnumerable<VwReporteProfit>>> GetReporteProfit( [FromQuery] int? idCliente, [FromQuery] DateOnly? fechaInicio, [FromQuery] DateOnly? fechaFin,[FromQuery] int? idProducto,[FromQuery] string? categoria,[FromQuery] string? marca,[FromQuery] string? nombreCliente, [FromQuery] int? año, [FromQuery] int? mes)
        {
            try
            {
                var query = _context.VwReporteProfits.AsQueryable();

                // ✅ APLICAR FILTROS
                if (idCliente.HasValue)
                {
                    query = query.Where(x => x.IdCliente == idCliente.Value);
                }

                if (fechaInicio.HasValue)
                {
                    query = query.Where(x => x.FechaEmision >= fechaInicio.Value);
                }

                if (fechaFin.HasValue)
                {
                    query = query.Where(x => x.FechaEmision <= fechaFin.Value);
                }

                if (idProducto.HasValue)
                {
                    query = query.Where(x => x.IdProducto == idProducto.Value);
                }

                if (!string.IsNullOrEmpty(categoria))
                {
                    query = query.Where(x => x.Categoria != null && x.Categoria.Contains(categoria));
                }

                if (!string.IsNullOrEmpty(marca))
                {
                    query = query.Where(x => x.Marca != null && x.Marca.Contains(marca));
                }

                if (!string.IsNullOrEmpty(nombreCliente))
                {
                    query = query.Where(x => x.NombreCliente != null && x.NombreCliente.Contains(nombreCliente));
                }

                if (año.HasValue)
                {
                    query = query.Where(x => x.Año == año.Value);
                }

                if (mes.HasValue)
                {
                    query = query.Where(x => x.Mes == mes.Value);
                }

                // Ordenar por fecha más reciente
                query = query.OrderByDescending(x => x.FechaEmision)
                            .ThenByDescending(x => x.IdVenta);

                var resultado = await query.ToListAsync();
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }
    }
}
