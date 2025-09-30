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
    public async Task<ActionResult<List<Device>>> GetSondaDevices(int page, string user, string password)
    {
        try
        {
            var devices = await _sondaIMApiService.GetAllDevices(user, password);
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
    public async Task<ActionResult<Device>> GetSondaDeviceById(int id, string user, string password)
    {
        try
        {
            var device = await _sondaIMApiService.GetDeviceById(id, user, password);
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
    public async Task<ActionResult<List<DeviceGroup>>> GetAllDeviceGroups(string user, string password)
    {
        try
        {
            var groups = await _sondaIMApiService.GetAllDeviceGroups(user, password);
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
    public async Task<ActionResult<DeviceGroup>> GetDeviceGroupById(int id, string user, string password)
    {
        try
        {
            var group = await _sondaIMApiService.GetDeviceGroupById(id, user, password);
            if (group == null) return NotFound($"No se encontró el DeviceGroup {id}");
            return Ok(group);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }

    [HttpGet("devices/group/{id}")]
    [ProducesResponseType(typeof(List<Device>), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<List<Device>>> GetDevicesOfGroup(int id, string user, string password)
    {
        try
        {
            var devices = await _sondaIMApiService.GetDeviceOfGroup(id, user, password);
            if (devices == null || !devices.Any())
            {
                return NotFound($"No se encontraron dispositivos para el grupo con ID {id}.");
            }
            return Ok(devices);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno al obtener los dispositivos del grupo: {ex.Message}");
        }
    }

    // ---------------- SOURCES ----------------
    [HttpGet("sources")]
    [ProducesResponseType(typeof(List<Source>), 200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<List<Source>>> GetAllSources(string user, string password)
    {
        try
        {
            var sources = await _sondaIMApiService.GetAllSources(user, password);
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
    public async Task<ActionResult<Source>> GetSourceById(int id, string user, string password)
    {
        try
        {
            var source = await _sondaIMApiService.GetSourceById(id, user, password);
            if (source == null) return NotFound($"No se encontró el Source {id}");
            return Ok(source);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }

    [HttpGet("devices/source/{id}")]
    [ProducesResponseType(typeof(List<Device>), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<List<Device>>> GetDevicesOfSource(int id, string user, string password)
    {
        try
        {
            var devices = await _sondaIMApiService.GetDeviceOfSource(id, user, password);
            if (devices == null || !devices.Any())
            {
                return NotFound($"No se encontraron dispositivos para el source con ID {id}.");
            }
            return Ok(devices);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno al obtener los dispositivos del source: {ex.Message}");
        }
    }

    // ---------------- SENSORS ----------------
    [HttpGet("sensors/data")]
    [ProducesResponseType(typeof(List<SensorData>), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<List<SensorData>>> GetSensorData(int deviceId, string sensorName, DateTime dateFrom, DateTime dateTo, string user, string password)
    {
        try
        {
            var sensorData = await _sondaIMApiService.GetSensorDataByDate(deviceId, sensorName, dateFrom, dateTo, user, password);
            if (sensorData == null || !sensorData.Any())
            {
                return NotFound($"No se encontraron datos para el sensor '{sensorName}' del dispositivo {deviceId} en el rango de fechas especificado.");
            }
            return Ok(sensorData);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno al obtener los datos del sensor: {ex.Message}");
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
