using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using OmniMonitor.Server.Services;
using OmniMonitor.Shared.Dtos;

namespace OmniMonitor.Server.Controllers
{
    /// <summary>
    /// Controlador para la gestión de dashboards personalizables.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;
        private readonly ISondaAuthService _sondaAuthService;

        public DashboardController(IDashboardService dashboardService, ISondaAuthService sondaAuthService)
        {
            _dashboardService = dashboardService;
            _sondaAuthService = sondaAuthService;
        }

        /// <summary>
        /// Crea un nuevo dashboard personalizable.
        /// </summary>
        /// <param name="request">Datos del dashboard a crear.</param>
        /// <returns>Dashboard creado con su layout completo.</returns>
        /// <response code="201">Dashboard creado exitosamente.</response>
        /// <response code="400">Datos de entrada inválidos.</response>
        /// <response code="401">Usuario no autenticado.</response>
        /// <response code="500">Error interno del servidor.</response>
        [HttpPost]
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

                DashboardResponse nuevoDashboard = await _dashboardService.CreateDashboardAsync(request, request.Username);
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
        /// Obtiene un dashboard específico por su ID y nombre de usuario.
        /// </summary>
        /// <param name="id">ID del dashboard.</param>
        /// <param name="token">Token de usuario.</param>
        /// <returns>Dashboard completo con su layout y tarjetas.</returns>
        /// <response code="200">Dashboard encontrado.</response>
        /// <response code="404">Dashboard no encontrado.</response>
        /// <response code="401">Usuario no autenticado.</response>
        /// <response code="403">Usuario no tiene permisos para ver este dashboard.</response>
        /// <response code="500">Error interno del servidor.</response>
        [HttpGet("GetDashboard")]
        [ProducesResponseType(typeof(DashboardResponse), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<DashboardResponse>> GetDashboard(int id, string token)
        {
            try
            {
                string username = await _sondaAuthService.GetUserByTokenOmAsync(token);
                DashboardResponse? dashboard = await _dashboardService.GetDashboardByIdAsync(id, username);
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

        [HttpGet("GetDashboardSinToken")]
        [ProducesResponseType(typeof(DashboardResponse), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<DashboardResponse>> GetDashboardSinToken(int id)
        {
            try
            {
                DashboardResponse? dashboard = await _dashboardService.GetDashboardByIdAsyncSinToken(id);
                if (dashboard == null)
                {
                    return NotFound($"No se encontró el dashboard con ID {id}");
                }

                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al obtener el dashboard: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene todos los dashboards de un usuario específico.
        /// </summary>
        /// <param name="token">Nombre de usuario.</param>
        /// <returns>Lista de dashboards del usuario.</returns>
        /// <response code="200">Lista de dashboards obtenida exitosamente.</response>
        /// <response code="401">Usuario no autenticado.</response>
        /// <response code="500">Error interno del servidor.</response>
        [HttpGet("GetAllDashboards")]
        [ProducesResponseType(typeof(List<DashboardSummaryResponse>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<DashboardSummaryResponse>>> GetAllDashboards(string token, string? query)
        {
            try
            {
                string username = await _sondaAuthService.GetUserByTokenOmAsync(token);
                List<DashboardSummaryResponse> dashboards = await _dashboardService.GetAllDashboardsAsync(username, query);
                return Ok(dashboards);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al obtener los dashboards: {ex.Message}");
            }
        }

        /// <summary>
        /// Valida una lista de cardIds (IdVisualizacion).
        /// </summary>
        /// <param name="cardIds">Lista de IDs de visualizaciones a validar.</param>
        /// <returns>Resultado de la validación.</returns>
        /// <response code="200">Validación completada.</response>
        /// <response code="400">Lista de IDs inválida.</response>
        /// <response code="500">Error interno del servidor.</response>
        [HttpPost("validate-cards")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> ValidateCardIds([FromBody] List<int> cardIds)
        {
            try
            {
                if (cardIds == null || cardIds.Count == 0)
                {
                    return BadRequest("La lista de IdVisualizacion no puede estar vacía");
                }

                bool isValid = await _dashboardService.ValidateCardIdsAsync(cardIds);

                return Ok(new
                {
                    isValid,
                    message = isValid ? "Todos los IdVisualizacion son válidos" : "Algunos IdVisualizacion no existen en el sistema",
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al validar los IdVisualizacion: {ex.Message}");
            }
        }

        /// <summary>
        /// Elimina un dashboard y sus GrupoVisualizaciones asociados (no elimina visualizaciones/KPIs).
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteDashboard(int id, [FromQuery] string username)
        {
            bool result = await _dashboardService.DeleteDashboardAsync(id, username);
            if (!result)
            {
                return NotFound(new { message = $"No se encontró el dashboard con id {id} para el usuario '{username}'" });
            }

            return Ok(new { message = $"Dashboard con id {id} eliminado correctamente para el usuario '{username}'" });
        }

        /// <summary>
        /// Actualiza el JSON de configuración (JsonDiseno) de un dashboard.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpPut("{id}/config")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateDashboardConfig(int id, [FromQuery] string username, [FromBody] string nuevoJsonDiseno)
        {
            bool result = await _dashboardService.UpdateDashboardConfigAsync(id, username, nuevoJsonDiseno);
            if (!result)
            {
                return NotFound(new { message = $"No se encontró el dashboard con id {id} para el usuario '{username}'" });
            }

            return Ok(new { message = $"Configuración actualizada correctamente para el dashboard {id}" });
        }

        /// <summary>
        /// Agrega una tarjeta (DashboardCard) a un dashboard existente.
        /// </summary>
        [HttpPost("{id}/card")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> AddDashboardCard(int id, [FromQuery] string username, [FromQuery] string? jsonConfig, [FromBody] DashboardCard nuevaCard)
        {
            try
            {
                bool result = await _dashboardService.AddDashboardCardAsync(id, username, jsonConfig!, nuevaCard);
                if (!result)
                {
                    return NotFound(new { message = $"No se encontró el dashboard con id {id} para el usuario '{username}'" });
                }

                return Ok(new { message = $"Tarjeta agregada correctamente al dashboard {id}" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error interno al agregar la tarjeta: {ex.Message}" });
            }
        }

        /// <summary>
        /// Reordena las tarjetas (GrupoVisualizaciones) de un dashboard según el orden de la lista recibida.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpPut("{id}/cards/order")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ReorderDashboardCards(int id, [FromQuery] string username, [FromQuery] string jsonConfig, [FromBody] List<DashboardCard> orderedCards)
        {
            bool result = await _dashboardService.ReorderDashboardCardsAsync(id, username, jsonConfig, orderedCards);
            if (!result)
            {
                return NotFound(new { message = $"No se encontró el dashboard con id {id} para el usuario '{username}'" });
            }

            return Ok(new { message = $"Orden de tarjetas actualizado correctamente para el dashboard {id}" });
        }

        /// <summary>
        /// Elimina una tarjeta (GrupoVisualizacion) de un dashboard y actualiza el orden de las restantes.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpDelete("{id}/card/{idCard}/{tipoCard}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteDashboardCard(int id, int idCard, int tipoCard, [FromQuery] string username)
        {
            bool result = await _dashboardService.DeleteDashboardCardAsync(id, username, idCard, tipoCard);
            if (!result)
            {
                return NotFound(new { message = $"No se encontró la tarjeta con idCard {idCard} y tipoCard {tipoCard} en el dashboard {id} para el usuario '{username}'" });
            }

            return Ok(new { message = $"Tarjeta eliminada correctamente del dashboard {id}" });
        }

        /// <summary>
        /// Actualiza el nombre y/o la descripción de un dashboard (pasa ambos como strings por query).
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpPut("{id}/info")]
        [ProducesResponseType(typeof(DashboardResponse), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> UpdateDashboardInfo(int id, [FromQuery] string username, [FromQuery] string? nombre, [FromQuery] string? descripcion)
        {
            try
            {
                DashboardResponse? updated = await _dashboardService.UpdateDashboardInfoAsync(id, username, nombre, descripcion);
                if (updated == null)
                {
                    return NotFound(new { message = $"No se encontró el dashboard con id {id} para el usuario '{username}'" });
                }

                return Ok(updated);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al actualizar la información del dashboard: {ex.Message}");
            }
        }

        /// <summary>
        /// Edita una tarjeta (GrupoVisualizacion) y su visualización asociada.
        /// </summary>
        [HttpPut("{id}/card/edit")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> EditDashboardCard(int id, [FromQuery] string username, [FromQuery] string? jsonConfig, [FromQuery] int idVisualizacion, [FromBody] CreateVisualizacionRequest request)
        {
            if (request == null || request.Nombre == null)
            {
                return BadRequest(new { message = "Datos inválidos para la edición de la tarjeta." });
            }

            bool result = await _dashboardService.EditDashboardCard(id, username, jsonConfig!, idVisualizacion, request);
            if (!result)
            {
                return NotFound(new { message = "No se encontró la tarjeta o la visualización asociada para editar." });
            }

            return Ok(new { message = "Tarjeta y visualización actualizadas correctamente." });
        }

        /// <summary>
        /// Busca dashboards por fragmento de texto en nombre o descripción.
        /// </summary>
        [HttpGet("search")]
        [ProducesResponseType(typeof(List<DashboardSummaryResponse>), 200)]
        public async Task<IActionResult> SearchDashboards([FromQuery] string query)
        {
            List<DashboardSummaryResponse> result = await _dashboardService.SearchDashboardsByTextAsync(query);
            return Ok(result);
        }

        [HttpPost("createShare/{dashboardId}/share")]
        [ProducesResponseType(typeof(ShareResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ShareResponseDto>> CreateShareLink(int dashboardId, [FromBody] ShareRequestDto request, [FromQuery] string token)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var username = await _sondaAuthService.GetUserByTokenOmAsync(token);
                if (string.IsNullOrWhiteSpace(username))
                {
                    return Unauthorized(new { message = "Token inválido." });
                }

                var response = await _dashboardService.CreateShareLinkAsync(dashboardId, request, username);
                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ocurrió un error interno al crear el enlace.", details = ex.Message });
            }
        }

        [HttpGet("getShares/{dashboardId}/share")]
        [ProducesResponseType(typeof(List<ShareResponseDto>), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<List<ShareResponseDto>>> GetShareLinksForDashboard(int dashboardId, [FromQuery] string token)
        {
            try
            {
                var username = await _sondaAuthService.GetUserByTokenOmAsync(token);
                if (string.IsNullOrWhiteSpace(username)) return Unauthorized(new { message = "Token inválido." });

                var response = await _dashboardService.GetAllByDashboardAsync(dashboardId, username);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ocurrió un error interno.", details = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpGet("getShare/{slug}")]
        [ProducesResponseType(typeof(ShareResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ShareResponseDto>> GetPublicShareLink(string slug)
        {
            try
            {
                var response = await _dashboardService.GetBySlugAsync(slug);
                if (response == null)
                {
                    return NotFound(new { message = "Enlace no encontrado, inválido o expirado." });
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno.", details = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpPost("ValidateShare/{slug}/validate")]
        [ProducesResponseType(typeof(ValidateSharePasswordResponseDto), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<ValidateSharePasswordResponseDto>> ValidateSharePassword(string slug, [FromBody] ValidateSharePasswordRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest();
            try
            {
                var response = await _dashboardService.ValidatePasswordAsync(slug, request.Password);
                if (!response.IsValid)
                {
                    return Unauthorized(response);
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno.", details = ex.Message });
            }
        }

        [HttpPut("UpdateShare/{slug}")]
        [ProducesResponseType(typeof(ShareResponseDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ShareResponseDto>> UpdateShareLink(string slug, [FromBody] ShareRequestDto request, [FromQuery] string token)
        {
            if (!ModelState.IsValid) return BadRequest();
            try
            {
                var username = await _sondaAuthService.GetUserByTokenOmAsync(token);
                if (string.IsNullOrWhiteSpace(username)) return Unauthorized(new { message = "Token inválido." });

                var response = await _dashboardService.UpdateShareLinkAsync(slug, request, username);
                if (response == null)
                {
                    return NotFound(new { message = "Enlace no encontrado o no autorizado para este usuario." });
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno.", details = ex.Message });
            }
        }

        [HttpDelete("DeleteShare/{slug}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteShareLink(string slug, [FromQuery] string token)
        {
            try
            {
                var username = await _sondaAuthService.GetUserByTokenOmAsync(token);
                if (string.IsNullOrWhiteSpace(username)) return Unauthorized(new { message = "Token inválido." });

                var success = await _dashboardService.DeleteShareLinkAsync(slug, username);
                if (!success)
                {
                    return NotFound(new { message = "Enlace no encontrado o no autorizado para este usuario." });
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno.", details = ex.Message });
            }
        }
    }
}
