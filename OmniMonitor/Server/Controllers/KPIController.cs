using Microsoft.AspNetCore.Mvc;

using OmniMonitor.Server.Services;
using OmniMonitor.Shared.Dtos;

namespace OmniMonitor.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class KPIController : ControllerBase
    {
        private readonly ISondaAuthService _sondaAuthService;
        private readonly IKpiService _kpiService;

        public KPIController(ISondaAuthService sondaAuthService, IKpiService kpiService)
        {
            _sondaAuthService = sondaAuthService;
            _kpiService = kpiService;
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
    }
}