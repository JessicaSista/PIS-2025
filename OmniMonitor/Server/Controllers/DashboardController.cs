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

                // Obtener el username del contexto de autenticación
                // Por ahora usamos un valor por defecto, pero debería venir del token JWT
                var username = GetCurrentUsername();
                if (string.IsNullOrEmpty(username))
                {
                    return Unauthorized("Usuario no autenticado");
                }

                var nuevoDashboard = await _dashboardService.CreateDashboardAsync(request, username);
                
                return CreatedAtAction(
                    nameof(GetDashboard), 
                    new { id = nuevoDashboard.IdDashboard }, 
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
        /// Obtiene un dashboard específico por su ID
        /// </summary>
        /// <param name="id">ID del dashboard</param>
        /// <returns>Dashboard completo con su layout y tarjetas</returns>
        /// <response code="200">Dashboard encontrado</response>
        /// <response code="404">Dashboard no encontrado</response>
        /// <response code="401">Usuario no autenticado</response>
        /// <response code="403">Usuario no tiene permisos para ver este dashboard</response>
        /// <response code="500">Error interno del servidor</response>
        [HttpGet("{id}")]
        [RequirePermission("Ver Dashboards")]
        [ProducesResponseType(typeof(DashboardResponse), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<DashboardResponse>> GetDashboard(int id)
        {
            try
            {
                var username = GetCurrentUsername();
                if (string.IsNullOrEmpty(username))
                {
                    return Unauthorized("Usuario no autenticado");
                }

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
        /// Obtiene todos los dashboards del usuario autenticado
        /// </summary>
        /// <returns>Lista de dashboards del usuario</returns>
        /// <response code="200">Lista de dashboards obtenida exitosamente</response>
        /// <response code="401">Usuario no autenticado</response>
        /// <response code="500">Error interno del servidor</response>
        [HttpGet]
        [RequirePermission("Ver Dashboards")]
        [ProducesResponseType(typeof(List<DashboardSummaryResponse>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<DashboardSummaryResponse>>> GetAllDashboards()
        {
            try
            {
                var username = GetCurrentUsername();
                if (string.IsNullOrEmpty(username))
                {
                    return Unauthorized("Usuario no autenticado");
                }

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

        /// <summary>
        /// Obtiene el username del usuario autenticado
        /// TODO: Implementar extracción del token JWT
        /// </summary>
        /// <returns>Username del usuario autenticado</returns>
        private string? GetCurrentUsername()
        {
            // Por ahora retornamos un usuario por defecto para pruebas
            // En producción, esto debería extraerse del token JWT
            return "admin"; // TODO: Implementar autenticación real
            
            // Ejemplo de implementación con JWT:
            // var claims = User.Claims;
            // return claims.FirstOrDefault(c => c.Type == "username")?.Value;
        }
    }
}
