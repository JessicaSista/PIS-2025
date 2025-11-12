using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;
using OmniMonitor.Shared.Dtos;

namespace OmniMonitor.Server.Controllers
{
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

            Report createdReport = await _reportService.CreateReportAsync(request);
            return CreatedAtAction(nameof(GetReportById), new { id = createdReport.Id }, createdReport);
        }

        [HttpPost("{reportId}/joins/create-and-add")]
        [ProducesResponseType(typeof(ReportJoin), 200)]
        [ProducesResponseType(404)] // Not Found
        public async Task<IActionResult> CreateAndAddJoinToReport(int reportId, [FromBody] CreateJoinRequestDto joinRequest, [FromQuery] string token)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                string username = await _sondaAuthService.GetUserByTokenOMAsync(token);
                ReportJoin createdLink = await _reportService.CreateAndAddJoinToReportAsync(reportId, joinRequest, username);

                return Ok(createdLink);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Gets a list of all reports for a specific user.
        /// </summary>
        [HttpGet("by-user")]
        [ProducesResponseType(typeof(List<Report>), 200)]
        public async Task<IActionResult> GetAllReportsByUsername([FromQuery] string token)
        {
            string username = await _sondaAuthService.GetUserByTokenOMAsync(token);
            List<Report> reports = await _reportService.GetAllReportsByUsernameAsync(username);
            return Ok(reports);
        }

        [HttpGet("GetAllReportsPaginated")]
        public async Task<ActionResult<object>> GetAllReportsPaginated(string token, int page = 1, int pageSize = 10, string? query = null)
        {
            string username = await _sondaAuthService.GetUserByTokenOMAsync(token);
            if (string.IsNullOrWhiteSpace(username))
                return BadRequest("Token inválido o usuario no encontrado.");

            if (page <= 0 || pageSize <= 0)
                return BadRequest("La página y el tamaño deben ser mayores a 0.");

            var reports = await _reportService.GetAllReportsPaginatedAsync(username, page, pageSize, query);
            var totalCount = await _reportService.GetReportsCountAsync(username, query);
            int totalPages = (int)System.Math.Ceiling(totalCount / (double)pageSize);

            return Ok(new {
                Items = reports,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasPreviousPage = page > 1,
                HasNextPage = page < totalPages
            });
        }

        /// <summary>
        /// Gets a single, detailed report by its ID.
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Report), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetReportById(int id, [FromQuery] string token)
        {
            string username = await _sondaAuthService.GetUserByTokenOMAsync(token);
            Report? report = await _reportService.GetReportByIdAsync(id, username);
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

            string username = await _sondaAuthService.GetUserByTokenOMAsync(token);
            CrossModuleJoin createdJoin = await _joinConfigService.CreateJoinAsync(request, username);
            return Ok(createdJoin);
        }

        /// <summary>
        /// Gets a list of all join configurations for a specific user.
        /// </summary>
        [HttpGet("joins/by-user")]
        [ProducesResponseType(typeof(List<CrossModuleJoinDto>), 200)]
        public async Task<IActionResult> GetJoinsByUsername([FromQuery] string token)
        {
            string username = await _sondaAuthService.GetUserByTokenOMAsync(token);
            List<CrossModuleJoinDto> joins = await _joinConfigService.GetJoinsByUsernameAsync(username);
            return Ok(joins);
        }

        [HttpGet("joins/{joinId}/execute")]
        [ProducesResponseType(typeof(List<dynamic>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ExecuteJoin(int joinId)
        {
            try
            {
                List<dynamic> results = await _joinConfigService.ExecuteJoinAsync(joinId);
                return Ok(results);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception)
            {
                // Log exception ex
                return StatusCode(500, "An error occurred while executing the join.");
            }
        }

        [HttpPost("joins/{joinId}/executefiltered")]
        [ProducesResponseType(typeof(List<dynamic>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ExecuteJoinWithFilters(int joinId, [FromBody] JoinFiltersConfig? filters = null)
        {
            try
            {
                List<dynamic> results = await _joinConfigService.ExecuteJoinWithFiltersAsync(joinId, filters);
                return Ok(results);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception)
            {
                // Log exception ex
                return StatusCode(500, "An error occurred while executing the join.");
            }
        }

        [HttpPut("UpdateReport")]
        [ProducesResponseType(typeof(Report), 200)]
        [ProducesResponseType(404)] // Not Found
        [ProducesResponseType(401)] // Unauthorized
        public async Task<IActionResult> UpdateReport(int id, [FromBody] UpdateReportRequestDto updateRequest, [FromQuery] string token)
        {
            try
            {
                string username = await _sondaAuthService.GetUserByTokenOMAsync(token);
                if (string.IsNullOrWhiteSpace(username))
                {
                    return Unauthorized(new { message = "Token inválido." });
                }

                // Serializar filtros a JSON si existen
                string? jsonFilters = null;
                if (updateRequest.Filters != null)
                {
                    var serializerOptions = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        Converters = { new JsonStringEnumConverter() }
                    };
                    jsonFilters = JsonSerializer.Serialize(updateRequest.Filters, serializerOptions);
                }

                Report? updatedReport = await _reportService.UpdateReportWithFiltersAsync(id, updateRequest.Name, updateRequest.Description, username, updateRequest.JSON_config, jsonFilters);

                if (updatedReport == null)
                {
                    return NotFound($"El reporte con ID {id} no fue encontrado para este usuario.");
                }

                return Ok(updatedReport);
            }
            catch (Exception)
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
                string username = await _sondaAuthService.GetUserByTokenOMAsync(token);
                if (string.IsNullOrWhiteSpace(username))
                {
                    return Unauthorized(new { message = "Token inválido." });
                }

                bool success = await _reportService.DeleteReportAsync(id, username);

                if (!success)
                {
                    return NotFound($"El reporte con ID {id} no fue encontrado para este usuario.");
                }

                // Retorna un 204 No Content, que es el estándar para un DELETE exitoso.
                return NoContent();
            }
            catch (Exception)
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
                string username = await _sondaAuthService.GetUserByTokenOMAsync(token);
                if (string.IsNullOrWhiteSpace(username))
                {
                    return Unauthorized(new { message = "Token inválido." });
                }

                bool success = await _reportService.RemoveJoinFromReportAsync(reportId, joinId, username);

                if (!success)
                {
                    return NotFound(new { message = $"No se encontró la asociación del Join con ID {joinId} en el Reporte con ID {reportId} para este usuario." });
                }

                return NoContent();
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Ocurrió un error interno al intentar quitar el join del reporte." });
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
                string username = await _sondaAuthService.GetUserByTokenOMAsync(token);
                if (string.IsNullOrWhiteSpace(username))
                {
                    return Unauthorized(new { message = "Token inválido." });
                }

                List<dynamic> results = await _reportService.ExecuteReportAsync(id, username);
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
}