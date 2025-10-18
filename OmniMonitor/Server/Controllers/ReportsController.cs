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

    [HttpPost("{reportId}/joins")]
    [ProducesResponseType(typeof(ReportJoin), 200)]
    [ProducesResponseType(404)] // Not Found
    public async Task<IActionResult> AddJoinToReport(int reportId, [FromBody] ReportJoinItemDto joinRequest, [FromQuery] string token)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var (username, password) = await _sondaAuthService.GetUserByTokenOMAsync(token);
            var createdLink = await _reportService.AddJoinToReportAsync(reportId, joinRequest, username);
            return Ok(createdLink);
        }
        catch (KeyNotFoundException ex)
        {
            // Esto se activará si el reporte o el join no existen.
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Gets a list of all reports for a specific user.
    /// Route: GET /api/reports/user/{username}
    /// </summary>
    [HttpGet("by-user")]
    [ProducesResponseType(typeof(List<Report>), 200)]
    public async Task<IActionResult> GetAllReportsByUsername([FromQuery] string token)
    {
        var (username, password) = await _sondaAuthService.GetUserByTokenOMAsync(token);
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
    [HttpGet("joins/by-user")]
    [ProducesResponseType(typeof(List<CrossModuleJoinDto>), 200)]
    public async Task<IActionResult> GetJoinsByUsername([FromQuery] string token)
    {
        var (username, password) = await _sondaAuthService.GetUserByTokenOMAsync(token);
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

    [HttpPut("UpdateReport")]
    [ProducesResponseType(typeof(Report), 200)]
    [ProducesResponseType(404)] // Not Found
    [ProducesResponseType(401)] // Unauthorized
    public async Task<IActionResult> UpdateReport(int id, string name, string description, string JSON_config, [FromQuery] string token)
    {
        try
        {
            var (username, password) = await _sondaAuthService.GetUserByTokenOMAsync(token);
            if (string.IsNullOrWhiteSpace(username))
            {
                return Unauthorized(new { message = "Token inválido." });
            }

            var updatedReport = await _reportService.UpdateReportAsync(id, name, description, username, JSON_config);

            if (updatedReport == null)
            {
                return NotFound($"El reporte con ID {id} no fue encontrado para este usuario.");
            }
            return Ok(updatedReport);
        }
        catch (Exception ex)
        {
            // Opcional: Loggear la excepción 'ex'
            return StatusCode(500, new { message = "Ocurrió un error interno al actualizar el reporte." });
        }
    }


    [HttpDelete("DeleteReport")]
    [ProducesResponseType(204)] // No Content (éxito)
    [ProducesResponseType(404)] // Not Found
    [ProducesResponseType(401)] // Unauthorized
    public async Task<IActionResult> DeleteReport(int id, [FromQuery] string token)
    {
        try
        {
            var (username, password) = await _sondaAuthService.GetUserByTokenOMAsync(token);
            if (string.IsNullOrWhiteSpace(username))
            {
                return Unauthorized(new { message = "Token inválido." });
            }

            var success = await _reportService.DeleteReportAsync(id, username);

            if (!success)
            {
                return NotFound($"El reporte con ID {id} no fue encontrado para este usuario.");
            }

            // Retorna un 204 No Content, que es el estándar para un DELETE exitoso.
            return NoContent();
        }
        catch (Exception ex)
        {
            // Opcional: Loggear la excepción 'ex'
            return StatusCode(500, new { message = "Ocurrió un error interno al eliminar el reporte." });
        }
    }


    [HttpDelete("RemoveJoinFromReport")]
    [ProducesResponseType(204)] // No Content (éxito)
    [ProducesResponseType(404)] // Not Found
    [ProducesResponseType(401)] // Unauthorized
    public async Task<IActionResult> RemoveJoinFromReport(int reportId, int joinId, [FromQuery] string token)
    {
        try
        {
            var (username, password) = await _sondaAuthService.GetUserByTokenOMAsync(token);
            if (string.IsNullOrWhiteSpace(username))
            {
                return Unauthorized(new { message = "Token inválido." });
            }

            var success = await _reportService.RemoveJoinFromReportAsync(reportId, joinId, username);

            if (!success)
            {
                return NotFound(new { message = $"No se encontró la asociación del Join con ID {joinId} en el Reporte con ID {reportId} para este usuario." });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            // Opcional: Loggear la excepción 'ex'
            return StatusCode(500, new { message = "Ocurrió un error interno al intentar quitar el join del reporte." });
        }
    }

    [HttpPost("{reportId}/datasets")]
    [ProducesResponseType(typeof(DatasetReports), 200)]
    [ProducesResponseType(404)] // Not Found
    [ProducesResponseType(401)] // Unauthorized
    public async Task<IActionResult> AddDatasetToReport(int reportId, ModuleType moduleType, int id_dataset, [FromQuery] string token)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var (username, password) = await _sondaAuthService.GetUserByTokenOMAsync(token);
            if (string.IsNullOrWhiteSpace(username))
            {
                return Unauthorized(new { message = "Token inválido." });
            }

            var createdLink = await _reportService.AddDatasetToReportAsync(reportId, moduleType, id_dataset, username);

            return Ok(createdLink);
        }
        catch (KeyNotFoundException ex)
        {
            // Esto se activará si el reporte no existe para el usuario.
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            // Opcional: Loggear la excepción 'ex'
            return StatusCode(500, new { message = "Ocurrió un error interno al añadir el dataset al reporte." });
        }
    }

    [HttpDelete("{reportId}/datasets")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)] 
    [ProducesResponseType(401)] 
    public async Task<IActionResult> RemoveDatasetFromReport(int reportId, ModuleType moduleType, int id_dataset, [FromQuery] string token)
    {
        try
        {
            var (username, password) = await _sondaAuthService.GetUserByTokenOMAsync(token);
            if (string.IsNullOrWhiteSpace(username))
            {
                return Unauthorized(new { message = "Token inválido." });
            }

            var success = await _reportService.RemoveDatasetFromReportAsync(reportId, moduleType, id_dataset, username);

            if (!success)
            {
                return NotFound(new { message = $"No se encontró la asociación del dataset (Módulo: {moduleType}, ID: {id_dataset}) en el Reporte con ID {reportId} para este usuario." });
            }

            // Retorna un 204 No Content, que es el estándar para un DELETE exitoso.
            return NoContent();
        }
        catch (Exception ex)
        {
            // Opcional: Loggear la excepción 'ex'
            return StatusCode(500, new { message = "Ocurrió un error interno al intentar quitar el dataset del reporte." });
        }
    }

    [HttpGet("{id}/execute")]
    [ProducesResponseType(typeof(List<dynamic>), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> ExecuteReport(int id, [FromQuery] string token)
    {
        try
        {
            var (username, password) = await _sondaAuthService.GetUserByTokenOMAsync(token);
            if (string.IsNullOrWhiteSpace(username))
            {
                return Unauthorized(new { message = "Token inválido." });
            }

            var results = await _reportService.ExecuteReportAsync(id, username);
            return Ok(results);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Ocurrió un error interno al ejecutar el reporte.", details = ex.Message });
        }
    }
}