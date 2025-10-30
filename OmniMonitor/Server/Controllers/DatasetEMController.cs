using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using OmniMonitor.Server.Attributes;
using OmniMonitor.Server.Context;
using OmniMonitor.Server.Services;
using OmniMonitor.Shared.Dtos;

namespace OmniMonitor.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]

    public class DatasetEMController : ControllerBase
    {
        private readonly IDatasetEMService _datasetEMService;
        private readonly ISondaAuthService _sondaAuthService;
        private readonly IDatasetUMService _datasetUMService;
        private readonly ApplicationDbContext _context;

        public DatasetEMController(IDatasetEMService datasetEMService, ISondaAuthService sondaAuthService, IDatasetUMService datasetUMService, ApplicationDbContext context)
        {
            _datasetEMService = datasetEMService;
            _sondaAuthService = sondaAuthService;
            _datasetUMService = datasetUMService;
            _context = context;
        }

        private bool IsUserAuthorized(string username)
        {
            // Verificar que el username no sea nulo o vacío
            return !string.IsNullOrWhiteSpace(username);
        }

        /// <summary>
        /// Crea un nuevo dataset EM.
        /// </summary>
        [HttpPost]
       // [RequirePermission("Crear Datasets EM")]
        [ProducesResponseType(typeof(DatasetEM), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<DatasetEM>> CreateDataset([FromBody] CreateDatasetEMRequest request)
        {
            try
            {
                if (!IsUserAuthorized(request.Username))
                    return Forbid();

                var requestDataset = new CreateDatasetRequest(request.Name, request.Username, ModuleType.EventManager);
                var Dataset = await _datasetUMService.CreateDatasetAsync(requestDataset);
                var createdDataset = await _datasetEMService.CreateDatasetEMAsync(request, Dataset.Id);
                await _datasetUMService.UpdateDatasetAsyncEM(Dataset.Id, requestDataset, createdDataset);

                return CreatedAtAction(nameof(GetDatasetById), new { datasetId = createdDataset.Id, username = createdDataset.Username }, createdDataset);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
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
       // [RequirePermission("Ver Datasets EM")]
        [ProducesResponseType(typeof(List<DatasetEM>), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<DatasetEM>>> GetAllDatasets(string token)
        {
            try
            {
                string username = await _sondaAuthService.GetUserByTokenOMAsync(token);
                List<DatasetEM> datasets = await _datasetEMService.GetAllDatasetsEMAsync(username);
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
    [HttpGet("GetDatasetById")]
    [ProducesResponseType(typeof(DatasetEM), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<DatasetEM>> GetDatasetById([FromQuery] int datasetId, [FromQuery] string token)
        {
            try
            {
                string username = await _sondaAuthService.GetUserByTokenOMAsync(token);
                if (!IsUserAuthorized(username))
                    return Forbid();

                var dataset = await _datasetEMService.GetDatasetEMByIdForEditAsync(datasetId, username);
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
        //[RequirePermission("Crear Datasets EM")]
        [ProducesResponseType(typeof(DatasetEM), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<DatasetEM>> UpdateDataset(int datasetId, [FromBody] CreateDatasetEMRequest request)
        {
            try
            {
                if (!IsUserAuthorized(request.Username))
                    return Forbid();

                var updatedDataset = await _datasetEMService.UpdateDatasetEMAsync(datasetId, request);
                var requestDataset = new CreateDatasetRequest(request.Name, request.Username, ModuleType.EventManager);
                Datasets dataset = await _datasetUMService.UpdateDatasetAsyncEM(updatedDataset.DatasetId, requestDataset, updatedDataset);
                return Ok(updatedDataset);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al actualizar el dataset: {ex.Message}");
            }
        }

        /// <summary>
        /// Elimina un dataset EM.
        /// </summary>
        [HttpDelete("{datasetId}")]
       // [RequirePermission("Eliminar Datasets EM")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> DeleteDataset(int datasetId, string token)
        {
            try
            {
                string username = await _sondaAuthService.GetUserByTokenOMAsync(token);
                DatasetEM? id = await _context.DatasetsEM
                .FirstOrDefaultAsync(d => d.Id == datasetId && d.Username == username);
                await _datasetEMService.DeleteDatasetEMAsync(datasetId, username);
                await _datasetUMService.DeleteDatasetAsync(id!.DatasetId, username);
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
