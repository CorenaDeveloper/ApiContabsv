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


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cliente"></param>
        /// <returns></returns>
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

                // Actualizar solo los campos que vienen en el request (no nulls ni vacíos)
                clienteExistente.Nombres = !string.IsNullOrEmpty(clienteActualizado.Nombres) ? clienteActualizado.Nombres : clienteExistente.Nombres;
                clienteExistente.Apellidos = !string.IsNullOrEmpty(clienteActualizado.Apellidos) ? clienteActualizado.Apellidos : clienteExistente.Apellidos;
                clienteExistente.PersonaJuridica = clienteActualizado.PersonaJuridica;
                clienteExistente.NombreRazonSocial = !string.IsNullOrEmpty(clienteActualizado.NombreRazonSocial) ? clienteActualizado.NombreRazonSocial : clienteExistente.NombreRazonSocial;
                clienteExistente.NombreComercial = !string.IsNullOrEmpty(clienteActualizado.NombreComercial) ? clienteActualizado.NombreComercial : clienteExistente.NombreComercial;
                clienteExistente.DuiCliente = !string.IsNullOrEmpty(clienteActualizado.DuiCliente) ? clienteActualizado.DuiCliente : clienteExistente.DuiCliente;
                clienteExistente.RepresentanteLegal = !string.IsNullOrEmpty(clienteActualizado.RepresentanteLegal) ? clienteActualizado.RepresentanteLegal : clienteExistente.RepresentanteLegal;
                clienteExistente.DuiRepresentanteLegal = !string.IsNullOrEmpty(clienteActualizado.DuiRepresentanteLegal) ? clienteActualizado.DuiRepresentanteLegal : clienteExistente.DuiRepresentanteLegal;
                clienteExistente.TelefonoCliente = !string.IsNullOrEmpty(clienteActualizado.TelefonoCliente) ? clienteActualizado.TelefonoCliente : clienteExistente.TelefonoCliente;
                clienteExistente.Celular = !string.IsNullOrEmpty(clienteActualizado.Celular) ? clienteActualizado.Celular : clienteExistente.Celular;
                clienteExistente.Nrc = !string.IsNullOrEmpty(clienteActualizado.Nrc) ? clienteActualizado.Nrc : clienteExistente.Nrc;
                clienteExistente.NitCliente = !string.IsNullOrEmpty(clienteActualizado.NitCliente) ? clienteActualizado.NitCliente : clienteExistente.NitCliente;
                clienteExistente.Direccion = !string.IsNullOrEmpty(clienteActualizado.Direccion) ? clienteActualizado.Direccion : clienteExistente.Direccion;
                clienteExistente.Correo = !string.IsNullOrEmpty(clienteActualizado.Correo) ? clienteActualizado.Correo : clienteExistente.Correo;
                clienteExistente.TipoContribuyente = !string.IsNullOrEmpty(clienteActualizado.TipoContribuyente) ? clienteActualizado.TipoContribuyente : clienteExistente.TipoContribuyente;
                clienteExistente.IdActividadEconomica = clienteActualizado.IdActividadEconomica ?? clienteExistente.IdActividadEconomica;
                clienteExistente.IdDepartamento = clienteActualizado.IdDepartamento ?? clienteExistente.IdDepartamento;
                clienteExistente.IdMunicipio = clienteActualizado.IdMunicipio ?? clienteExistente.IdMunicipio;
                clienteExistente.ApiKey = !string.IsNullOrEmpty(clienteActualizado.ApiKey) ? clienteActualizado.ApiKey : clienteExistente.ApiKey;
                clienteExistente.ApiSecret = !string.IsNullOrEmpty(clienteActualizado.ApiSecret) ? clienteActualizado.ApiSecret : clienteExistente.ApiSecret;
                clienteExistente.UserHacienda = !string.IsNullOrEmpty(clienteActualizado.UserHacienda) ? clienteActualizado.UserHacienda : clienteExistente.UserHacienda;
                clienteExistente.PassHacienda = !string.IsNullOrEmpty(clienteActualizado.PassHacienda) ? clienteActualizado.PassHacienda : clienteExistente.PassHacienda;
                clienteExistente.Ambiente = !string.IsNullOrEmpty(clienteActualizado.Ambiente) ? clienteActualizado.Ambiente : clienteExistente.Ambiente;
                clienteExistente.UserDte = clienteActualizado.UserDte ?? clienteExistente.UserDte;  
                clienteExistente.Imagen = !string.IsNullOrEmpty(clienteActualizado.Imagen) ? clienteActualizado.Imagen : clienteExistente.Imagen;
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
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
    }
}
