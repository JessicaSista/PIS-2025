using Microsoft.AspNetCore.Mvc;
using OmniMonitor.Server.Services;
using OmniMonitor.Shared.Dtos.Kpi;

namespace OmniMonitor.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class KpiController : ControllerBase
    {
        private readonly IKpiService _kpiService;

        public KpiController(IKpiService kpiService)
        {
            _kpiService = kpiService;
        }

        /// <summary>
        /// Calcula un KPI para un dataset específico
        /// </summary>
        [HttpPost("calculate/{username}")]
        [ProducesResponseType(typeof(KpiResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<KpiResponse>> CalculateKpi(string username, [FromBody] CalculateKpiRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var kpiResponse = await _kpiService.CalculateKpiAsync(request, username);
                return Ok(kpiResponse);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al calcular el KPI: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene todos los KPIs calculados para un usuario específico
        /// </summary>
        [HttpGet("user/{username}")]
        [ProducesResponseType(typeof(List<KpiResponse>), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<KpiResponse>>> GetAllKpis(string username)
        {
            try
            {
                var kpis = await _kpiService.GetAllKpisAsync(username);
                return Ok(kpis);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al obtener los KPIs: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene un KPI específico por su ID y nombre de usuario
        /// </summary>
        [HttpGet("{kpiId}/{username}")]
        [ProducesResponseType(typeof(KpiResponse), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<KpiResponse>> GetKpiById(int kpiId, string username)
        {
            try
            {
                var kpi = await _kpiService.GetKpiByIdAsync(kpiId, username);
                if (kpi == null)
                {
                    return NotFound($"No se encontró el KPI con ID {kpiId} para el usuario {username}.");
                }
                return Ok(kpi);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al obtener el KPI: {ex.Message}");
            }
        }
    }
}