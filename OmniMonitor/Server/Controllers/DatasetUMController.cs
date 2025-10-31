using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmniMonitor.Server.Context;
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
    private readonly ISondaAuthService _sondaAuthService;
    private readonly ApplicationDbContext _context;

    public DatasetUMController(IDatasetUMService datasetUMService, ISondaAuthService sondaAuthService,ApplicationDbContext context)
    {
        _context = context;
        _datasetUMService = datasetUMService;
        _sondaAuthService = sondaAuthService;
    }

   

    /// <summary>
    /// Crea un nuevo dataset.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(DatasetUM), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<DatasetUM>> CreateDataset([FromBody] CreateDatasetUMRequest request, string token)
    {
        try
        {
            string username = await _sondaAuthService.GetUserByTokenOmAsync(token);
            var requestDataset = new CreateDatasetRequest(request.Name, request.Username, ModuleType.UrbanMonitor);
            var Dataset = await _datasetUMService.CreateDatasetAsync(requestDataset);
            var newDataset = await _datasetUMService.CreateDatasetUMAsync(request,Dataset.Id);
            await _datasetUMService.UpdateDatasetAsyncUM(Dataset.Id, requestDataset, newDataset);
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
    [HttpGet("GetAllDatasets")]
    [ProducesResponseType(typeof(List<DatasetUM>), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<List<DatasetUM>>> GetAllDatasets(string token)
    {
        try
        {
            string username = await _sondaAuthService.GetUserByTokenOmAsync(token);
            

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
    [HttpGet("GetDatasetById")]
    [ProducesResponseType(typeof(DatasetUM), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<DatasetUM>> GetDatasetById(int datasetId, string token)
    {
        try
        {
            string username = await _sondaAuthService.GetUserByTokenOmAsync(token);
           

            var dataset = await _datasetUMService.GetDatasetUMByIdForEditAsync(datasetId, username);
            if (dataset == null)
                return NotFound($"No se encontró el dataset con ID {datasetId} para el usuario {username}.");
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
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<DatasetUM>> UpdateDataset(int datasetId, [FromBody] CreateDatasetUMRequest request)
    {
        try
        {
            
            var requestDataset = new CreateDatasetRequest(request.Name, request.Username, ModuleType.UrbanMonitor);
            var updatedDataset = await _datasetUMService.UpdateDatasetUMAsync(datasetId, request);
            var id = updatedDataset.DatasetId;
            var Dataset = await _datasetUMService.UpdateDatasetAsyncUM(id, requestDataset, updatedDataset);
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
    [HttpDelete("{datasetId}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult> DeleteDataset(int datasetId, string token)
    {
        try
        {
            string username = await _sondaAuthService.GetUserByTokenOmAsync(token);
           

            var dataset = await _datasetUMService.GetDatasetUMByIdForEditAsync(datasetId, username);
            if (dataset == null)
                return NotFound($"No se encontró el dataset con ID {datasetId} para el usuario {username}.");
            var id= await _context.DatasetsUM
                .FirstOrDefaultAsync(d => d.DatasetId == datasetId && d.Username == username);
            await _datasetUMService.DeleteDatasetUMAsync(id.Id, username);
            await _datasetUMService.DeleteDatasetAsync(datasetId, username);
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno al eliminar el dataset: {ex.Message}");
        }
    }

    [HttpGet("GetAllDataset")]
    [ProducesResponseType(typeof(List<Datasets>), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<List<DatasetUM>>> GetAllDataset(string token)
    {
        try
        {
            string username = await _sondaAuthService.GetUserByTokenOmAsync(token);
          
            var datasets = await _datasetUMService.GetAllDatasetsAsync(username);
            return Ok(datasets);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno al obtener los datasets: {ex.Message}");
        }
    }
}