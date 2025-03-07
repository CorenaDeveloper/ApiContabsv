using ApiContabsv.DTO.DB_ContabilidadDTO;
using ApiContabsv.Models.Contabilidad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace ApiContabsv.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DBContabilidad_ProveedoresController : ControllerBase
    {
        private readonly ContabilidadContext _context;

        public DBContabilidad_ProveedoresController(ContabilidadContext context)
        {
            _context = context;
        }

        // 🔵 LISTAR PROVEEDORES FILTRADOS POR IdCliente
        [HttpGet("Proveedores")]
        public async Task<ActionResult<IEnumerable<Proveedore>>> GetProveedores([FromQuery] int? idCliente)
        {
            try
            {
                var query = _context.Proveedores
                            .AsQueryable();

                if (idCliente.HasValue)
                {
                    query = query.Where(p => p.IdCliente == idCliente);
                }

                var proveedores = await query.Select(c => new
                {
                    c.IdProveedor,
                    c.Nombres,
                    c.Apellidos,
                    c.NombreRazonSocial,
                    c.NombreComercial,
                    c.PersonaJuridica,
                    c.DuiCliente,
                    c.RepresentanteLegal,
                    c.DuiRepresentanteLegal,
                    c.TelefonoCliente,
                    c.Celular,
                    c.Nrc,
                    c.NitProveedor,
                    c.IdCliente,
                    c.Email,
                    c.Direccion,
                    c.IdSector,
                    c.Creado,
                    c.TipoContribuyente,
                    descripcionSector = c.IdSectorNavigation != null ? c.IdSectorNavigation.Detalle : null,
                    codigoSector = c.IdSectorNavigation != null ? c.IdSectorNavigation.CodigoSector : null
                }).ToListAsync();

                return Ok(proveedores);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        // 🔵 OBTENER UN PROVEEDOR POR ID
        [HttpGet("Proveedores/{id}")]
        public async Task<ActionResult<Proveedore>> GetProveedor(int id)
        {
            try
            {
                var c = await _context.Proveedores.FindAsync(id);
                if (c == null)
                {
                    return NotFound("Proveedor no encontrado.");
                }

                var proveedores = new
                {
                    c.IdProveedor,
                    c.Nombres,
                    c.Apellidos,
                    c.NombreRazonSocial,
                    c.NombreComercial,
                    c.PersonaJuridica,
                    c.DuiCliente,
                    c.RepresentanteLegal,
                    c.DuiRepresentanteLegal,
                    c.TelefonoCliente,
                    c.Celular,
                    c.Nrc,
                    c.NitProveedor,
                    c.IdCliente,
                    c.Email,
                    c.Direccion,
                    c.IdSector,
                    c.Creado,
                    c.TipoContribuyente,
                    descripcionSector = c.IdSectorNavigation != null ? c.IdSectorNavigation.Detalle : null,
                    codigoSector = c.IdSectorNavigation != null ? c.IdSectorNavigation.CodigoSector : null
                };

                return Ok(proveedores);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        // 🔵 CREAR UN NUEVO PROVEEDOR
        [HttpPost("Proveedores")]
        [SwaggerOperation(
         Summary = "REGISTRA UN NUEVO PROVEEDOR ",
         Description = "Este endpoint registrar proveedores de un Cliente en especifico"
        )]
        [SwaggerResponse(200, "Usuario Registrado exitosamente")]
        [SwaggerResponse(400, "Solicitud incorrecta (datos inválidos)")]
        [SwaggerResponse(404, "Usuario no encontrado")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<ActionResult<ProveedoresDTO>> CreateProveedor(ProveedoresDTO add)
        {
            try
            {
                // Normalizar datos eliminando espacios adicionales
                add.Nrc = add.Nrc?.Trim();
                add.NitProveedor = add.NitProveedor?.Trim();
                add.DuiCliente = add.DuiCliente?.Trim();

                // Verificar si ya existe un proveedor con el mismo NRC, NIT o DUI
                var existeProveedor = await _context.Proveedores
                    .AnyAsync(p => p.Nrc == add.Nrc || p.NitProveedor == add.NitProveedor || p.DuiCliente == add.DuiCliente);

                if (existeProveedor)
                {
                    return Conflict("Ya existe un proveedor con el mismo NRC, NIT o DUI.");
                }

                var c = new Proveedore
                {
                    Nombres = add.Nombres.Trim(),
                    Apellidos = add.Apellidos.Trim(),
                    PersonaJuridica = add.PersonaJuridica,
                    NombreRazonSocial = add.NombreRazonSocial?.Trim(),
                    NombreComercial = add.NombreComercial?.Trim(),
                    DuiCliente = add.DuiCliente,
                    RepresentanteLegal = add.RepresentanteLegal?.Trim(),
                    DuiRepresentanteLegal = add.DuiRepresentanteLegal?.Trim(),
                    TelefonoCliente = add.TelefonoCliente?.Trim(),
                    Celular = add.Celular?.Trim(),
                    Nrc = add.Nrc,
                    NitProveedor = add.NitProveedor,
                    IdCliente = add.IdCliente,
                    Email = add.Email?.Trim(),
                    Direccion = add.Direccion?.Trim(),
                    IdSector = add.IdSector,
                    Creado = add.Creado,
                    TipoContribuyente = add.TipoContribuyente?.Trim()
                };

                 _context.Proveedores.Add(c);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetProveedor), new { id = add.IdProveedor }, add);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        // 🔵 ACTUALIZAR UN PROVEEDOR
        [HttpPut("Proveedores")]
        public async Task<IActionResult> UpdateProveedor(Proveedore c)
        {
            if (c.IdProveedor == 0)
            {
                return BadRequest("El ID del proveedor es inválido.");
            }

            try
            {
                // Buscar el proveedor existente en la base de datos
                var cp = await _context.Proveedores.FindAsync(c.IdProveedor);
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
                cp.NitProveedor = c.NitProveedor;
                cp.Email = c.Email;
                cp.Direccion = c.Direccion;
                cp.IdSector = c.IdSector;
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

        // 🔵 ELIMINAR UN PROVEEDOR
        [HttpDelete("Proveedores/{id}")]
        public async Task<IActionResult> DeleteProveedor(int id)
        {
            try
            {
                var proveedor = await _context.Proveedores.FindAsync(id);
                if (proveedor == null)
                {
                    return NotFound("Proveedor no encontrado.");
                }

                _context.Proveedores.Remove(proveedor);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        private bool ProveedorExists(int id)
        {
            return _context.Proveedores.Any(e => e.IdProveedor == id);
        }
    }
}