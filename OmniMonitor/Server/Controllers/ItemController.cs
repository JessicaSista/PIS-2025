using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmniMonitor.Server.Context; 
using OmniMonitor.Shared.Dtos;     

namespace OmniMonitor.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ItemController : ControllerBase
    {
        private readonly ILogger<ItemController> _logger;
        private readonly ApplicationDbContext _context;

        public ItemController(ILogger<ItemController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("Getting all items from database");
            var items = await _context.Items.ToListAsync();
            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            _logger.LogInformation("Getting item with ID {id}", id);

            var item = await _context.Items.FindAsync(id);

            if (item == null)
                return NotFound();

            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Item nuevo)
        {
            if (nuevo == null)
                return BadRequest("El objeto no puede ser nulo");

            _logger.LogInformation("Creating a new item");

            _context.Items.Add(nuevo);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = nuevo.Id }, nuevo);
        }
    }
}