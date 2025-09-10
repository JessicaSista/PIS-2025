using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class SondaController : ControllerBase
{
    private readonly ISondaApiService _sondaApiService;

    public SondaController(ISondaApiService sondaApiService)
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
            var devicesJson = await _sondaApiService.GetDevicesAsync();
            return Content(devicesJson, "application/json");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ocurrió un error interno: {ex.Message}");
        }
    }
}