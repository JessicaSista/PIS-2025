using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using OmniMonitor.Server.Attributes;
using OmniMonitor.Server.Context;
using OmniMonitor.Server.Services;
using OmniMonitor.Shared.Dtos;
using OmniMonitor.Shared.Dtos.AM;

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
        private readonly IKpiService _kpiService;
        private readonly ApplicationDbContext _context;
        private readonly ISondaAMService _sondaAMService;

        public DatasetAMController(IDatasetAmService datasetAmService, ISondaAuthService sondaAuthService, IDatasetUMService datasetUMService, IKpiService kpiService, ApplicationDbContext context, ISondaAMService sondaAMService)
        {
            _datasetAmService = datasetAmService;
            _sondaAuthService = sondaAuthService;
            _datasetUMService = datasetUMService;
            _kpiService = kpiService;
            _context = context;
            _sondaAMService = sondaAMService;
        }

        [HttpPost("filtered")]
        [RequirePermission("Datasets.Create")]
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
                // Mapear ContentType a Type_Dataset
                if (req.ContentType == "2") // Asset
                {
                    req.Type_Dataset = 2; // Establecer Type_Dataset para Asset
                    var allAssets = await _sondaAMService.GetAssets(null, null, null, null, null, null, req.Username);
                    
                    // Si hay filtros, aplicarlos y validar que haya resultados
                    // Si no hay filtros, incluir todo (no filtrar)
                    IEnumerable<object> filtrados;
                    if (request.Filters != null && request.Filters.Any())
                    {
                        filtrados = ApiDataService.StaticFilterObjects(allAssets, request.Filters);
                        if (!filtrados.Any())
                        {
                            return BadRequest("El filtro no encontró ningún asset. El dataset no puede crearse sin resultados.");
                        }
                    }
                    else
                    {
                        // Sin filtros: incluir todo
                        filtrados = allAssets.Cast<object>();
                    }
                    
                    var assetIds = new List<string>();
                    foreach (var assetObj in filtrados)
                    {
                        if (assetObj is AssetDto asset && asset.Id != null)
                        {
                            var idStr = asset.Id.ToString();
                            if (!string.IsNullOrEmpty(idStr))
                            {
                                assetIds.Add(idStr);
                            }
                        }
                    }
                    req.Grupo_Asset_Ids = assetIds;
                    
                    // [CREATE AM DATASET] Grupo_Asset_Ids asignado (log de consola removido).
                }
                else if (req.ContentType == "1") // EventTask
                {
                    req.Type_Dataset = 1; // Establecer Type_Dataset para EventTask
                    var allEventTasks = await _sondaAMService.GetEventTaskInstances(
                        "1900-11-01,3030-11-06", null, null, null, null, null, null, null, null, false, false, req.Username);
                    
                    // Si hay filtros, aplicarlos y validar que haya resultados
                    // Si no hay filtros, incluir todo (no filtrar)
                    IEnumerable<object> filtrados;
                    if (request.Filters != null && request.Filters.Any())
                    {
                        filtrados = ApiDataService.StaticFilterObjects(allEventTasks, request.Filters);
                        if (!filtrados.Any())
                        {
                            return BadRequest("El filtro no encontró ningún Event Task. El dataset no puede crearse sin resultados.");
                        }
                    }
                    else
                    {
                        // Sin filtros: incluir todo
                        filtrados = allEventTasks.Cast<object>();
                    }
                    
                    req.Grupo_Event_Task_Instance_Ids = filtrados
                        .OfType<EventTaskInstanceDto>()
                        .Select(e => e.Id != null ? Convert.ToInt32(e.Id) : 0)
                        .OfType<int>()
                        .ToList();
                }
                else if (req.ContentType == "3") // Stock
                {
                    req.Type_Dataset = 3; // Establecer Type_Dataset para Stock
                    var allStocks = await _sondaAMService.GetAllStock(null, null, null, null, null, req.Username);
                    
                    // Si hay filtros, aplicarlos y validar que haya resultados
                    // Si no hay filtros, incluir todo (no filtrar)
                    IEnumerable<object> filtrados;
                    if (request.Filters != null && request.Filters.Any())
                    {
                        filtrados = ApiDataService.StaticFilterObjects(allStocks, request.Filters);
                        if (!filtrados.Any())
                        {
                            return BadRequest("El filtro no encontró ningún Stock. El dataset no puede crearse sin resultados.");
                        }
                    }
                    else
                    {
                        // Sin filtros: incluir todo
                        filtrados = allStocks.Cast<object>();
                    }
                    
                    req.StockIds = filtrados
                        .OfType<EventTaskInstanceStockDto>()
                        .Select(e => e.Id != null ? Convert.ToInt32(e.Id) : 0)
                        .OfType<int>()
                        .ToList();
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
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al crear el DatasetAM filtrado: {ex.Message}");
            }
        }

        [HttpPut("with-filters/{id}")]
        [RequirePermission("Datasets.Edit")]
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

                // Validar filtros ANTES de actualizar el dataset
                // Mapear ContentType a Type_Dataset
                if (req.ContentType == "2") // Asset
                {
                    req.Type_Dataset = 2; // Establecer Type_Dataset para Asset
                    var allAssets = await _sondaAMService.GetAssets(null, null, null, null, null, null, username);
                    
                    // Si hay filtros, aplicarlos y validar que haya resultados
                    // Si no hay filtros, incluir todo (no filtrar)
                    IEnumerable<object> filtrados;
                    if (request.Filters != null && request.Filters.Any())
                    {
                        filtrados = ApiDataService.StaticFilterObjects(allAssets, request.Filters);
                        if (!filtrados.Any())
                        {
                            return BadRequest("El filtro no encontró ningún asset. El dataset no puede actualizarse sin resultados.");
                        }
                    }
                    else
                    {
                        // Sin filtros: incluir todo
                        filtrados = allAssets.Cast<object>();
                    }
                    
                    if (req.Grupo_Asset_Ids == null) req.Grupo_Asset_Ids = new List<string>();
                    req.Grupo_Asset_Ids.Clear();
                    
                    var assetIds = new List<string>();
                    foreach (var assetObj in filtrados)
                    {
                        if (assetObj is AssetDto asset && asset.Id != null)
                        {
                            var idStr = asset.Id.ToString();
                            if (!string.IsNullOrEmpty(idStr))
                            {
                                assetIds.Add(idStr);
                            }
                        }
                    }
                    req.Grupo_Asset_Ids = assetIds;
                }
                else if (req.ContentType == "1") // EventTask
                {
                    req.Type_Dataset = 1; // Establecer Type_Dataset para EventTask
                    var allEventTasks = await _sondaAMService.GetEventTaskInstances(
                        "1900-11-01,3030-11-06", null, null, null, null, null, null, null, null, false, false, username);
                    
                    // Si hay filtros, aplicarlos y validar que haya resultados
                    // Si no hay filtros, incluir todo (no filtrar)
                    IEnumerable<object> filtrados;
                    if (request.Filters != null && request.Filters.Any())
                    {
                        filtrados = ApiDataService.StaticFilterObjects(allEventTasks, request.Filters);
                        if (!filtrados.Any())
                        {
                            return BadRequest("El filtro no encontró ningún Event Task. El dataset no puede actualizarse sin resultados.");
                        }
                    }
                    else
                    {
                        // Sin filtros: incluir todo
                        filtrados = allEventTasks.Cast<object>();
                    }
                    
                    if (req.Grupo_Event_Task_Instance_Ids == null) req.Grupo_Event_Task_Instance_Ids = new List<int>();
                    req.Grupo_Event_Task_Instance_Ids.Clear();
                    req.Grupo_Event_Task_Instance_Ids.AddRange(filtrados
                        .OfType<EventTaskInstanceDto>()
                        .Select(e => e.Id != null ? Convert.ToInt32(e.Id) : 0)
                        .OfType<int>()
                        .ToList());
                }
                else if (req.ContentType == "3") // Stock
                {
                    req.Type_Dataset = 3; // Establecer Type_Dataset para Stock
                    var allStocks = await _sondaAMService.GetAllStock(null, null, null, null, null, username);
                    
                    // Si hay filtros, aplicarlos y validar que haya resultados
                    // Si no hay filtros, incluir todo (no filtrar)
                    IEnumerable<object> filtrados;
                    if (request.Filters != null && request.Filters.Any())
                    {
                        filtrados = ApiDataService.StaticFilterObjects(allStocks, request.Filters);
                        if (!filtrados.Any())
                        {
                            return BadRequest("El filtro no encontró ningún Stock. El dataset no puede actualizarse sin resultados.");
                        }
                    }
                    else
                    {
                        // Sin filtros: incluir todo
                        filtrados = allStocks.Cast<object>();
                    }
                    
                    if (req.StockIds == null) req.StockIds = new List<int>();
                    req.StockIds.Clear();
                    req.StockIds.AddRange(filtrados
                        .OfType<EventTaskInstanceStockDto>()
                        .Select(e => e.Id != null ? Convert.ToInt32(e.Id) : 0)
                        .OfType<int>()
                        .ToList());
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
        [HttpGet("GetAllDatasetAMs")]
        [RequirePermission("Datasets.View")]
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
        [HttpGet("GetDatasetAMById")]
        [RequirePermission("Datasets.View")]
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
        [HttpGet("GetDatasetAMByIdForEdit")]
        [RequirePermission("Datasets.View")]
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
        /// Elimina un DatasetAM y todas sus relaciones hijas.
        /// </summary>
        [HttpDelete("{id}")]
        [RequirePermission("Datasets.Delete")]
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
                // 1. Buscar visualizaciones que solo tengan este dataset
                var visualizacionesAEliminar = await _context.Set<Visualizacion>()
                    .Include(v => v.GrupoDatasets)
                    .Where(v => v.GrupoDatasets.Count == 1 && v.GrupoDatasets.Any(gd => gd.DatasetId == datasetid.DatasetId))
                    .ToListAsync();
                foreach (var visualizacion in visualizacionesAEliminar)
                {
                    try
                    {
                        // Eliminar todos los GrupoVisualizacion asociados
                        IQueryable<GrupoVisualizacion> gruposVisualizacion = _context.GrupoVisualizaciones.Where(gv => gv.IdVisualizacion == visualizacion.IdVisualizacion);
                        _context.GrupoVisualizaciones.RemoveRange(gruposVisualizacion);

                        // Eliminar todos los GrupoDataset asociados
                        _context.GrupoDatasets.RemoveRange(visualizacion.GrupoDatasets);
                        _context.Visualizaciones.Remove(visualizacion);
                        await _context.SaveChangesAsync();
                    }
                    catch
                    {
                        // Si falla la eliminación de una Visualizacion, continuar con los demás
                    }
                }
                var grupos = await _context.GrupoDatasets
                    .Where(gd => gd.DatasetId == datasetid.DatasetId)
                    .ToListAsync();
                _context.GrupoDatasets.RemoveRange(grupos);
                await _context.SaveChangesAsync();

                // Eliminar KPIs asociados a este dataset
                var kpisToDelete = await _context.Kpi
                    .Where(k => k.DatasetId == id && k.SourceModule.ToUpper() == "AM")
                    .ToListAsync();
                
                foreach (var kpi in kpisToDelete)
                {
                    try
                    {
                        await _kpiService.DeleteKpiAsync(kpi.Id, username);
                    }
                    catch
                    {
                        // Si falla la eliminación de un KPI, continuar con los demás
                    }
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
