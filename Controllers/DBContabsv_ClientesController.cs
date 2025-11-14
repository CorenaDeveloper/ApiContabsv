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
                cliente.FechaRegistro = DateTime.Now;
                cliente.EstadoCliente = "Activo";
                contabsv_context.Clientes.Add(cliente);
                await contabsv_context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetCliente), new { id = cliente.IdCliente }, cliente);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="clienteActualizado"></param>
        /// <returns></returns>
        [HttpPut("Clientes/{id}")]
        public async Task<IActionResult> PutCliente(int id, Cliente clienteActualizado)
        {
            if (id != clienteActualizado.IdCliente)
            {
                return BadRequest("El ID del cliente no coincide.");
            }

            try
            {
                // Obtener el cliente existente
                var clienteExistente = await contabsv_context.Clientes.FindAsync(id);
                if (clienteExistente == null)
                {
                    return NotFound("Cliente no encontrado.");
                }

                // Actualizar solo los campos que vienen en el request (no nulls)
                clienteExistente.Nombres = clienteActualizado.Nombres ?? clienteExistente.Nombres;
                clienteExistente.Apellidos = clienteActualizado.Apellidos ?? clienteExistente.Apellidos;
                clienteExistente.PersonaJuridica = clienteActualizado.PersonaJuridica;
                clienteExistente.NombreRazonSocial = clienteActualizado.NombreRazonSocial ?? clienteExistente.NombreRazonSocial;
                clienteExistente.NombreComercial = clienteActualizado.NombreComercial ?? clienteExistente.NombreComercial;
                clienteExistente.DuiCliente = clienteActualizado.DuiCliente ?? clienteExistente.DuiCliente;
                clienteExistente.RepresentanteLegal = clienteActualizado.RepresentanteLegal ?? clienteExistente.RepresentanteLegal;
                clienteExistente.DuiRepresentanteLegal = clienteActualizado.DuiRepresentanteLegal ?? clienteExistente.DuiRepresentanteLegal;
                clienteExistente.TelefonoCliente = clienteActualizado.TelefonoCliente ?? clienteExistente.TelefonoCliente;
                clienteExistente.Celular = clienteActualizado.Celular ?? clienteExistente.Celular;
                clienteExistente.Nrc = clienteActualizado.Nrc ?? clienteExistente.Nrc;
                clienteExistente.NitCliente = clienteActualizado.NitCliente ?? clienteExistente.NitCliente;
                clienteExistente.Direccion = clienteActualizado.Direccion ?? clienteExistente.Direccion;
                clienteExistente.Correo = clienteActualizado.Correo ?? clienteExistente.Correo;
                clienteExistente.TipoContribuyente = clienteActualizado.TipoContribuyente ?? clienteExistente.TipoContribuyente;
                clienteExistente.IdActividadEconomica = clienteActualizado.IdActividadEconomica ?? clienteExistente.IdActividadEconomica;
                clienteExistente.IdDepartamento = clienteActualizado.IdDepartamento ?? clienteExistente.IdDepartamento;
                clienteExistente.IdMunicipio = clienteActualizado.IdMunicipio ?? clienteExistente.IdMunicipio;
                clienteExistente.ApiKey = clienteActualizado.ApiKey ?? clienteExistente.ApiKey;
                clienteExistente.ApiSecret = clienteActualizado.ApiSecret ?? clienteExistente.ApiSecret;
                clienteExistente.UserHacienda = clienteActualizado.UserHacienda ?? clienteExistente.UserHacienda;
                clienteExistente.PassHacienda = clienteActualizado.PassHacienda ?? clienteExistente.PassHacienda;

                // Campos boolean - actualizar solo si vienen definidos
                if (clienteActualizado.IsDeclarante != null)
                    clienteExistente.IsDeclarante = clienteActualizado.IsDeclarante;
                if (clienteActualizado.IsDte != null)
                    clienteExistente.IsDte = clienteActualizado.IsDte;

                // NO tocar: Token, FechaRegistro, EstadoCliente

                await contabsv_context.SaveChangesAsync();
                return NoContent();
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
