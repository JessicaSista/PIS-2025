using Microsoft.AspNetCore.Mvc;
using OmniMonitor.Server.Services;
using OmniMonitor.Shared.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class DatasetController : ControllerBase
{
    private readonly IDatasetService _datasetService;

    public DatasetController(IDatasetService datasetService)
    {
        _datasetService = datasetService;
    }

    /// <summary>
    /// Crea un nuevo dataset.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Dataset), 201)] // 201 Created
    [ProducesResponseType(400)] // Bad Request
    [ProducesResponseType(500)]
    public async Task<ActionResult<Dataset>> CreateDataset([FromBody] CreateDatasetRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var newDataset = await _datasetService.CreateDatasetAsync(request);
            // Devuelve una respuesta 201 Created con la ubicación del nuevo recurso
            return CreatedAtAction(nameof(GetDatasetById), new { datasetId = newDataset.Id, username = newDataset.Username }, newDataset);
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
            return StatusCode(500, $"Error interno al crear el dataset: {ex.Message}");
        }
    }

    /// <summary>
    /// Obtiene todos los datasets para un usuario específico.
    /// </summary>
    [HttpGet("user/{username}")]
    [ProducesResponseType(typeof(List<Dataset>), 200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<List<Dataset>>> GetAllDatasets(string username, [FromQuery] string? search = null)
    {
        try
        {
            var datasets = string.IsNullOrWhiteSpace(search) 
                ? await _datasetService.GetAllDatasetsAsync(username)
                : await _datasetService.GetAllDatasetsAsync(username, search);
            
            return Ok(datasets);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno al obtener los datasets: {ex.Message}");
        }
    }

    /// <summary>
    /// Obtiene un dataset específico por su ID y nombre de usuario.
    /// </summary>
    [HttpGet("{datasetId}/{username}")]
    [ProducesResponseType(typeof(Dataset), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<Dataset>> GetDatasetById(int datasetId, string username)
    {
        try
        {
            var dataset = await _datasetService.GetDatasetByIdForEditAsync(datasetId, username);
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
    /// Actualiza un dataset existente.
    /// </summary>
    [HttpPut("{datasetId}")]
    [ProducesResponseType(typeof(Dataset), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<Dataset>> UpdateDataset(int datasetId, [FromBody] CreateDatasetRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingDataset = await _datasetService.GetDatasetByIdForEditAsync(datasetId, request.Username);
            if (existingDataset == null)
            {
                return NotFound($"No se encontró el dataset con ID {datasetId} para el usuario {request.Username}.");
            }

            // Crear un dataset temporal con los nuevos valores para la validación
            var datasetToUpdate = new Dataset
            {
                Id = existingDataset.Id,
                Name = request.Name,
                Description = request.Description,
                Id_Source = request.SourceId,
                Id_Group = request.GroupId,
                SensorName = request.SensorName,
                Is_Dataset = request.IsDataset,
                ContentType = request.ContentType,
                Username = existingDataset.Username
            };

            // Actualizar los devices si se proporcionaron
            if (request.DeviceIds != null && request.DeviceIds.Any())
            {
                datasetToUpdate.DatasetDevices = new List<DatasetDevice>();
                foreach (var deviceId in request.DeviceIds)
                {
                    datasetToUpdate.DatasetDevices.Add(new DatasetDevice 
                    { 
                        DatasetId = existingDataset.Id,
                        Id_device = deviceId 
                    });
                }
            }
            else
            {
                datasetToUpdate.DatasetDevices = new List<DatasetDevice>();
            }

            // Llamar al servicio que incluye la validación de nombres únicos
            var updatedDataset = await _datasetService.UpdateDatasetAsync(datasetToUpdate);
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
            return StatusCode(500, $"Error interno al actualizar el dataset: {ex.Message}");
        }
    }

    /// <summary>
    /// Elimina un dataset.
    /// </summary>
    [HttpDelete("{datasetId}/{username}")]
    [ProducesResponseType(204)] // No Content
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult> DeleteDataset(int datasetId, string username)
    {
        try
        {
            var dataset = await _datasetService.GetDatasetByIdForEditAsync(datasetId, username);
            if (dataset == null)
            {
                return NotFound($"No se encontró el dataset con ID {datasetId} para el usuario {username}.");
            }

            await _datasetService.DeleteDatasetAsync(datasetId, username);
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno al eliminar el dataset: {ex.Message}");
        }
    }
}
