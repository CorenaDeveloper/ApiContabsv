using ApiContabsv.Models.Contabsv;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiContabsv.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DBContabsv_ClientesController : ControllerBase
    {
        private readonly ContabsvContext contabsv_context;

        public DBContabsv_ClientesController(ContabsvContext context)
        {
            contabsv_context = context;
        }


        // 🔵 LISTAR TODOS LOS CLIENTES
        [HttpGet("Clientes")]
        public async Task<ActionResult<IEnumerable<Cliente>>> GetClientes()
        {
            try
            {
                return await contabsv_context.Clientes
               .ToListAsync();
            }
            catch (Exception ex)
            {

                return StatusCode(500, $"Error interno: {ex.Message}");
            }
           
        }

        // 🔵 CLIENTE POR ID
        [HttpGet("Clientes/{id}")]
        public async Task<ActionResult<Cliente>> GetCliente(int id)
        {
            try
            {
                var result = await contabsv_context.Clientes
                            .FirstOrDefaultAsync(a => a.IdCliente == id);

                return Ok(result);
            }
            catch (Exception ex)
            {

                return StatusCode(500, $"Error interno: {ex.Message}");
            }

        }

        // ⚫ CREAR CLIENTE (POST)
        [HttpPost("Clientes")]
        public async Task<ActionResult<Cliente>> PostCliente(Cliente cliente)
        {
            try
            {
                contabsv_context.Clientes.Add(cliente);
                await contabsv_context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetCliente), new { id = cliente.IdCliente }, cliente);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        // ⚫ ACTUALIZAR CLIENTE (PUT)
        [HttpPut("Clientes/{id}")]
        public async Task<IActionResult> PutCliente(int id, Cliente cliente)
        {
            if (id != cliente.IdCliente)
            {
                return BadRequest("El ID del cliente no coincide.");
            }
            try
            {
                contabsv_context.Entry(cliente).State = EntityState.Modified;
                await contabsv_context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ClienteExists(id))
                {
                    return NotFound("Cliente no encontrado.");
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        // ⚫ ELIMINAR CLIENTE (DELETE)
        [HttpDelete("Clientes/{id}")]
        public async Task<IActionResult> DeleteCliente(int id)
        {
            try
            {
                var cliente = await contabsv_context.Clientes.FindAsync(id);
                if (cliente == null)
                {
                    return NotFound("Cliente no encontrado.");
                }

                contabsv_context.Clientes.Remove(cliente);
                await contabsv_context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        private bool ClienteExists(int id)
        {
            return contabsv_context.Clientes.Any(e => e.IdCliente == id);
        }
    }
}
