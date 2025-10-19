using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmniMonitor.Server.Context;
using OmniMonitor.Server.Services;
using OmniMonitor.Shared.Dtos;

[ApiController]
[Route("api/[controller]")]
public class KPIController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ISondaAuthService _sondaAuthService;
    private readonly IKpiService _kpiService;
    private readonly ISondaIMService _sondaIMService;
    public KPIController(ApplicationDbContext context, ISondaAuthService sondaAuthService, IKpiService kpiService, ISondaIMService sondaIMService)
    {
        _sondaAuthService = sondaAuthService;
        _context = context;
        _kpiService = kpiService;
        _sondaIMService = sondaIMService;
    }

    [HttpPost("")]
    [ProducesResponseType(typeof(Kpi), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<Kpi>> CreateKpi([FromBody] KpiRequest request, [FromQuery] string token)
    {
        try
        {
            if (request == null)
                return BadRequest("El objeto KPI es nulo.");

            // Validar token y obtener usuario
            var (username, _) = await _sondaAuthService.GetUserByTokenOMAsync(token);
            if (string.IsNullOrEmpty(username))
                return BadRequest("Token inválido.");

            // Crear KPI usando el servicio, pasándole el username
            var newKpi = await _kpiService.CreateKpiAsync(request, username);

            return Ok(newKpi);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }




    //Obtener kpi por id
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(KpiResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<KpiResponse>> GetKpiById(int id, [FromQuery] string token)
    {
        try
        {
            // Validar token y obtener usuario
            var (username, password) = await _sondaAuthService.GetUserByTokenOMAsync(token);
            if (string.IsNullOrEmpty(username))
                return BadRequest("Token inválido.");

            // Buscar KPI en la base de datos
            var kpi = await _kpiService.CalculateKpiValueAsync(id, username, password);
            if (kpi == null)
                return NotFound($"No se encontró el KPI con ID {id} para el usuario {username}.");

            return Ok(kpi);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }

    //Obtener todos los kpis

    // Obtener todos los KPIs del usuario
    [HttpGet("kpis")]
    [ProducesResponseType(typeof(List<KpiResponse>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<List<KpiResponse>>> GetAllKpis([FromQuery] string token)
    {
        try
        {
            var (username, password) = await _sondaAuthService.GetUserByTokenOMAsync(token);
            if (string.IsNullOrEmpty(username))
                return BadRequest("Token inválido.");

            var kpis = await _kpiService.CalculateAllKpisForUserAsync(username, password);
            return Ok(kpis);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }




    // ⚙️ Endpoint temporal para probar GetDeviceDataByDate
    [HttpGet("testDates")]
    [ProducesResponseType(typeof(List<DeviceData>), 200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<List<DeviceData>>> TestGetDeviceDataByDate()
    {
        try
        {
            // 🔧 Datos de prueba (ajustá según tus datos reales)
            string username = "admin";
            string password = "admin";
            int deviceId = 52726;

            DateTime dateFrom = DateTime.UtcNow.AddDays(-2); // hace 2 día
            DateTime dateTo = DateTime.UtcNow;               // ahora

            var data = await _sondaIMService.GetDeviceDataByDate(deviceId, dateFrom, dateTo, username, password);

            if (data == null || data.Count == 0)
                return Ok("No se encontraron datos para el rango de fechas.");

            return Ok(data);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }



    [HttpGet("test")]
    public ActionResult<Kpi> GetTestKpi()
    {
        var kpi = new Kpi
        {
            Id = 1,
            Name = "Temperature Sensor",
            Description = "Average temperature of last hour",
            SourceModule = "UM",
            DatasetId = 101,
            Unit = "°C",
            Metric = "Average",
            Multiplier = 1.0,
            DefaultColor = "#00FF00"
        };

        return Ok(kpi);
    }

    [HttpGet("test-response")]
    public ActionResult<KpiResponse> GetTestKpiResponse()
    {
        var response = new KpiResponse
        {
            Name = "Temperature Sensor",
            Description = "Average temperature of last hour",
            Type = "float",
            Value = 23.7,
            ActualColor = "#00FF00"
        };

        return Ok(response);
    }
}







