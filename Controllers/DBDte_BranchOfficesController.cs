using ApiContabsv.DTO.DB_DteDTO;
using ApiContabsv.Models.Dte;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace ApiContabsv.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DBDte_BranchOfficesController : ControllerBase
    {
        private readonly dteContext _context;

        public DBDte_BranchOfficesController(dteContext context)
        {
            _context = context;
        }

        /// <summary>
        /// LISTAR TODAS LAS SUCURSALES DE UN USUARIO
        /// </summary>
        /// <param name="userId">ID del usuario</param>
        /// <returns></returns>
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<BranchOfficeResponseDTO>>> GetBranchOfficesByUser(int userId)
        {
            try
            {
                // Verificar que el usuario existe
                var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
                if (!userExists)
                    return NotFound("Usuario no encontrado");

                var branchOffices = await _context.BranchOffices
                    .Include(b => b.Addresses) // ✅ Colección de direcciones
                    .Where(b => b.UserId == userId && b.IsActive)
                    .Select(b => new BranchOfficeResponseDTO
                    {
                        Id = b.Id,
                        UserId = b.UserId,
                        EstablishmentCode = b.EstablishmentCode,
                        EstablishmentCodeMh = b.EstablishmentCodeMh,
                        Email = b.Email,
                        ApiKey = b.ApiKey,
                        ApiSecret = b.ApiSecret,
                        Phone = b.Phone,
                        EstablishmentType = b.EstablishmentType,
                        PosCode = b.PosCode,
                        PosCodeMh = b.PosCodeMh,
                        IsActive = b.IsActive,
                        Address = b.Addresses.FirstOrDefault() != null ? new AddressResponseDTO
                        {
                            Id = b.Addresses.First().Id,
                            Department = b.Addresses.First().Department,
                            Municipality = b.Addresses.First().Municipality,
                            Address = b.Addresses.First().Address1,
                            Complement = b.Addresses.First().Complement
                        } : null
                    })
                    .ToListAsync();

                return Ok(branchOffices);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        /// <summary>
        /// OBTENER SUCURSAL POR ID
        /// </summary>
        /// <param name="id">ID de la sucursal</param>
        /// <returns></returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<BranchOfficeResponseDTO>> GetBranchOffice(int id)
        {
            try
            {
                var branchOffice = await _context.BranchOffices
                    .Include(b => b.Addresses)
                    .FirstOrDefaultAsync(b => b.Id == id);

                if (branchOffice == null)
                    return NotFound("Sucursal no encontrada");

                var address = branchOffice.Addresses.FirstOrDefault();

                var response = new BranchOfficeResponseDTO
                {
                    Id = branchOffice.Id,
                    UserId = branchOffice.UserId,
                    EstablishmentCode = branchOffice.EstablishmentCode,
                    EstablishmentCodeMh = branchOffice.EstablishmentCodeMh,
                    Email = branchOffice.Email,
                    ApiKey = branchOffice.ApiKey,
                    ApiSecret = branchOffice.ApiSecret,
                    Phone = branchOffice.Phone,
                    EstablishmentType = branchOffice.EstablishmentType,
                    PosCode = branchOffice.PosCode,
                    PosCodeMh = branchOffice.PosCodeMh,
                    IsActive = branchOffice.IsActive,
                    Address = address != null ? new AddressResponseDTO
                    {
                        Id = address.Id,
                        Department = address.Department,
                        Municipality = address.Municipality,
                        Address = address.Address1,
                        Complement = address.Complement
                    } : null
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        /// <summary>
        /// CREAR NUEVA SUCURSAL CON DIRECCIÓN
        /// </summary>
        /// <param name="branchDto">Datos de la sucursal a crear</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult<BranchOfficeResponseDTO>> CreateBranchOffice(CreateBranchOfficeDTO branchDto)
        {
            try
            {
                // Validar ModelState
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Verificar que el usuario existe
                var user = await _context.Users.FindAsync(branchDto.UserId);
                if (user == null)
                    return BadRequest("El usuario no existe");

                // Validar que no tenga más de una casa matriz
                if (branchDto.EstablishmentType == "02")
                {
                    var hasMatrixOffice = await _context.BranchOffices
                        .AnyAsync(b => b.UserId == branchDto.UserId &&
                                      b.EstablishmentType == "02" &&
                                      b.IsActive);

                    if (hasMatrixOffice)
                        return BadRequest("El usuario ya tiene una casa matriz registrada");
                }

                // Generar API Key y Secret únicos
                var apiKey = GenerateApiKey();
                var apiSecret = GenerateApiSecret();

                // Crear sucursal
                var branchOffice = new BranchOffice
                {
                    UserId = branchDto.UserId,
                    EstablishmentCode = branchDto.EstablishmentCode,
                    EstablishmentCodeMh = branchDto.EstablishmentCodeMh,
                    Email = branchDto.Email,
                    ApiKey = apiKey,
                    ApiSecret = apiSecret,
                    Phone = branchDto.Phone,
                    EstablishmentType = branchDto.EstablishmentType,
                    PosCode = branchDto.PosCode,
                    PosCodeMh = branchDto.PosCodeMh,
                    IsActive = true
                };

                _context.BranchOffices.Add(branchOffice);
                await _context.SaveChangesAsync();

                // Crear dirección
                var address = new Address
                {
                    BranchId = branchOffice.Id,
                    Department = branchDto.Address.Department,
                    Municipality = branchDto.Address.Municipality,
                    Address1 = branchDto.Address.Address,
                    Complement = branchDto.Address.Complement
                };

                _context.Addresses.Add(address);
                await _context.SaveChangesAsync();

                // Preparar respuesta
                var response = new BranchOfficeResponseDTO
                {
                    Id = branchOffice.Id,
                    UserId = branchOffice.UserId,
                    EstablishmentCode = branchOffice.EstablishmentCode,
                    EstablishmentCodeMh = branchOffice.EstablishmentCodeMh,
                    Email = branchOffice.Email,
                    ApiKey = branchOffice.ApiKey,
                    ApiSecret = branchOffice.ApiSecret,
                    Phone = branchOffice.Phone,
                    EstablishmentType = branchOffice.EstablishmentType,
                    PosCode = branchOffice.PosCode,
                    PosCodeMh = branchOffice.PosCodeMh,
                    IsActive = branchOffice.IsActive,
                    Address = new AddressResponseDTO
                    {
                        Id = address.Id,
                        Department = address.Department,
                        Municipality = address.Municipality,
                        Address = address.Address1,
                        Complement = address.Complement
                    },
                    Message = "Sucursal creada exitosamente"
                };

                return CreatedAtAction(nameof(GetBranchOffice), new { id = branchOffice.Id }, response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        /// <summary>
        /// ACTUALIZAR SUCURSAL
        /// </summary>
        /// <param name="id">ID de la sucursal</param>
        /// <param name="branchDto">Datos actualizados</param>
        /// <returns></returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBranchOffice(int id, CreateBranchOfficeDTO branchDto)
        {
            try
            {
                var existingBranch = await _context.BranchOffices
                    .Include(b => b.Addresses)
                    .FirstOrDefaultAsync(b => b.Id == id);

                if (existingBranch == null)
                    return NotFound("Sucursal no encontrada");

                // Validar que pertenece al usuario correcto
                if (existingBranch.UserId != branchDto.UserId)
                    return BadRequest("La sucursal no pertenece a este usuario");

                // Validar casa matriz si está cambiando el tipo
                if (branchDto.EstablishmentType == "02" && existingBranch.EstablishmentType != "02")
                {
                    var hasOtherMatrix = await _context.BranchOffices
                        .AnyAsync(b => b.UserId == branchDto.UserId &&
                                      b.EstablishmentType == "02" &&
                                      b.IsActive &&
                                      b.Id != id);

                    if (hasOtherMatrix)
                        return BadRequest("El usuario ya tiene otra casa matriz registrada");
                }

                // Actualizar sucursal
                existingBranch.EstablishmentCode = branchDto.EstablishmentCode;
                existingBranch.EstablishmentCodeMh = branchDto.EstablishmentCodeMh;
                existingBranch.Email = branchDto.Email;
                existingBranch.Phone = branchDto.Phone;
                existingBranch.EstablishmentType = branchDto.EstablishmentType;
                existingBranch.PosCode = branchDto.PosCode;
                existingBranch.PosCodeMh = branchDto.PosCodeMh;

                // Actualizar dirección (primera de la colección)
                var existingAddress = existingBranch.Addresses.FirstOrDefault();
                if (existingAddress != null)
                {
                    existingAddress.Department = branchDto.Address.Department;
                    existingAddress.Municipality = branchDto.Address.Municipality;
                    existingAddress.Address1 = branchDto.Address.Address;
                    existingAddress.Complement = branchDto.Address.Complement;
                }

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        /// <summary>
        /// DESACTIVAR SUCURSAL (soft delete)
        /// </summary>
        /// <param name="id">ID de la sucursal</param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBranchOffice(int id)
        {
            try
            {
                var branchOffice = await _context.BranchOffices.FindAsync(id);
                if (branchOffice == null)
                    return NotFound("Sucursal no encontrada");

                // Validar que no sea la casa matriz
                if (branchOffice.EstablishmentType == "02")
                {
                    var hasOtherBranches = await _context.BranchOffices
                        .AnyAsync(b => b.UserId == branchOffice.UserId &&
                                      b.EstablishmentType != "02" &&
                                      b.IsActive &&
                                      b.Id != id);

                    if (hasOtherBranches)
                        return BadRequest("No se puede eliminar la casa matriz mientras existan sucursales activas");
                }

                // Soft delete
                branchOffice.IsActive = false;
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        /// <summary>
        /// BUSCAR SUCURSALES POR CRITERIO
        /// </summary>
        /// <param name="userId">ID del usuario (opcional)</param>
        /// <param name="establishmentType">Tipo de establecimiento (opcional)</param>
        /// <param name="isActive">Estado activo (opcional)</param>
        /// <returns></returns>
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<BranchOfficeResponseDTO>>> SearchBranchOffices(
            [FromQuery] int? userId = null,
            [FromQuery] string? establishmentType = null,
            [FromQuery] bool? isActive = true)
        {
            try
            {
                var query = _context.BranchOffices.Include(b => b.Addresses).AsQueryable();

                if (userId.HasValue)
                    query = query.Where(b => b.UserId == userId.Value);

                if (!string.IsNullOrEmpty(establishmentType))
                    query = query.Where(b => b.EstablishmentType == establishmentType);

                if (isActive.HasValue)
                    query = query.Where(b => b.IsActive == isActive.Value);

                var results = await query
                    .Take(50)
                    .Select(b => new BranchOfficeResponseDTO
                    {
                        Id = b.Id,
                        UserId = b.UserId,
                        EstablishmentCode = b.EstablishmentCode,
                        EstablishmentCodeMh = b.EstablishmentCodeMh,
                        Email = b.Email,
                        ApiKey = b.ApiKey,
                        ApiSecret = b.ApiSecret,
                        Phone = b.Phone,
                        EstablishmentType = b.EstablishmentType,
                        PosCode = b.PosCode,
                        PosCodeMh = b.PosCodeMh,
                        IsActive = b.IsActive,
                        Address = b.Addresses.FirstOrDefault() != null ? new AddressResponseDTO
                        {
                            Id = b.Addresses.First().Id,
                            Department = b.Addresses.First().Department,
                            Municipality = b.Addresses.First().Municipality,
                            Address = b.Addresses.First().Address1,
                            Complement = b.Addresses.First().Complement
                        } : null
                    })
                    .ToListAsync();

                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        #region Helper Methods

        /// <summary>
        /// Generar API Key única
        /// </summary>
        /// <returns></returns>
        private string GenerateApiKey()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[32];
            rng.GetBytes(bytes);
            return Convert.ToHexString(bytes).ToLower();
        }

        /// <summary>
        /// Generar API Secret único
        /// </summary>
        /// <returns></returns>
        private string GenerateApiSecret()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[48];
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
        }

        #endregion
    }
}