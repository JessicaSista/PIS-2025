using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic;
using OmniMonitor.Server.Services;
using OmniMonitor.Shared.Dtos;

namespace OmniMonitor.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DatasetTableController : ControllerBase
    {
        private readonly IDatasetTableService _service;

        public DatasetTableController(IDatasetTableService service)
        {
            _service = service;
        }

        // GET: api/DatasetTable/all/{username}
        [HttpGet("all/{username}")]
        public async Task<ActionResult<List<ResumenDataset>>> GetAll(string username)
        {
            var result = await _service.GetAllAsync(username);
            return Ok(result);
        }

        // GET: api/DatasetTable/{id}/{username}
        [HttpGet("{id}/{username}")]
        public async Task<ActionResult<ResponseDatasetTable>> GetById(int id, string username)
        {
            var result = await _service.GetByIdAsync(id, username);
            if (result == null)
            {
                return NotFound(new { error = $"No se encontró el dataset con ID {id} para el usuario {username}." });
            }
            return Ok(result);
        }

        // POST: api/DatasetTable
        [HttpPost]
        public async Task<ActionResult<DatasetTable>> Create([FromBody] CreateDatasetTableRequest request)
        {
            try
            {
                var datasetTable = await _service.AddAsync(request.Data, request.TipoDataset, request.Username);
                return CreatedAtAction(nameof(GetById), new { id = datasetTable.ID, username = request.Username }, datasetTable);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

            // DELETE: api/DatasetTable/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDatasetTable(int id, string username)
        {
            var deleted = await _service.DeleteAsync(id, username);
            if (deleted)
                return NoContent();
            else
                return NotFound(new { error = $"No se encontró el DatasetTable con ID {id}." });
        }
    }

}


