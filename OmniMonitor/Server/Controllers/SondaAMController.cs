using Microsoft.AspNetCore.Mvc;
using OmniMonitor.Shared.Dtos;
using OmniMonitor.Shared.Dtos.AM;
using System;
using System.Threading.Tasks;


[ApiController]
[Route("api/[controller]")]
public class SondaAMController : ControllerBase
{
    private readonly ISondaAMService _sondaAMService;

    public SondaAMController(ISondaAMService sondaAMService)
    {
        _sondaAMService = sondaAMService;
    }
    
    
    [HttpGet("asset/assetsBasicData")]
    [ProducesResponseType(typeof(List<AssetDto>), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<List<AssetDto>>> GetAssetsBasicData(
        [FromQuery] int? page,
        [FromQuery] string? queryString,
        [FromQuery] int? pageSize,
        [FromQuery] int? bundleId,
        [FromQuery] string user,
        [FromQuery] string pass)
    {
        try
        {
            var assets = await _sondaAMService.GetAssetsBasicData(page, queryString, pageSize, bundleId, user, pass);
            if (assets == null || assets.Count == 0) return NotFound("No se encontraron assets básicos.");
            return Ok(assets);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }
    

    [HttpGet("asset/assets")]
    [ProducesResponseType(typeof(List<AssetDto>), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<List<AssetDto>>> GetAssets(
        [FromQuery] int? page,
        [FromQuery] string? queryString,
        [FromQuery] string? bundles,
        [FromQuery] int? assetTypeId,
        [FromQuery] string? sort,
        [FromQuery] int? pageSize,
        [FromQuery] string user,
        [FromQuery] string pass)
    {
        try
        {
            var assets = await _sondaAMService.GetAssets(page, queryString, bundles, assetTypeId, sort, pageSize, user, pass);
            if (assets == null || assets.Count == 0) return NotFound("No se encontraron assets.");
            return Ok(assets);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }

    [HttpGet("stock/{stockId}")]
    [ProducesResponseType(typeof(StockDto), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<StockDto>> GetStockById(int stockId, [FromQuery] string user, [FromQuery] string pass)
    {
        try
        {
            var stock = await _sondaAMService.GetStockById(stockId, user, pass);
            if (stock == null) return NotFound($"No se encontró el stock {stockId}");
            return Ok(stock);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }

    [HttpGet("bundle")]
    [ProducesResponseType(typeof(List<BundleDto>), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
        public async Task<ActionResult<BundleDto>> GetStockParametersByBundleId([FromQuery] int bundleId, [FromQuery] string user, [FromQuery] string pass)
    {
        try
        {
                var bundle = await _sondaAMService.GetStockParametersByBundleId(bundleId, user, pass);
                if (bundle == null) return NotFound("No se encontró bundle para ese bundleId.");
                return Ok(bundle);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }

    // Ejemplo: Obtener un asset por ID
    [HttpGet("asset/{id}")]
    [ProducesResponseType(typeof(AssetDto), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<AssetDto>> GetAssetById(int id, string user, string pass)
    {
        try
        {
            var asset = await _sondaAMService.GetAssetById(id, user, pass);
            if (asset == null) return NotFound($"No se encontró el asset {id}");
            return Ok(asset);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }

    [HttpGet("stock")]
    [ProducesResponseType(typeof(List<StockDto>), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<List<StockDto>>> GetAllStock(
        [FromQuery] int? page,
        [FromQuery] string? queryString,
        [FromQuery] string? sort,
        [FromQuery] int? pageSize,
        [FromQuery] string? bundlesId,
        [FromQuery] string user,
        [FromQuery] string pass)
    {
        try
        {
            var stocks = await _sondaAMService.GetAllStock(page, queryString, sort, pageSize, bundlesId, user, pass);
            if (stocks == null || stocks.Count == 0) return NotFound("No se encontraron stocks.");
            return Ok(stocks);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }

    
    [HttpGet("relation/asset/{assetId}")]
    [ProducesResponseType(typeof(List<RelatedAssetDto>), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<List<RelatedAssetDto>>> GetAssetRelations(
        int assetId,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string user,
        [FromQuery] string pass)
    {
        try
        {
            var assets = await _sondaAMService.GetAssetRelations(assetId, page, pageSize, user, pass);
            if (assets == null || assets.Count == 0) return NotFound("No se encontraron relaciones para ese asset.");
            return Ok(assets);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }

    [HttpGet("bundle/bundles")]
    [ProducesResponseType(typeof(List<BundleDto>), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<List<BundleDto>>> GetBundles(
        [FromQuery] int? page,
        [FromQuery] string? queryString,
        [FromQuery] string? sort,
        [FromQuery] int? pageSize,
        [FromQuery] string user,
        [FromQuery] string pass)
    {
        try
        {
            var bundles = await _sondaAMService.GetBundles(page, queryString, sort, pageSize, user, pass);
            if (bundles == null || bundles.Count == 0) return NotFound("No se encontraron bundles.");
            return Ok(bundles);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }

    /*    [HttpGet("asset/history")]
    [ProducesResponseType(typeof(List<AssetDto>), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<List<AssetDto>>> GetAssetHistory(
        [FromQuery] int? page,
        [FromQuery] string? queryString,
        [FromQuery] string? sort,
        [FromQuery] int? pageSize,
        [FromQuery] string? bundlesId,
        [FromQuery] string user,
        [FromQuery] string pass)
    {
        try
        {
            var assets = await _sondaAMService.GetAssetHistory(page, queryString, sort, pageSize, bundlesId, user, pass);
            if (assets == null || assets.Count == 0) return NotFound("No se encontraron historial de assets.");
            return Ok(assets);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }*/

    [HttpGet("eventTaskInstance/{eventTaskInstanceId}")]
    [ProducesResponseType(typeof(EventTaskInstanceDto), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<EventTaskInstanceDto>> GetEventTaskInstanceById(int eventTaskInstanceId, [FromQuery] string username, [FromQuery] string password)
    {
        try
        {
            var result = await _sondaAMService.GetEventTaskInstanceById(eventTaskInstanceId, username, password);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
    [HttpGet("eventTaskInstances")]
    [ProducesResponseType(typeof(List<EventTaskInstanceDto>), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<List<EventTaskInstanceDto>>> GetEventTasks(
        [FromQuery] string dates,
        [FromQuery] int? page,
        [FromQuery] string? queryString,
        [FromQuery] int? bundleId,
        [FromQuery] string? state,
        [FromQuery] string? sort,
        [FromQuery] int? taskTypeId,
        [FromQuery] int? groupId,
        [FromQuery] int? pageSize,
        [FromQuery] string username,
        [FromQuery] string password,
        [FromQuery] bool tasksAssignedToMe = false,
        [FromQuery] bool tasksPendingApproval = false)
    {
        try
        {
            var result = await _sondaAMService.GetEventTaskInstances(dates, page, queryString, bundleId, state, sort, taskTypeId, groupId, pageSize, tasksAssignedToMe, tasksPendingApproval, username, password);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

        [HttpGet("eventTaskInstance/actions/{taskInstanceId}")]
    [ProducesResponseType(typeof(List<EventTaskActionDto>), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<List<EventTaskActionDto>>> GetEventTaskInstanceActions(int taskInstanceId, [FromQuery] string username, [FromQuery] string password)
    {
        try
        {
            var actions = await _sondaAMService.GetEventTaskInstanceActions(taskInstanceId, username, password);
            if (actions == null || actions.Count == 0) return NotFound("No se encontraron acciones para esa instancia de tarea.");
            return Ok(actions);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }

    [HttpGet("eventTaskInstance/stock/{taskInstanceId}")]
    [ProducesResponseType(typeof(List<EventTaskInstanceStockDto>), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<List<EventTaskInstanceStockDto>>> GetEventTaskInstanceStock(int taskInstanceId, [FromQuery] string username, [FromQuery] string password)
    {
        try
        {
            var stocks = await _sondaAMService.GetEventTaskInstanceStock(taskInstanceId, username, password);
            if (stocks == null || stocks.Count == 0) return NotFound("No se encontró stock para esa instancia de tarea.");
            return Ok(stocks);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }

    /// <summary>
    /// Obtiene una lista de TaskTypeDto únicos de todas las instancias de tareas para el usuario y password dados.
    /// </summary>
    [HttpGet("typeDtos")]
    public async Task<ActionResult<List<TaskTypeDto>>> GetTypeDtos([FromQuery] string username, [FromQuery] string password)
    {
        var typeDtos = await _sondaAMService.GetTaskTypeDtosFromEventTaskInstances(username, password);
        return Ok(typeDtos);
    }

    [HttpGet("asset/types")]
    [ProducesResponseType(typeof(List<AssetTypeDto>), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<List<AssetTypeDto>>> GetAllAssetTypes([FromQuery] string username, [FromQuery] string password)
    {
        try
        {
            var types = await _sondaAMService.GetAllAssetTypes(username, password);
            if (types == null || types.Count == 0) return NotFound("No se encontraron tipos de asset.");
            return Ok(types);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }

}
