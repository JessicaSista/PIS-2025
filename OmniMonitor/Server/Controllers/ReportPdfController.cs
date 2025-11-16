using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniMonitor.Server.Services;

namespace OmniMonitor.Server.Controllers
{
    [ApiController]
    [Route("api/reports")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ReportPdfController : ControllerBase
    {
        private readonly IReportService _reportService;
        private readonly ILogger<ReportPdfController> _logger;

        public ReportPdfController(
            IReportService reportService,
            ILogger<ReportPdfController> logger)
        {
            _reportService = reportService;
            _logger = logger;
        }

        /// <summary>
        /// Descarga el reporte como PDF
        /// </summary>
        /// <param name="reportId">ID del reporte</param>
        /// <returns>Archivo PDF para descarga</returns>
        [RequirePermission("Reports.Export")]
        [HttpGet("{reportId}/download-pdf")]
        [ProducesResponseType(typeof(FileResult), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> DownloadReportPdf(int reportId)
        {
            try
            {
                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                {
                    _logger.LogWarning($"Usuario no autenticado intentando descargar PDF del reporte {reportId}");
                    return Unauthorized("Usuario no autenticado");
                }

                _logger.LogInformation($"Iniciando descarga de PDF para reporte {reportId}, usuario {username}");

                // Verificar que el reporte existe y pertenece al usuario
                var reportExists = await _reportService.GetReportByIdAsync(reportId, username);
                if (reportExists == null)
                {
                    _logger.LogWarning($"Reporte {reportId} no encontrado para usuario {username}");
                    return NotFound($"Reporte {reportId} no encontrado");
                }

                // Generar el "PDF" (HTML)
                var pdfBytes = await _reportService.GenerateReportPdfAsync(reportId, username);
                
                var fileName = GenerateFileName(reportExists.Name, reportId, "pdf");
                
                _logger.LogInformation($"Reporte PDF generado exitosamente para reporte {reportId}. Tamaño: {pdfBytes.Length} bytes");

                return File(
                    fileContents: pdfBytes,
                    contentType: "application/pdf",
                    fileDownloadName: fileName
                );
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, $"Reporte no encontrado: {reportId}");
                return NotFound($"Reporte {reportId} no encontrado: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, $"Error de operación generando PDF para reporte {reportId}");
                return BadRequest($"Error procesando reporte: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error inesperado generando PDF para reporte {reportId}");
                return StatusCode(500, $"Error interno generando PDF: {ex.Message}");
            }
        }

        /// <summary>
        /// Abre el reporte como PDF en el navegador (vista previa)
        /// </summary>
        /// <param name="reportId">ID del reporte</param>
        /// <returns>Archivo PDF para vista previa</returns>
        [RequirePermission("Reports.View")]
        [HttpGet("{reportId}/preview-pdf")]
        [ProducesResponseType(typeof(FileResult), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> PreviewReportPdf(int reportId)
        {
            try
            {
                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                {
                    _logger.LogWarning($"Usuario no autenticado intentando previsualizar PDF del reporte {reportId}");
                    return Unauthorized("Usuario no autenticado");
                }

                _logger.LogInformation($"Iniciando vista previa de PDF para reporte {reportId}, usuario {username}");

                // Verificar que el reporte existe y pertenece al usuario
                var reportExists = await _reportService.GetReportByIdAsync(reportId, username);
                if (reportExists == null)
                {
                    _logger.LogWarning($"Reporte {reportId} no encontrado para usuario {username}");
                    return NotFound($"Reporte {reportId} no encontrado");
                }

                // Generar el "PDF" (HTML)
                var pdfBytes = await _reportService.GenerateReportPdfAsync(reportId, username);

                _logger.LogInformation($"Reporte PDF generado exitosamente para vista previa del reporte {reportId}");

                // Para vista previa - PDF que se muestra en el navegador
                return File(
                    fileContents: pdfBytes,
                    contentType: "application/pdf"
                );
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, $"Reporte no encontrado: {reportId}");
                return NotFound($"Reporte {reportId} no encontrado: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, $"Error de operación generando PDF para vista previa del reporte {reportId}");
                return BadRequest($"Error procesando reporte: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error inesperado generando PDF para vista previa del reporte {reportId}");
                return StatusCode(500, $"Error interno generando PDF: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene información básica del reporte antes de generar PDF
        /// </summary>
        /// <param name="reportId">ID del reporte</param>
        /// <returns>Información del reporte</returns>
        [RequirePermission("Reports.View")]
        [HttpGet("{reportId}/pdf-info")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetReportPdfInfo(int reportId)
        {
            try
            {
                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized("Usuario no autenticado");

                var report = await _reportService.GetReportByIdAsync(reportId, username);
                if (report == null)
                    return NotFound($"Reporte {reportId} no encontrado");

                // Obtener datos para contar registros sin generar PDF completo
                var reportData = await _reportService.ExecuteReportAsync(reportId, username);
                
                return Ok(new
                {
                    reportId = report.Id,
                    name = report.Name,
                    description = report.Description,
                    recordCount = reportData?.Count ?? 0,
                    estimatedFileSize = EstimateFileSize(reportData?.Count ?? 0),
                    lastModified = DateTime.Now // Podrías agregar este campo a tu modelo Report
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error obteniendo información del reporte {reportId}");
                return BadRequest($"Error obteniendo información: {ex.Message}");
            }
        }

        private string GenerateFileName(string reportName, int reportId, string extension = "pdf")
        {
            // Limpiar caracteres no válidos para nombres de archivo
            var cleanName = string.Join("_", reportName.Split(Path.GetInvalidFileNameChars()));
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return $"{cleanName}_{reportId}_{timestamp}.{extension}";
        }

        private string EstimateFileSize(int recordCount)
        {
            // Estimación aproximada basada en número de registros
            var estimatedBytes = 50000 + (recordCount * 100); // Base + 100 bytes por registro
            
            if (estimatedBytes < 1024 * 1024) // < 1MB
                return $"{estimatedBytes / 1024:F0} KB";
            else
                return $"{estimatedBytes / (1024 * 1024):F1} MB";
        }
    }
}