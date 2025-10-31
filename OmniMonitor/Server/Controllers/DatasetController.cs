using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmniMonitor.Server.Context;
using OmniMonitor.Server.Services;
using OmniMonitor.Shared.Dtos;

[ApiController]
[Route("api/[controller]")]
public class DatasetController : ControllerBase
{
    private readonly IDatasetService _datasetService;
    private readonly ISondaAuthService _sondaAuthService;
    private readonly ISondaIMService _sondaIMService;
    private readonly IDatasetUMService _datasetUMService;
    private readonly ApplicationDbContext _context;
    public DatasetController(IDatasetService datasetService, ISondaAuthService sondaAuthService, ISondaIMService sondaIMService, IDatasetUMService datasetUMService,ApplicationDbContext context)
    {
        _datasetService = datasetService;
        _sondaAuthService = sondaAuthService;
        _sondaIMService = sondaIMService;
        _datasetUMService = datasetUMService;
        _context = context;
    }

    /// <summary>
    /// Crea un nuevo dataset.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(DatasetIM), 201)] // 201 Created
    [ProducesResponseType(400)] // Bad Request
    [ProducesResponseType(500)]
    public async Task<ActionResult<DatasetIM>> CreateDataset([FromBody] CreateDatasetIMRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var requestDataset = new CreateDatasetRequest(request.Name, request.Username, ModuleType.InsightMonitor);
            var Dataset = await _datasetUMService.CreateDatasetAsync(requestDataset);
            var newDataset = await _datasetService.CreateDatasetIMAsync(request,Dataset.Id);
            await _datasetUMService.UpdateDatasetAsyncIM(Dataset.Id, requestDataset, newDataset);
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
    [HttpGet("user")]
    [ProducesResponseType(typeof(List<DatasetIM>), 200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<List<DatasetIM>>> GetAllDatasets(string token, [FromQuery] string? search = null)
    {
        try
        {
            string username = await _sondaAuthService.GetUserByTokenOmAsync(token);
            
            // Por ahora usamos el método sin búsqueda y filtramos en memoria
            // TODO: Implementar búsqueda en el servicio cuando sea necesario
            var datasets = await _datasetService.GetAllDatasetsIMAsync(username);
            
            // Si hay un término de búsqueda, filtramos en memoria
            if (!string.IsNullOrWhiteSpace(search))
            {
                var normalizedSearch = NormalizeText(search);
                datasets = datasets
                    .Where(d => NormalizeText(d.Name).Contains(normalizedSearch))
                    .ToList();
            }
            
            return Ok(datasets);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno al obtener los datasets: {ex.Message}");
        }
    }

    /// <summary>
    /// Normaliza el texto para búsquedas insensibles a acentos y mayúsculas.
    /// </summary>
    private string NormalizeText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        // 1) Normalizar a FormD y remover diacríticos (acentos)
        var formD = text.Trim().ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD);
        var withoutDiacritics = new string(formD.Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark).ToArray());

        // 2) Reemplazos adicionales: espacios fuera, ñ->n, subíndices -> dígitos normales
        withoutDiacritics = withoutDiacritics
            .Replace(" ", string.Empty)
            .Replace("ñ", "n")
            .Replace("₀", "0").Replace("₁", "1").Replace("₂", "2").Replace("₃", "3").Replace("₄", "4")
            .Replace("₅", "5").Replace("₆", "6").Replace("₇", "7").Replace("₈", "8").Replace("₉", "9");

        // 3) Normalizar de vuelta a FormC
        return withoutDiacritics.Normalize(System.Text.NormalizationForm.FormC);
    }

    /// <summary>
    /// Identifica rápidamente a qué módulo pertenece un dataset.
    /// Retorna: "Insight Monitor", "Asset Manager", "Urban Monitor", o null si no se encuentra.
    /// </summary>
    [HttpGet("GetDatasetModule")]
    [ProducesResponseType(typeof(string), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<string>> GetDatasetModule(int datasetId, string token)
    {
        try
        {
            string username = await _sondaAuthService.GetUserByTokenOmAsync(token);
            var module = await _datasetService.IdentifyDatasetModuleAsync(datasetId, username);
            
            if (module == null)
            {
                return NotFound($"No se encontró el dataset con ID {datasetId}.");
            }
            
            return Ok(module);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno al identificar el módulo: {ex.Message}");
        }
    }

    /// <summary>
    /// Obtiene un dataset específico por su ID y nombre de usuario.
    /// </summary>
    [HttpGet("GetDataset")]
    [ProducesResponseType(typeof(DatasetIM), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<DatasetIM>> GetDatasetById(int datasetId, string token)
    {
        try
        {
            string username = await _sondaAuthService.GetUserByTokenOmAsync(token);
            var dataset = await _datasetService.GetDatasetIMByIdForEditAsync(datasetId, username);
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
    [ProducesResponseType(typeof(DatasetIM), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<DatasetIM>> UpdateDataset(int datasetId, [FromBody] CreateDatasetIMRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingDataset = await _datasetService.GetDatasetIMByIdForEditAsync(datasetId, request.Username);
            if (existingDataset == null)
            {
                return NotFound($"No se encontró el dataset con ID {datasetId} para el usuario {request.Username}.");
            }


            // Llamar al servicio que incluye la validación de nombres únicos
            var updatedDataset = await _datasetService.UpdateDatasetIMAsync(existingDataset,request);
            var requestDataset = new CreateDatasetRequest(request.Name, request.Username, ModuleType.UrbanMonitor);
            var Dataset = await _datasetUMService.UpdateDatasetAsyncIM(updatedDataset.DatasetId, requestDataset, updatedDataset);
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
    [HttpDelete("DeleteDataset")]
    [ProducesResponseType(204)] // No Content
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult> DeleteDataset(int datasetId, string token)
    {
        try
        {
            string username = await _sondaAuthService.GetUserByTokenOmAsync(token);
            var dataset = await _datasetService.GetDatasetIMByIdForEditAsync(datasetId, username);
            if (dataset == null)
            {
                return NotFound($"No se encontró el dataset con ID {datasetId} para el usuario {username}.");
            }
            var id = await _context.DatasetsIM
                .FirstOrDefaultAsync(d => d.Id == datasetId && d.Username == username);
            await _datasetService.DeleteDatasetIMAsync(datasetId, username);
            await _datasetUMService.DeleteDatasetAsync(id.DatasetId, username);
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno al eliminar el dataset: {ex.Message}");
        }
    }

[HttpGet("GetSensorType")]
[ProducesResponseType(typeof(string), 200)]
[ProducesResponseType(404)]
[ProducesResponseType(500)]
public async Task<ActionResult<string>> GetSensorType(int datasetId, [FromQuery] string token)
{
    try
    {
        Console.WriteLine($"[TRACE] Token recibido: {token}");
        string username = await _sondaAuthService.GetUserByTokenOmAsync(token);
        Console.WriteLine($"[TRACE] Usuario: {username}");

        var dataset = await _datasetService.GetDatasetIMByIdAsync(datasetId, username);
        Console.WriteLine($"[TRACE] Dataset encontrado: {dataset?.Id}, Source: {dataset?.Id_Source}, SensorName: {dataset?.SensorName}");
        if (dataset == null)
            return NotFound($"No se encontró el dataset con ID {datasetId}.");

        if (dataset.Id_Source == null || string.IsNullOrEmpty(dataset.SensorName))
        {
            Console.WriteLine($"[TRACE] Dataset sin Source o SensorName");
            return BadRequest("El dataset no contiene información suficiente (Source o SensorName).");
        }


        var source = await _sondaIMService.GetSourceById((int)dataset.Id_Source, username);
        Console.WriteLine($"[TRACE] Source encontrado: {source?.Id}");
        if (source == null)
            return NotFound($"No se encontró el Source con ID {dataset.Id_Source}.");

        // Recorrer los devices del source, obtener cada uno por GetDeviceById y buscar el sensor ahí
        if (source.Devices != null)
        {
            foreach (var dev in source.Devices)
            {
                Console.WriteLine($"[TRACE] Device: {dev.Id}, Name: {dev.Name}");
                var fullDevice = await _sondaIMService.GetDeviceById(dev.Id, username);
                if (fullDevice == null)
                {
                    Console.WriteLine($"[TRACE] No se pudo obtener el device completo para ID {dev.Id}");
                    continue;
                }
                if (fullDevice.Sensors != null)
                {
                    
                    var sensor = fullDevice.Sensors.FirstOrDefault(s => string.Equals(s.Name, dataset.SensorName, StringComparison.OrdinalIgnoreCase));
                    if (sensor != null)
                    {
                        //Console.WriteLine($"[TRACE] Sensor encontrado: {sensor.Name}, Tipo: {sensor.Type}");
                        return Ok(sensor.Type ?? "unknown");
                    }
                }
            }
        }
        return NotFound($"No se encontró el sensor '{dataset.SensorName}' en ningún device del Source.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] {ex.Message}");
        return StatusCode(500, $"Error interno al obtener el tipo del sensor: {ex.Message}");
    }
}
}
