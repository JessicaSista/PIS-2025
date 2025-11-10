using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using OmniMonitor.Shared.Dtos;
using OmniMonitor.Shared.Dtos.AM;

namespace OmniMonitor.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class SondaAMController : ControllerBase
    {
        private readonly ISondaAMService _sondaAMService;
        private readonly ISondaAuthService _sondaAuthService;

        public SondaAMController(ISondaAMService sondaAMService, ISondaAuthService sondaAuthService)
        {
            _sondaAMService = sondaAMService;
            _sondaAuthService = sondaAuthService;
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("asset/assetsBasicData")]
        [ProducesResponseType(typeof(List<AssetDto>), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<AssetDto>>> GetAssetsBasicData(
            [FromQuery] int? page,
            [FromQuery] string? queryString,
            [FromQuery] int? pageSize,
            [FromQuery] int? bundleId)
        {
            try
            {
                var username = User.Identity?.Name;
                List<AssetDto> assets = await _sondaAMService.GetAssetsBasicData(page, queryString, pageSize, bundleId, username);
                if (assets == null || assets.Count == 0)
                {
                    return NotFound("No se encontraron assets básicos.");
                }

                return Ok(assets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
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
            [FromQuery] int? pageSize)
        {
            try
            {
                var username = User.Identity?.Name;
                List<AssetDto> assets = await _sondaAMService.GetAssets(page, queryString, bundles, assetTypeId, sort, pageSize, username);
                if (assets == null || assets.Count == 0)
                {
                    return NotFound("No se encontraron assets.");
                }

                return Ok(assets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("stock/{stockId}")]
        [ProducesResponseType(typeof(StockDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<StockDto>> GetStockById(int stockId)
        {
            try
            {
                var username = User.Identity?.Name;
                StockDto? stock = await _sondaAMService.GetStockById(stockId, username);
                if (stock == null)
                {
                    return NotFound($"No se encontró el stock {stockId}");
                }

                return Ok(stock);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("bundle")]
        [ProducesResponseType(typeof(List<BundleDto>), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<BundleDto>> GetStockParametersByBundleId([FromQuery] int bundleId, [FromQuery] string token)
        {
            try
            {
                var username = User.Identity?.Name;
                BundleDto bundle = await _sondaAMService.GetStockParametersByBundleId(bundleId, username);
                if (bundle == null)
                {
                    return NotFound("No se encontró bundle para ese bundleId.");
                }

                return Ok(bundle);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        // Ejemplo: Obtener un asset por ID
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("asset/{id}")]
        [ProducesResponseType(typeof(AssetDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<AssetDto>> GetAssetById(int id)
        {
            try
            {
                var username = User.Identity?.Name;
                AssetDto? asset = await _sondaAMService.GetAssetById(id, username);
                if (asset == null)
                {
                    return NotFound($"No se encontró el asset {id}");
                }

                return Ok(asset);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("stock")]
        [ProducesResponseType(typeof(List<StockDto>), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<StockDto>>> GetAllStock(
            [FromQuery] int? page,
            [FromQuery] string? queryString,
            [FromQuery] string? sort,
            [FromQuery] int? pageSize,
            [FromQuery] string? bundlesId)
        {
            try
            {
                var username = User.Identity?.Name;
                List<StockDto> stocks = await _sondaAMService.GetAllStock(page, queryString, sort, pageSize, bundlesId, username);
                if (stocks == null || stocks.Count == 0)
                {
                    return NotFound("No se encontraron stocks.");
                }

                return Ok(stocks);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("relation/asset/{assetId}")]
        [ProducesResponseType(typeof(List<RelatedAssetDto>), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<RelatedAssetDto>>> GetAssetRelations(
            int assetId,
            [FromQuery] int? page,
            [FromQuery] int? pageSize)
        {
            try
            {
                var username = User.Identity?.Name;
                List<RelatedAssetDto> assets = await _sondaAMService.GetAssetRelations(assetId, page, pageSize, username);
                if (assets == null || assets.Count == 0)
                {
                    return NotFound("No se encontraron relaciones para ese asset.");
                }

                return Ok(assets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("bundle/bundles")]
        [ProducesResponseType(typeof(List<BundleDto>), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<BundleDto>>> GetBundles(
            [FromQuery] int? page,
            [FromQuery] string? queryString,
            [FromQuery] string? sort,
            [FromQuery] int? pageSize)
        {
            try
            {
                var username = User.Identity?.Name;
                List<BundleDto> bundles = await _sondaAMService.GetBundles(page, queryString, sort, pageSize, username);
                if (bundles == null || bundles.Count == 0)
                {
                    return NotFound("No se encontraron bundles.");
                }

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

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("eventTaskInstance/{eventTaskInstanceId}")]
        [ProducesResponseType(typeof(EventTaskInstanceDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<EventTaskInstanceDto>> GetEventTaskInstanceById(int eventTaskInstanceId)
        {
            try
            {
                var username = User.Identity?.Name;
                EventTaskInstanceDto? result = await _sondaAMService.GetEventTaskInstanceById(eventTaskInstanceId, username);
                if (result == null)
                {
                    return NotFound(new { error = "No se encontraron eventtaskinstances" });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
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
            [FromQuery] bool tasksAssignedToMe = false,
            [FromQuery] bool tasksPendingApproval = false)
        {
            try
            {
                var username = User.Identity?.Name;
                List<EventTaskInstanceDto> result = await _sondaAMService.GetEventTaskInstances(dates, page, queryString!, bundleId, state!, sort!, taskTypeId, groupId, pageSize, tasksAssignedToMe, tasksPendingApproval, username);
                if (result == null || result.Count == 0)
                {
                    return NotFound(new { error = "No se encontraron eventtasks" });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("eventTaskInstance/actions/{taskInstanceId}")]
        [ProducesResponseType(typeof(List<EventTaskActionDto>), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<EventTaskActionDto>>> GetEventTaskInstanceActions(int taskInstanceId)
        {
            try
            {
                var username = User.Identity?.Name;
                List<EventTaskActionDto> actions = await _sondaAMService.GetEventTaskInstanceActions(taskInstanceId, username);
                if (actions == null || actions.Count == 0)
                {
                    return NotFound("No se encontraron acciones para esa instancia de tarea.");
                }

                return Ok(actions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("eventTaskInstance/stock/{taskInstanceId}")]
        [ProducesResponseType(typeof(List<EventTaskInstanceStockDto>), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<EventTaskInstanceStockDto>>> GetEventTaskInstanceStock(int taskInstanceId)
        {
            try
            {
                var username = User.Identity?.Name;
                List<EventTaskInstanceStockDto> stocks = await _sondaAMService.GetEventTaskInstanceStock(taskInstanceId, username);
                if (stocks == null || stocks.Count == 0)
                {
                    return NotFound("No se encontró stock para esa instancia de tarea.");
                }

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
        public async Task<ActionResult<List<TaskTypeDto>>> GetTypeDtos()
        {
            var username = User.Identity?.Name;
            List<TaskTypeDto> typeDtos = await _sondaAMService.GetTaskTypeDtosFromEventTaskInstances(username);
            return Ok(typeDtos);
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("asset/types")]
        [ProducesResponseType(typeof(List<AssetTypeDto>), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<AssetTypeDto>>> GetAllAssetTypes()
        {
            try
            {
                var username = User.Identity?.Name;
                List<AssetTypeDto> types = await _sondaAMService.GetAllAssetTypes(username);
                if (types == null || types.Count == 0)
                {
                    return NotFound("No se encontraron tipos de asset.");
                }

                return Ok(types);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }
    }
}
