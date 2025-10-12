using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq.Dynamic.Core.Tokenizer;
using System.Threading.Tasks;

[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly IJoinConfigurationService _joinConfigService;
    private readonly ISondaAuthService _sondaAuthService;

    public ReportsController(IReportService reportService, IJoinConfigurationService joinConfigService, ISondaAuthService sondaAuthService)
    {
        _reportService = reportService;
        _joinConfigService = joinConfigService;
        _sondaAuthService = sondaAuthService;
    }

    // ===============================================
    // Report-related Endpoints
    // ===============================================

    /// <summary>
    /// Creates a new report with a specified list of joins.
    /// Route: POST /api/reports
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Report), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateReport([FromBody] CreateReportRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        var createdReport = await _reportService.CreateReportAsync(request);
        return CreatedAtAction(nameof(GetReportById), new { id = createdReport.Id }, createdReport);
    }

    /// <summary>
    /// Gets a list of all reports for a specific user.
    /// Route: GET /api/reports/user/{username}
    /// </summary>
    [HttpGet("user/{username}")]
    [ProducesResponseType(typeof(List<Report>), 200)]
    public async Task<IActionResult> GetAllReportsByUsername(string username)
    {
        var reports = await _reportService.GetAllReportsByUsernameAsync(username);
        return Ok(reports);
    }

    /// <summary>
    /// Gets a single, detailed report by its ID.
    /// Route: GET /api/reports/{id}
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Report), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetReportById(int id, [FromQuery] string token)
    {
        var (username, password) = await _sondaAuthService.GetUserByTokenOMAsync(token);
        var report = await _reportService.GetReportByIdAsync(id, username);
        if (report == null)
        {
            return NotFound();
        }
        return Ok(report);
    }

    // ===============================================
    // Join-related Endpoints
    // ===============================================

    /// <summary>
    /// Creates a new join configuration.
    /// Route: POST /api/reports/joins
    /// </summary>
    [HttpPost("joins")]
    [ProducesResponseType(typeof(CrossModuleJoin), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateJoin([FromBody] CreateJoinRequestDto request, [FromQuery] string token)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        var (username, password) = await _sondaAuthService.GetUserByTokenOMAsync(token);
        var createdJoin = await _joinConfigService.CreateJoinAsync(request, username);
        return Ok(createdJoin);
    }

    /// <summary>
    /// Gets a list of all join configurations for a specific user.
    /// Route: GET /api/reports/joins/user/{username}
    /// </summary>
    [HttpGet("joins/user/{username}")]
    [ProducesResponseType(typeof(List<CrossModuleJoinDto>), 200)]
    public async Task<IActionResult> GetJoinsByUsername(string username)
    {
        var joins = await _joinConfigService.GetJoinsByUsernameAsync(username);
        return Ok(joins);
    }


    [HttpGet("joins/{joinId}/execute")]
    [ProducesResponseType(typeof(List<dynamic>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ExecuteJoin(int joinId)
    {
        try
        {
            var results = await _joinConfigService.ExecuteJoinAsync(joinId);
            return Ok(results);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (System.Exception ex)
        {
            // Log exception ex
            return StatusCode(500, "An error occurred while executing the join.");
        }
    }
}