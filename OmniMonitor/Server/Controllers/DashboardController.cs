using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using OmniMonitor.Server.Attributes;
using OmniMonitor.Server.Resources;
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
        #region Fields

        private readonly IDashboardService _dashboardService;
        private readonly ISondaAuthService _sondaAuthService;

        #endregion

        #region Constructors

        public DashboardController(IDashboardService dashboardService, ISondaAuthService sondaAuthService)
        {
            _dashboardService = dashboardService;
            _sondaAuthService = sondaAuthService;
        }

        #endregion

        #region Methods

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
        public async Task<ActionResult<DashboardResponse>> CreateDashboardAsync([FromBody] CreateDashboardRequest request)
        {
            try
            {
                var username = User.Identity?.Name;
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                DashboardResponse nuevoDashboard = await _dashboardService.CreateDashboardAsync(request, username);
                return CreatedAtAction(
                    nameof(GetDashboardAsync),
                    new { id = nuevoDashboard.IdDashboard, username = nuevoDashboard.Username },
                    nuevoDashboard);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, string.Format(Language.DashboardCreateError, ex.Message));
            }
        }

        /// <summary>
        /// Obtiene un dashboard específico por su ID y nombre de usuario.
        /// </summary>
        /// <param name="id">ID del dashboard.</param>
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
        public async Task<ActionResult<DashboardResponse>> GetDashboardAsync(int id)
        {
            try
            {
                var username = User.Identity?.Name;
                DashboardResponse? dashboard = await _dashboardService.GetDashboardByIdAsync(id, username);
                if (dashboard == null)
                {
                    return NotFound(string.Format(Language.DashboardNotFoundUser, id, username));
                }

                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                return StatusCode(500, string.Format(Language.DashboardGetError, ex.Message));
            }
        }

        [AllowAnonymous]
        [HttpGet("GetDashboardSinToken")]
        [ProducesResponseType(typeof(DashboardResponse), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<DashboardResponse>> GetDashboardWithoutTokenAsync(int id)
        {
            try
            {
                DashboardResponse? dashboard = await _dashboardService.GetDashboardByIdWithoutTokenAsync(id);
                if (dashboard == null)
                {
                    return NotFound(string.Format(Language.DashboardNotFound, id));
                }

                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                return StatusCode(500, string.Format(Language.DashboardGetError, ex.Message));
            }
        }

        /// <summary>
        /// Obtiene todos los dashboards de un usuario específico.
        /// </summary>
        /// <param name="query">Texto de búsqueda opcional.</param>
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
        public async Task<ActionResult<List<DashboardSummaryResponse>>> GetAllDashboardsAsync(string? query)
        {
            try
            {
                var username = User.Identity?.Name;
                List<DashboardSummaryResponse> dashboards = await _dashboardService.GetAllDashboardsAsync(username, query);
                return Ok(dashboards);
            }
            catch (Exception ex)
            {
                return StatusCode(500, string.Format(Language.DashboardsGetError, ex.Message));
            }
        }

        /// <summary>
        /// Obtiene todos los dashboards de un usuario específico con paginación.
        /// </summary>
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
        public async Task<ActionResult<PaginatedDashboardDto>> GetAllDashboardsPaginatedAsync(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 9,
            [FromQuery] string? query = null)
        {
            try
            {
                var username = User.Identity?.Name;

                // Obtener todos los dashboards (con filtro de búsqueda si existe)

                if (page <= 0 || pageSize <= 0)
                    return BadRequest(Language.PageSizeInvalid);

                // Obtener solo los dashboards necesarios para la página
                List<DashboardSummaryResponse> paginatedItems = await _dashboardService.GetAllDashboardsPaginatedAsync(username, query, page, pageSize);

                // Calcular totales
                int totalCount = await _dashboardService.GetDashboardsCountAsync(username, query);
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
                return StatusCode(500, string.Format(Language.DashboardsGetError, ex.Message));
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
        public async Task<ActionResult> ValidateCardIdsAsync([FromBody] List<int> cardIds)
        {
            try
            {
                if (cardIds == null || cardIds.Count == 0)
                {
                    return BadRequest(Language.CardIdsEmpty);
                }

                bool isValid = await _dashboardService.ValidateCardIdsAsync(cardIds);

                return Ok(new
                {
                    isValid,
                    message = isValid ? Language.CardIdsValid : Language.CardIdsInvalid,
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, string.Format(Language.CardIdsValidationError, ex.Message));
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
        public async Task<IActionResult> DeleteDashboardAsync(int id)
        {
            var username = User.Identity?.Name;
            bool result = await _dashboardService.DeleteDashboardAsync(id, username);
            if (!result)
            {
                return NotFound(new { message = string.Format(Language.DashboardNotFoundUser, id, username) });
            }

            return Ok(new { message = string.Format(Language.DashboardDeleted, id, username) });
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
        public async Task<IActionResult> UpdateDashboardConfigAsync(int id, [FromBody] string nuevoJsonDiseno)
        {
            var username = User.Identity?.Name;
            bool result = await _dashboardService.UpdateDashboardConfigAsync(id, username, nuevoJsonDiseno);
            if (!result)
            {
                return NotFound(new { message = string.Format(Language.DashboardNotFoundUser, id, username) });
            }

            return Ok(new { message = string.Format(Language.DashboardConfigUpdated, id) });
        }

        /// <summary>
        /// Agrega una tarjeta (DashboardCard) a un dashboard existente.
        /// </summary>
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [RequirePermission("Dashboards.Edit")]
        [HttpPost("{id}/card")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> AddDashboardCardAsync(int id, [FromQuery] string? jsonConfig, [FromBody] DashboardCard nuevaCard)
        {
            try
            {
                var username = User.Identity?.Name;
                bool result = await _dashboardService.AddDashboardCardAsync(id, username, jsonConfig!, nuevaCard);
                if (!result)
                {
                    return NotFound(new { message = string.Format(Language.DashboardNotFoundUser, id, username) });
                }

                return Ok(new { message = string.Format(Language.DashboardCardAdded, id) });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = string.Format(Language.DashboardCardAddError, ex.Message) });
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
        public async Task<IActionResult> ReorderDashboardCardsAsync(int id, [FromQuery] string jsonConfig, [FromBody] List<DashboardCard> orderedCards)
        {
            var username = User.Identity?.Name;
            bool result = await _dashboardService.ReorderDashboardCardsAsync(id, username, jsonConfig, orderedCards);
            if (!result)
            {
                return NotFound(new { message = string.Format(Language.DashboardNotFoundUser, id, username) });
            }

            return Ok(new { message = string.Format(Language.DashboardCardsReordered, id) });
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
        public async Task<IActionResult> DeleteDashboardCardAsync(int id, int idCard, int tipoCard)
        {
            var username = User.Identity?.Name;
            bool result = await _dashboardService.DeleteDashboardCardAsync(id, username, idCard, tipoCard);
            if (!result)
            {
                return NotFound(new { message = string.Format(Language.DashboardCardNotFound, idCard, tipoCard, id, username) });
            }

            return Ok(new { message = string.Format(Language.DashboardCardDeleted, id) });
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
        public async Task<IActionResult> UpdateDashboardInfoAsync(int id, [FromQuery] string? nombre, [FromQuery] string? descripcion)
        {
            try
            {
                var username = User.Identity?.Name;
                DashboardResponse? updated = await _dashboardService.UpdateDashboardInfoAsync(id, username, nombre, descripcion);
                if (updated == null)
                {
                    return NotFound(new { message = string.Format(Language.DashboardNotFoundUser, id, username) });
                }

                return Ok(updated);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, string.Format(Language.DashboardInfoUpdateError, ex.Message));
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
        public async Task<IActionResult> EditDashboardCardAsync(int id, [FromQuery] string? jsonConfig, [FromQuery] int idVisualizacion, [FromBody] CreateVisualizacionRequest request)
        {
            var username = User.Identity?.Name;
            if (request == null || request.Nombre == null)
            {
                return BadRequest(new { message = Language.DashboardCardEditInvalid });
            }

            bool result = await _dashboardService.EditDashboardCardAsync(id, username, jsonConfig!, idVisualizacion, request);
            if (!result)
            {
                return NotFound(new { message = Language.DashboardCardEditNotFound });
            }

            return Ok(new { message = Language.DashboardCardEditSuccess });
        }

        /// <summary>
        /// Busca dashboards por fragmento de texto en nombre o descripción.
        /// </summary>
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [RequirePermission("Dashboards.View")]
        [HttpGet("search")]
        [ProducesResponseType(typeof(List<DashboardSummaryResponse>), 200)]
        public async Task<IActionResult> SearchDashboardsAsync([FromQuery] string query)
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
        public async Task<ActionResult<ShareResponseDto>> CreateShareLinkAsync(int dashboardId, [FromBody] ShareRequestDto request)
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
                    return Unauthorized(new { message = Language.TokenInvalid });
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
                return StatusCode(500, new { message = Language.ShareLinkCreateError, details = ex.Message });
            }
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [RequirePermission("Dashboards.View")]
        [HttpGet("getShares/{dashboardId}/share")]
        [ProducesResponseType(typeof(List<ShareResponseDto>), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<List<ShareResponseDto>>> GetShareLinksForDashboardAsync(int dashboardId)
        {
            try
            {
                var username = User.Identity?.Name;
                if (string.IsNullOrWhiteSpace(username)) return Unauthorized(new { message = Language.TokenInvalid });

                var response = await _dashboardService.GetAllByDashboardAsync(dashboardId, username);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = Language.InternalError, details = ex.Message });
            }
        }

        [HttpGet("getShare/{slug}")]
        [ProducesResponseType(typeof(ShareResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ShareResponseDto>> GetPublicShareLinkAsync(string slug)
        {
            try
            {
                var response = await _dashboardService.GetBySlugAsync(slug);
                if (response == null)
                {
                    return NotFound(new { message = Language.ShareLinkNotFound });
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = Language.InternalErrorDetails, details = ex.Message });
            }
        }

        [HttpPost("ValidateShare/{slug}/validate")]
        [ProducesResponseType(typeof(ValidateSharePasswordResponseDto), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<ValidateSharePasswordResponseDto>> ValidateSharePasswordAsync(string slug, [FromBody] ValidateSharePasswordRequestDto request)
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
                return StatusCode(500, new { message = Language.InternalErrorDetails, details = ex.Message });
            }
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [RequirePermission("Dashboards.Share")]
        [HttpPut("UpdateShare/{slug}")]
        [ProducesResponseType(typeof(ShareResponseDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ShareResponseDto>> UpdateShareLinkAsync(string slug, [FromBody] ShareRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest();
            try
            {
                var username = User.Identity?.Name;
                if (string.IsNullOrWhiteSpace(username)) return Unauthorized(new { message = Language.TokenInvalid });

                var response = await _dashboardService.UpdateShareLinkAsync(slug, request, username);
                if (response == null)
                {
                    return NotFound(new { message = Language.ShareLinkNotFoundOrUnauthorized });
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = Language.InternalErrorDetails, details = ex.Message });
            }
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [RequirePermission("Dashboards.Share")]
        [HttpDelete("DeleteShare/{slug}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteShareLinkAsync(string slug)
        {
            try
            {
                var username = User.Identity?.Name;
                if (string.IsNullOrWhiteSpace(username)) return Unauthorized(new { message = Language.TokenInvalid });

                var success = await _dashboardService.DeleteShareLinkAsync(slug, username);
                if (!success)
                {
                    return NotFound(new { message = Language.ShareLinkNotFoundOrUnauthorized });
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = Language.InternalErrorDetails, details = ex.Message });
            }
        }

        #endregion
    }
}
