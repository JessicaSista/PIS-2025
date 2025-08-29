using Microsoft.AspNetCore.Mvc;
using OmniMonitor.Shared.Dtos;

namespace OmniMonitor.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MarrunoController(ILogger<MarrunoController> logger)
        : Controller(logger)
    {
        private static List<Negro> negros = new List<Negro>
{
            new() { Id = 1, Nombre = "Tupac", Edad = 25, BalasRecibidas = 4, NombreRapero = "2Pac" },
            new() { Id = 2, Nombre = "Christopher", Edad = 24, BalasRecibidas = 3, NombreRapero = "The Notorious B.I.G." },
            new() { Id = 3, Nombre = "Curtis", Edad = 30, BalasRecibidas = 9, NombreRapero = "50 Cent" },
            new() { Id = 4, Nombre = "Shawn", Edad = 32, BalasRecibidas = 0, NombreRapero = "Jay-Z" },
            new() { Id = 5, Nombre = "Kanye", Edad = 28, BalasRecibidas = 0, NombreRapero = "Ye" },
            new() { Id = 6, Nombre = "Marshall", Edad = 27, BalasRecibidas = 0, NombreRapero = "Eminem" },
            new() { Id = 7, Nombre = "Kendrick", Edad = 29, BalasRecibidas = 0, NombreRapero = "Kendrick Lamar" },
            new() { Id = 8, Nombre = "Nasir", Edad = 30, BalasRecibidas = 1, NombreRapero = "Nas" },
            new() { Id = 9, Nombre = "Andre", Edad = 31, BalasRecibidas = 0, NombreRapero = "Dr. Dre" },
            new() { Id = 10, Nombre = "Calvin", Edad = 33, BalasRecibidas = 0, NombreRapero = "Snoop Dogg" }
        };

        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(negros);
        }

        [HttpPost]
        public IActionResult Create([FromBody] Negro nuevoNegro)
        {
            if (nuevoNegro == null)
                return BadRequest("El objeto no puede ser nulo");

            // Generar nuevo Id
            nuevoNegro.Id = negros.Any() ? negros.Max(n => n.Id) + 1 : 1;

            negros.Add(nuevoNegro);
            return CreatedAtAction(nameof(GetById), new { id = nuevoNegro.Id }, nuevoNegro);
        }



        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var negro = negros.FirstOrDefault(n => n.Id == id);
            if (negro == null)
                return NotFound();
            return Ok(negro);
        }

    }
}
