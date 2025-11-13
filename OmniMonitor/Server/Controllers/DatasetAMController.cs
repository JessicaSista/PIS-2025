using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using OmniMonitor.Server.Context;
using OmniMonitor.Server.Services;
using OmniMonitor.Shared.Dtos;

namespace OmniMonitor.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class DatasetAMController : ControllerBase
    {
        private readonly IDatasetAmService _datasetAmService;
        private readonly ISondaAuthService _sondaAuthService;
        private readonly IDatasetUMService _datasetUMService;
        private readonly ApplicationDbContext _context;
        private readonly ISondaAMService _sondaAMService;

        public DatasetAMController(IDatasetAmService datasetAmService, ISondaAuthService sondaAuthService, IDatasetUMService datasetUMService, ApplicationDbContext context, ISondaAMService sondaAMService)
        {
            _datasetAmService = datasetAmService;
            _sondaAuthService = sondaAuthService;
            _datasetUMService = datasetUMService;
            _context = context;
            _sondaAMService = sondaAMService;
        }

        /// <summary>
        /// Crea un nuevo DatasetAM.
        /// </summary>
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost]
        [ProducesResponseType(typeof(DatasetAM), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<DatasetAM>> CreateDatasetAM([FromBody] CreateDatasetAMRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var requestDataset = new CreateDatasetRequest(request.Nombre, request.Username, ModuleType.AssetManager);
                Datasets newDataset = await _datasetUMService.CreateDatasetAsync(requestDataset);
                DatasetAM newDatasetAM = await _datasetAmService.CreateDatasetAMAsync(request, newDataset.Id);
                await _datasetUMService.UpdateDatasetAsyncAM(newDataset.Id, requestDataset, newDatasetAM);

                return CreatedAtAction(nameof(GetDatasetAMByIdForEdit), new { id = newDatasetAM.Id_Dataset, username = newDatasetAM.Username }, newDatasetAM);
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
                return StatusCode(500, $"Error interno al crear el DatasetAM: {ex.Message}");
            }
        }
                /// <summary>
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost("filtered")]
        [ProducesResponseType(typeof(DatasetAM), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<DatasetAM>> CreateDatasetAMFiltered([FromBody] CreateDatasetAMFilteredRequest request)
        {
            try
            {
                var req = request.DatasetRequest;
                var username = User.Identity?.Name;
                if (string.IsNullOrWhiteSpace(username))
                    return BadRequest("Usuario no encontrado.");
                
                // Usar el username desde JWT
                req.Username = username;
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Validar filtros ANTES de crear el dataset general
                if (req.ContentType == "2") // Asset
                {
                    var allAssets = await _sondaAMService.GetAssets(null, null, null, null, null, null, req.Username);
                    Console.WriteLine($"[CREATE AM DATASET] Total Assets obtenidos: {allAssets.Count()}");
                    
                    var filtrados = ApiDataService.StaticFilterObjects(allAssets, request.Filters);
                    Console.WriteLine($"[CREATE AM DATASET] Assets después de filtrar: {filtrados.Count()}");
                    
                    if (!filtrados.Any())
                    {
                        return BadRequest("El filtro no encontró ningún asset. El dataset no puede crearse sin resultados.");
                    }
                    
                    Console.WriteLine($"[CREATE AM DATASET] IDs de assets filtrados:");
                    foreach (var asset in filtrados)
                    {
                        Console.WriteLine($"[CREATE AM DATASET]   - Asset Id: {asset.Id} (tipo: {asset.Id?.GetType().Name})");
                    }
                    
                    var assetIds = new List<string>();
                    foreach (var asset in filtrados)
                    {
                        if (asset.Id != null)
                        {
                            var idStr = asset.Id.ToString();
                            if (!string.IsNullOrEmpty(idStr))
                            {
                                assetIds.Add(idStr);
                            }
                        }
                    }
                    req.Grupo_Asset_Ids = assetIds;
                    
                    Console.WriteLine($"[CREATE AM DATASET] Grupo_Asset_Ids asignado: {string.Join(", ", req.Grupo_Asset_Ids ?? new List<string>())}");
                }
                else if (req.ContentType == "1") // EventTask
                {
                    var allEventTasks = await _sondaAMService.GetEventTaskInstances(
                        "1900-11-01,3030-11-06", null, null, null, null, null, null, null, null, false, false, req.Username);
                    Console.WriteLine($"[CREATE AM DATASET] Total EventTasks obtenidos: {allEventTasks.Count()}");
                    
                    var filtrados = ApiDataService.StaticFilterObjects(allEventTasks, request.Filters);
                    Console.WriteLine($"[CREATE AM DATASET] EventTasks después de filtrar: {filtrados.Count()}");
                    
                    if (!filtrados.Any())
                    {
                        return BadRequest("El filtro no encontró ningún Event Task. El dataset no puede crearse sin resultados.");
                    }
                    
                    req.Grupo_Event_Task_Instance_Ids = filtrados.Select(e => e.Id != null ? Convert.ToInt32(e.Id) : 0).OfType<int>().ToList();
                }
                else if (req.ContentType == "3") // Stock
                {
                    var allStocks = await _sondaAMService.GetAllStock(null, null, null, null, null, req.Username);
                    Console.WriteLine($"[CREATE AM DATASET] Total Stocks obtenidos: {allStocks.Count()}");
                    
                    var filtrados = ApiDataService.StaticFilterObjects(allStocks, request.Filters);
                    Console.WriteLine($"[CREATE AM DATASET] Stocks después de filtrar: {filtrados.Count()}");
                    
                    if (!filtrados.Any())
                    {
                        return BadRequest("El filtro no encontró ningún Stock. El dataset no puede crearse sin resultados.");
                    }
                    
                    req.StockIds = filtrados.Select(e => e.Id != null ? Convert.ToInt32(e.Id) : 0).OfType<int>().ToList();
                }
                else
                {
                    return BadRequest("ContentType inválido o no soportado");
                }

                // Crear el dataset general SOLO después de validar los filtros
                var requestDataset = new CreateDatasetRequest(req.Nombre, req.Username, ModuleType.AssetManager);
                Datasets newDataset = await _datasetUMService.CreateDatasetAsync(requestDataset);

                DatasetAM newDatasetAM = await _datasetAmService.CreateDatasetAMWithFiltersAsync(req, newDataset.Id, request.Filters);
                await _datasetUMService.UpdateDatasetAsyncAM(newDataset.Id, requestDataset, newDatasetAM);
                return CreatedAtAction(nameof(GetDatasetAMByIdForEdit), new { id = newDatasetAM.Id_Dataset, username = newDatasetAM.Username }, newDatasetAM);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"[DEBUG] InvalidOperationException: {ex.Message}");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] Exception: {ex.Message}");
                return StatusCode(500, $"Error interno al crear el DatasetAM filtrado: {ex.Message}");
            }
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPut("with-filters/{id}")]
        [ProducesResponseType(typeof(DatasetAM), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<DatasetAM>> UpdateDatasetAMFiltered(int id, [FromBody] CreateDatasetAMFilteredRequest request)
        {
            try
            {
                var req = request.DatasetRequest;
                var username = User.Identity?.Name;
                if (string.IsNullOrWhiteSpace(username))
                    return BadRequest("Usuario no encontrado.");
                
                // Usar el username desde JWT
                req.Username = username;
                DatasetAM? existingDataset = await _datasetAmService.GetDatasetAMByIdForEditAsync(id, req.Username);
                if (existingDataset == null)
                {
                    return NotFound($"No se encontró el DatasetAM con ID {id} para el usuario {req.Username}.");
                }

                await _datasetUMService.ValidateDatasetNameAsync(req.Nombre, req.Username, ModuleType.AssetManager, existingDataset.DatasetId);

                var requestDataset = new CreateDatasetRequest(req.Nombre, req.Username, ModuleType.AssetManager);

                // Filtrado para EventTask, Asset o Stock
                List<int> filteredIds = new List<int>();
                if (req.ContentType == "2") // Asset
                {
                    var allAssets = await _sondaAMService.GetAssets(null, null, null, null, null, null, username);
                    Console.WriteLine($"[EDIT AM DATASET] Total Assets obtenidos: {allAssets.Count()}");
                    
                    var filtrados = ApiDataService.StaticFilterObjects(allAssets, request.Filters);
                    Console.WriteLine($"[EDIT AM DATASET] Assets después de filtrar: {filtrados.Count()}");
                    
                    if (req.Grupo_Asset_Ids == null) req.Grupo_Asset_Ids = new List<string>();
                    req.Grupo_Asset_Ids.Clear();
                    req.Grupo_Asset_Ids.AddRange(filtrados.Select(a => a.Id != null ? a.Id.ToString() : string.Empty).OfType<string>().ToList());
                }
                else if (req.ContentType == "1") // EventTask
                {
                    var allEventTasks = await _sondaAMService.GetEventTaskInstances(
                        "1900-11-01,3030-11-06", null, null, null, null, null, null, null, null, false, false, username);
                    Console.WriteLine($"[EDIT AM DATASET] Total EventTasks obtenidos: {allEventTasks.Count()}");
                    
                    var filtrados = ApiDataService.StaticFilterObjects(allEventTasks, request.Filters);
                    Console.WriteLine($"[EDIT AM DATASET] EventTasks después de filtrar: {filtrados.Count()}");
                    
                    if (req.Grupo_Event_Task_Instance_Ids == null) req.Grupo_Event_Task_Instance_Ids = new List<int>();
                    req.Grupo_Event_Task_Instance_Ids.Clear();
                    req.Grupo_Event_Task_Instance_Ids.AddRange(filtrados.Select(e => e.Id != null ? Convert.ToInt32(e.Id) : 0).OfType<int>().ToList());
                }
                else if (req.ContentType == "3") // Stock
                {
                    var allStocks = await _sondaAMService.GetAllStock(null, null, null, null, null, username);
                    Console.WriteLine($"[EDIT AM DATASET] Total Stocks obtenidos: {allStocks.Count()}");
                    
                    var filtrados = ApiDataService.StaticFilterObjects(allStocks, request.Filters);
                    Console.WriteLine($"[EDIT AM DATASET] Stocks después de filtrar: {filtrados.Count()}");
                    
                    if (req.StockIds == null) req.StockIds = new List<int>();
                    req.StockIds.Clear();
                    req.StockIds.AddRange(filtrados.Select(e => e.Id != null ? Convert.ToInt32(e.Id) : 0).OfType<int>().ToList());
                }
                else
                {
                    return BadRequest("ContentType inválido o no soportado");
                }

                DatasetAM updatedDataset = await _datasetAmService.UpdateDatasetAMWithFiltersAsync(id, req, request.Filters);
                await _datasetUMService.UpdateDatasetAsyncAM(updatedDataset.DatasetId, requestDataset, updatedDataset);
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
                return StatusCode(500, $"Error interno al actualizar el DatasetAM filtrado: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene todos los DatasetAM para un usuario específico.
        /// </summary>
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("GetAllDatasetAMs")]
        [ProducesResponseType(typeof(List<DatasetAM>), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<DatasetAM>>> GetAllDatasetAMs()
        {
            try
            {
                var username = User.Identity?.Name;
                List<DatasetAM> datasets = await _datasetAmService.GetAllDatasetAMsAsync(username);
                return Ok(datasets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al obtener los DatasetAM: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene un DatasetAM específico por su ID y nombre de usuario (con lógica dinámica).
        /// </summary>
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("GetDatasetAMById")]
        [ProducesResponseType(typeof(DatasetAM), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<DatasetAM>> GetDatasetAMById(int id)
        {
            try
            {
                var username = User.Identity?.Name;
                DatasetAM? dataset = await _datasetAmService.GetDatasetAMByIdAsync(id, username);
                if (dataset == null)
                {
                    return NotFound($"No se encontró el DatasetAM con ID {id} para el usuario {username}.");
                }

                return Ok(dataset);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al obtener el DatasetAM: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene un DatasetAM específico por su ID y nombre de usuario para edición (SIN lógica dinámica).
        /// </summary>
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("GetDatasetAMByIdForEdit")]
        [ProducesResponseType(typeof(DatasetAM), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<DatasetAM>> GetDatasetAMByIdForEdit(int id)
        {
            try
            {
                var username = User.Identity?.Name;
                DatasetAM? dataset = await _datasetAmService.GetDatasetAMByIdForEditAsync(id, username);
                if (dataset == null)
                {
                    return NotFound($"No se encontró el DatasetAM con ID {id} para el usuario {username}.");
                }

                return Ok(dataset);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al obtener el DatasetAM para edición: {ex.Message}");
            }
        }

        /// <summary>
        /// Actualiza un DatasetAM existente.
        /// </summary>
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(DatasetAM), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<DatasetAM>> UpdateDatasetAM(int id, [FromBody] CreateDatasetAMRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                DatasetAM? existingDataset = await _datasetAmService.GetDatasetAMByIdForEditAsync(id, request.Username);
                if (existingDataset == null)
                {
                    return NotFound($"No se encontró el DatasetAM con ID {id} para el usuario {request.Username}.");
                }

                // Primero validar el nombre en la tabla general antes de actualizar cualquier tabla
                await _datasetUMService.ValidateDatasetNameAsync(request.Nombre, request.Username, ModuleType.AssetManager, existingDataset.DatasetId);

                // Llamar al servicio que incluye la validación de nombres únicos
                DatasetAM updatedDataset = await _datasetAmService.UpdateDatasetAMAsync(existingDataset, request);
                var requestDataset = new CreateDatasetRequest(request.Nombre, request.Username, ModuleType.AssetManager);
                Datasets newDataset = await _datasetUMService.UpdateDatasetAsyncAM(updatedDataset.DatasetId, requestDataset, updatedDataset);
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
                return StatusCode(500, $"Error interno al actualizar el DatasetAM: {ex.Message}");
            }
        }

        /// <summary>
        /// Elimina un DatasetAM y todas sus relaciones hijas.
        /// </summary>
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpDelete("{id}")]
        [ProducesResponseType(204)] // No Content
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> DeleteDatasetAM(int id)
        {
            try
            {
                var username = User.Identity?.Name;
                DatasetAM? datasetid = await _context.DatasetAM
                .FirstOrDefaultAsync(d => d.Id_Dataset == id && d.Username == username);
                
                if (datasetid == null)
                {
                    return NotFound($"No se encontró el dataset con ID {id} para el usuario {username}.");
                }
                
                await _datasetAmService.DeleteDatasetAMAsync(id, username);
                await _datasetUMService.DeleteDatasetAsync(datasetid.DatasetId, username);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
