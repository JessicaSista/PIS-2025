using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmniMonitor.Server.Context;
using OmniMonitor.Shared.Dtos; // Make sure your SensorClimax class is in this namespace
using System.Threading.Tasks;

namespace OmniMonitor.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SensorClimaxController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SensorClimaxController> _logger;

        public SensorClimaxController(ApplicationDbContext context, ILogger<SensorClimaxController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /sensorclimax
        [HttpGet]
        public async Task<IActionResult> GetAllReadings()
        {
            _logger.LogInformation("Fetching all SensorClimax readings.");
            var readings = await _context.SensorClimaxs.ToListAsync();
            return Ok(readings);
        }

        // POST: /sensorclimax
        [HttpPost]
        public async Task<IActionResult> AddReading([FromBody] SensorClimax newReading)
        {
            if (newReading == null)
            {
                return BadRequest("Reading cannot be null.");
            }

            _logger.LogInformation("Adding a new SensorClimax reading.");

            _context.SensorClimaxs.Add(newReading);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAllReadings), new { id = newReading.Id }, newReading);
        }
    }
}
