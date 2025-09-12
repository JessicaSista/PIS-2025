using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class SondaController : ControllerBase
{
    private readonly ISondaApiGetDevicesService _sondaApiService;

    public SondaController(ISondaApiGetDevicesService sondaApiService)
    {
        _sondaApiService = sondaApiService;
    }

    [HttpGet("devices")]
    [ProducesResponseType(typeof(List<Device>), 200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<List<Device>>> GetSondaDevices()
    {
        try
        {
            var devices = await _sondaApiService.GetAllDevicesAsync("admin", "admin");
            return Ok(devices);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ocurrió un error interno: {ex.Message}");
        }
    }
}