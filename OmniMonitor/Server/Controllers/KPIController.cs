using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using OmniMonitor.Server.Services;
using OmniMonitor.Shared.Dtos;
using OmniMonitor.Shared.Dtos.AM;

namespace OmniMonitor.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class KPIController : ControllerBase
    {
        private readonly ISondaAuthService _sondaAuthService;
        private readonly IKpiService _kpiService;
        private readonly ISondaIMService _sondaIMService;

        public KPIController(ISondaAuthService sondaAuthService, IKpiService kpiService, ISondaIMService sondaIMService)
        {
            _sondaAuthService = sondaAuthService;
            _kpiService = kpiService;
            _sondaIMService = sondaIMService;
        }

        [HttpPost("")]
        [ProducesResponseType(typeof(Kpi), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<Kpi>> CreateKpi([FromBody] KpiRequest request, [FromQuery] string token)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("El objeto KPI es nulo.");
                }

                // Validar token y obtener usuario
                string username = await _sondaAuthService.GetUserByTokenOMAsync(token);
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
                return StatusCode(500, $"DB Error: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
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
        [ProducesResponseType(typeof(KpiResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<KpiResponse>> GetKpiById(int id, [FromQuery] string token)
        {
            try
            {
                // Validar token y obtener usuario
                string username = await _sondaAuthService.GetUserByTokenOMAsync(token);
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
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        // Eliminar KPI por ID
        [HttpDelete("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> DeleteKpi(int id, [FromQuery] string? token)
        {
            try
            {
                string username = await _sondaAuthService.GetUserByTokenOMAsync(token!);
                if (string.IsNullOrEmpty(username))
                {
                    return BadRequest("Token inválido.");
                }

                await _kpiService.DeleteKpiAsync(id, username);

                return Ok(new { Message = $"KPI con ID {id} eliminado correctamente." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Error interno: {ex.Message}" });
            }
        }

        [HttpPatch("{id}")]
        [ProducesResponseType(typeof(Kpi), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<Kpi>> UpdateKpiPartial(int id, [FromBody] KpiRequest request, [FromQuery] string? token)
        {
            if (request == null)
            {
                return BadRequest("El objeto KPI es nulo.");
            }

            try
            {
                string? username = null;

                if (!string.IsNullOrEmpty(token))
                {
                    // Obtener usuario del token
                    string user = await _sondaAuthService.GetUserByTokenOMAsync(token);
                    if (string.IsNullOrEmpty(user))
                    {
                        return BadRequest("Token inválido.");
                    }

                    username = user;
                }

                Kpi updatedKpi = await _kpiService.UpdateKpiAsync(id, request, username);
                return Ok(updatedKpi);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        // Obtener todos los KPIs del usuario
        [HttpGet("kpis")]
        [ProducesResponseType(typeof(List<KpiResponse>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<KpiResponse>>> GetAllKpis([FromQuery] string token)
        {
            try
            {
                string username = await _sondaAuthService.GetUserByTokenOMAsync(token);
                if (string.IsNullOrEmpty(username))
                {
                    return BadRequest("Token inválido.");
                }

                List<KpiResponse> kpis = await _kpiService.CalculateAllKpisForUserAsync(username);
                return Ok(kpis);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [HttpGet("metrics/{module}")]
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
                return StatusCode(500, $"Error interno al obtener métricas: {ex.Message}");
            }
        }



        [HttpGet("testDates")]
        [ProducesResponseType(typeof(List<DeviceData>), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<DeviceData>>> TestGetDeviceDataByDate()
        {
            try
            {
                // 🔧 Datos de prueba (ajustá según tus datos reales)
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
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }



        [HttpGet("test")]
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
                    else
                        fieldTypes = typeof(OmniMonitor.Shared.Dtos.AM.DatasetReducedAMEventsDTO).GetProperties().Select(p => p.Name).ToList();
                    break;
                case "em":
                    // Puedes elegir el DTO según el tipo de dato que quieras mostrar
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
                        fieldTypes = typeof(OmniMonitor.Shared.Dtos.UM.DatasetReducedEventUMDTO).GetProperties().Select(p => p.Name).ToList();
                    else
                        fieldTypes = typeof(OmniMonitor.Shared.Dtos.UM.DatasetReducedEventsUMDTO).GetProperties().Select(p => p.Name).ToList();
                    break;
                default:
                    fieldTypes.Add("Tipo de módulo no soportado");
                    break;
            }
            return Ok(fieldTypes);
        }

        [HttpGet("field-values")]
        [ProducesResponseType(typeof(List<string>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<string>>> GetFieldValues(
            [FromQuery] int datasetId,
            [FromQuery] string modulo,
            [FromQuery] string campo,
            [FromQuery] int choice,
            [FromQuery] string token)
        {
            try
            {
                if (datasetId <= 0)
                    return BadRequest("Debe especificar un ID de dataset válido.");

                if (string.IsNullOrWhiteSpace(modulo))
                    return BadRequest("Debe especificar el módulo.");

                if (string.IsNullOrWhiteSpace(campo))
                    return BadRequest("Debe especificar el campo.");

                // Validar token y obtener usuario
                string username = await _sondaAuthService.GetUserByTokenOMAsync(token);
                if (string.IsNullOrEmpty(username))
                    return BadRequest("Token inválido.");

                List<string> fieldValues = await _kpiService.GetFieldValuesAsync(datasetId, modulo, campo, choice, username);

                if (fieldValues == null || !fieldValues.Any())
                    return Ok(new List<string>()); // Retornar lista vacía en lugar de NotFound

                return Ok(fieldValues);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }
    }
}





