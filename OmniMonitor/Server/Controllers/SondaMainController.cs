using Microsoft.AspNetCore.Mvc;
using OmniMonitor.Shared.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class SondaMainController : ControllerBase
{
    private readonly ISondaIMService _sondaApiService;

    public SondaMainController(ISondaIMService sondaApiService)
    {
        _sondaApiService = sondaApiService;
    }

    // ---------------- DEVICES ----------------
    [HttpGet("devices")]
    [ProducesResponseType(typeof(List<Device>), 200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<List<Device>>> GetSondaDevices(int page, string user, string pass)
    {
        try
        {
            var devices = await _sondaApiService.GetAllDevicesByPage(page, user, pass);
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
            var device = await _sondaApiService.GetDeviceById(id, "admin", "admin");
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
            var groups = await _sondaApiService.GetAllDeviceGroups("admin", "admin");
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
            var group = await _sondaApiService.GetDeviceGroupById(id, "admin", "admin");
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
            var sources = await _sondaApiService.GetAllSources("admin", "admin");
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
            var source = await _sondaApiService.GetSourceById(id, "admin", "admin");
            if (source == null) return NotFound($"No se encontró el Source {id}");
            return Ok(source);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }

   
}
