using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using OmniMonitor.Server.Attributes;
using OmniMonitor.Server.Context;
using OmniMonitor.Server.Services;
using OmniMonitor.Shared.Dtos;
using OmniMonitor.Shared.Dtos.EM;

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
        private readonly IKpiService _kpiService;
        private readonly ApplicationDbContext _context;
        private readonly IReportService _reportService;

        public DatasetEMController(IDatasetEMService datasetEMService, ISondaAuthService sondaAuthService, IDatasetUMService datasetUMService, ISondaEMService sondaEMService, IKpiService kpiService, ApplicationDbContext context, IReportService reportService)
        {
            _datasetEMService = datasetEMService;
            _sondaAuthService = sondaAuthService;
            _datasetUMService = datasetUMService;
            _sondaEMService = sondaEMService;
            _kpiService = kpiService;
            _context = context;
            _reportService = reportService;
        }

        [HttpPost("filtered")]
        [RequirePermission("Datasets.Create")]
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
                // Validar filtros ANTES de crear el dataset general
                if (req.ContentType == "1") // Alert
                {
                    var allAlerts = await _sondaEMService.GetAlerts(null, null, null, null, null, null, null, null, null, username);
                    // Si hay filtros, aplicarlos y validar que haya resultados
                    // Si no hay filtros, incluir todo (no filtrar)
                    IEnumerable<object> filtrados;
                    if (request.Filters != null && request.Filters.Any())
                    {
                        filtrados = ApiDataService.StaticFilterObjects(allAlerts, request.Filters);
                        if (!filtrados.Any())
                        {
                            return BadRequest("El filtro no encontró ninguna alerta. El dataset no puede crearse sin resultados.");
                        }
                    }
                    else
                    {
                        // Sin filtros: incluir todo
                        filtrados = allAlerts.Cast<object>();
                    }
                    
                    if (req.AlertIds == null) req.AlertIds = new List<int>();
                    req.AlertIds.Clear();
                    req.AlertIds.AddRange(filtrados
                        .OfType<AlertDto>()
                        .Select(a => a.AlertId != null ? (int)a.AlertId : 0)
                        .OfType<int>()
                        .ToList());
                }
                else if (req.ContentType == "2") // Event
                {
                    var allEvents = await _sondaEMService.GetEvents(null, null, null, null, username);
                    // Si hay filtros, aplicarlos y validar que haya resultados
                    // Si no hay filtros, incluir todo (no filtrar)
                    IEnumerable<object> filtrados;
                    if (request.Filters != null && request.Filters.Any())
                    {
                        filtrados = ApiDataService.StaticFilterObjects(allEvents, request.Filters);
                        if (!filtrados.Any())
                        {
                            return BadRequest("El filtro no encontró ningún evento. El dataset no puede crearse sin resultados.");
                        }
                    }
                    else
                    {
                        // Sin filtros: incluir todo
                        filtrados = allEvents.Cast<object>();
                    }
                    
                    if (req.EventIds == null) req.EventIds = new List<int>();
                    req.EventIds.Clear();
                    req.EventIds.AddRange(filtrados
                        .OfType<EventDto>()
                        .Select(e => e.Id != null ? (int)e.Id : 0)
                        .OfType<int>()
                        .ToList());
                }
                else if (req.ContentType == "3") // Extension
                {
                    var allExtensions = await _sondaEMService.GetExtensions(null, null, null, null, null, null, null, null, null, username);
                    // Si hay filtros, aplicarlos y validar que haya resultados
                    // Si no hay filtros, incluir todo (no filtrar)
                    IEnumerable<object> filtrados;
                    if (request.Filters != null && request.Filters.Any())
                    {
                        filtrados = ApiDataService.StaticFilterObjects(allExtensions, request.Filters);
                        if (!filtrados.Any())
                        {
                            return BadRequest("El filtro no encontró ninguna extensión. El dataset no puede crearse sin resultados.");
                        }
                    }
                    else
                    {
                        // Sin filtros: incluir todo
                        filtrados = allExtensions.Cast<object>();
                    }
                    
                    if (req.ExtensionIds == null) req.ExtensionIds = new List<int>();
                    req.ExtensionIds.Clear();
                    req.ExtensionIds.AddRange(filtrados
                        .OfType<ExtensionDto>()
                        .Select(e => e.ExtensionId != null ? (int)e.ExtensionId : 0)
                        .OfType<int>()
                        .ToList());
                }
                else
                {
                    return BadRequest("ContentType inválido o no soportado");
                }

                // Crear el dataset general SOLO después de validar los filtros
                var requestDataset = new CreateDatasetRequest(req.Name, req.Username, ModuleType.EventManager);
                Datasets newDataset = await _datasetUMService.CreateDatasetAsync(requestDataset);

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

        [HttpPut("with-filters/{datasetId}")]
        [RequirePermission("Datasets.Edit")]
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

                // Validar filtros ANTES de actualizar el dataset
                if (req.ContentType == "1") // Alert
                {
                    var allAlerts = await _sondaEMService.GetAlerts(null, null, null, null, null, null, null, null, null, username);
                    // Si hay filtros, aplicarlos y validar que haya resultados
                    // Si no hay filtros, incluir todo (no filtrar)
                    IEnumerable<object> filtrados;
                    if (request.Filters != null && request.Filters.Any())
                    {
                        filtrados = ApiDataService.StaticFilterObjects(allAlerts, request.Filters);
                        if (!filtrados.Any())
                        {
                            return BadRequest("El filtro no encontró ninguna alerta. El dataset no puede actualizarse sin resultados.");
                        }
                    }
                    else
                    {
                        // Sin filtros: incluir todo
                        filtrados = allAlerts.Cast<object>();
                    }
                    
                    if (req.AlertIds == null) req.AlertIds = new List<int>();
                    req.AlertIds.Clear();
                    req.AlertIds.AddRange(filtrados
                        .OfType<AlertDto>()
                        .Select(a => a.AlertId != null ? (int)a.AlertId : 0)
                        .OfType<int>()
                        .ToList());
                }
                else if (req.ContentType == "2") // Event
                {
                    var allEvents = await _sondaEMService.GetEvents(null, null, null, null, username);
                    // Si hay filtros, aplicarlos y validar que haya resultados
                    // Si no hay filtros, incluir todo (no filtrar)
                    IEnumerable<object> filtrados;
                    if (request.Filters != null && request.Filters.Any())
                    {
                        filtrados = ApiDataService.StaticFilterObjects(allEvents, request.Filters);
                        if (!filtrados.Any())
                        {
                            return BadRequest("El filtro no encontró ningún evento. El dataset no puede actualizarse sin resultados.");
                        }
                    }
                    else
                    {
                        // Sin filtros: incluir todo
                        filtrados = allEvents.Cast<object>();
                    }
                    
                    if (req.EventIds == null) req.EventIds = new List<int>();
                    req.EventIds.Clear();
                    req.EventIds.AddRange(filtrados
                        .OfType<EventDto>()
                        .Select(e => e.Id != null ? (int)e.Id : 0)
                        .OfType<int>()
                        .ToList());
                }
                else if (req.ContentType == "3") // Extension
                {
                    var allExtensions = await _sondaEMService.GetExtensions(null, null, null, null, null, null, null, null, null, username);
                    // Si hay filtros, aplicarlos y validar que haya resultados
                    // Si no hay filtros, incluir todo (no filtrar)
                    IEnumerable<object> filtrados;
                    if (request.Filters != null && request.Filters.Any())
                    {
                        filtrados = ApiDataService.StaticFilterObjects(allExtensions, request.Filters);
                        if (!filtrados.Any())
                        {
                            return BadRequest("El filtro no encontró ninguna extensión. El dataset no puede actualizarse sin resultados.");
                        }
                    }
                    else
                    {
                        // Sin filtros: incluir todo
                        filtrados = allExtensions.Cast<object>();
                    }
                    
                    if (req.ExtensionIds == null) req.ExtensionIds = new List<int>();
                    req.ExtensionIds.Clear();
                    req.ExtensionIds.AddRange(filtrados
                        .OfType<ExtensionDto>()
                        .Select(e => e.ExtensionId != null ? (int)e.ExtensionId : 0)
                        .OfType<int>()
                        .ToList());
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
        [HttpGet("GetAllDatasets")]
        [RequirePermission("Datasets.View")]
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
        [HttpGet("{datasetId}")]
        [RequirePermission("Datasets.View")]
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
        /// Elimina un dataset EM.
        /// </summary>
        [HttpDelete("{datasetId}")]
        [RequirePermission("Datasets.Delete")]
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

                // --- INICIO LÓGICA DE REPORTES Y JOINS ---
                // Buscar todos los joins donde este dataset
                var joinOperands = await _context.JoinOperands
                   .Where(j => j.DatasetId == id.Id && j.ModuleType == ModuleType.EventManager)
                    .ToListAsync();

                foreach (var joinOperand in joinOperands)
                {
                    // Buscar el join completo
                    var join = await _context.CrossModuleJoins
                        .Include(j => j.LeftOperand)
                        .Include(j => j.RightOperand)
                        .FirstOrDefaultAsync(j =>
                            (j.LeftOperand.DatasetId == joinOperand.DatasetId || j.RightOperand.DatasetId == joinOperand.DatasetId));

                    if (join == null) continue;

                    // Buscar todos los reportes que usan este join
                    var reportJoins = await _context.ReportJoins
                        .Where(rj => rj.CrossModuleJoinId == join.Id)
                        .ToListAsync();
                    //en este momento el join esta en un solo reporte
                    foreach (var reportJoin in reportJoins)
                    {
                        // ¿Cuántos joins tiene este reporte?
                        var joinsDelReporte = await _context.ReportJoins
                            .Where(rj => rj.ReportId == reportJoin.ReportId)
                            .ToListAsync();

                        if (joinsDelReporte.Count == 1)
                        {
                            // Es el único join del reporte, borrar el reporte completo
                            await _reportService.DeleteReportAsync(reportJoin.ReportId, username);
                        }
                        else
                        {
                            // El reporte tiene más de un join, solo eliminar el join
                            await _reportService.RemoveJoinFromReportAsync(reportJoin.ReportId, join.Id, username);
                        }
                    }

                }

                // 1. Buscar visualizaciones que solo tengan este dataset
                var visualizacionesAEliminar = await _context.Set<Visualizacion>()
                    .Include(v => v.GrupoDatasets)
                    .Where(v => v.GrupoDatasets.Count == 1 && v.GrupoDatasets.Any(gd => gd.DatasetId == id.DatasetId))
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
                    catch(Exception ex)
                    {
                        return StatusCode(500, $"Error interno al eliminar la visualizacion: {ex.Message}");
                    }
                }
                var grupos = await _context.GrupoDatasets
                    .Where(gd => gd.DatasetId == id.DatasetId)
                    .ToListAsync();
                _context.GrupoDatasets.RemoveRange(grupos);
                await _context.SaveChangesAsync();

                // Eliminar KPIs asociados a este dataset
                var kpisToDelete = await _context.Kpi
                    .Where(k => k.DatasetId == datasetId && k.SourceModule.ToUpper() == "EM")
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
