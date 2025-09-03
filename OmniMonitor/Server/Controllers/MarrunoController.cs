using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Required for ToListAsync and FindAsync
using OmniMonitor.Server.Context;
using OmniMonitor.Shared.Dtos;
using System.Threading.Tasks;       // Required for async operations

namespace OmniMonitor.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MarrunoController : ControllerBase
    {
        private readonly ILogger<MarrunoController> _logger;
        private readonly ApplicationDbContext _context;

        public MarrunoController(ILogger<MarrunoController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // MODIFIED: Fetches all records from the database asynchronously.
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("Getting all Marrunos from database");
            var marrunos = await _context.Raperos.ToListAsync(); // Assuming the DbSet is named Negros
            return Ok(marrunos);
        }

        // MODIFIED: Fetches a single record by its ID from the database.
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            _logger.LogInformation("Getting Marruno with ID {id}", id);

            // FindAsync is an efficient way to query by primary key.
            var marruno = await _context.Raperos.FindAsync(id);

            if (marruno == null)
                return NotFound();

            return Ok(marruno);
        }

        // MODIFIED: Creates a new record in the database.
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Rapero nuevo)
        {
            if (nuevo == null)
                return BadRequest("El objeto no puede ser nulo");

            _logger.LogInformation("Creating a new Marruno");

            // Add the new object to the DbContext.
            _context.Raperos.Add(nuevo);

            // Save changes to the database. The database will automatically assign the new Id.
            await _context.SaveChangesAsync();

            // Return a 201 Created response.
            return CreatedAtAction(nameof(GetById), new { id = nuevo.Id }, nuevo);
        }
    }
}