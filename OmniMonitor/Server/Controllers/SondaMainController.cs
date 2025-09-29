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
    public async Task<ActionResult<List<Device>>> GetSondaDevices(int page)
    {
        try
        {
            var devices = await _sondaIMApiService.GetAllDevices("admin", "admin");
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
}
