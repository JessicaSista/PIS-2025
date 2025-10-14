using Microsoft.AspNetCore.Mvc;
using OmniMonitor.Server.Services;
using OmniMonitor.Shared.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class DatasetUMController : ControllerBase
{
    private readonly IDatasetUMService _datasetUMService;

    public DatasetUMController(IDatasetUMService datasetUMService)
    {
        _datasetUMService = datasetUMService;
    }

    /// <summary>
    /// Crea un nuevo dataset.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(DatasetUM), 201)] // 201 Created
    [ProducesResponseType(400)] // Bad Request
    [ProducesResponseType(500)]
    public async Task<ActionResult<DatasetUM>> CreateDataset([FromBody] CreateDatasetUMRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var newDataset = await _datasetUMService.CreateDatasetUMAsync(request);
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
    [ProducesResponseType(typeof(List<DatasetUM>), 200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<List<DatasetUM>>> GetAllDatasets(string username)
    {
        try
        {
            var datasets = await _datasetUMService.GetAllDatasetsUMAsync(username);
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
    [ProducesResponseType(typeof(DatasetUM), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<DatasetUM>> GetDatasetById(int datasetId, string username)
    {
        try
        {
            var dataset = await _datasetUMService.GetDatasetUMByIdForEditAsync(datasetId, username);
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
    [ProducesResponseType(typeof(DatasetUM), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<DatasetUM>> UpdateDataset(int datasetId, [FromBody] CreateDatasetUMRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingDataset = await _datasetUMService.GetDatasetUMByIdForEditAsync(datasetId, request.Username);
            if (existingDataset == null)
            {
                return NotFound($"No se encontró el dataset con ID {datasetId} para el usuario {request.Username}.");
            }

            // Crear un dataset temporal con los nuevos valores para la validación
            var datasetToUpdate = new DatasetUM
            {
                Id = existingDataset.Id,
                Name = request.Name,
                Description = request.Description,
                Id_Zone = request.ZoneId,
                Id_News = request.NewsId,
                EventName = request.EventName,
                Is_Dataset = request.IsDataset,
                ContentType = request.ContentType,
                Username = existingDataset.Username
            };

            // Actualizar los events si se proporcionaron
            if (request.EventIds != null && request.EventIds.Any())
            {
                datasetToUpdate.DatasetEvents = new List<DatasetEvent>();
                foreach (var eventId in request.EventIds)
                {
                    datasetToUpdate.DatasetEvents.Add(new DatasetEvent 
                    { 
                        DatasetId = existingDataset.Id,
                        Id_event = eventId 
                    });
                }
            }
            else
            {
                datasetToUpdate.DatasetEvents = new List<DatasetEvent>();
            }

            // Actualizar los news si se proporcionaron
            if (request.NewsIds != null && request.NewsIds.Any())
            {
                datasetToUpdate.DatasetNews = new List<DatasetNews>();
                foreach (var newsId in request.NewsIds)
                {
                    datasetToUpdate.DatasetNews.Add(new DatasetNews 
                    { 
                        DatasetId = existingDataset.Id,
                        Id_news = newsId 
                    });
                }
            }
            else
            {
                datasetToUpdate.DatasetNews = new List<DatasetNews>();
            }

            // Llamar al servicio que incluye la validación de nombres únicos
            var updatedDataset = await _datasetUMService.UpdateDatasetUMAsync(datasetToUpdate);
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
            var dataset = await _datasetUMService.GetDatasetUMByIdForEditAsync(datasetId, username);
            if (dataset == null)
            {
                return NotFound($"No se encontró el dataset con ID {datasetId} para el usuario {username}.");
            }

            await _datasetUMService.DeleteDatasetUMAsync(datasetId, username);
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno al eliminar el dataset: {ex.Message}");
        }
    }
}