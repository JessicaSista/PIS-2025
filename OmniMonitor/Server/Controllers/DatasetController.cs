using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniMonitor.Server.Attributes;
using OmniMonitor.Server.Services;
using OmniMonitor.Shared.Dtos;
using System.Security.Claims;

namespace OmniMonitor.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DatasetController : ControllerBase
    {
        private readonly IDatasetService _datasetService;
        private readonly ILogger<DatasetController> _logger;

        public DatasetController(IDatasetService datasetService, ILogger<DatasetController> logger)
        {
            _datasetService = datasetService;
            _logger = logger;
        }

        /// <summary>
        /// Crea un nuevo dataset
        /// </summary>
        [HttpPost]
        [RequirePermission("Gestionar Datasets")]
        public async Task<ActionResult<DatasetResponseDto>> CreateDataset([FromBody] DatasetCreateDto createDto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _datasetService.CreateDatasetAsync(createDto, userId);
                return CreatedAtAction(nameof(GetDatasetById), new { id = result.Id }, result);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando dataset");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene un dataset por ID
        /// </summary>
        [HttpGet("{id}")]
        [RequirePermission("Ver Datasets")]
        public async Task<ActionResult<DatasetResponseDto>> GetDatasetById(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var dataset = await _datasetService.GetDatasetByIdAsync(id, userId);
                
                if (dataset == null)
                    return NotFound(new { message = "Dataset no encontrado" });

                return Ok(dataset);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo dataset {DatasetId}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene todos los datasets del usuario con filtros y paginación
        /// </summary>
        [HttpGet]
        [RequirePermission("Ver Datasets")]
        public async Task<ActionResult<DatasetListResponseDto>> GetAllDatasets([FromQuery] DatasetListRequestDto request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _datasetService.GetAllDatasetsAsync(userId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo datasets");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Actualiza un dataset existente
        /// </summary>
        [HttpPut("{id}")]
        [RequirePermission("Gestionar Datasets")]
        public async Task<ActionResult<DatasetResponseDto>> UpdateDataset(int id, [FromBody] DatasetUpdateDto updateDto)
        {
            try
            {
                if (id != updateDto.Id)
                    return BadRequest(new { message = "El ID en la URL no coincide con el ID en el cuerpo de la petición" });

                var userId = GetCurrentUserId();
                var result = await _datasetService.UpdateDatasetAsync(updateDto, userId);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando dataset {DatasetId}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Elimina un dataset
        /// </summary>
        [HttpDelete("{id}")]
        [RequirePermission("Gestionar Datasets")]
        public async Task<ActionResult> DeleteDataset(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var deleted = await _datasetService.DeleteDatasetAsync(id, userId);
                
                if (!deleted)
                    return NotFound(new { message = "Dataset no encontrado" });

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando dataset {DatasetId}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Valida los miembros de un dataset contra las APIs de SONDA
        /// </summary>
        [HttpPost("validate")]
        [RequirePermission("Ver Datasets")]
        public async Task<ActionResult<DatasetValidationResultDto>> ValidateDatasetMembers([FromBody] DatasetValidationRequestDto validationRequest)
        {
            try
            {
                // Obtener credenciales del usuario actual
                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                var password = User.FindFirst("password")?.Value; // Asumiendo que se almacena en el token

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    return BadRequest(new { message = "Credenciales de usuario no disponibles" });
                }

                var result = await _datasetService.ValidateDatasetMembersAsync(validationRequest, username, password);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validando miembros del dataset");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Crea un dataset interno para visualización de entidades sueltas
        /// </summary>
        [HttpPost("internal")]
        [RequirePermission("Ver Datasets")]
        public async Task<ActionResult<DatasetResponseDto>> CreateInternalDataset(
            [FromQuery] string tipoEntidad, 
            [FromQuery] int entityId, 
            [FromQuery] int sensorId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _datasetService.CreateInternalDatasetAsync(tipoEntidad, entityId, sensorId, userId);
                return CreatedAtAction(nameof(GetDatasetById), new { id = result.Id }, result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando dataset interno");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene el ID del usuario actual desde el token JWT
        /// </summary>
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                throw new UnauthorizedAccessException("Usuario no válido");
            }
            return userId;
        }
    }
}
