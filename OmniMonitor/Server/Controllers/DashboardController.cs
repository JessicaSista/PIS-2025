using Microsoft.AspNetCore.Mvc;
using OmniMonitor.Server.Attributes;
using OmniMonitor.Server.Services;
using OmniMonitor.Shared.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OmniMonitor.Server.Controllers
{
    /// <summary>
    /// Controlador para la gestión de dashboards personalizables
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        /// <summary>
        /// Crea un nuevo dashboard personalizable
        /// </summary>
        /// <param name="request">Datos del dashboard a crear</param>
        /// <returns>Dashboard creado con su layout completo</returns>
        /// <response code="201">Dashboard creado exitosamente</response>
        /// <response code="400">Datos de entrada inválidos</response>
        /// <response code="401">Usuario no autenticado</response>
        /// <response code="500">Error interno del servidor</response>
        [HttpPost]
        [RequirePermission("Crear Dashboards")]
        [ProducesResponseType(typeof(DashboardResponse), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<DashboardResponse>> CreateDashboard([FromBody] CreateDashboardRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (string.IsNullOrWhiteSpace(request.Username))
                {
                    return BadRequest("El nombre de usuario es requerido.");
                }

                var nuevoDashboard = await _dashboardService.CreateDashboardAsync(request, request.Username);
                
                return CreatedAtAction(
                    nameof(GetDashboard), 
                    new { id = nuevoDashboard.IdDashboard, username = nuevoDashboard.Username }, 
                    nuevoDashboard);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al crear el dashboard: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene un dashboard específico por su ID y nombre de usuario
        /// </summary>
        /// <param name="id">ID del dashboard</param>
        /// <param name="username">Nombre de usuario</param>
        /// <returns>Dashboard completo con su layout y tarjetas</returns>
        /// <response code="200">Dashboard encontrado</response>
        /// <response code="404">Dashboard no encontrado</response>
        /// <response code="401">Usuario no autenticado</response>
        /// <response code="403">Usuario no tiene permisos para ver este dashboard</response>
        /// <response code="500">Error interno del servidor</response>
        [HttpGet("{id}/{username}")]
        [RequirePermission("Ver Dashboards")]
        [ProducesResponseType(typeof(DashboardResponse), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<DashboardResponse>> GetDashboard(int id, string username)
        {
            try
            {
                var dashboard = await _dashboardService.GetDashboardByIdAsync(id, username);
                if (dashboard == null)
                {
                    return NotFound($"No se encontró el dashboard con ID {id} para el usuario {username}.");
                }

                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al obtener el dashboard: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene todos los dashboards de un usuario específico
        /// </summary>
        /// <param name="username">Nombre de usuario</param>
        /// <returns>Lista de dashboards del usuario</returns>
        /// <response code="200">Lista de dashboards obtenida exitosamente</response>
        /// <response code="401">Usuario no autenticado</response>
        /// <response code="500">Error interno del servidor</response>
        [HttpGet("user/{username}")]
        [RequirePermission("Ver Dashboards")]
        [ProducesResponseType(typeof(List<DashboardSummaryResponse>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<DashboardSummaryResponse>>> GetAllDashboards(string username)
        {
            try
            {
                var dashboards = await _dashboardService.GetAllDashboardsAsync(username);
                return Ok(dashboards);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al obtener los dashboards: {ex.Message}");
            }
        }

        /// <summary>
        /// Valida una lista de cardIds (IdVisualizacion)
        /// </summary>
        /// <param name="cardIds">Lista de IDs de visualizaciones a validar</param>
        /// <returns>Resultado de la validación</returns>
        /// <response code="200">Validación completada</response>
        /// <response code="400">Lista de IDs inválida</response>
        /// <response code="500">Error interno del servidor</response>
        [HttpPost("validate-cards")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> ValidateCardIds([FromBody] List<int> cardIds)
        {
            try
            {
            if (cardIds == null || !cardIds.Any())
            {
                return BadRequest("La lista de IdVisualizacion no puede estar vacía");
            }

                var isValid = await _dashboardService.ValidateCardIdsAsync(cardIds);
                
                return Ok(new { 
                    isValid = isValid,
                    message = isValid ? "Todos los IdVisualizacion son válidos" : "Algunos IdVisualizacion no existen en el sistema"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al validar los IdVisualizacion: {ex.Message}");
            }
        }

    }
}
