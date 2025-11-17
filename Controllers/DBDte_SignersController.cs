using ApiContabsv.DTO.DB_DteDTO;
using ApiContabsv.Models.Dte;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Text;

namespace ApiContabsv.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DBDte_SignersController : ControllerBase
    {
        private readonly dteContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        public DBDte_SignersController(dteContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// LISTAR TODOS LOS FIRMADORES
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SignerResponseDTO>>> GetAllSigners()
        {
            try
            {
                var signers = await _context.Signers
                    .Select(s => new SignerResponseDTO
                    {
                        Id = s.Id,
                        SignerName = s.SignerName,
                        CertificatePath = s.CertificatePath,
                        CertificatePassword = s.CertificatePassword,
                        EndpointUrl = s.EndpointUrl,
                        IsActive = s.IsActive,
                        MaxConcurrentSigns = s.MaxConcurrentSigns,
                        CurrentLoad = s.CurrentLoad,
                        Priority = s.Priority,
                        HealthStatus = s.HealthStatus,
                        TotalDocumentsSigned = s.TotalDocumentsSigned,
                        AvgResponseTimeMs = s.AvgResponseTimeMs,
                        LastUsed = s.LastUsed,
                        CreatedAt = s.CreatedAt,
                        UpdatedAt = s.UpdatedAt
                    })
                    .ToListAsync();

                return Ok(signers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        /// <summary>
        /// OBTENER FIRMADOR POR ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<SignerResponseDTO>> GetSigner(int id)
        {
            try
            {
                var signer = await _context.Signers.FindAsync(id);

                if (signer == null)
                    return NotFound("Firmador no encontrado");

                var response = new SignerResponseDTO
                {
                    Id = signer.Id,
                    SignerName = signer.SignerName,
                    CertificatePath = signer.CertificatePath,
                    CertificatePassword = signer.CertificatePassword,
                    EndpointUrl = signer.EndpointUrl,
                    IsActive = signer.IsActive,
                    MaxConcurrentSigns = signer.MaxConcurrentSigns,
                    CurrentLoad = signer.CurrentLoad,
                    Priority = signer.Priority,
                    HealthStatus = signer.HealthStatus,
                    TotalDocumentsSigned = signer.TotalDocumentsSigned,
                    AvgResponseTimeMs = signer.AvgResponseTimeMs,
                    LastUsed = signer.LastUsed,
                    CreatedAt = signer.CreatedAt,
                    UpdatedAt = signer.UpdatedAt
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        /// <summary>
        /// CREAR NUEVO FIRMADOR
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<SignerResponseDTO>> CreateSigner(CreateSignerDTO signerDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Verificar que no exista un firmador con el mismo nombre
                var existingSigner = await _context.Signers
                    .AnyAsync(s => s.SignerName == signerDto.SignerName);

                if (existingSigner)
                    return BadRequest("Ya existe un firmador con ese nombre");

                // Crear nuevo firmador
                var signer = new Signer
                {
                    SignerName = signerDto.SignerName,
                    CertificatePath = signerDto.CertificatePath,
                    CertificatePassword = signerDto.CertificatePassword,
                    EndpointUrl = signerDto.EndpointUrl,
                    IsActive = signerDto.IsActive,
                    MaxConcurrentSigns = signerDto.MaxConcurrentSigns,
                    CurrentLoad = 0,
                    Priority = signerDto.Priority,
                    HealthStatus = "Unknown",
                    TotalDocumentsSigned = 0,
                    AvgResponseTimeMs = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Signers.Add(signer);
                await _context.SaveChangesAsync();

                // Realizar health check inicial
                await PerformHealthCheck(signer);

                var response = new SignerResponseDTO
                {
                    Id = signer.Id,
                    SignerName = signer.SignerName,
                    CertificatePath = signer.CertificatePath,
                    CertificatePassword = signer.CertificatePassword,
                    EndpointUrl = signer.EndpointUrl,
                    IsActive = signer.IsActive,
                    MaxConcurrentSigns = signer.MaxConcurrentSigns,
                    CurrentLoad = signer.CurrentLoad,
                    Priority = signer.Priority,
                    HealthStatus = signer.HealthStatus,
                    TotalDocumentsSigned = signer.TotalDocumentsSigned,
                    AvgResponseTimeMs = signer.AvgResponseTimeMs,
                    LastUsed = signer.LastUsed,
                    CreatedAt = signer.CreatedAt,
                    UpdatedAt = signer.UpdatedAt,
                    Message = "Firmador creado exitosamente"
                };

                return CreatedAtAction(nameof(GetSigner), new { id = signer.Id }, response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        /// <summary>
        /// ACTUALIZAR FIRMADOR
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSigner(int id, CreateSignerDTO signerDto)
        {
            try
            {
                var signer = await _context.Signers.FindAsync(id);
                if (signer == null)
                    return NotFound("Firmador no encontrado");

                // Verificar que no exista otro firmador con el mismo nombre
                var existingSigner = await _context.Signers
                    .AnyAsync(s => s.SignerName == signerDto.SignerName && s.Id != id);

                if (existingSigner)
                    return BadRequest("Ya existe otro firmador con ese nombre");

                // Actualizar propiedades
                signer.SignerName = signerDto.SignerName;
                signer.CertificatePath = signerDto.CertificatePath;
                signer.CertificatePassword = signerDto.CertificatePassword;
                signer.EndpointUrl = signerDto.EndpointUrl;
                signer.IsActive = signerDto.IsActive;
                signer.MaxConcurrentSigns = signerDto.MaxConcurrentSigns;
                signer.Priority = signerDto.Priority;
                signer.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        /// <summary>
        /// DESACTIVAR FIRMADOR
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSigner(int id)
        {
            try
            {
                var signer = await _context.Signers.FindAsync(id);
                if (signer == null)
                    return NotFound("Firmador no encontrado");

                // Soft delete - solo desactivar
                signer.IsActive = false;
                signer.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        /// <summary>
        /// HEALTH CHECK DE TODOS LOS FIRMADORES
        /// </summary>
        [HttpPost("health-check")]
        public async Task<ActionResult<IEnumerable<SignerHealthCheckDTO>>> HealthCheckAll()
        {
            try
            {
                var signers = await _context.Signers.Where(s => s.IsActive).ToListAsync();
                var healthChecks = new List<SignerHealthCheckDTO>();

                foreach (var signer in signers)
                {
                    var healthCheck = await PerformHealthCheck(signer);
                    healthChecks.Add(healthCheck);
                }

                return Ok(healthChecks);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        /// <summary>
        /// HEALTH CHECK DE UN FIRMADOR ESPECÍFICO
        /// </summary>
        [HttpPost("{id}/health-check")]
        public async Task<ActionResult<SignerHealthCheckDTO>> HealthCheckSigner(int id)
        {
            try
            {
                var signer = await _context.Signers.FindAsync(id);
                if (signer == null)
                    return NotFound("Firmador no encontrado");

                var healthCheck = await PerformHealthCheck(signer);
                return Ok(healthCheck);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        /// <summary>
        /// OBTENER FIRMADOR ÓPTIMO PARA USUARIO
        /// </summary>
        [HttpGet("optimal/{userId}")]
        public async Task<ActionResult<SignerResponseDTO>> GetOptimalSigner(int userId)
        {
            try
            {
                // Buscar firmadores asignados al usuario y que estén activos
                var userAssignments = await _context.SignerAssignments
                    .Include(sa => sa.Signer)
                    .Where(sa => sa.UserId == userId && sa.Signer.IsActive && sa.Signer.HealthStatus == "Healthy")
                    .ToListAsync();

                if (!userAssignments.Any())
                {
                    // Si no tiene firmadores asignados, buscar uno disponible
                    var availableSigners = await _context.Signers
                        .Where(s => s.IsActive && s.HealthStatus == "Healthy")
                        .ToListAsync();

                    var optimalAvailable = availableSigners
                        .Where(s => s.CurrentLoad < s.MaxConcurrentSigns)
                        .OrderBy(s => s.CurrentLoad)
                        .ThenBy(s => s.Priority)
                        .FirstOrDefault();

                    if (optimalAvailable == null)
                        return BadRequest("Todos los firmadores están sobrecargados");

                    var responseAvailable = new SignerResponseDTO
                    {
                        Id = optimalAvailable.Id,
                        SignerName = optimalAvailable.SignerName,
                        CertificatePath = optimalAvailable.CertificatePath,
                        CertificatePassword = optimalAvailable.CertificatePassword,
                        EndpointUrl = optimalAvailable.EndpointUrl,
                        IsActive = optimalAvailable.IsActive,
                        MaxConcurrentSigns = optimalAvailable.MaxConcurrentSigns,
                        CurrentLoad = optimalAvailable.CurrentLoad,
                        Priority = optimalAvailable.Priority,
                        HealthStatus = optimalAvailable.HealthStatus,
                        TotalDocumentsSigned = optimalAvailable.TotalDocumentsSigned,
                        AvgResponseTimeMs = optimalAvailable.AvgResponseTimeMs,
                        LastUsed = optimalAvailable.LastUsed,
                        CreatedAt = optimalAvailable.CreatedAt,
                        UpdatedAt = optimalAvailable.UpdatedAt
                    };

                    return Ok(responseAvailable);
                }

                // Seleccionar el firmador asignado con prioridad al principal
                var optimalAssignment = userAssignments
                    .Where(sa => sa.Signer.CurrentLoad < sa.Signer.MaxConcurrentSigns)
                    .OrderByDescending(sa => sa.IsPrimary)  // ✅ Principal primero
                    .ThenBy(sa => sa.Signer.CurrentLoad)    // ✅ Menor carga
                    .ThenBy(sa => sa.Signer.Priority)       // ✅ Prioridad
                    .FirstOrDefault();

                if (optimalAssignment == null)
                    return BadRequest("Todos los firmadores están sobrecargados");

                var optimalSigner = optimalAssignment.Signer;

                var response = new SignerResponseDTO
                {
                    Id = optimalSigner.Id,
                    SignerName = optimalSigner.SignerName,
                    CertificatePath = optimalSigner.CertificatePath,
                    CertificatePassword = optimalSigner.CertificatePassword,
                    EndpointUrl = optimalSigner.EndpointUrl,
                    IsActive = optimalSigner.IsActive,
                    MaxConcurrentSigns = optimalSigner.MaxConcurrentSigns,
                    CurrentLoad = optimalSigner.CurrentLoad,
                    Priority = optimalSigner.Priority,
                    HealthStatus = optimalSigner.HealthStatus,
                    TotalDocumentsSigned = optimalSigner.TotalDocumentsSigned,
                    AvgResponseTimeMs = optimalSigner.AvgResponseTimeMs,
                    LastUsed = optimalSigner.LastUsed,
                    CreatedAt = optimalSigner.CreatedAt,
                    UpdatedAt = optimalSigner.UpdatedAt
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        /// <summary>
        /// ASIGNAR FIRMADOR A USUARIO
        /// </summary>
        [HttpPost("assignments")]
        public async Task<ActionResult<SignerAssignmentResponseDTO>> CreateAssignment(CreateSignerAssignmentDTO assignmentDto)
        {
            try
            {
                // Verificar que el usuario existe
                var userExists = await _context.Users.AnyAsync(u => u.Id == assignmentDto.UserId);
                if (!userExists)
                    return BadRequest("El usuario no existe");

                // Verificar que el firmador existe
                var signer = await _context.Signers.FindAsync(assignmentDto.SignerId);
                if (signer == null)
                    return BadRequest("El firmador no existe");

                // Verificar que no exista la asignación
                var existingAssignment = await _context.SignerAssignments
                    .AnyAsync(sa => sa.UserId == assignmentDto.UserId && sa.SignerId == assignmentDto.SignerId);

                if (existingAssignment)
                    return BadRequest("La asignación ya existe");

                // Si es principal, desmarcar otros como principales
                if (assignmentDto.IsPrimary)
                {
                    var currentPrimary = await _context.SignerAssignments
                        .Where(sa => sa.UserId == assignmentDto.UserId && sa.IsPrimary)
                        .ToListAsync();

                    foreach (var assignment in currentPrimary)
                    {
                        assignment.IsPrimary = false;
                    }
                }

                // Crear nueva asignación
                var newAssignment = new SignerAssignment
                {
                    UserId = assignmentDto.UserId,
                    SignerId = assignmentDto.SignerId,
                    IsPrimary = assignmentDto.IsPrimary,
                    AssignedAt = DateTime.UtcNow
                };

                _context.SignerAssignments.Add(newAssignment);
                await _context.SaveChangesAsync();

                var response = new SignerAssignmentResponseDTO
                {
                    Id = newAssignment.Id,
                    UserId = newAssignment.UserId,
                    SignerId = newAssignment.SignerId,
                    IsPrimary = newAssignment.IsPrimary,
                    AssignedAt = newAssignment.AssignedAt,
                    LastUsed = newAssignment.LastUsed,
                    Signer = new SignerResponseDTO
                    {
                        Id = signer.Id,
                        SignerName = signer.SignerName,
                        EndpointUrl = signer.EndpointUrl,
                        IsActive = signer.IsActive,
                        HealthStatus = signer.HealthStatus
                    }
                };

                return CreatedAtAction(nameof(GetSigner), new { id = signer.Id }, response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        /// <summary>
        /// LISTAR ASIGNACIONES DE UN USUARIO
        /// </summary>
        [HttpGet("assignments/user/{userId}")]
        public async Task<ActionResult<IEnumerable<SignerAssignmentResponseDTO>>> GetUserAssignments(int userId)
        {
            try
            {
                var assignments = await _context.SignerAssignments
                    .Include(sa => sa.Signer)
                    .Where(sa => sa.UserId == userId)
                    .Select(sa => new SignerAssignmentResponseDTO
                    {
                        Id = sa.Id,
                        UserId = sa.UserId,
                        SignerId = sa.SignerId,
                        IsPrimary = sa.IsPrimary,
                        AssignedAt = sa.AssignedAt,
                        LastUsed = sa.LastUsed,
                        Signer = new SignerResponseDTO
                        {
                            Id = sa.Signer.Id,
                            SignerName = sa.Signer.SignerName,
                            CertificatePath = sa.Signer.CertificatePath,
                            EndpointUrl = sa.Signer.EndpointUrl,
                            IsActive = sa.Signer.IsActive,
                            HealthStatus = sa.Signer.HealthStatus,
                            CurrentLoad = sa.Signer.CurrentLoad,
                            MaxConcurrentSigns = sa.Signer.MaxConcurrentSigns
                        }
                    })
                    .ToListAsync();

                return Ok(assignments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        #region Helper Methods

        /// <summary>
        /// Realizar health check a un firmador específico
        /// </summary>
        private async Task<SignerHealthCheckDTO> PerformHealthCheck(Signer signer)
        {
            var healthCheck = new SignerHealthCheckDTO
            {
                SignerId = signer.Id,
                SignerName = signer.SignerName,
                EndpointUrl = signer.EndpointUrl,
                CheckedAt = DateTime.UtcNow
            };

            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30); // Timeout fijo de 30 segundos

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                // Intentar hacer ping al endpoint
                var healthUrl = signer.EndpointUrl?.TrimEnd('/') + "/status";
                var response = await httpClient.GetAsync(healthUrl);

                stopwatch.Stop();

                healthCheck.ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds;
                healthCheck.IsHealthy = response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound;

                if (healthCheck.IsHealthy)
                {
                    healthCheck.StatusMessage = "Firmador respondiendo correctamente";
                    signer.HealthStatus = "Healthy";
                    signer.AvgResponseTimeMs = (signer.AvgResponseTimeMs + healthCheck.ResponseTimeMs) / 2;
                }
                else
                {
                    healthCheck.StatusMessage = $"Error HTTP: {response.StatusCode}";
                    signer.HealthStatus = "Unhealthy";
                }
            }
            catch (TaskCanceledException)
            {
                healthCheck.IsHealthy = false;
                healthCheck.StatusMessage = "Timeout - Firmador no responde";
                healthCheck.ResponseTimeMs = 30000;
                signer.HealthStatus = "Timeout";
            }
            catch (Exception ex)
            {
                healthCheck.IsHealthy = false;
                healthCheck.StatusMessage = $"Error de conexión: {ex.Message}";
                signer.HealthStatus = "Error";
            }

            // Actualizar estado en base de datos
            signer.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return healthCheck;
        }


        /// <summary>
        /// PROBAR FIRMADO CON CERTIFICADO DE CLIENTE POR NIT
        /// </summary>
        [HttpPost("test-signing")]
        public async Task<ActionResult> TestSigning([FromBody] TestSigningDTO testDto)
        {
            try
            {
                // ✅ Usar la misma lógica que optimal para buscar firmador del usuario
                var userAssignments = await _context.SignerAssignments
                    .Include(sa => sa.Signer)
                    .Where(sa => sa.UserId == testDto.UserId && sa.Signer.IsActive && sa.Signer.HealthStatus == "Healthy")
                    .ToListAsync();

                Signer signer;

                if (userAssignments.Any())
                {
                    // Usuario tiene firmadores asignados - usar el principal
                    var optimalAssignment = userAssignments
                        .Where(sa => sa.Signer.CurrentLoad < sa.Signer.MaxConcurrentSigns)
                        .OrderByDescending(sa => sa.IsPrimary)  // ✅ Principal primero
                        .ThenBy(sa => sa.Signer.CurrentLoad)
                        .ThenBy(sa => sa.Signer.Priority)
                        .FirstOrDefault();

                    if (optimalAssignment == null)
                        return BadRequest("Todos los firmadores asignados están sobrecargados");

                    signer = optimalAssignment.Signer;
                }
                else
                {
                    // Usuario sin firmadores asignados - usar cualquiera disponible
                    signer = await _context.Signers
                        .Where(s => s.IsActive && s.HealthStatus == "Healthy")
                        .OrderBy(s => s.CurrentLoad)
                        .FirstOrDefaultAsync();

                    if (signer == null)
                        return BadRequest("No hay firmadores disponibles");
                }

   

                // Construir ruta del certificado basado en NIT
                var certificatePath = Path.Combine(signer.CertificatePath, $"{testDto.Nit}.crt");

                // Verificar que el certificado existe
                if (!System.IO.File.Exists(certificatePath))
                    return BadRequest($"Certificado no encontrado para NIT: {testDto.Nit} en ruta: {certificatePath}");

                // Documento de prueba
                var testDocument = new
                {
                    version = 1,
                    ambiente = "00", // ambiente de prueba
                    tipoDte = "01", // factura
                    numeroControl = "DTE-01-TEST-00000001",
                    codigoGeneracion = Guid.NewGuid().ToString().ToUpper(),
                    tipoModelo = 1,
                    tipoOperacion = 1,
                    fecEmi = DateTime.Now.ToString("yyyy-MM-dd"),
                    horEmi = DateTime.Now.ToString("HH:mm:ss"),
                    tipoMoneda = "USD",
                    emisor = new
                    {
                        nit = testDto.Nit,
                        nrc = "12345-6",
                        nombre = "EMPRESA DE PRUEBA",
                        codActividad = "01111",
                        descActividad = "Actividad de prueba"
                    },
                    receptor = new
                    {
                        nit = "99999999-9",
                        nrc = "99999-9",
                        nombre = "CLIENTE DE PRUEBA"
                    },
                    cuerpoDocumento = new[]
                    {
                new
                {
                    numItem = 1,
                    tipoItem = 1,
                    descripcion = "PRODUCTO DE PRUEBA",
                    cantidad = 1.0,
                    precioUni = 10.00,
                    ventaNoSuj = 0.00,
                    ventaExenta = 0.00,
                    ventaGravada = 10.00
                }
            },
                    resumen = new
                    {
                        totalNoSuj = 0.00,
                        totalExenta = 0.00,
                        totalGravada = 10.00,
                        subTotalVentas = 10.00,
                        totalPagar = 10.00
                    }
                };

                // Preparar petición al firmador (formato que espera el firmador Java)
                var firmingRequest = new
                {
                    nit = testDto.Nit,
                    jsonDTE = System.Text.Json.JsonSerializer.Serialize(testDocument),
                    clavePrivada = testDto.CertificatePassword ?? "",
                    certificadoPath = certificatePath
                };

                // Enviar al firmador
                var httpClient = _httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                var jsonContent = System.Text.Json.JsonSerializer.Serialize(firmingRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(signer.EndpointUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Documento firmado exitosamente",
                        signer = new
                        {
                            id = signer.Id,
                            name = signer.SignerName,
                            endpoint = signer.EndpointUrl
                        },
                        certificatePath = certificatePath,
                        signedDocument = responseContent
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Error al firmar documento",
                        error = responseContent,
                        signer = signer.SignerName,
                        certificatePath = certificatePath
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error interno",
                    error = ex.Message
                });
            }
        }
        #endregion
    }
}