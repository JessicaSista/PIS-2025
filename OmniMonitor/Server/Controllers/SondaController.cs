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
    [ProducesResponseType(typeof(string), 200)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetSondaDevices()
    {
        try
        {
            var devicesJson = await _sondaApiService.GetAllDevicesAsync("admin", "admin");
            return Content(devicesJson, "application/json");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ocurrió un error interno: {ex.Message}");
        }
    }
}