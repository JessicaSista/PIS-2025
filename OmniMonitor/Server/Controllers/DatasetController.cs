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
    public class DatasetController : ControllerBase
    {
        private readonly IDatasetService _datasetService;
        private readonly ISondaAuthService _sondaAuthService;
        private readonly ISondaIMService _sondaIMService;
        private readonly IDatasetUMService _datasetUMService;
        private readonly ApplicationDbContext _context;

        public DatasetController(IDatasetService datasetService, ISondaAuthService sondaAuthService, ISondaIMService sondaIMService, IDatasetUMService datasetUMService, ApplicationDbContext context)
        {
            _datasetService = datasetService;
            _sondaAuthService = sondaAuthService;
            _sondaIMService = sondaIMService;
            _datasetUMService = datasetUMService;
            _context = context;
        }

        /// <summary>
        /// Crea un nuevo dataset.
        /// </summary>
        [RequirePermission("Datasets.Create")]
        [HttpPost]
        [ProducesResponseType(typeof(DatasetIM), 201)] // 201 Created
        [ProducesResponseType(400)] // Bad Request
        [ProducesResponseType(500)]
        public async Task<ActionResult<DatasetIM>> CreateDataset([FromBody] CreateDatasetIMRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var requestDataset = new CreateDatasetRequest(request.Name, request.Username, ModuleType.InsightMonitor);
                Datasets dataset = await _datasetUMService.CreateDatasetAsync(requestDataset);
                DatasetIM newDataset = await _datasetService.CreateDatasetIMAsync(request, dataset.Id);
                await _datasetUMService.UpdateDatasetAsyncIM(dataset.Id, requestDataset, newDataset);

                // Devuelve una respuesta 201 Created con la ubicación del nuevo recurso
                return CreatedAtAction(nameof(GetDatasetById), new { datasetId = newDataset.Id, username = newDataset.Username }, newDataset);
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
        /// Obtiene todos los datasets para un usuario específico.
        /// </summary>
        [HttpGet("user")]
        [RequirePermission("Datasets.View")]
        [ProducesResponseType(typeof(List<DatasetIM>), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<DatasetIM>>> GetAllDatasets([FromQuery] string? search = null)
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
                    datasets = [.. datasets.Where(d => NormalizeText(d.Name).Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase))];
                }

                return Ok(datasets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al obtener los datasets: {ex.Message}");
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
        public async Task<ActionResult<string>> GetDatasetModule(int datasetId)
        {
            try
            {
                var username = User.Identity?.Name;
                string? module = await _datasetService.IdentifyDatasetModuleAsync(datasetId, username);

                if (module == null)
                {
                    return NotFound($"No se encontró el dataset con ID {datasetId}.");
                }

                return Ok(module);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al identificar el módulo: {ex.Message}");
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
        public async Task<ActionResult<DatasetIM>> GetDatasetById(int datasetId)
        {
            try
            {
                var username = User.Identity?.Name;
                DatasetIM? dataset = await _datasetService.GetDatasetIMByIdForEditAsync(datasetId, username);
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

        [AllowAnonymous]
        [HttpGet("GetDatasetSinToken")]
        [ProducesResponseType(typeof(DatasetIM), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<DatasetIM>> GetDatasetByIdSinToken(int datasetId)
        {
            try
            {
                DatasetIM? dataset = await _datasetService.GetDatasetIMByIdForEditAsyncSinToken(datasetId);
                if (dataset == null)
                {
                    return NotFound($"No se encontró el dataset con ID {datasetId}");
                }

                return Ok(dataset);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al obtener el dataset: {ex.Message}");
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
        public async Task<ActionResult<DatasetIM>> UpdateDataset(int datasetId, [FromBody] CreateDatasetIMRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                DatasetIM? existingDataset = await _datasetService.GetDatasetIMByIdForEditAsync(datasetId, request.Username);
                if (existingDataset == null)
                {
                    return NotFound($"No se encontró el dataset con ID {datasetId} para el usuario {request.Username}.");
                }
                await _datasetUMService.ValidateDatasetNameAsync(request.Name, request.Username, ModuleType.InsightMonitor, existingDataset.DatasetId);

                // Actualizar la tabla específica del módulo
                DatasetIM updatedDataset = await _datasetService.UpdateDatasetIMAsync(existingDataset, request);
                var requestDataset = new CreateDatasetRequest(request.Name, request.Username, ModuleType.InsightMonitor);
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
                return StatusCode(500, $"Error interno al actualizar el dataset: {ex.Message}");
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
        public async Task<ActionResult> DeleteDataset(int datasetId)
        {
            try
            {
                var username = User.Identity?.Name;
                DatasetIM? dataset = await _datasetService.GetDatasetIMByIdForEditAsync(datasetId, username);
                if (dataset == null)
                {
                    return NotFound($"No se encontró el dataset con ID {datasetId} para el usuario {username}.");
                }

                DatasetIM? id = await _context.DatasetsIM
                    .FirstOrDefaultAsync(d => d.Id == datasetId && d.Username == username);
                await _datasetService.DeleteDatasetIMAsync(datasetId, username);
                await _datasetUMService.DeleteDatasetAsync(id!.DatasetId, username);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al eliminar el dataset: {ex.Message}");
            }
        }

        [HttpGet("GetSensorType")]
        [RequirePermission("Datasets.View")]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<string>> GetSensorType(int datasetId)
        {
            try
            {
                var username = User.Identity?.Name;

                DatasetIM? dataset = await _datasetService.GetDatasetIMByIdAsync(datasetId, username);
                if (dataset == null)
                {
                    return NotFound($"No se encontró el dataset con ID {datasetId}.");
                }

                if (dataset.Id_Source == null || string.IsNullOrEmpty(dataset.SensorName))
                {
                    return BadRequest("El dataset no contiene información suficiente (Source o SensorName).");
                }

                Source? source = await _sondaIMService.GetSourceById((int)dataset.Id_Source, username);
                if (source == null)
                {
                    return NotFound($"No se encontró el Source con ID {dataset.Id_Source}.");
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

                return NotFound($"No se encontró el sensor '{dataset.SensorName}' en ningún device del Source.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al obtener el tipo del sensor: {ex.Message}");
            }
        }

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
            string withoutDiacritics = new ([.. formD.Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)]);

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
