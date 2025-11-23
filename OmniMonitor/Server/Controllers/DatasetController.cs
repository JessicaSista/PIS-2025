using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

using OmniMonitor.Server.Attributes;
using OmniMonitor.Server.Context;
using OmniMonitor.Server.Resources;
using OmniMonitor.Server.Services;
using OmniMonitor.Shared.Dtos;

namespace OmniMonitor.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class DatasetController : ControllerBase
    {
        #region Fields

        private readonly IDatasetService _datasetService;
        private readonly ISondaAuthService _sondaAuthService;
        private readonly ISondaIMService _sondaIMService;
        private readonly IDatasetUMService _datasetUMService;
        private readonly IKpiService _kpiService;
        private readonly ApplicationDbContext _context;

        #endregion

        #region Constructors

        public DatasetController(IDatasetService datasetService, ISondaAuthService sondaAuthService, ISondaIMService sondaIMService, IDatasetUMService datasetUMService, IKpiService kpiService, ApplicationDbContext context)
        {
            _datasetService = datasetService;
            _sondaAuthService = sondaAuthService;
            _sondaIMService = sondaIMService;
            _datasetUMService = datasetUMService;
            _kpiService = kpiService;
            _context = context;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Crea un nuevo dataset.
        /// </summary>
        [RequirePermission("Datasets.Create")]
        [HttpPost]
        [ProducesResponseType(typeof(DatasetIM), 201)] // 201 Created
        [ProducesResponseType(400)] // Bad Request
        [ProducesResponseType(500)]
        public async Task<ActionResult<DatasetIM>> CreateDatasetAsync([FromBody] CreateDatasetIMRequest request)
        {
            try
            {
                var username = User.Identity?.Name;
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var requestDataset = new CreateDatasetRequest(request.Name, username, ModuleType.InsightMonitor);
                Datasets dataset = await _datasetUMService.CreateDatasetAsync(requestDataset);
                DatasetIM newDataset = await _datasetService.CreateDatasetIMAsync(request, dataset.Id, username);
                await _datasetUMService.UpdateDatasetAsyncIM(dataset.Id, requestDataset, newDataset);

                // Devuelve una respuesta 201 Created con la ubicación del nuevo recurso
                return CreatedAtAction(nameof(GetDatasetByIdAsync), new { datasetId = newDataset.Id, username = newDataset.Username }, newDataset);
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
                return StatusCode(500, string.Format(Language.DatasetCreateError, ex.Message));
            }
        }

        /// <summary>
        /// Crea un nuevo dataset no formal aplicando filtros y persistiendo los elementos filtrados.
        /// </summary>
        [RequirePermission("Datasets.Create")]
        [HttpPost("filtered")]
        [ProducesResponseType(typeof(DatasetIM), 201)] // 201 Created
        [ProducesResponseType(400)] // Bad Request
        [ProducesResponseType(500)]
        public async Task<ActionResult<DatasetIM>> CreateFilteredDatasetAsync([FromBody] CreateDatasetIMRequest request)
        {
            try
            {
                var username = User.Identity?.Name;
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (request.IsDataset == "S")
                {
                    return BadRequest(Language.DatasetFilteredOnly);
                }

                // Permitir datasets sin filtros (se crearán vacíos)
                if (request.Filters == null)
                {
                    request.Filters = new List<FilterCondition>();
                }

                if (string.IsNullOrWhiteSpace(username))
                    return BadRequest(Language.UserNotFound);

                // Validar filtros ANTES de crear el dataset general
                IEnumerable<object> entidades = Enumerable.Empty<object>();
                string entidadNombre = "";

                if (request.ContentType == "1") // Device
                {
                    entidades = await _sondaIMService.GetAllDevices(username) ?? new List<Device>();
                    entidadNombre = "dispositivo";
                }
                else if (request.ContentType == "2") // Source
                {
                    var sources = await _sondaIMService.GetAllSources(username) ?? new List<Source>();

                    // Si hay filtros que requieren Devices o Sensors, poblar esas propiedades
                    bool needsDevices = request.Filters.Any(f => f.AttributeName.StartsWith("Devices.", StringComparison.OrdinalIgnoreCase));
                    bool needsSensors = request.Filters.Any(f => f.AttributeName.StartsWith("Sensors.", StringComparison.OrdinalIgnoreCase));

                    if (needsDevices || needsSensors)
                    {
                        // Poblar Devices para cada Source
                        foreach (var source in sources)
                        {
                            if (source != null)
                            {
                                var devices = await _sondaIMService.GetDeviceOfSource(source.Id, username) ?? new List<Device>();
                                source.Devices = devices;

                                // Si también se necesitan Sensors, extraerlos de los devices
                                if (needsSensors && devices.Any())
                                {
                                    var sensors = devices
                                        .Where(d => d.Sensors != null)
                                        .SelectMany(d => d.Sensors!)
                                        .DistinctBy(s => s.Name)
                                        .ToList();
                                    source.Sensors = sensors;
                                }
                            }
                        }
                    }

                    entidades = sources;
                    entidadNombre = "fuente";
                }
                else if (request.ContentType == "3") // Sensor
                {
                    var allDevices = await _sondaIMService.GetAllDevices(username) ?? new List<Device>();
                    var allSensors = allDevices
                        .Where(d => d.Sensors != null)
                        .SelectMany(d => d.Sensors!)
                        .GroupBy(s => s.Name)
                        .Select(g => g.First())
                        .Cast<object>()
                        .ToList();
                    entidades = allSensors;
                    entidadNombre = "sensor";
                }
                else
                {
                    return BadRequest(Language.ContentTypeInvalid);
                }

                // Si hay filtros, aplicarlos y validar que haya resultados
                // Si no hay filtros, incluir todo (no filtrar)
                IEnumerable<object> filtrados;
                if (request.Filters != null && request.Filters.Any())
                {
                    filtrados = ApiDataService.StaticFilterObjects(entidades, request.Filters);
                    // Si hay filtros pero no hay resultados, no crear el dataset
                    if (!filtrados.Any())
                    {
                        return BadRequest(string.Format(Language.FilterNoResults, entidadNombre));
                    }
                    request.JsonFilters = JsonSerializer.Serialize(request.Filters);
                }
                else
                {
                    // Sin filtros: incluir todo
                    filtrados = entidades;
                    request.JsonFilters = "[]";
                }

                // Crear el dataset general
                var requestDataset = new CreateDatasetRequest(request.Name, username, ModuleType.InsightMonitor);
                Datasets dataset = null;
                try
                {
                    dataset = await _datasetUMService.CreateDatasetAsync(requestDataset);

                    // Esta llamada validará nuevamente los filtros y lanzará excepción si no hay resultados
                    DatasetIM newDataset = await _datasetService.CreateDatasetIMFilteredAsync(request, dataset.Id, username);
                    await _datasetUMService.UpdateDatasetAsyncIM(dataset.Id, requestDataset, newDataset);

                    // Devuelve una respuesta 201 Created con la ubicación del nuevo recurso
                    return CreatedAtAction(nameof(GetDatasetByIdAsync), new { datasetId = newDataset.Id, username = newDataset.Username }, newDataset);
                }
                catch (Exception)
                {
                    // Si falla la creación del dataset IM, eliminar el dataset general que se creó
                    if (dataset != null)
                    {
                        try
                        {
                            var datasetToDelete = await _context.Datasets.FindAsync(dataset.Id);
                            if (datasetToDelete != null)
                            {
                                _context.Datasets.Remove(datasetToDelete);
                                await _context.SaveChangesAsync();
                            }
                        }
                        catch
                        {
                            // Si falla la eliminación, registrar el error pero no lanzar excepción
                            // para que el error original se propague
                        }
                    }
                    throw;
                }
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
                var errorMessage = string.Format(Language.DatasetCreateFilteredError, ex.Message);
                if (ex.InnerException != null)
                {
                    errorMessage += $" | Inner Exception: {ex.InnerException.Message}";
                }
                return StatusCode(500, errorMessage);
            }
        }

        /// <summary>
        /// Obtiene todos los datasets para un usuario específico.
        /// </summary>
        [HttpGet("user")]
        [RequirePermission("Datasets.View")]
        [ProducesResponseType(typeof(List<DatasetIM>), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<DatasetIM>>> GetAllDatasetsAsync([FromQuery] string? search = null)
        {
            try
            {
                var username = User.Identity?.Name;

                // Por ahora usamos el método sin búsqueda y filtramos en memoria
                // TODO: Implementar búsqueda en el servicio cuando sea necesario
                List<DatasetIM> datasets = await _datasetService.GetAllDatasetsIMAsync(username);

                // Si hay un término de búsqueda, filtramos en memoria
                if (!string.IsNullOrWhiteSpace(search))
                {
                    string normalizedSearch = NormalizeText(search);
                    datasets = datasets.Where(d => NormalizeText(d.Name).Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                return Ok(datasets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, string.Format(Language.DatasetGetError, ex.Message));
            }
        }

        /// <summary>
        /// Identifica rápidamente a qué módulo pertenece un dataset.
        /// Retorna: "Insight Monitor", "Asset Manager", "Urban Monitor", o null si no se encuentra.
        /// </summary>
        [HttpGet("GetDatasetModule")]
        [RequirePermission("Datasets.View")]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<string>> GetDatasetModuleAsync(int datasetId)
        {
            try
            {
                var username = User.Identity?.Name ?? string.Empty;
                string? module = await _datasetService.IdentifyDatasetModuleAsync(datasetId, username);

                if (module == null)
                {
                    return NotFound(string.Format(Language.DatasetNotFoundSimple, datasetId));
                }

                return Ok(module);
            }
            catch (Exception ex)
            {
                return StatusCode(500, string.Format(Language.DatasetModuleError, ex.Message));
            }
        }

        /// <summary>
        /// Obtiene un dataset específico por su ID y nombre de usuario.
        /// </summary>
        [HttpGet("GetDataset")]
        [RequirePermission("Datasets.View")]
        [ProducesResponseType(typeof(DatasetIM), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<DatasetIM>> GetDatasetByIdAsync(int datasetId)
        {
            try
            {
                var username = User.Identity?.Name ?? string.Empty;
                DatasetIM? dataset = await _datasetService.GetDatasetIMByIdForEditAsync(datasetId, username);
                if (dataset == null)
                {
                    return NotFound(string.Format(Language.DatasetNotFound, datasetId, username));
                }

                return Ok(dataset);
            }
            catch (Exception ex)
            {
                return StatusCode(500, string.Format(Language.DatasetGetByIdError, ex.Message));
            }
        }

        [AllowAnonymous]
        [HttpGet("GetDatasetSinToken")]
        [ProducesResponseType(typeof(DatasetIM), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<DatasetIM>> GetDatasetByIdWithoutTokenAsync(int datasetId)
        {
            try
            {
                DatasetIM? dataset = await _datasetService.GetDatasetIMByIdForEditAsyncSinToken(datasetId);
                if (dataset == null)
                {
                    return NotFound(string.Format(Language.DatasetNotFoundSimple, datasetId));
                }

                return Ok(dataset);
            }
            catch (Exception ex)
            {
                return StatusCode(500, string.Format(Language.DatasetGetByIdError, ex.Message));
            }
        }

        /// <summary>
        /// Actualiza un dataset existente.
        /// </summary>
        [HttpPut("{datasetId}")]
        [RequirePermission("Datasets.Edit")]
        [ProducesResponseType(typeof(DatasetIM), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<DatasetIM>> UpdateDatasetAsync(int datasetId, [FromBody] CreateDatasetIMRequest request)
        {
            try
            {
                var username = User.Identity?.Name ?? string.Empty;
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                DatasetIM? existingDataset = await _datasetService.GetDatasetIMByIdForEditAsync(datasetId, username);
                if (existingDataset == null)
                {
                    return NotFound(string.Format(Language.DatasetNotFound, datasetId, username));
                }

                // Si es un dataset no formal y tiene filtros, validar que encuentren resultados
                if (request.IsDataset == "N" && request.Filters != null && request.Filters.Any())
                {
                    // Validar filtros ANTES de actualizar
                    if (string.IsNullOrWhiteSpace(username))
                        return BadRequest(Language.UserNotFound);

                    IEnumerable<object> entidades = Enumerable.Empty<object>();
                    string entidadNombre = "";

                    if (request.ContentType == "1") // Device
                    {
                        entidades = await _sondaIMService.GetAllDevices(username) ?? new List<Device>();
                        entidadNombre = "dispositivo";
                    }
                    else if (request.ContentType == "2") // Source
                    {
                        var sources = await _sondaIMService.GetAllSources(username) ?? new List<Source>();

                        // Si hay filtros que requieren Devices o Sensors, poblar esas propiedades
                        bool needsDevices = request.Filters.Any(f => f.AttributeName.StartsWith("Devices.", StringComparison.OrdinalIgnoreCase));
                        bool needsSensors = request.Filters.Any(f => f.AttributeName.StartsWith("Sensors.", StringComparison.OrdinalIgnoreCase));

                        if (needsDevices || needsSensors)
                        {
                            foreach (var source in sources)
                            {
                                if (source != null)
                                {
                                    var devices = await _sondaIMService.GetDeviceOfSource(source.Id, username) ?? new List<Device>();
                                    source.Devices = devices;

                                    if (needsSensors && devices.Any())
                                    {
                                        var sensors = devices
                                            .Where(d => d.Sensors != null)
                                            .SelectMany(d => d.Sensors!)
                                            .DistinctBy(s => s.Name)
                                            .ToList();
                                        source.Sensors = sensors;
                                    }
                                }
                            }
                        }

                        entidades = sources;
                        entidadNombre = "fuente";
                    }
                    else if (request.ContentType == "3") // Sensor
                    {
                        var allDevices = await _sondaIMService.GetAllDevices(username) ?? new List<Device>();
                        var allSensors = allDevices
                            .Where(d => d.Sensors != null)
                            .SelectMany(d => d.Sensors!)
                            .GroupBy(s => s.Name)
                            .Select(g => g.First())
                            .Cast<object>()
                            .ToList();
                        entidades = allSensors;
                        entidadNombre = "sensor";
                    }

                    // Si hay filtros, aplicarlos y validar que haya resultados
                    // Si no hay filtros, incluir todo (no filtrar)
                    IEnumerable<object> filtrados;
                    if (request.Filters != null && request.Filters.Any())
                    {
                        filtrados = ApiDataService.StaticFilterObjects(entidades, request.Filters);
                        // Si hay filtros pero no hay resultados, no actualizar el dataset
                        if (!filtrados.Any())
                        {
                            return BadRequest(string.Format(Language.FilterNoResultsUpdate, entidadNombre));
                        }
                        request.JsonFilters = JsonSerializer.Serialize(request.Filters);
                    }
                    else
                    {
                        // Sin filtros: incluir todo
                        filtrados = entidades;
                        request.JsonFilters = "[]";
                    }
                }

                // Primero validar el nombre en la tabla general antes de actualizar cualquier tabla
                await _datasetUMService.ValidateDatasetNameAsync(request.Name, username, ModuleType.InsightMonitor, existingDataset.DatasetId);

                // Actualizar la tabla específica del módulo
                DatasetIM updatedDataset = await _datasetService.UpdateDatasetIMAsync(existingDataset, request, username);
                var requestDataset = new CreateDatasetRequest(request.Name, username, ModuleType.InsightMonitor);
                Datasets dataset = await _datasetUMService.UpdateDatasetAsyncIM(updatedDataset.DatasetId, requestDataset, updatedDataset);
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
                return StatusCode(500, string.Format(Language.DatasetUpdateError, ex.Message));
            }
        }

        /// <summary>
        /// Elimina un dataset.
        /// </summary>
        [HttpDelete("DeleteDataset")]
        [RequirePermission("Datasets.Delete")]
        [ProducesResponseType(204)] // No Content
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> DeleteDatasetAsync(int datasetId)
        {
            try
            {
                var username = User.Identity?.Name ?? string.Empty;
                DatasetIM? dataset = await _datasetService.GetDatasetIMByIdForEditAsync(datasetId, username);
                if (dataset == null)
                {
                    return NotFound(string.Format(Language.DatasetNotFound, datasetId, username));
                }

                DatasetIM? id = await _context.DatasetsIM
                    .FirstOrDefaultAsync(d => d.Id == datasetId && d.Username == username);

                // Eliminar KPIs asociados a este dataset
                var kpisToDelete = await _context.Kpi
                    .Where(k => k.DatasetId == datasetId && k.SourceModule.ToUpper() == "IM")
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

                await _datasetService.DeleteDatasetIMAsync(datasetId, username);
                await _datasetUMService.DeleteDatasetAsync(id!.DatasetId, username);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, string.Format(Language.DatasetDeleteError, ex.Message));
            }
        }

        [HttpGet("GetSensorType")]
        [RequirePermission("Datasets.View")]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<string>> GetSensorTypeAsync(int datasetId)
        {
            try
            {
                var username = User.Identity?.Name ?? string.Empty;

                DatasetIM? dataset = await _datasetService.GetDatasetIMByIdAsync(datasetId, username);
                if (dataset == null)
                {
                    return NotFound(string.Format(Language.DatasetNotFoundSimple, datasetId));
                }

                if (dataset.Id_Source == null || string.IsNullOrEmpty(dataset.SensorName))
                {
                    return BadRequest(Language.DatasetInsufficientInfo);
                }

                Source? source = await _sondaIMService.GetSourceById((int)dataset.Id_Source, username);
                if (source == null)
                {
                    return NotFound(string.Format(Language.SourceNotFound, dataset.Id_Source));
                }

                if (source.Devices != null)
                {
                    foreach (Device dev in source.Devices)
                    {
                        Device? fullDevice = await _sondaIMService.GetDeviceById(dev.Id, username);
                        if (fullDevice == null)
                        {
                            continue;
                        }

                        if (fullDevice.Sensors != null)
                        {
                            Sensor? sensor = fullDevice.Sensors.FirstOrDefault(s => string.Equals(s.Name, dataset.SensorName, StringComparison.OrdinalIgnoreCase));
                            if (sensor != null)
                            {
                                return Ok(sensor.Type ?? "unknown");
                            }
                        }
                    }
                }

                return NotFound(string.Format(Language.SensorNotFoundInDevice, dataset.SensorName));
            }
            catch (Exception ex)
            {
                return StatusCode(500, string.Format(Language.DatasetSensorTypeError, ex.Message));
            }
        }

        #endregion

        /// <summary>
        /// Normaliza el texto para búsquedas insensibles a acentos y mayúsculas.
        /// </summary>
        private static string NormalizeText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            // 1) Normalizar a FormD y remover diacríticos (acentos)
            string formD = text.Trim().ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD);
            string withoutDiacritics = new string(formD.Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark).ToArray());

            // 2) Reemplazos adicionales: espacios fuera, ñ->n, subíndices -> dígitos normales
            withoutDiacritics = withoutDiacritics
                .Replace(" ", string.Empty, StringComparison.Ordinal)
                .Replace("ñ", "n", StringComparison.Ordinal)
                .Replace("₀", "0", StringComparison.Ordinal).Replace("₁", "1", StringComparison.Ordinal).Replace("₂", "2", StringComparison.Ordinal)
                .Replace("₃", "3", StringComparison.Ordinal).Replace("₄", "4", StringComparison.Ordinal).Replace("₅", "5", StringComparison.Ordinal)
                .Replace("₆", "6", StringComparison.Ordinal).Replace("₇", "7", StringComparison.Ordinal).Replace("₈", "8", StringComparison.Ordinal)
                .Replace("₉", "9", StringComparison.Ordinal);

            // 3) Normalizar de vuelta a FormC
            return withoutDiacritics.Normalize(System.Text.NormalizationForm.FormC);
        }
    }
}
