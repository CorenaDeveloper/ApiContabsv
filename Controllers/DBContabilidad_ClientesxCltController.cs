using ApiContabsv.Models.Contabilidad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiContabsv.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DBContabilidad_ClientexCltsController : ControllerBase
    {
        private readonly ContabilidadContext _context;

        public DBContabilidad_ClientexCltsController(ContabilidadContext context)
        {
            _context = context;
        }
        // 🔵 LISTAR CLIENTES FILTRADOS POR IdCliente
        [HttpGet("ClientexClts")]
        public async Task<ActionResult<IEnumerable<ClientexClt>>> GetClientexClts([FromQuery] int? idCliente)
        {
            try
            {
                var query = _context.ClientexClts.AsQueryable();

                if (idCliente.HasValue)
                {
                    query = query.Where(c => c.IdCliente == idCliente);
                }

                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        // 🔵 OBTENER UN CLIENTE POR ID
        [HttpGet("ClientexClts/{id}")]
        public async Task<ActionResult<ClientexClt>> GetCliente(int id)
        {
            try
            {
                var cliente = await _context.ClientexClts.FindAsync(id);
                if (cliente == null)
                {
                    return NotFound("Cliente no encontrado.");
                }
                return Ok(cliente);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        // 🔵 CREAR UN NUEVO CLIENTE
        [HttpPost("ClientexClts")]
        public async Task<ActionResult<ClientexClt>> CreateCliente(ClientexClt cliente)
        {
            try
            {
                _context.ClientexClts.Add(cliente);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetCliente), new { id = cliente.IdClienteClt }, cliente);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        // 🔵 ACTUALIZAR UN CLIENTE
        [HttpPut("ClientexClts")]
        public async Task<IActionResult> UpdateCliente(ClientexClt c)
        {
            if (c.IdClienteClt == 0)
            {
                return BadRequest("El ID del proveedor es inválido.");
            }

            try
            {
                // Buscar el proveedor existente en la base de datos
                var cp = await _context.ClientexClts.FindAsync(c.IdClienteClt);
                if (cp == null)
                {
                    return NotFound("Proveedor no encontrado.");
                }

                // Actualizar las propiedades permitidas
                cp.Nombres = c.Nombres;
                cp.Apellidos = c.Apellidos;
                cp.PersonaJuridica = c.PersonaJuridica;
                cp.NombreRazonSocial = c.NombreRazonSocial;
                cp.NombreComercial = c.NombreComercial;
                cp.DuiCliente = c.DuiCliente;
                cp.RepresentanteLegal = c.RepresentanteLegal;
                cp.DuiRepresentanteLegal = c.DuiRepresentanteLegal;
                cp.TelefonoCliente = c.TelefonoCliente;
                cp.Celular = c.Celular;
                cp.Nrc = c.Nrc;
                cp.NitCliente = c.NitCliente;
                cp.TipoContribuyente = c.TipoContribuyente;

                // Guardar los cambios en la base de datos
                await _context.SaveChangesAsync();
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

        // 🔵 ELIMINAR UN CLIENTE
        [HttpDelete("ClientexClts/{id}")]
        public async Task<IActionResult> DeleteCliente(int id)
        {
            try
            {
                var cliente = await _context.ClientexClts.FindAsync(id);
                if (cliente == null)
                {
                    return NotFound("Cliente no encontrado.");
                }

                _context.ClientexClts.Remove(cliente);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        private bool ClienteExists(int id)
        {
            return _context.ClientexClts.Any(e => e.IdClienteClt == id);
        }
    }
}

