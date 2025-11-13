using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
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
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class DatasetEMController : ControllerBase
    {
        private readonly IDatasetEMService _datasetEMService;
        private readonly ISondaAuthService _sondaAuthService;
        private readonly IDatasetUMService _datasetUMService;
        private readonly ISondaEMService _sondaEMService;
        private readonly ApplicationDbContext _context;

        public DatasetEMController(IDatasetEMService datasetEMService, ISondaAuthService sondaAuthService, IDatasetUMService datasetUMService, ISondaEMService sondaEMService, ApplicationDbContext context)
        {
            _datasetEMService = datasetEMService;
            _sondaAuthService = sondaAuthService;
            _datasetUMService = datasetUMService;
            _sondaEMService = sondaEMService;
            _context = context;
        }

        /// <summary>
        /// Crea un nuevo dataset EM.
        /// </summary>
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost]
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

                var requestDataset = new CreateDatasetRequest(request.Name, request.Username, ModuleType.EventManager);
                Datasets dataset = await _datasetUMService.CreateDatasetAsync(requestDataset);
                DatasetEM createdDataset = await _datasetEMService.CreateDatasetEMAsync(request, dataset.Id);
                await _datasetUMService.UpdateDatasetAsyncEM(dataset.Id, requestDataset, createdDataset);
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

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost("filtered")]
        [ProducesResponseType(typeof(DatasetEM), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<DatasetEM>> CreateDatasetEMFiltered([FromBody] CreateDatasetEMFilteredRequest request)
        {
            try
            {
                var username = User.Identity?.Name;
                if (string.IsNullOrWhiteSpace(username))
                    return BadRequest("Usuario no encontrado.");
                
                var req = request.DatasetRequest;
                // Usar el username desde JWT
                req.Username = username;
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                var requestDataset = new CreateDatasetRequest(req.Name, req.Username, ModuleType.EventManager);
                Datasets newDataset = await _datasetUMService.CreateDatasetAsync(requestDataset);

                // Filtrado para Alert, Event, Extension, Category
                if (req.ContentType == "1") // Alert
                {
                    var allAlerts = await _sondaEMService.GetAlerts(null, null, null, null, null, null, null, null, null, username);
                    var filtrados = ApiDataService.StaticFilterObjects(allAlerts, request.Filters);
                    if (req.AlertIds == null) req.AlertIds = new List<int>();
                    req.AlertIds.Clear();
                    req.AlertIds.AddRange(filtrados.Select(a => a.AlertId != null ? (int)a.AlertId : 0).OfType<int>().ToList());
                }
                else if (req.ContentType == "2") // Event
                {
                    var allEvents = await _sondaEMService.GetEvents(null, null, null, null, username);
                    var filtrados = ApiDataService.StaticFilterObjects(allEvents, request.Filters);
                    if (req.EventIds == null) req.EventIds = new List<int>();
                    req.EventIds.Clear();
                    req.EventIds.AddRange(filtrados.Select(e => e.Id != null ? (int)e.Id : 0).OfType<int>().ToList());
                }
                else if (req.ContentType == "3") // Extension
                {
                    var allExtensions = await _sondaEMService.GetExtensions(null, null, null, null, null, null, null, null, null, username);
                    var filtrados = ApiDataService.StaticFilterObjects(allExtensions, request.Filters);
                    if (req.ExtensionIds == null) req.ExtensionIds = new List<int>();
                    req.ExtensionIds.Clear();
                    req.ExtensionIds.AddRange(filtrados.Select(e => e.ExtensionId != null ? (int)e.ExtensionId : 0).OfType<int>().ToList());
                }
                else
                {
                    return BadRequest("ContentType inválido o no soportado");
                }

                DatasetEM newDatasetEM = await _datasetEMService.CreateDatasetEMWithFiltersAsync(req, newDataset.Id, request.Filters);
                await _datasetUMService.UpdateDatasetAsyncEM(newDataset.Id, requestDataset, newDatasetEM);
                return CreatedAtAction(nameof(GetDatasetById), new { datasetId = newDatasetEM.Id, username = newDatasetEM.Username }, newDatasetEM);
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
                return StatusCode(500, $"Error interno al crear el DatasetEM filtrado: {ex.Message}");
            }
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPut("with-filters/{datasetId}")]
        [ProducesResponseType(typeof(DatasetEM), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<DatasetEM>> UpdateDatasetEMFiltered(int datasetId, [FromBody] CreateDatasetEMFilteredRequest request)
        {
            try
            {
                var req = request.DatasetRequest;
                var username = User.Identity?.Name;
                if (string.IsNullOrWhiteSpace(username))
                    return BadRequest("Usuario no encontrado.");
                
                // Usar el username desde JWT
                req.Username = username;
                DatasetEM? existingDataset = await _datasetEMService.GetDatasetEMByIdForEditAsync(datasetId, req.Username);
                if (existingDataset == null)
                {
                    return NotFound($"No se encontró el DatasetEM con ID {datasetId} para el usuario {req.Username}.");
                }

                await _datasetUMService.ValidateDatasetNameAsync(req.Name, req.Username, ModuleType.EventManager, existingDataset.DatasetId);
                var requestDataset = new CreateDatasetRequest(req.Name, req.Username, ModuleType.EventManager);

                // Filtrado para Alert, Event, Extension, Category
                if (req.ContentType == "1") // Alert
                {
                    var allAlerts = await _sondaEMService.GetAlerts(null, null, null, null, null, null, null, null, null, username);
                    var filtrados = ApiDataService.StaticFilterObjects(allAlerts, request.Filters);
                    if (req.AlertIds == null) req.AlertIds = new List<int>();
                    req.AlertIds.Clear();
                    req.AlertIds.AddRange(filtrados.Select(a => a.AlertId != null ? (int)a.AlertId : 0).OfType<int>().ToList());
                }
                else if (req.ContentType == "2") // Event
                {
                    var allEvents = await _sondaEMService.GetEvents(null, null, null, null, username);
                    var filtrados = ApiDataService.StaticFilterObjects(allEvents, request.Filters);
                    if (req.EventIds == null) req.EventIds = new List<int>();
                    req.EventIds.Clear();
                    req.EventIds.AddRange(filtrados.Select(e => e.Id != null ? (int)e.Id : 0).OfType<int>().ToList());
                }
                else if (req.ContentType == "3") // Extension
                {
                    var allExtensions = await _sondaEMService.GetExtensions(null, null, null, null, null, null, null, null, null, username);
                    var filtrados = ApiDataService.StaticFilterObjects(allExtensions, request.Filters);
                    if (req.ExtensionIds == null) req.ExtensionIds = new List<int>();
                    req.ExtensionIds.Clear();
                    req.ExtensionIds.AddRange(filtrados.Select(e => e.ExtensionId != null ? (int)e.ExtensionId : 0).OfType<int>().ToList());
                }
                else
                {
                    return BadRequest("ContentType inválido o no soportado");
                }

                DatasetEM updatedDataset = await _datasetEMService.UpdateDatasetEMWithFiltersAsync(datasetId, req, request.Filters);
                await _datasetUMService.UpdateDatasetAsyncEM(updatedDataset.DatasetId, requestDataset, updatedDataset);
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
                return StatusCode(500, $"Error interno al actualizar el DatasetEM filtrado: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene todos los datasets EM de un usuario.
        /// </summary>
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("GetAllDatasets")]
        [ProducesResponseType(typeof(List<DatasetEM>), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<DatasetEM>>> GetAllDatasets()
        {
            try
            {
                var username = User.Identity?.Name;
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
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("{datasetId}")]
        [ProducesResponseType(typeof(DatasetEM), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<DatasetEM>> GetDatasetById(int datasetId)
        {
            try
            {
                var username = User.Identity?.Name;
                DatasetEM? dataset = await _datasetEMService.GetDatasetEMByIdAsync(datasetId, username);
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
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
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
                DatasetEM? existingDataset = await _datasetEMService.GetDatasetEMByIdForEditAsync(datasetId, request.Username);
                if (existingDataset == null)
                {
                    return NotFound($"No se encontró el dataset con ID {datasetId} para el usuario {request.Username}.");
                }
                await _datasetUMService.ValidateDatasetNameAsync(request.Name, request.Username, ModuleType.EventManager, existingDataset.DatasetId);

                DatasetEM updatedDataset = await _datasetEMService.UpdateDatasetEMAsync(datasetId, request);
                var requestDataset = new CreateDatasetRequest(request.Name, request.Username, ModuleType.EventManager);
                Datasets dataset = await _datasetUMService.UpdateDatasetAsyncEM(updatedDataset.DatasetId, requestDataset, updatedDataset);
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
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpDelete("{datasetId}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> DeleteDataset(int datasetId)
        {
            try
            {
                var username = User.Identity?.Name;
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
