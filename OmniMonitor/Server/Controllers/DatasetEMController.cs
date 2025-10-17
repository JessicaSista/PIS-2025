using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniMonitor.Server.Attributes;
using OmniMonitor.Server.Services;
using OmniMonitor.Shared.Dtos;

namespace OmniMonitor.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DatasetEMController : ControllerBase
    {
        private readonly IDatasetEMService _datasetEMService;
        private readonly ISondaAuthService _sondaAuthService;
        public DatasetEMController(IDatasetEMService datasetEMService, ISondaAuthService sondaAuthService)
        {
            _datasetEMService = datasetEMService;
            _sondaAuthService = sondaAuthService;
        }

        /// <summary>
        /// Crea un nuevo dataset EM.
        /// </summary>
        [HttpPost]
        [RequirePermission("Crear Datasets EM")]
        [ProducesResponseType(typeof(DatasetEM), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<DatasetEM>> CreateDataset([FromBody] CreateDatasetEMRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("El cuerpo de la petición no puede estar vacío.");
                }

                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return BadRequest("El nombre del dataset es requerido.");
                }

                if (string.IsNullOrWhiteSpace(request.Username))
                {
                    return BadRequest("El nombre de usuario es requerido.");
                }

                if (string.IsNullOrWhiteSpace(request.IsDataset))
                {
                    return BadRequest("El tipo de dataset es requerido.");
                }

                var createdDataset = await _datasetEMService.CreateDatasetEMAsync(request);
                return CreatedAtAction(nameof(GetDatasetById), new { datasetId = createdDataset.Id, username = createdDataset.Username }, createdDataset);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al crear el dataset: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene todos los datasets EM de un usuario.
        /// </summary>
        [HttpGet("GetAllDatasets")]
        [RequirePermission("Ver Datasets EM")]
        [ProducesResponseType(typeof(List<DatasetEM>), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<DatasetEM>>> GetAllDatasets(string token)
        {
            try
            {
                var (username, password) = await _sondaAuthService.GetUserByTokenOMAsync(token);
                var datasets = await _datasetEMService.GetAllDatasetsEMAsync(username);
                return Ok(datasets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al obtener los datasets: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene un dataset EM por su ID y nombre de usuario.
        /// </summary>
        [HttpGet("{datasetId}/{username}")]
        [RequirePermission("Ver Datasets EM")]
        [ProducesResponseType(typeof(DatasetEM), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<DatasetEM>> GetDatasetById(int datasetId ,string token)
        {
            try
            {
                var (username, password) = await _sondaAuthService.GetUserByTokenOMAsync(token);
                var dataset = await _datasetEMService.GetDatasetEMByIdAsync(datasetId, username);
                if (dataset == null)
                {
                    return NotFound($"No se encontró el dataset con ID {datasetId} para el usuario {username}.");
                }
                return Ok(dataset);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al obtener el dataset: {ex.Message}");
            }
        }


        /// <summary>
        /// Actualiza un dataset EM existente.
        /// </summary>
        [HttpPut("{datasetId}")]
        [RequirePermission("Crear Datasets EM")]
        [ProducesResponseType(typeof(DatasetEM), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<DatasetEM>> UpdateDataset(int datasetId, [FromBody] CreateDatasetEMRequest request)
        {
            try
            {
                var updatedDataset = await _datasetEMService.UpdateDatasetEMAsync(datasetId, request);
                return Ok(updatedDataset);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error interno al actualizar el dataset: {ex.Message}" });
            }
        }

        /// <summary>
        /// Elimina un dataset EM.
        /// </summary>
        [HttpDelete("{datasetId}/{username}")]
        [RequirePermission("Eliminar Datasets EM")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> DeleteDataset(int datasetId, string token)
        {
            try
            {
                var (username, password) = await _sondaAuthService.GetUserByTokenOMAsync(token);
                await _datasetEMService.DeleteDatasetEMAsync(datasetId, username);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al eliminar el dataset: {ex.Message}");
            }
        }
    }
}
