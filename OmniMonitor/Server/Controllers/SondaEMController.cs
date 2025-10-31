using Microsoft.AspNetCore.Mvc;

using OmniMonitor.Shared.Dtos.EM;

namespace OmniMonitor.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SondaEMController : ControllerBase
    {
        private readonly ISondaEMService _sondaEMService;
        private readonly ISondaAuthService _sondaAuthService;

        public SondaEMController(ISondaEMService sondaEMService, ISondaAuthService sondaAuthService)
        {
            _sondaEMService = sondaEMService;
            _sondaAuthService = sondaAuthService;
        }

        [HttpGet("event/{eventId}")]
        [ProducesResponseType(typeof(EventDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<EventDto>> GetEventById(int eventId, [FromQuery] string token)
        {
            try
            {
                string username = await _sondaAuthService.GetUserByTokenOMAsync(token);
                EventDto? eventDto = await _sondaEMService.GetEventById(eventId, username);
                if (eventDto == null)
                {
                    return NotFound("No se encontró el evento.");
                }

                return Ok(eventDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [HttpGet("alert/{alertId}")]
        [ProducesResponseType(typeof(AlertDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<AlertDto>> GetAlertById(int alertId, [FromQuery] string token)
        {
            try
            {
                string username = await _sondaAuthService.GetUserByTokenOMAsync(token);
                AlertDto? alertDto = await _sondaEMService.GetAlertById(alertId, username);
                if (alertDto == null)
                {
                    return NotFound("No se encontró la alerta.");
                }

                return Ok(alertDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [HttpGet("alert")]
        [ProducesResponseType(typeof(List<AlertDto>), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<AlertDto>>> GetAlerts(
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromQuery] string? query,
            [FromQuery] string? stateList,
            [FromQuery] double? x,
            [FromQuery] double? y,
            [FromQuery] double? r,
            [FromQuery] bool? forceGps,
            [FromQuery] string? sort,
            [FromQuery] string token)
        {
            try
            {
                string username = await _sondaAuthService.GetUserByTokenOMAsync(token);
                List<AlertDto> alerts = await _sondaEMService.GetAlerts(page, pageSize, query, stateList, x, y, r, forceGps, sort, username);
                if (alerts == null || alerts.Count == 0)
                {
                    return NotFound("No se han encontrado alertas.");
                }

                return Ok(alerts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [HttpGet("alert/stored")]
        [ProducesResponseType(typeof(List<AlertDto>), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<AlertDto>>> GetStoredAlerts(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? query,
        [FromQuery] string? stateList,
        [FromQuery] double? x,
        [FromQuery] double? y,
        [FromQuery] double? r,
        [FromQuery] string? sort,
        [FromQuery] string token)
        {
            try
            {
                string username = await _sondaAuthService.GetUserByTokenOMAsync(token);
                List<AlertDto> alerts = await _sondaEMService.GetStoredAlerts(page, pageSize, query, stateList, x, y, r, sort, username);
                if (alerts == null || alerts.Count == 0)
                {
                    return NotFound("No se han encontrado alertas.");
                }

                return Ok(alerts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [HttpGet("event/events")]
        [ProducesResponseType(typeof(List<EventDto>), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<EventDto>>> GetEvents(
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromQuery] string? sort,
            [FromQuery] string? query,
            [FromQuery] string token)
        {
            try
            {
                string username = await _sondaAuthService.GetUserByTokenOMAsync(token);
                List<EventDto> events = await _sondaEMService.GetEvents(page, pageSize, sort, query, username);
                if (events == null || events.Count == 0)
                {
                    return NotFound("No se han encontrado eventos.");
                }

                return Ok(events);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [HttpGet("eventtype/eventtypes")]
        [ProducesResponseType(typeof(List<EventTypeDto>), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<EventTypeDto>>> GetEventTypes(
        [FromQuery] string token)
        {
            try
            {
                string username = await _sondaAuthService.GetUserByTokenOMAsync(token);
                List<EventTypeDto> eventTypes = await _sondaEMService.GetEventTypes(username);
                if (eventTypes == null || eventTypes.Count == 0)
                {
                    return NotFound("No se han encontrado tipos de evento.");
                }

                return Ok(eventTypes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [HttpGet("extensions/{extensionId}")]
        [ProducesResponseType(typeof(ExtensionDtoDup), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<ExtensionDtoDup>> GetExtensionById(int extensionId, [FromQuery] string token)
        {
            try
            {
                string username = await _sondaAuthService.GetUserByTokenOMAsync(token);
                ExtensionDtoDup? extension = await _sondaEMService.GetExtensionById(extensionId, username);
                if (extension == null)
                {
                    return NotFound("No se encontró la extensión.");
                }

                return Ok(extension);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [HttpGet("extensions")]
        [ProducesResponseType(typeof(List<ExtensionDto>), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<ExtensionDto>>> GetExtensions(
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromQuery] string? sort,
            [FromQuery] string? query,
            [FromQuery] string? states,
            [FromQuery] string? dates,
            [FromQuery] string? priorities,
            [FromQuery] string? categories,
            [FromQuery] string? zones,
            [FromQuery] string token)
        {
            try
            {
                string username = await _sondaAuthService.GetUserByTokenOMAsync(token);
                List<ExtensionDto> extensions = await _sondaEMService.GetExtensions(page, pageSize, sort, query, states, dates, priorities, categories, zones, username);
                if (extensions == null || extensions.Count == 0)
                {
                    return NotFound("No se han encontrado extensiones.");
                }

                return Ok(extensions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [HttpGet("extensions/{extensionId}/attachedItems")]
        [ProducesResponseType(typeof(List<AttachmentDto>), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<AttachmentDto>>> GetAttachedItems(int extensionId, [FromQuery] string token)
        {
            try
            {
                string username = await _sondaAuthService.GetUserByTokenOMAsync(token);
                List<AttachmentDto> items = await _sondaEMService.GetAttachedItems(extensionId, username);
                if (items == null || items.Count == 0)
                {
                    return NotFound("No se encontraron archivos adjuntos.");
                }

                return Ok(items);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [HttpGet("Event/{eventId}/extensions")]
        [ProducesResponseType(typeof(List<ExtensionDto>), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<ExtensionDto>>> GetextensionsByEventId(
            int eventId,
            [FromQuery] string token)
        {
            try
            {
                string username = await _sondaAuthService.GetUserByTokenOMAsync(token);
                List<ExtensionDtoDup> items = await _sondaEMService.GetExtensionByEventId(eventId, username);
                if (items == null || items.Count == 0)
                {
                    return NotFound("No se han encontrado extensiones.");
                }

                return Ok(items);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [HttpGet("category")]
        [ProducesResponseType(typeof(List<CategoryDto>), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<EventDto>>> GetCategory(
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromQuery] string? sort,
            [FromQuery] string? query,
            [FromQuery] string token)
        {
            try
            {
                string username = await _sondaAuthService.GetUserByTokenOMAsync(token);
                List<CategoryDto> items = await _sondaEMService.GetCategory(page, pageSize, sort, query, username);
                return Ok(items ?? new List<CategoryDto>());
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [HttpGet("Category/alert")]
        [ProducesResponseType(typeof(List<AlertDto>), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<AlertDto>>> GetAlertsCategory(
            [FromQuery] int categoryid,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromQuery] string? query,
            [FromQuery] string? stateList,
            [FromQuery] double? x,
            [FromQuery] double? y,
            [FromQuery] double? r,
            [FromQuery] bool? forceGps,
            [FromQuery] string? sort,
            [FromQuery] string token)
        {
            try
            {
                string username = await _sondaAuthService.GetUserByTokenOMAsync(token);
                List<AlertDto> alerts = await _sondaEMService.GetAlertsCategory(categoryid, page, pageSize, query, stateList, x, y, r, forceGps, sort, username);
                return Ok(alerts ?? new List<AlertDto>());
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [HttpGet("Category/event")]
        [ProducesResponseType(typeof(List<EventDto>), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<EventDto>>> GetEventsByIdCategory(
            [FromQuery] int categoryid,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromQuery] string? query,
            [FromQuery] string? sort,
            [FromQuery] string token)
        {
            try
            {
                string username = await _sondaAuthService.GetUserByTokenOMAsync(token);
                List<EventDto> events = await _sondaEMService.GetEventsByCategory(categoryid, page, pageSize, query, sort, username);
                return Ok(events ?? new List<EventDto>());
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [HttpGet("Category/{categoryId}")]
        [ProducesResponseType(typeof(CategoryDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<EventDto>> GetCategoryById(int categoryId, [FromQuery] string token)
        {
            try
            {
                string username = await _sondaAuthService.GetUserByTokenOMAsync(token);
                CategoryDto? categoriaDto = await _sondaEMService.GetCategoryById(categoryId, username);
                if (categoriaDto == null)
                {
                    return NotFound("No se encontró la categoria.");
                }

                return Ok(categoriaDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }
    }
}
