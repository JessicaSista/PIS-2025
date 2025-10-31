using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OmniMonitor.Shared.Dtos;
using OmniMonitor.Shared.Dtos.EM;
using System;
using System.Threading.Tasks;


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
            string username = await _sondaAuthService.GetUserByTokenOmAsync(token);
            var eventDto = await _sondaEMService.GetEventById(eventId, username);
            if (eventDto == null) return NotFound("No se encontró el evento.");
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
            string username = await _sondaAuthService.GetUserByTokenOmAsync(token);
            var alertDto = await _sondaEMService.GetAlertById(alertId, username);
            if (alertDto == null) return NotFound("No se encontró la alerta.");
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
            string username = await _sondaAuthService.GetUserByTokenOmAsync(token);
            var alerts = await _sondaEMService.GetAlerts(page, pageSize, query, stateList, x, y, r, forceGps, sort, username);
              if (alerts == null || alerts.Count == 0) return NotFound("No se han encontrado alertas.");
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
            string username = await _sondaAuthService.GetUserByTokenOmAsync(token);
            var alerts = await _sondaEMService.GetStoredAlerts(page, pageSize, query, stateList, x, y, r, sort, username);
              if (alerts == null || alerts.Count == 0) return NotFound("No se han encontrado alertas.");
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
            string username = await _sondaAuthService.GetUserByTokenOmAsync(token);
            var events = await _sondaEMService.GetEvents(page, pageSize, sort, query, username);
              if (events == null || events.Count == 0) return NotFound("No se han encontrado eventos.");
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
            string username = await _sondaAuthService.GetUserByTokenOmAsync(token);
            var eventTypes = await _sondaEMService.GetEventTypes(username);
            if (eventTypes == null || eventTypes.Count == 0) return NotFound("No se han encontrado tipos de evento.");
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
            string username = await _sondaAuthService.GetUserByTokenOmAsync(token);
            var extension = await _sondaEMService.GetExtensionById(extensionId, username);
            if (extension == null) return NotFound("No se encontró la extensión.");
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
            string username = await _sondaAuthService.GetUserByTokenOmAsync(token);
            var extensions = await _sondaEMService.GetExtensions(page, pageSize, sort, query, states, dates, priorities, categories, zones, username);
            if (extensions == null || extensions.Count == 0) return NotFound("No se han encontrado extensiones.");
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
            string username = await _sondaAuthService.GetUserByTokenOmAsync(token);
            var items = await _sondaEMService.GetAttachedItems(extensionId, username);
            if (items == null || items.Count == 0) return NotFound("No se encontraron archivos adjuntos.");
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
            string username = await _sondaAuthService.GetUserByTokenOmAsync(token);
            var items = await _sondaEMService.GetExtensionByEventId(eventId, username);
            if (items == null || items.Count == 0) return NotFound("No se han encontrado extensiones.");
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
            string username = await _sondaAuthService.GetUserByTokenOmAsync(token);
            var items = await _sondaEMService.GetCategory(page,pageSize,sort,query,username);
            // Return empty list instead of NotFound to allow UI to display empty select
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
        [FromQuery] int Categoryid,
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
            string username = await _sondaAuthService.GetUserByTokenOmAsync(token);
            var alerts = await _sondaEMService.GetAlertsCategory(Categoryid, page, pageSize, query, stateList, x, y, r, forceGps, sort, username);
            // Return empty list instead of NotFound to allow UI to display empty select
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
        [FromQuery] int Categoryid,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? query,
        [FromQuery] string? sort,
        [FromQuery] string token)
    {
        try
        {
            string username = await _sondaAuthService.GetUserByTokenOmAsync(token);
            var events = await _sondaEMService.GetEventsByCategory(Categoryid, page, pageSize, query, sort, username);
            // Return empty list instead of NotFound to allow UI to display empty select
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
            string username = await _sondaAuthService.GetUserByTokenOmAsync(token);
            var CategoriaDto = await _sondaEMService.GetCategoryById(categoryId, username);
            if (CategoriaDto == null) return NotFound("No se encontró la categoria.");
            return Ok(CategoriaDto);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }



}
