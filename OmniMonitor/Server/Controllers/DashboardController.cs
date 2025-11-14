using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using OmniMonitor.Server.Attributes;
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
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [RequirePermission("Dashboards.Create")]
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
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [RequirePermission("Dashboards.View")]
        [HttpGet("GetDashboard")]
        [ProducesResponseType(typeof(DashboardResponse), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<DashboardResponse>> GetDashboard(int id)
        {
            try
            {
                var username = User.Identity?.Name;
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

        [AllowAnonymous]
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
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [RequirePermission("Dashboards.View")]
        [HttpGet("GetAllDashboards")]
        [ProducesResponseType(typeof(List<DashboardSummaryResponse>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<DashboardSummaryResponse>>> GetAllDashboards(string? query)
        {
            try
            {
                var username = User.Identity?.Name;
                List<DashboardSummaryResponse> dashboards = await _dashboardService.GetAllDashboardsAsync(username, query);
                return Ok(dashboards);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al obtener los dashboards: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene todos los dashboards de un usuario específico con paginación.
        /// </summary>
        /// <param name="token">Token de autenticación del usuario.</param>
        /// <param name="page">Número de página (default: 1).</param>
        /// <param name="pageSize">Tamaño de página (default: 9).</param>
        /// <param name="query">Texto de búsqueda opcional.</param>
        /// <returns>Dashboards paginados del usuario.</returns>
        /// <response code="200">Lista paginada de dashboards obtenida exitosamente.</response>
        /// <response code="401">Usuario no autenticado.</response>
        /// <response code="500">Error interno del servidor.</response>
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [RequirePermission("Dashboards.View")]
        [HttpGet("GetAllDashboardsPaginated")]
        [ProducesResponseType(typeof(PaginatedDashboardDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<PaginatedDashboardDto>> GetAllDashboardsPaginated( 
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 9,
            [FromQuery] string? query = null)
        {
            try
            {
                var username = User.Identity?.Name;

                // Obtener todos los dashboards (con filtro de búsqueda si existe)

                if (page <= 0 || pageSize <= 0)
                    return BadRequest("La página y el tamaño deben ser mayores a 0.");

                // Obtener solo los dashboards necesarios para la página
                List<DashboardSummaryResponse> paginatedItems = await _dashboardService.GetAllDashboardsPaginatedAsync(username, query, page, pageSize);

                // Calcular totales
                int totalCount = await _dashboardService.GetDashboardsCount(username, query);
                int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                // Validar página
                if (page < 1) page = 1;
                if (page > totalPages && totalPages > 0) page = totalPages;

                // Crear respuesta paginada
                var result = new PaginatedDashboardDto
                {
                    Items = paginatedItems,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = totalPages,
                    HasPreviousPage = page > 1,
                    HasNextPage = page < totalPages
                };

                return Ok(result);
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
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [RequirePermission("Dashboards.Edit")]
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
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [RequirePermission("Dashboards.Delete")]
        [HttpDelete("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteDashboard(int id)
        {
            var username = User.Identity?.Name;
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
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [RequirePermission("Dashboards.Edit")]
        [HttpPut("{id}/config")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateDashboardConfig(int id, [FromBody] string nuevoJsonDiseno)
        {
            var username = User.Identity?.Name;
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
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [RequirePermission("Dashboards.Edit")]
        [HttpPost("{id}/card")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> AddDashboardCard(int id, [FromQuery] string? jsonConfig, [FromBody] DashboardCard nuevaCard)
        {
            try
            {
                var username = User.Identity?.Name;
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
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [RequirePermission("Dashboards.Edit")]
        [HttpPut("{id}/cards/order")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ReorderDashboardCards(int id, [FromQuery] string jsonConfig, [FromBody] List<DashboardCard> orderedCards)
        {
            var username = User.Identity?.Name;
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
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [RequirePermission("Dashboards.Edit")]
        [HttpDelete("{id}/card/{idCard}/{tipoCard}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteDashboardCard(int id, int idCard, int tipoCard)
        {
            var username = User.Identity?.Name;
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
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [RequirePermission("Dashboards.Edit")]
        [HttpPut("{id}/info")]
        [ProducesResponseType(typeof(DashboardResponse), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> UpdateDashboardInfo(int id,[FromQuery] string? nombre, [FromQuery] string? descripcion)
        {
            try
            {
                var username = User.Identity?.Name;
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
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [RequirePermission("Dashboards.Edit")]
        [HttpPut("{id}/card/edit")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> EditDashboardCard(int id, [FromQuery] string? jsonConfig, [FromQuery] int idVisualizacion, [FromBody] CreateVisualizacionRequest request)
        {
            var username = User.Identity?.Name;
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
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [RequirePermission("Dashboards.View")]
        [HttpGet("search")]
        [ProducesResponseType(typeof(List<DashboardSummaryResponse>), 200)]
        public async Task<IActionResult> SearchDashboards([FromQuery] string query)
        {
            List<DashboardSummaryResponse> result = await _dashboardService.SearchDashboardsByTextAsync(query);
            return Ok(result);
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [RequirePermission("Dashboards.Share")]
        [HttpPost("createShare/{dashboardId}/share")]
        [ProducesResponseType(typeof(ShareResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ShareResponseDto>> CreateShareLink(int dashboardId, [FromBody] ShareRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var username = User.Identity?.Name;
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

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [RequirePermission("Dashboards.View")]
        [HttpGet("getShares/{dashboardId}/share")]
        [ProducesResponseType(typeof(List<ShareResponseDto>), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<List<ShareResponseDto>>> GetShareLinksForDashboard(int dashboardId)
        {
            try
            {
                var username = User.Identity?.Name;
                if (string.IsNullOrWhiteSpace(username)) return Unauthorized(new { message = "Token inválido." });

                var response = await _dashboardService.GetAllByDashboardAsync(dashboardId, username);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ocurrió un error interno.", details = ex.Message });
            }
        }

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

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [RequirePermission("Dashboards.Share")]
        [HttpPut("UpdateShare/{slug}")]
        [ProducesResponseType(typeof(ShareResponseDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ShareResponseDto>> UpdateShareLink(string slug, [FromBody] ShareRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest();
            try
            {
                var username = User.Identity?.Name;
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

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [RequirePermission("Dashboards.Share")]
        [HttpDelete("DeleteShare/{slug}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteShareLink(string slug)
        {
            try
            {
                var username = User.Identity?.Name;
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
