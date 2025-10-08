using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniMonitor.Server.Attributes;
using OmniMonitor.Server.Services;
using OmniMonitor.Shared.Dtos;

namespace OmniMonitor.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DatasetEMController : ControllerBase
    {
        private readonly IDatasetEMService _datasetEMService;

        public DatasetEMController(IDatasetEMService datasetEMService)
        {
            _datasetEMService = datasetEMService;
        }

        /// <summary>
        /// Crea un nuevo dataset EM.
        /// </summary>
        [HttpPost]
        [RequirePermission("Crear Datasets EM")]
        [ProducesResponseType(typeof(DatasetEM), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<DatasetEM>> CreateDataset([FromBody] CreateDatasetEMRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("El cuerpo de la petición no puede estar vacío.");
                }

                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return BadRequest("El nombre del dataset es requerido.");
                }

                if (string.IsNullOrWhiteSpace(request.Username))
                {
                    return BadRequest("El nombre de usuario es requerido.");
                }

                if (string.IsNullOrWhiteSpace(request.IsDataset))
                {
                    return BadRequest("El tipo de dataset es requerido.");
                }

                var createdDataset = await _datasetEMService.CreateDatasetEMAsync(request);
                return CreatedAtAction(nameof(GetDatasetById), new { datasetId = createdDataset.Id, username = createdDataset.Username }, createdDataset);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al crear el dataset: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene todos los datasets EM de un usuario.
        /// </summary>
        [HttpGet("user/{username}")]
        [RequirePermission("Ver Datasets EM")]
        [ProducesResponseType(typeof(List<DatasetEM>), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<DatasetEM>>> GetAllDatasets(string username)
        {
            try
            {
                var datasets = await _datasetEMService.GetAllDatasetsEMAsync(username);
                return Ok(datasets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al obtener los datasets: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene un dataset EM por su ID y nombre de usuario.
        /// </summary>
        [HttpGet("{datasetId}/{username}")]
        [RequirePermission("Ver Datasets EM")]
        [ProducesResponseType(typeof(DatasetEM), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<DatasetEM>> GetDatasetById(int datasetId, string username)
        {
            try
            {
                var dataset = await _datasetEMService.GetDatasetEMByIdAsync(datasetId, username);
                if (dataset == null)
                {
                    return NotFound($"No se encontró el dataset con ID {datasetId} para el usuario {username}.");
                }
                return Ok(dataset);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al obtener el dataset: {ex.Message}");
            }
        }

        /// <summary>
        /// Actualiza un dataset EM existente.
        /// </summary>
        [HttpPut("{datasetId}")]
        [RequirePermission("Crear Datasets EM")]
        [ProducesResponseType(typeof(DatasetEM), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<DatasetEM>> UpdateDataset(int datasetId, [FromBody] CreateDatasetEMRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("El cuerpo de la petición no puede estar vacío.");
                }

                // Obtener el dataset existente
                var existingDataset = await _datasetEMService.GetDatasetEMByIdForEditAsync(datasetId, request.Username);
                if (existingDataset == null)
                {
                    return NotFound($"No se encontró el dataset con ID {datasetId} para el usuario {request.Username}.");
                }

                // Actualizar las propiedades básicas
                var datasetToUpdate = new DatasetEM
                {
                    Id = existingDataset.Id,
                    Name = request.Name,
                    Description = request.Description,
                    Is_Dataset = request.IsDataset,
                    ContentType = request.ContentType,
                    Username = existingDataset.Username,
                    Id_Alert = request.AlertId,
                    Id_Event = request.EventId,
                    Id_Extension = request.ExtensionId,
                    Id_Resource = request.ResourceId,
                    AlertState = request.AlertState,
                    EventState = request.EventState,
                    ExtensionState = request.ExtensionState,
                    ResourceState = request.ResourceState
                };

                // Actualizar los alerts si se proporcionaron
                if (request.AlertIds != null && request.AlertIds.Any())
                {
                    datasetToUpdate.DatasetAlerts = new List<DatasetAlert>();
                    foreach (var alertId in request.AlertIds)
                    {
                        datasetToUpdate.DatasetAlerts.Add(new DatasetAlert 
                        { 
                            DatasetId = existingDataset.Id,
                            Id_alert = alertId 
                        });
                    }
                }
                else
                {
                    datasetToUpdate.DatasetAlerts = new List<DatasetAlert>();
                }

                // Actualizar los events si se proporcionaron
                if (request.EventIds != null && request.EventIds.Any())
                {
                    datasetToUpdate.DatasetEvents = new List<DatasetEventEM>();
                    foreach (var eventId in request.EventIds)
                    {
                        datasetToUpdate.DatasetEvents.Add(new DatasetEventEM 
                        { 
                            DatasetId = existingDataset.Id,
                            Id_event = eventId 
                        });
                    }
                }
                else
                {
                    datasetToUpdate.DatasetEvents = new List<DatasetEventEM>();
                }

                // Actualizar las extensions si se proporcionaron
                if (request.ExtensionIds != null && request.ExtensionIds.Any())
                {
                    datasetToUpdate.DatasetExtensions = new List<DatasetExtension>();
                    foreach (var extensionId in request.ExtensionIds)
                    {
                        datasetToUpdate.DatasetExtensions.Add(new DatasetExtension 
                        { 
                            DatasetId = existingDataset.Id,
                            Id_extension = extensionId 
                        });
                    }
                }
                else
                {
                    datasetToUpdate.DatasetExtensions = new List<DatasetExtension>();
                }

                // Actualizar los resources si se proporcionaron
                if (request.ResourceIds != null && request.ResourceIds.Any())
                {
                    datasetToUpdate.DatasetResources = new List<DatasetResource>();
                    foreach (var resourceId in request.ResourceIds)
                    {
                        datasetToUpdate.DatasetResources.Add(new DatasetResource 
                        { 
                            DatasetId = existingDataset.Id,
                            Id_resource = resourceId 
                        });
                    }
                }
                else
                {
                    datasetToUpdate.DatasetResources = new List<DatasetResource>();
                }

                // Llamar al servicio que incluye la validación de nombres únicos
                var updatedDataset = await _datasetEMService.UpdateDatasetEMAsync(datasetToUpdate);
                return Ok(updatedDataset);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al actualizar el dataset: {ex.Message}");
            }
        }

        /// <summary>
        /// Elimina un dataset EM.
        /// </summary>
        [HttpDelete("{datasetId}/{username}")]
        [RequirePermission("Eliminar Datasets EM")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> DeleteDataset(int datasetId, string username)
        {
            try
            {
                await _datasetEMService.DeleteDatasetEMAsync(datasetId, username);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al eliminar el dataset: {ex.Message}");
            }
        }
    }
}
