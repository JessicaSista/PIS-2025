using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using OmniMonitor.Server.Context;
using OmniMonitor.Server.Services;
using OmniMonitor.Shared.Dtos;

namespace OmniMonitor.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DatasetAMController : ControllerBase
    {
        private readonly IDatasetAmService _datasetAmService;
        private readonly ISondaAuthService _sondaAuthService;
        private readonly IDatasetUMService _datasetUMService;
        private readonly ApplicationDbContext _context;

        public DatasetAMController(IDatasetAmService datasetAmService, ISondaAuthService sondaAuthService, IDatasetUMService datasetUMService, ApplicationDbContext context)
        {
            _datasetAmService = datasetAmService;
            _sondaAuthService = sondaAuthService;
            _datasetUMService = datasetUMService;
            _context = context;
        }

        /// <summary>
        /// Crea un nuevo DatasetAM.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(DatasetAM), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<DatasetAM>> CreateDatasetAM([FromBody] CreateDatasetAMRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var requestDataset = new CreateDatasetRequest(request.Nombre, request.Username, ModuleType.AssetManager);
                Datasets newDataset = await _datasetUMService.CreateDatasetAsync(requestDataset);
                DatasetAM newDatasetAM = await _datasetAmService.CreateDatasetAMAsync(request, newDataset.Id);
                await _datasetUMService.UpdateDatasetAsyncAM(newDataset.Id, requestDataset, newDatasetAM);

                return CreatedAtAction(nameof(GetDatasetAMByIdForEdit), new { id = newDatasetAM.Id_Dataset, username = newDatasetAM.Username }, newDatasetAM);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al crear el DatasetAM: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene todos los DatasetAM para un usuario específico.
        /// </summary>
        [HttpGet("GetAllDatasetAMs")]
        [ProducesResponseType(typeof(List<DatasetAM>), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<DatasetAM>>> GetAllDatasetAMs(string token)
        {
            try
            {
                string username = await _sondaAuthService.GetUserByTokenOMAsync(token);
                List<DatasetAM> datasets = await _datasetAmService.GetAllDatasetAMsAsync(username);
                return Ok(datasets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al obtener los DatasetAM: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene un DatasetAM específico por su ID y nombre de usuario (con lógica dinámica).
        /// </summary>
        [HttpGet("GetDatasetAMById")]
        [ProducesResponseType(typeof(DatasetAM), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<DatasetAM>> GetDatasetAMById(int id, string token)
        {
            try
            {
                string username = await _sondaAuthService.GetUserByTokenOMAsync(token);
                DatasetAM? dataset = await _datasetAmService.GetDatasetAMByIdAsync(id, username);
                if (dataset == null)
                {
                    return NotFound($"No se encontró el DatasetAM con ID {id} para el usuario {username}.");
                }

                return Ok(dataset);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al obtener el DatasetAM: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene un DatasetAM específico por su ID y nombre de usuario para edición (SIN lógica dinámica).
        /// </summary>
        [HttpGet("GetDatasetAMByIdForEdit")]
        [ProducesResponseType(typeof(DatasetAM), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<DatasetAM>> GetDatasetAMByIdForEdit(int id, string token)
        {
            try
            {
                string username = await _sondaAuthService.GetUserByTokenOMAsync(token);
                DatasetAM? dataset = await _datasetAmService.GetDatasetAMByIdForEditAsync(id, username);
                if (dataset == null)
                {
                    return NotFound($"No se encontró el DatasetAM con ID {id} para el usuario {username}.");
                }

                return Ok(dataset);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al obtener el DatasetAM para edición: {ex.Message}");
            }
        }

        /// <summary>
        /// Actualiza un DatasetAM existente.
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(DatasetAM), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<DatasetAM>> UpdateDatasetAM(int id, [FromBody] CreateDatasetAMRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                DatasetAM? existingDataset = await _datasetAmService.GetDatasetAMByIdForEditAsync(id, request.Username);
                if (existingDataset == null)
                {
                    return NotFound($"No se encontró el DatasetAM con ID {id} para el usuario {request.Username}.");
                }

                // Primero validar el nombre en la tabla general antes de actualizar cualquier tabla
                await _datasetUMService.ValidateDatasetNameAsync(request.Nombre, request.Username, ModuleType.AssetManager, existingDataset.DatasetId);

                // Llamar al servicio que incluye la validación de nombres únicos
                DatasetAM updatedDataset = await _datasetAmService.UpdateDatasetAMAsync(existingDataset, request);
                var requestDataset = new CreateDatasetRequest(request.Nombre, request.Username, ModuleType.AssetManager);
                Datasets newDataset = await _datasetUMService.UpdateDatasetAsyncAM(updatedDataset.DatasetId, requestDataset, updatedDataset);
                return Ok(updatedDataset);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al actualizar el DatasetAM: {ex.Message}");
            }
        }

        /// <summary>
        /// Elimina un DatasetAM y todas sus relaciones hijas.
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)] // No Content
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> DeleteDatasetAM(int id, [FromQuery] string token)
        {
            try
            {
                string username = await _sondaAuthService.GetUserByTokenOMAsync(token);
                DatasetAM? datasetid = await _context.DatasetAM
                .FirstOrDefaultAsync(d => d.Id_Dataset == id && d.Username == username);
                
                if (datasetid == null)
                {
                    return NotFound($"No se encontró el dataset con ID {id} para el usuario {username}.");
                }
                
                await _datasetAmService.DeleteDatasetAMAsync(id, username);
                await _datasetUMService.DeleteDatasetAsync(datasetid.DatasetId, username);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
