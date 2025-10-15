using Microsoft.AspNetCore.Mvc;
using OmniMonitor.Shared.Dtos.EM;
using System;
using System.Threading.Tasks;


[ApiController]
[Route("api/[controller]")]
public class SondaEMController : ControllerBase
{
    private readonly ISondaEMService _sondaEMService;

    public SondaEMController(ISondaEMService sondaEMService)
    {
        _sondaEMService = sondaEMService;
    }

    /*[HttpGet("event/{eventId}")]
    [ProducesResponseType(typeof(EventDto), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<EventDto>> GetEventById(int eventId, [FromQuery] string user, [FromQuery] string pass)
    {
        try
        {
            var eventDto = await _sondaEMService.GetEventById(eventId, user, pass);
            if (eventDto == null) return NotFound("No se encontró el evento.");
            return Ok(eventDto);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }*/

    [HttpGet("alert/{alertId}")]
    [ProducesResponseType(typeof(AlertDto), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<AlertDto>> GetAlertById(int alertId, [FromQuery] string user, [FromQuery] string pass)
    {
        try
        {
            var alertDto = await _sondaEMService.GetAlertById(alertId, user, pass);
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
        [FromQuery] string user,
        [FromQuery] string pass)
    {
        try
        {
            var alerts = await _sondaEMService.GetAlerts(page, pageSize, query, stateList, x, y, r, forceGps, sort, user, pass);
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
        [FromQuery] string user,
        [FromQuery] string pass)
    {
        try
        {
            var alerts = await _sondaEMService.GetStoredAlerts(page, pageSize, query, stateList, x, y, r, sort, user, pass);
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
        [FromQuery] string user,
        [FromQuery] string pass)
    {
        try
        {
            var events = await _sondaEMService.GetEvents(page, pageSize, sort, query, user, pass);
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
        [FromQuery] string user,
        [FromQuery] string pass)
    {
        try
        {
            var eventTypes = await _sondaEMService.GetEventTypes(user, pass);
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
    public async Task<ActionResult<ExtensionDtoDup>> GetExtensionById(int extensionId, [FromQuery] string user, [FromQuery] string pass)
    {
        try
        {
            var extension = await _sondaEMService.GetExtensionById(extensionId, user, pass);
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
        [FromQuery] string user,
        [FromQuery] string pass)
    {
        try
        {
            var extensions = await _sondaEMService.GetExtensions(page, pageSize, sort, query, states, dates, priorities, categories, zones, user, pass);
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
    public async Task<ActionResult<List<AttachmentDto>>> GetAttachedItems(int extensionId, [FromQuery] string user, [FromQuery] string pass)
    {
        try
        {
            var items = await _sondaEMService.GetAttachedItems(extensionId, user, pass);
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
       [FromQuery] string user,
       [FromQuery] string pass)
    {
        try
        {
            var events = await _sondaEMService.GetExtensionByEventId(eventId, user, pass);
            if (events == null || events.Count == 0) return NotFound("No se han encontrado eventos.");
            return Ok(events);
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
        [FromQuery] string user,
        [FromQuery] string pass)
    {
        try
        {
            var category = await _sondaEMService.GetCategory(page, pageSize, sort, query, user, pass);
            if (category == null || category.Count == 0) return NotFound("No se han encontrado Categorias.");
            return Ok(category);
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
        int Categoryid,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? query,
        [FromQuery] string? stateList,
        [FromQuery] double? x,
        [FromQuery] double? y,
        [FromQuery] double? r,
        [FromQuery] bool? forceGps,
        [FromQuery] string? sort,
        [FromQuery] string user,
        [FromQuery] string pass)
    {
        try
        {
            var alerts = await _sondaEMService.GetAlertsCategory(Categoryid,page, pageSize, query, stateList, x, y, r, forceGps, sort, user, pass);
            if (alerts == null || alerts.Count == 0) return NotFound("No se han encontrado alertas.");
            return Ok(alerts);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }

    [HttpGet("Category/event")]
    [ProducesResponseType(typeof(List<AlertDto>), 200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<List<AlertDto>>> GetEventsByIdCategory(
        int Categoryid,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? query,
        [FromQuery] string? sort,
        [FromQuery] string user,
        [FromQuery] string pass)
    {
        try
        {
            var events = await _sondaEMService.GetEventsByCategory(Categoryid, page, pageSize, query, sort, user, pass);
            if (events == null || events.Count == 0) return NotFound("No se han encontrado eventos.");
            return Ok(events);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }

}
