

using Microsoft.AspNetCore.Mvc;
using OmniMonitor.Server.Services;
using OmniMonitor.Shared.Dtos;
using System.Threading.Tasks;
using System.Linq;

namespace OmniMonitor.Server.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class DatasetAMController : ControllerBase
    {
        private readonly IDatasetAmService _datasetAmService;

        public DatasetAMController(IDatasetAmService datasetAmService)
        {
            _datasetAmService = datasetAmService;
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

                var newDatasetAM = await _datasetAmService.CreateDatasetAMAsync(request);
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
        [HttpGet("user/{username}")]
        [ProducesResponseType(typeof(List<DatasetAM>), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<DatasetAM>>> GetAllDatasetAMs(string username)
        {
            try
            {
                var datasets = await _datasetAmService.GetAllDatasetAMsAsync(username);
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
        [HttpGet("{id}/{username}")]
        [ProducesResponseType(typeof(DatasetAM), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<DatasetAM>> GetDatasetAMById(int id, string username)
        {
            try
            {
                var dataset = await _datasetAmService.GetDatasetAMByIdAsync(id, username);
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
        [HttpGet("edit/{id}/{username}")]
        [ProducesResponseType(typeof(DatasetAM), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<DatasetAM>> GetDatasetAMByIdForEdit(int id, string username)
        {
            try
            {
                var dataset = await _datasetAmService.GetDatasetAMByIdForEditAsync(id, username);
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

                var existingDataset = await _datasetAmService.GetDatasetAMByIdForEditAsync(id, request.Username);
                if (existingDataset == null)
                {
                    return NotFound($"No se encontró el DatasetAM con ID {id} para el usuario {request.Username}.");
                }

                // Crear el nuevo objeto DatasetAM igual que en CreateDatasetAMAsync
                var datasetToUpdate = new DatasetAM
                {
                    Id_Dataset = existingDataset.Id_Dataset,
                    Username = existingDataset.Username,
                    Nombre = request.Nombre,
                    Descripcion = request.Descripcion,
                    Is_Dataset = request.IsDataset,
                    Type_Dataset = request.Type_Dataset,
                    Id_Event_Task = request.Type_Dataset == 1 ? request.Id_Event_Task : null,
                    Id_Asset_Type = request.Type_Dataset == 2 ? request.Id_Asset_Type : null
                };

                if (request.IsDataset == "S")
                {
                    datasetToUpdate.ContentType = "0";
                }
                else
                {
                    datasetToUpdate.ContentType = request.ContentType;
                }

                // Actualizar los Event Task Instances si se proporcionaron
                if (request.Type_Dataset == 1 && request.Grupo_Event_Task_Instance_Ids != null && request.Grupo_Event_Task_Instance_Ids.Any())
                {
                    if (request.StockIds != null && request.StockIds.Count > 0)
                    {
                        if (request.Grupo_Event_Task_Instance_Ids.Count != 1)
                            return BadRequest("Solo se pueden asociar stocks si se selecciona un único Event Task Instance.");

                        var eventTaskInstance = new OmniMonitor.Shared.Dtos.DatasetEventTaskInstance
                        {
                            DatasetAMId = existingDataset.Id_Dataset, // Link to existing dataset
                            Id_Event_Task_Instance = request.Grupo_Event_Task_Instance_Ids[0],
                            Grupo_Stock = request.StockIds.Select(stockId => new OmniMonitor.Shared.Dtos.DatasetStock 
                            { 
                                Id_Stock = stockId,
                                DatasetEventTaskInstanceId = 0 // Will be set when the event task instance is saved
                            }).ToList()
                        };
                        datasetToUpdate.Grupo_Event_Task_Instance = new List<OmniMonitor.Shared.Dtos.DatasetEventTaskInstance> { eventTaskInstance };
                    }
                    else
                    {
                        datasetToUpdate.Grupo_Event_Task_Instance = new List<OmniMonitor.Shared.Dtos.DatasetEventTaskInstance>();
                        foreach (var eventTaskInstanceId in request.Grupo_Event_Task_Instance_Ids)
                        {
                            datasetToUpdate.Grupo_Event_Task_Instance.Add(new OmniMonitor.Shared.Dtos.DatasetEventTaskInstance
                            {
                                DatasetAMId = existingDataset.Id_Dataset, // Link to existing dataset
                                Id_Event_Task_Instance = eventTaskInstanceId
                            });
                        }
                    }
                }
                else
                {
                    datasetToUpdate.Grupo_Event_Task_Instance = new List<OmniMonitor.Shared.Dtos.DatasetEventTaskInstance>();
                }

                // Actualizar los Assets si se proporcionaron
                if (request.Type_Dataset == 2 && request.Grupo_Asset_Ids != null && request.Grupo_Asset_Ids.Any())
                {
                    datasetToUpdate.Grupo_Asset = new List<OmniMonitor.Shared.Dtos.DatasetAsset>();
                    foreach (var idAsset in request.Grupo_Asset_Ids)
                    {
                        datasetToUpdate.Grupo_Asset.Add(new OmniMonitor.Shared.Dtos.DatasetAsset 
                        { 
                            Id_Asset = idAsset,
                            DatasetAMId = existingDataset.Id_Dataset // Link to existing dataset
                        });
                    }
                }
                else
                {
                    datasetToUpdate.Grupo_Asset = new List<OmniMonitor.Shared.Dtos.DatasetAsset>();
                }

                // Llamar al servicio que incluye la validación de nombres únicos
                var updatedDataset = await _datasetAmService.UpdateDatasetAMAsync(datasetToUpdate);
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
                Console.WriteLine($"[UpdateDatasetAM] Exception: {ex.Message}\n{ex.StackTrace}");
                if (ex.InnerException != null)
                    Console.WriteLine($"[UpdateDatasetAM] Inner: {ex.InnerException.Message}\n{ex.InnerException.StackTrace}");
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
        public async Task<IActionResult> DeleteDatasetAM(int id, [FromQuery] string username)
        {
            try
            {
                await _datasetAmService.DeleteDatasetAMAsync(id, username);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
