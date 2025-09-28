using Microsoft.AspNetCore.Mvc;
using OmniMonitor.Shared.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class SondaMainController : ControllerBase
{
    private readonly ISondaIMService _sondaIMApiService;
    private readonly ISondaUMService _sondaUMApiService;

    public SondaMainController(ISondaIMService sondaIMApiService, ISondaUMService sondaUMApiService)
    {
        _sondaIMApiService = sondaIMApiService;
        _sondaUMApiService = sondaUMApiService;
    }

    // ---------------- DEVICES ----------------
    [HttpGet("devices")]
    [ProducesResponseType(typeof(List<Device>), 200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<List<Device>>> GetSondaDevices(int page, string user, string pass)
    {
        try
        {
            var devices = await _sondaIMApiService.GetAllDevicesByPage(page, user, pass);
            return Ok(devices);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }

    [HttpGet("devices/{id}")]
    [ProducesResponseType(typeof(Device), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<Device>> GetSondaDeviceById(int id)
    {
        try
        {
            var device = await _sondaIMApiService.GetDeviceById(id, "admin", "admin");
            if (device == null) return NotFound($"No se encontró el dispositivo {id}");
            return Ok(device);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }



    // ---------------- DEVICE GROUPS ----------------
    [HttpGet("groups")]
    [ProducesResponseType(typeof(List<DeviceGroup>), 200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<List<DeviceGroup>>> GetAllDeviceGroups()
    {
        try
        {
            var groups = await _sondaIMApiService.GetAllDeviceGroups("admin", "admin");
            return Ok(groups);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }

    [HttpGet("groups/{id}")]
    [ProducesResponseType(typeof(DeviceGroup), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<DeviceGroup>> GetDeviceGroupById(int id)
    {
        try
        {
            var group = await _sondaIMApiService.GetDeviceGroupById(id, "admin", "admin");
            if (group == null) return NotFound($"No se encontró el DeviceGroup {id}");
            return Ok(group);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }


    // ---------------- SOURCES ----------------
    [HttpGet("sources")]
    [ProducesResponseType(typeof(List<Source>), 200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<List<Source>>> GetAllSources()
    {
        try
        {
            var sources = await _sondaIMApiService.GetAllSources("admin", "admin");
            return Ok(sources);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }

    [HttpGet("sources/{id}")]
    [ProducesResponseType(typeof(Source), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<Source>> GetSourceById(int id)
    {
        try
        {
            var source = await _sondaIMApiService.GetSourceById(id, "admin", "admin");
            if (source == null) return NotFound($"No se encontró el Source {id}");
            return Ok(source);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }

    [HttpGet("um")]
    [ProducesResponseType(typeof(string), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> TestUMAPI()
    {
        try
        {
            Console.WriteLine("TEST UM API");
            var source = await _sondaUMApiService.TestUMAPI("admin", "admin");

            if (string.IsNullOrEmpty(source))
                return NotFound("No se pudo obtener token.");

            return Ok(source);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }

    [HttpGet("zones")]
    [ProducesResponseType(typeof(List<Zone>), 200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<List<Zone>>> GetAllZones()
    {
        try
        {
            var zones = await _sondaUMApiService.GetAllZones("admin", "admin");
            return Ok(zones);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }

    [HttpGet("zones/{id}")]
    [ProducesResponseType(typeof(Zone), 200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<Zone>> GetZoneById(int id)
    {
        try
        {
            var zone = await _sondaUMApiService.GetZoneById(id, "admin", "admin");
            if (zone == null) return NotFound($"No se encontró la zona {id}");
            return Ok(zone);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }

    [HttpGet("newsUM")]
    [ProducesResponseType(typeof(List<News>), 200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<List<News>>> GetAllNews(
        [FromQuery] int startIndex = 1,
        [FromQuery] string? queryString = null,
        [FromQuery] string? sort = null,
        [FromQuery] int count = 10)
    {
        try
        {
            // Pasar los parámetros al servicio
            var news = await _sondaUMApiService.GetAllNews("admin", "admin", startIndex, queryString, sort, count);
            return Ok(news);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }

    [HttpGet("newsUM/{id}")]
    [ProducesResponseType(typeof(News), 200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<News>> GetNewsById(int id)
    {
        try
        {
            var newsItem = await _sondaUMApiService.GetNewsById(id, "admin", "admin");
            if (newsItem == null) return NotFound($"No se encontró la noticia {id}");
            return Ok(newsItem);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }

    [HttpGet("events")]
    [ProducesResponseType(typeof(List<Event>), 200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<List<Event>>> GetAllEvents()
    {
        try
        {
            var events = await _sondaUMApiService.GetAllEvents("admin", "admin");
            return Ok(events);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }

    [HttpGet("events/{id}")]
    [ProducesResponseType(typeof(Event), 200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<Event>> GetEventById(int id)
    {
        try
        {
            var eventItem = await _sondaUMApiService.GetEventById(id, "admin", "admin");
            if (eventItem == null) return NotFound($"No se encontró el evento {id}");
            return Ok(eventItem);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }
}
