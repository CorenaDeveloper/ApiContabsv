using ApiContabsv.DTO.DB_DteDTO;
using ApiContabsv.Models.Dte;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiContabsv.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DBDte_UsersController : Controller
    {
        private readonly dteContext _context;

        public DBDte_UsersController(dteContext context)
        {
            _context = context;
        }

       /// <summary>
       /// Lista de Usuario
       /// </summary>
       /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            try
            {
                return await _context.Users
                    .Where(u => u.Status == true) // Solo activos
                    .OrderByDescending(u => u.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        /// <summary>
        /// Unico Usuario por Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.ParentUser)
                    .Include(u => u.InverseParentUser)
                    .Include(u => u.BranchOffices)
                    //.Include(u => u.ContingencyDocuments)
                    .Include(u => u.ControlNumberSequences)
                    .Include(u => u.DteBalanceControls)
                    .Include(u => u.DteBalanceTransactions)
                    //.Include(u => u.DteDetails)
                    //.Include(u => u.DteDocuments)
                    .Include(u => u.FailedSequenceNumbers)
                    .Include(u => u.NotificationUsers)
                    .Include(u => u.SignerAssignments)
                    .Include(u => u.UserNotifications)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                    return NotFound("Usuario no encontrado");

                return user;
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        ///// <summary>
        ///// OBTENER USUARIO POR NIT
        ///// </summary>
        ///// <param name="nit"></param>
        ///// <returns></returns>
        //[HttpGet("nit/{nit}")]
        //public async Task<ActionResult<User>> GetUserByNIT(string nit)
        //{
        //    try
        //    {
        //        var user = await _context.Users
        //            .FirstOrDefaultAsync(u => u.Nit == nit && u.Status == true);

        //        if (user == null)
        //            return NotFound("Usuario no encontrado");

        //        return user;
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, $"Error interno: {ex.Message}");
        //    }
        //}


        /// <summary>
        /// CREAR NUEVO USUARIO 
        /// </summary>
        /// <param name="userDto">Datos del usuario a crear</param>
        /// <returns>Usuario creado con información de respuesta</returns>
        [HttpPost]
        public async Task<ActionResult<UserResponseDTO>> CreateUser(CreateUserDTO userDto)
        {
            try
            {
                // Validar ModelState
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Validaciones de unicidad
                if (await _context.Users.AnyAsync(u => u.Nit == userDto.Nit))
                    return BadRequest("Ya existe un usuario con este NIT");

                if (await _context.Users.AnyAsync(u => u.Nrc == userDto.Nrc))
                    return BadRequest("Ya existe un usuario con este NRC");

                if (await _context.Users.AnyAsync(u => u.Email == userDto.Email))
                    return BadRequest("Ya existe un usuario con este Email");

                if (await _context.Users.AnyAsync(u => u.Phone == userDto.Phone))
                    return BadRequest("Ya existe un usuario con este Teléfono");

                // Validar jerarquía si es sub-usuario
                if (!userDto.IsMaster && userDto.ParentUserId.HasValue)
                {
                    var parentUser = await _context.Users.FindAsync(userDto.ParentUserId.Value);
                    if (parentUser == null)
                        return BadRequest("El usuario padre no existe");

                    if (!parentUser.IsMaster)
                        return BadRequest("El usuario padre debe ser un cliente master");
                }

                // Mapear DTO a entidad User
                var user = new User
                {
                    ClienteId = userDto.ClienteId,
                    IsMaster = userDto.IsMaster,
                    ParentUserId = userDto.ParentUserId,
                    Nit = userDto.Nit,
                    Nrc = userDto.Nrc,
                    PasswordPri = userDto.PasswordPri,
                    CommercialName = userDto.CommercialName,
                    EconomicActivity = userDto.EconomicActivity,
                    EconomicActivityDesc = userDto.EconomicActivityDesc,
                    BusinessName = userDto.BusinessName,
                    Email = userDto.Email,
                    Phone = userDto.Phone,
                    YearInDte = userDto.YearInDte,
                    TokenLifetime = userDto.TokenLifetime,

                    // Valores automáticos
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    Status = true,
                    AuthType = "standard"
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Mapear a DTO de respuesta
                var responseDto = new UserResponseDTO
                {
                    Id = user.Id,
                    ClienteId = user.ClienteId,
                    IsMaster = user.IsMaster,
                    ParentUserId = user.ParentUserId,
                    Nit = user.Nit,
                    Nrc = user.Nrc,
                    Status = user.Status,
                    AuthType = user.AuthType,
                    CommercialName = user.CommercialName,
                    EconomicActivity = user.EconomicActivity,
                    EconomicActivityDesc = user.EconomicActivityDesc,
                    BusinessName = user.BusinessName,
                    Email = user.Email,
                    Phone = user.Phone,
                    YearInDte = user.YearInDte,
                    TokenLifetime = user.TokenLifetime,
                    CreatedAt = user.CreatedAt,
                    Message = "Usuario DTE creado exitosamente"
                };

                return CreatedAtAction(nameof(GetUser), new { id = user.Id }, responseDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        /// <summary>
        /// ACTUALIZAR USUARIO
        /// </summary>
        /// <param name="id"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, User user)
        {
            if (id != user.Id)
                return BadRequest("El ID no coincide");

            try
            {
                var existingUser = await _context.Users.FindAsync(id);
                if (existingUser == null)
                    return NotFound("Usuario no encontrado");

                // Validar NIT único (excluyendo el usuario actual)
                if (await _context.Users.AnyAsync(u => u.Nit == user.Nit && u.Id != id))
                    return BadRequest("Ya existe otro usuario con este NIT");

                // Actualizar campos permitidos
                existingUser.CommercialName = user.CommercialName;
                existingUser.EconomicActivity = user.EconomicActivity;
                existingUser.EconomicActivityDesc = user.EconomicActivityDesc;
                existingUser.BusinessName = user.BusinessName;
                existingUser.Email = user.Email;
                existingUser.Phone = user.Phone;
                existingUser.YearInDte = user.YearInDte;
                existingUser.TokenLifetime = user.TokenLifetime;
                existingUser.Status = user.Status;
                existingUser.UpdatedAt = DateTime.Now;

                // Solo actualizar password si viene
                if (!string.IsNullOrEmpty(user.PasswordPri))
                    existingUser.PasswordPri = user.PasswordPri;

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }


        /// <summary>
        /// DESACTIVAR USUARIO (soft delete)
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                    return NotFound("Usuario no encontrado");

                // Soft delete - solo cambiar status
                user.Status = false;
                user.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }


        ///// <summary>
        ///// LISTAR SUB-USUARIOS DE UN CLIENTE MASTER
        ///// </summary>
        ///// <param name="parentId"></param>
        ///// <returns></returns>
        //[HttpGet("{parentId}/sub-users")]
        //public async Task<ActionResult<IEnumerable<User>>> GetSubUsers(int parentId)
        //{
        //    try
        //    {
        //        var subUsers = await _context.Users
        //            .Where(u => u.ParentUserId == parentId && u.Status == true)
        //            .ToListAsync();

        //        return subUsers;
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, $"Error interno: {ex.Message}");
        //    }
        //}

        ///// <summary>
        ///// BUSCAR USUARIOS POR CRITERIO
        ///// </summary>
        ///// <param name="nit"></param>
        ///// <param name="commercialName"></param>
        ///// <param name="isMaster"></param>
        ///// <returns></returns>
        //[HttpGet("search")]
        //public async Task<ActionResult<IEnumerable<User>>> SearchUsers(
        //    [FromQuery] string? nit = null,
        //    [FromQuery] string? commercialName = null,
        //    [FromQuery] bool? isMaster = null)
        //{
        //    try
        //    {
        //        var query = _context.Users.Where(u => u.Status == true);

        //        if (!string.IsNullOrEmpty(nit))
        //            query = query.Where(u => u.Nit.Contains(nit));

        //        if (!string.IsNullOrEmpty(commercialName))
        //            query = query.Where(u => u.CommercialName.Contains(commercialName));

        //        if (isMaster.HasValue)
        //            query = query.Where(u => u.IsMaster == isMaster.Value);

        //        var results = await query.Take(50).ToListAsync(); // Limitar resultados
        //        return results;
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, $"Error interno: {ex.Message}");
        //    }
        //}
    }
}










