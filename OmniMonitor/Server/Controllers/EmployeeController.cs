using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Add this for .ToListAsync()
using OmniMonitor.Server.Context;    // Add this to reference your DbContext
using OmniMonitor.Shared.Dtos;
using System.Collections.Generic;
using System.Threading.Tasks;       // Add this for async operations

namespace OmniMonitor.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EmployeeController : ControllerBase
    {
        private readonly ILogger<EmployeeController> _logger;
        private readonly ApplicationDbContext _context; // 1. Add a field for the DbContext

        // 2. Inject ApplicationDbContext into the constructor
        public EmployeeController(ILogger<EmployeeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // 3. The Get method now correctly fetches from the database
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Employee>>> Get()
        {
            _logger.LogInformation("Getting all employees from the database");

            // This line now queries the database asynchronously
            var employees = await _context.Employees.ToListAsync();

            return Ok(employees);
        }

        // NEW: Method to get a single employee by ID
        [HttpGet("{id}")]
        public async Task<ActionResult<Employee>> GetEmployee(int id)
        {
            var employee = await _context.Employees.FindAsync(id);

            if (employee == null)
            {
                return NotFound();
            }

            return employee;
        }

        // NEW: Method to create a new employee
        // To use this, send a POST request to /api/employee with the employee data in the body.
        // Do NOT include an "id" in the body; the database will create it.
        [HttpPost]
        public async Task<ActionResult<Employee>> PostEmployee(Employee employee)
        {
            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            // Returns a 201 Created response with a link to the new employee resource
            return CreatedAtAction(nameof(GetEmployee), new { id = employee.Id }, employee);
        }


        // This test connection method is no longer needed, but you can keep it for future diagnostics
        [HttpGet("testconnection")]
        public async Task<IActionResult> TestDatabaseConnection()
        {
            try
            {
                var dbContext = HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
                await dbContext.Database.CanConnectAsync();
                return Ok("Success: Database connection was successful!");
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, $"Failed: Could not connect to the database. Error: {ex.Message}");
            }
        }
    }
}
