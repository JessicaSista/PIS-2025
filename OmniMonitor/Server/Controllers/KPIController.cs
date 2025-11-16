using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using OmniMonitor.Server.Attributes;
using OmniMonitor.Server.Services;
using OmniMonitor.Shared.Dtos;
using OmniMonitor.Shared.Dtos.AM;

namespace OmniMonitor.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class KPIController : ControllerBase
    {
        private readonly ISondaAuthService _sondaAuthService;
        private readonly IKpiService _kpiService;
        private readonly ISondaIMService _sondaIMService;
        private readonly ILogger<KPIController> _logger;

        public KPIController(
            ISondaAuthService sondaAuthService,
            IKpiService kpiService,
            ISondaIMService sondaIMService,
            ILogger<KPIController> logger)
        {
            _sondaAuthService = sondaAuthService ?? throw new ArgumentNullException(nameof(sondaAuthService));
            _kpiService = kpiService ?? throw new ArgumentNullException(nameof(kpiService));
            _sondaIMService = sondaIMService ?? throw new ArgumentNullException(nameof(sondaIMService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost("")]
        [RequirePermission("Kpis.Create")]
        [ProducesResponseType(typeof(Kpi), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<Kpi>> CreateKpi([FromBody] KpiRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("El objeto KPI es nulo.");
                }

                // Validar token y obtener usuario
                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                {
                    return BadRequest("Token inválido.");
                }

                // Crear KPI usando el servicio, pasándole el username
                Kpi newKpi = await _kpiService.CreateKpiAsync(request, username);

                return Ok(newKpi);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "CreateKpi: DB error");
                return StatusCode(500, $"DB Error: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "CreateKpi: argument error");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateKpi: unexpected error");
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtener KPI por id.
        /// </summary>
        /// <param name="id">id del KPI.</param>
        /// <param name="token">Token del Usuario.</param>
        /// <returns>Devuelve el KPI.</returns>
        [HttpGet("{id}")]
        [RequirePermission("Kpis.View")]
        [ProducesResponseType(typeof(KpiResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<KpiResponse>> GetKpiById(int id)
        {
            try
            {
                // Validar token y obtener usuario
                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                {
                    return BadRequest("Token inválido.");
                }

                // Buscar KPI en la base de datos
                KpiResponse kpi = await _kpiService.CalculateKpiValueAsync(id, username);
                if (kpi == null)
                {
                    return NotFound($"No se encontró el KPI con ID {id} para el usuario {username}.");
                }

                return Ok(kpi);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetKpiById: unexpected error for id={Id}", id);
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [AllowAnonymous]
        [HttpGet("getKpiSinToken")]
        [ProducesResponseType(typeof(KpiResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<KpiResponse>> GetKpiByIdSinToken(int id)
        {
            try
            {
                KpiResponse kpi = await _kpiService.CalculateKpiValueAsyncSinToken(id);
                if (kpi == null)
                {
                    return NotFound($"No se encontró el KPI con ID {id}");
                }

                return Ok(kpi);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        // Eliminar KPI por ID
        [HttpDelete("{id}")]
        [RequirePermission("Kpis.Delete")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> DeleteKpi(int id)
        {
            try
            {
                var username = User.Identity?.Name;                  
                if (string.IsNullOrEmpty(username))
                {
                    return BadRequest("Token inválido.");
                }

                await _kpiService.DeleteKpiAsync(id, username);

                return Ok(new { Message = $"KPI con ID {id} eliminado correctamente." });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "DeleteKpi: not found id={Id}", id);
                return NotFound(new { ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "DeleteKpi: unauthorized id={Id}", id);
                return StatusCode(403, new { ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeleteKpi: unexpected error for id={Id}", id);
                return StatusCode(500, new { Message = $"Error interno: {ex.Message}" });
            }
        }

        [HttpPatch("{id}")]
        [RequirePermission("Kpis.Edit")]
        [ProducesResponseType(typeof(Kpi), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<Kpi>> UpdateKpiPartial(int id, [FromBody] KpiRequest request)
        {
            if (request == null)
            {
                return BadRequest("El objeto KPI es nulo.");
            }

            try
            {

                 var user = User.Identity?.Name;


                Kpi updatedKpi = await _kpiService.UpdateKpiAsync(id, request, user);
                return Ok(updatedKpi);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "UpdateKpiPartial: not found id={Id}", id);
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "UpdateKpiPartial: unauthorized id={Id}", id);
                return BadRequest(ex.Message);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "UpdateKpiPartial: argument error for id={Id}", id);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateKpiPartial: unexpected error for id={Id}", id);
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        // Obtener todos los KPIs del usuario
        [HttpGet("kpis")]
        [RequirePermission("Kpis.View")]
        [ProducesResponseType(typeof(List<KpiResponse>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<KpiResponse>>> GetAllKpis()
        {
            try
            {
                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                {
                    return BadRequest("Token inválido.");
                }

                List<KpiResponse> kpis = await _kpiService.CalculateAllKpisForUserAsync(username);
                return Ok(kpis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAllKpis: unexpected error");
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        // Obtener todos los KPIs del usuario
        [HttpGet("all-KPIs")]
        [RequirePermission("Kpis.View")]
        [ProducesResponseType(typeof(List<KpiResponse>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<Kpi>>> GetAllKpisnoCalculate()
        {
            try
            {
                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                {
                    return BadRequest("Token inválido.");
                }

                List<Kpi> kpis = await _kpiService.GetAllKpisForUserAsync(username);
                return Ok(kpis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAllKpis: unexpected error");
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [HttpGet("metrics/{module}")]
        [RequirePermission("Kpis.View")]
        [ProducesResponseType(typeof(List<MetricInfo>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<MetricInfo>>> GetMetricInfoByModule(string module)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(module))
                {
                    return BadRequest("Debe especificarse el módulo.");
                }

                List<MetricInfo> metrics = await _kpiService.GetMetricInfoListAsync(module.ToUpperInvariant());
                if (metrics == null || metrics.Count == 0)
                {
                    return NotFound($"No se encontraron métricas para el módulo {module}.");
                }

                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetMetricInfoByModule: unexpected error for module={Module}", module);
                return StatusCode(500, $"Error interno al obtener métricas: {ex.Message}");
            }
        }

        [HttpGet("testDates")]
        [RequirePermission("Kpis.View")]
        [ProducesResponseType(typeof(List<DeviceData>), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<DeviceData>>> TestGetDeviceDataByDate()
        {
            try
            {
                string username = "admin";
                string password = "admin";
                int deviceId = 52726;

                DateTime dateFrom = DateTime.UtcNow.AddDays(-2); // hace 2 día
                DateTime dateTo = DateTime.UtcNow;               // ahora

                var data = await _sondaIMService.GetDeviceDataByDate(deviceId, dateFrom, dateTo, username);

                if (data == null || data.Count == 0)
                    return Ok("No se encontraron datos para el rango de fechas.");

                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TestGetDeviceDataByDate: unexpected error");
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [HttpGet("test")]
        [RequirePermission("Kpis.View")]
        public ActionResult<Kpi> GetTestKpi()
        {
            var kpi = new Kpi
            {
                Id = 1,
                Name = "Temperature Sensor",
                Description = "Average temperature of last hour",
                SourceModule = "UM",
                DatasetId = 101,
                Unit = "°C",
                Metric = "Average",
                Multiplier = 1.0,
                DefaultColor = "#00FF00"
            };

            return Ok(kpi);
        }

        [HttpGet("test-response")]
        [RequirePermission("Kpis.View")]
        public ActionResult<KpiResponse> GetTestKpiResponse()
        {
            var response = new KpiResponse
            {
                Name = "Temperature Sensor",
                Description = "Average temperature of last hour",
                Type = "float",
                Value = 23.7,
                ActualColor = "#00FF00"
            };

            return Ok(response);
        }

        // Devuelve los tipos de campos posibles para un KPI según el módulo
        [HttpGet("field-types")]
        [RequirePermission("Kpis.View")]
        [ProducesResponseType(typeof(List<string>), 200)]
        [ProducesResponseType(400)]
        public ActionResult<List<string>> GetKpiFieldTypes([FromQuery] string modulo, [FromQuery] int choice)
        {
            if (string.IsNullOrWhiteSpace(modulo))
                return BadRequest("Debe especificar el módulo.");

            List<string> fieldTypes = new List<string>();
            switch (modulo.ToLower())
            {
                case "am":
                    if (choice == 1)
                        fieldTypes = typeof(OmniMonitor.Shared.Dtos.AM.DatasetReducedAMDTO).GetProperties().Select(p => p.Name).ToList();
                    else if (choice == 2)
                        fieldTypes = typeof(OmniMonitor.Shared.Dtos.AM.DatasetReducedAMEventsDTO).GetProperties().Select(p => p.Name).ToList();
                    else  
                        fieldTypes = typeof(OmniMonitor.Shared.Dtos.ReducedStockDatasetAM).GetProperties().Select(p => p.Name).ToList();
                    break;
                case "em":
                    if (choice == 1)
                        fieldTypes = typeof(OmniMonitor.Shared.Dtos.EM.DatasetReducedAlertEMDTO).GetProperties().Select(p => p.Name).ToList();
                    else if (choice == 2)
                        fieldTypes.AddRange(typeof(OmniMonitor.Shared.Dtos.EM.DatasetReducedEventEMDTO).GetProperties().Select(p => p.Name));
                    else
                        fieldTypes.AddRange(typeof(OmniMonitor.Shared.Dtos.EM.DatasetReducedExtensionEMDTO).GetProperties().Select(p => p.Name));

                    fieldTypes = fieldTypes.Distinct().ToList();
                    break;
                case "um":
                    if (choice == 1)
                        fieldTypes = typeof(OmniMonitor.Shared.Dtos.UM.DatasetReducedEventsUMDTO).GetProperties().Select(p => p.Name).ToList();

                    else
                        fieldTypes = typeof(OmniMonitor.Shared.Dtos.UM.DatasetReducedEventUMDTO).GetProperties().Select(p => p.Name).ToList();
                    break;
                default:
                    fieldTypes.Add("Tipo de módulo no soportado");
                    break;
            }
            return Ok(fieldTypes);
        }

        /// <summary>
        /// Obtiene todos los KPIs de un usuario con paginación, devolviendo solo nombre y descripción.
        /// </summary>
        [HttpGet("GetAllKpiDtoPaginated")]
        [RequirePermission("Kpis.View")]
        [ProducesResponseType(typeof(KpiSimplePaginatedResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<KpiSimplePaginatedResponse>> GetAllKpiDtoPaginated(int page = 1, int pageSize = 10, string? query = null)
        {
            try
            {
                var username = User.Identity?.Name;
                if (string.IsNullOrWhiteSpace(username))
                    return BadRequest("Usuario no encontrado.");
                
                if (page <= 0 || pageSize <= 0)
                    return BadRequest("La página y el tamaño deben ser mayores a 0.");
                
                var response = await _kpiService.GetAllKpisPaginatedAsync(username, page, pageSize, query);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener KPIs paginados para el usuario");
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [HttpGet("field-values")]
        [RequirePermission("Kpis.View")]
        [ProducesResponseType(typeof(List<string>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<string>>> GetFieldValues(
            [FromQuery] int datasetId,
            [FromQuery] string modulo,
            [FromQuery] string campo,
            [FromQuery] int choice)
        {
            // Log: llegada de la petición
            _logger.LogInformation("GET field-values called. datasetId={DatasetId}, modulo={Modulo}, campo={Campo}, choice={Choice}", datasetId, modulo, campo, choice);

            try
            {
                if (datasetId <= 0)
                {
                    _logger.LogWarning("GetFieldValues: invalid datasetId {DatasetId}", datasetId);
                    return BadRequest("Debe especificar un ID de dataset válido.");
                }

                if (string.IsNullOrWhiteSpace(modulo))
                {
                    _logger.LogWarning("GetFieldValues: missing modulo");
                    return BadRequest("Debe especificar el módulo.");
                }

                if (string.IsNullOrWhiteSpace(campo))
                {
                    _logger.LogWarning("GetFieldValues: missing campo");
                    return BadRequest("Debe especificar el campo.");
                }

                // Validar token y obtener usuario
                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                {
                    _logger.LogWarning("GetFieldValues: invalid token");
                    return BadRequest("Token inválido.");
                }

                List<string> fieldValues = await _kpiService.GetFieldValuesAsync(datasetId, modulo, campo, choice, username);

                if (fieldValues == null || !fieldValues.Any())
                {
                    _logger.LogInformation("GetFieldValues: no values found for datasetId={DatasetId}, modulo={Modulo}, campo={Campo}, choice={Choice}", datasetId, modulo, campo, choice);
                    return Ok(new List<string>()); // Retornar lista vacía en lugar de NotFound
                }

                // Log: mostrar resumen de la respuesta
                if (fieldValues.Count <= 20)
                {
                    // para respuestas pequeñas loguea el contenido
                    _logger.LogInformation("GetFieldValues: returning {Count} values: {Values}", fieldValues.Count, string.Join(", ", fieldValues));
                }
                else
                {
                    // para respuestas grandes solo loguea el count
                    _logger.LogInformation("GetFieldValues: returning {Count} values (content omitted in logs)", fieldValues.Count);
                }

                return Ok(fieldValues);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "GetFieldValues: argument error for datasetId={DatasetId}, modulo={Modulo}, campo={Campo}, choice={Choice}", datasetId, modulo, campo, choice);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetFieldValues: unexpected error for datasetId={DatasetId}, modulo={Modulo}, campo={Campo}, choice={Choice}", datasetId, modulo, campo, choice);
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }
    }
}
