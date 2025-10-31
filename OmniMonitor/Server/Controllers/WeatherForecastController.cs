using Microsoft.AspNetCore.Mvc;
using OmniMonitor.Shared.Dtos;

namespace OmniMonitor.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController(ILogger<WeatherForecastController> logger)
        : Controller(logger)
    {
        private static readonly string[] _summaries =
        [
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching",
        ];

        [HttpGet]
        public IActionResult Get()
        {
            try
            {
                LogInformation();
                IEnumerable<WeatherForecast> result = [.. Enumerable.Range(1, 5)
                    .Select(index => new WeatherForecast
                    {
                        Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                        TemperatureC = Random.Shared.Next(-20, 55),
                        Summary = _summaries[Random.Shared.Next(_summaries.Length)],
                    })];
                return Ok(result);
            }
            catch (Exception ex)
            {
                LogError(ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
