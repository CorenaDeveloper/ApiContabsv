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
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="idCliente"></param>
        /// <returns></returns>
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

                var lista = await query
                    .Select(m => new
                    {
                        m.IdCliente,
                        m.IdClienteClt,
                        m.Nombres,
                        m.Apellidos,
                        m.PersonaJuridica,
                        m.NombreRazonSocial,
                        m.NombreComercial,
                        m.DuiCliente,
                        m.RepresentanteLegal,
                        m.DuiRepresentanteLegal,
                        m.TelefonoCliente,
                        m.Celular,
                        m.Nrc,
                        m.NitCliente,
                        m.TipoContribuyente,
                        m.Email,
                        m.Direccion,
                        m.IdActividadEconomica,
                        codigoActividadEconomica = _context.ActividadesEconomicas
                            .Where(ae => ae.Id == m.IdActividadEconomica)
                            .Select(ae => ae.Codigo)
                            .FirstOrDefault(),
                        actividadNombre = _context.ActividadesEconomicas
                            .Where(ae => ae.Id == m.IdActividadEconomica)
                            .Select(ae => ae.Descripcion)
                            .FirstOrDefault(),
                        m.IdDepartamento,
                        codigoDepartamento = _context.Departamentos
                            .Where(d => d.Id == m.IdDepartamento)
                            .Select(d => d.Codigodep)
                            .FirstOrDefault(),
                        m.IdMunicipio,
                        codigoMunicipio = _context.Municipios
                            .Where(mu => mu.Id == m.IdMunicipio)
                            .Select(mu => mu.Codigo)
                            .FirstOrDefault(),
                        m.CuentaBolson                  
                    })
                    .ToListAsync();

                return Ok(lista);
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

       /// <summary>
       /// 
       /// </summary>
       /// <param name="cliente"></param>
       /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        [HttpPut("ClientexClts")]
        public async Task<IActionResult> UpdateCliente(ClientexClt c)
        {
            if (c.IdClienteClt == 0)
            {
                return BadRequest("El ID del Cliente es inválido.");
            }

            try
            {
                // Buscar el proveedor existente en la base de datos
                var cp = await _context.ClientexClts.FindAsync(c.IdClienteClt);
                if (cp == null)
                {
                    return NotFound("Cliente no encontrado.");
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
                cp.Email = c.Email;
                cp.Direccion = c.Direccion;
                cp.IdActividadEconomica = c.IdActividadEconomica;
                cp.IdDepartamento = c.IdDepartamento;
                cp.IdMunicipio = c.IdMunicipio;
                cp.CuentaBolson = c.CuentaBolson;
                // Guardar los cambios en la base de datos
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(500, "Error de concurrencia al actualizar el cliente.");
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

     
    }
}

