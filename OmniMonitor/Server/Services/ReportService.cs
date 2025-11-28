using Microsoft.EntityFrameworkCore;
using OmniMonitor.Server.Context;
using OmniMonitor.Shared.Dtos;
using System.Dynamic;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Colors;
using iText.IO.Font.Constants;

using iTextSharp.text.pdf;

using Microsoft.EntityFrameworkCore;

using OmniMonitor.Server.Context;
using OmniMonitor.Server.Services;
using OmniMonitor.Shared;
using OmniMonitor.Shared.Dtos;

public interface IReportService
{
    Task<Report> CreateReportAsync(CreateReportRequestDto request, string username);
    Task<ReportJoin> CreateAndAddJoinToReportAsync(int reportId, CreateJoinRequestDto joinRequest, string username);
    Task<List<Report>> GetAllReportsByUsernameAsync(string username);
    Task<Report?> GetReportByIdAsync(int reportId, string username);
    Task<bool> DeleteReportAsync(int reportId, string username);
    Task<Report?> UpdateReportAsync(int reportId, string name, string descripcion, string username, string JSON_config);
    Task<Report?> UpdateReportWithFiltersAsync(int reportId, string name, string descripcion, string username, string JSON_config, string? JSON_filters);
    Task<bool> RemoveJoinFromReportAsync(int reportId, int joinId, string username);
    Task<List<dynamic>> ExecuteReportAsync(int reportId, string username);
    Task<List<Report>> GetAllReportsPaginatedAsync(string username, int page = 1, int pageSize = 10, string? query = null);
    Task<int> GetReportsCountAsync(string username, string? query = null);

    // PDF Generation Methods
    Task<byte[]> GenerateReportPdfAsync(int reportId, string username);
    byte[] GenerateReportPdfFromData(List<dynamic> reportData, List<string> columns, string reportTitle);
    string GenerateReportHtml(List<dynamic> reportData, List<string> columns, string reportTitle);

    Task<int> CreateScheduledReportAsync(ScheduledReportRequest dto, string username);

    Task<List<ScheduledReport>> GetScheduledReports();
    Task<List<ScheduledReportResponse>> GetScheduledReportsByUserAsync(string username);
    Task<ScheduledReportResponse?> GetScheduledReportByIdAsync(int id, string username);

    Task DeleteScheduledReportAsync(int id);

    Task<ScheduledReportResponse?> UpdateScheduledReportAsync(int id, ScheduledReportRequest dto, string username);

    Task ProcessScheduledReportsAsync();

}

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _context;
    private readonly IJoinConfigurationService _joinConfigService;
    private readonly IApiDataService _apiDataService;
    private readonly ISondaAuthService _sondaAuthService;
    private readonly ISondaIMService _sondaIMService;
    private readonly ILogger<ReportService> _logger;
    private readonly IMailService _mailService;

    public ReportService(ApplicationDbContext context, IJoinConfigurationService JoinConfigurationService,
        IApiDataService ApiDataService, ISondaAuthService SondaAuthService, ISondaIMService SondaIMService, ILogger<ReportService> logger, IMailService mailService)
    {
        _context = context;
        _joinConfigService = JoinConfigurationService;
        _apiDataService = ApiDataService;
        _sondaAuthService = SondaAuthService;
        _sondaIMService = SondaIMService;
        _logger = logger;
        _mailService = mailService;
    }

    /// <summary>
    /// Creates a new report and returns the complete Report entity.
    /// </summary>
    public async Task<Report> CreateReportAsync(CreateReportRequestDto request, string username)
    {
        var report = new Report
        {
            Name = request.Name,
            Description = request.Description,
            Username = username,
            JSON_config = request.JSON_config,
            JSON_filters = request.JSON_filters
        };
        _context.Reports.Add(report);
        await _context.SaveChangesAsync();

        return report;
    }

    public async Task<ReportJoin> CreateAndAddJoinToReportAsync(int reportId, CreateJoinRequestDto joinRequest, string username)
    {
        var report = await _context.Reports
            .FirstOrDefaultAsync(r => r.Id == reportId && r.Username == username);

        if (report == null)
        {
            throw new KeyNotFoundException($"El reporte con ID {reportId} no fue encontrado para este usuario.");
        }

        var newJoin = await _joinConfigService.CreateJoinAsync(joinRequest, username);

        var newReportJoin = new ReportJoin
        {
            ReportId = report.Id,
            CrossModuleJoinId = newJoin.Id,
            ExecutionOrder = 1
        };

        _context.ReportJoins.Add(newReportJoin);
        await _context.SaveChangesAsync();

        return newReportJoin;
    }

    /// <summary>
    /// Retrieves a list of all Report entities for a user.
    /// </summary>
    public async Task<List<Report>> GetAllReportsByUsernameAsync(string username)
    {
        return await _context.Reports
            .AsNoTracking()
            .Where(r => r.Username == username)
            .ToListAsync();
    }

    public async Task<List<Report>> GetAllReportsPaginatedAsync(string username, int page = 1, int pageSize = 10, string? query = null)
    {
            var reportsQuery = _context.Reports
                .Where(r => r.Username == username);

            if (!string.IsNullOrWhiteSpace(query))
            {
                var loweredQuery = query.ToLowerInvariant();
                reportsQuery = reportsQuery.Where(r =>
                    (r.Name != null && r.Name.ToLower().Contains(loweredQuery)) ||
                    (r.Description != null && r.Description.ToLower().Contains(loweredQuery)));
            }

            return await reportsQuery
                .OrderByDescending(r => r.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
    }

    public async Task<int> GetReportsCountAsync(string username, string? query = null)
    {
            var reportsQuery = _context.Reports
                .Where(r => r.Username == username);

            if (!string.IsNullOrWhiteSpace(query))
            {
                var loweredQuery = query.ToLowerInvariant();
                reportsQuery = reportsQuery.Where(r =>
                    (r.Name != null && r.Name.ToLower().Contains(loweredQuery)) ||
                    (r.Description != null && r.Description.ToLower().Contains(loweredQuery)));
            }

            return await reportsQuery.CountAsync();
    }

    /// <summary>
    /// Retrieves a single, fully detailed Report entity by its ID.
    /// </summary>
    public async Task<Report?> GetReportByIdAsync(int reportId, string username)
    {
        return await _context.Reports
            .AsNoTracking()
            .Where(r => r.Id == reportId && r.Username == username)
            .Include(r => r.ReportJoins)
                .ThenInclude(rj => rj.CrossModuleJoin)
                    .ThenInclude(j => j.LeftOperand)
            .Include(r => r.ReportJoins)
                .ThenInclude(rj => rj.CrossModuleJoin)
                    .ThenInclude(j => j.RightOperand) 
            .FirstOrDefaultAsync();
    }

    public async Task<bool> DeleteReportAsync(int reportId, string username)
    {
        // 1. Busca el reporte asegurándote de que pertenezca al usuario correcto.
        var reportToDelete = await _context.Reports
            .FirstOrDefaultAsync(r => r.Id == reportId && r.Username == username);

        // 2. Si no se encuentra, retorna false.
        if (reportToDelete == null)
        {
            return false;
        }
        var reportenvio = await _context.ScheduledReports
            .Where(r => r.ReportId == reportToDelete.Id && r.Username == username).ToListAsync();
        foreach (var reporte in reportenvio)
        {
            await DeleteScheduledReportAsync(reporte.Id);
        }
        var join = await _context.ReportJoins.Where(j => j.ReportId == reportToDelete.Id).ToListAsync();
        foreach(var joinid in join)
        {
            await RemoveJoinFromReportAsync(joinid.ReportId, joinid.CrossModuleJoinId, username);
        }
        //    las entradas correspondientes en la tabla 'ReportJoins'.
        _context.Reports.Remove(reportToDelete);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<Report?> UpdateReportAsync(int reportId, string name, string descripcion, string username, string JSON_config)
    {
        // 1. Busca el reporte asegurándote de que pertenezca al usuario correcto.
        var reportToUpdate = await _context.Reports
            .FirstOrDefaultAsync(r => r.Id == reportId && r.Username == username);

        // 2. Si no se encuentra, retorna null.
        if (reportToUpdate == null)
        {
            return null;
        }

        // 3. Actualiza las propiedades y guarda los cambios.
        reportToUpdate.Name = name;
        reportToUpdate.Description = descripcion;
        reportToUpdate.JSON_config = JSON_config;

        await _context.SaveChangesAsync();

        return reportToUpdate;
    }

    public async Task<Report?> UpdateReportWithFiltersAsync(int reportId, string name, string descripcion, string username, string JSON_config, string? JSON_filters)
    {
        // 1. Busca el reporte asegurándote de que pertenezca al usuario correcto.
        var reportToUpdate = await _context.Reports
            .FirstOrDefaultAsync(r => r.Id == reportId && r.Username == username);

        // 2. Si no se encuentra, retorna null.
        if (reportToUpdate == null)
        {
            return null;
        }

        // 3. Actualiza las propiedades incluyendo filtros y guarda los cambios.
        reportToUpdate.Name = name;
        reportToUpdate.Description = descripcion;
        reportToUpdate.JSON_config = JSON_config;
        reportToUpdate.JSON_filters = JSON_filters;

        await _context.SaveChangesAsync();

        return reportToUpdate;
    }





    public async Task<bool> RemoveJoinFromReportAsync(int reportId, int joinId, string username)
    {
        var joinLinkToRemove = await _context.ReportJoins
            .FirstOrDefaultAsync(rj =>
                rj.ReportId == reportId &&
                rj.CrossModuleJoinId == joinId &&
                rj.Report.Username == username);

        if (joinLinkToRemove == null)
        {
            return false;
        }

        _context.ReportJoins.Remove(joinLinkToRemove);
        await _context.SaveChangesAsync();
       
        // Eliminar el join del JSON_config del reporte
        var report = await _context.Reports.FirstOrDefaultAsync(r => r.Id == reportId && r.Username == username);
        if (report != null && !string.IsNullOrWhiteSpace(report.JSON_config))
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            try
            {
                var config = JsonSerializer.Deserialize<ReportJsonConfig>(report.JSON_config, options);
                if (config?.Sources != null)
                {
                    var originalCount = config.Sources.Count;
                    var filteredSources = config.Sources
                        .Where(s => !(s.SourceType?.ToLower() == "join" && s.SourceId == joinId))
                        .ToList();

                    if (filteredSources.Count != originalCount && filteredSources.Count != 0)
                    {
                        config.Sources = filteredSources; 
                        report.JSON_config = JsonSerializer.Serialize(config, options);
                        _context.Reports.Update(report);
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando JSON_config al eliminar join del reporte");
                throw;
            }
        }


        var join = await _context.CrossModuleJoins
                .FirstOrDefaultAsync(j => j.Id == joinId);

            if (join != null)
            {
                // Guardar los IDs antes de eliminar el join
                var leftOperandId = join.LeftOperandId;
                var rightOperandId = join.RightOperandId;

                _context.CrossModuleJoins.Remove(join);
                await _context.SaveChangesAsync();

                var leftOperand = await _context.JoinOperands.FirstOrDefaultAsync(o => o.Id == leftOperandId);
                if (leftOperand != null)
                        _context.JoinOperands.Remove(leftOperand);
                
                var rightOperand = await _context.JoinOperands.FirstOrDefaultAsync(o => o.Id == rightOperandId);
                if (rightOperand != null)
                {
                    _context.JoinOperands.Remove(rightOperand);
                }
                await _context.SaveChangesAsync();
            }
        

        return true;
    }

    public async Task<List<dynamic>> ExecuteReportAsync(int reportId, string username)
    {
        
        // 1. Obtener el reporte y su configuración JSON
        var report = await _context.Reports.FirstOrDefaultAsync(r => r.Id == reportId && r.Username == username);
        if (report == null || string.IsNullOrWhiteSpace(report.JSON_config))
        {
            throw new KeyNotFoundException($"El reporte con ID {reportId} no fue encontrado o no tiene una configuración JSON válida.");
        }


        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        var config = JsonSerializer.Deserialize<ReportJsonConfig>(report.JSON_config, serializerOptions);
        
        ReportFiltersConfig? filtersConfig = null;
        if (!string.IsNullOrWhiteSpace(report.JSON_filters))
        {
            try
            {
                filtersConfig = JsonSerializer.Deserialize<ReportFiltersConfig>(report.JSON_filters, serializerOptions);
            }
            catch (JsonException ex)
            {
                // Si hay error en la deserialización de filtros, continuar sin filtros
                filtersConfig = null;
            }
        }
        else
        {
        }

        var finalResults = new List<dynamic>();

        var sources = config?.Sources ?? new List<ReportSourceConfig>();
        
        foreach (var sourceConfig in sources)
        {
            IEnumerable<dynamic> rawData;
            switch (sourceConfig.SourceType.ToLower())
            {
                case "join":
                    if (!sourceConfig.SourceId.HasValue) 
                    {
                        continue;
                    }
                    
                    
                    // Para joins, necesitamos obtener la información del join y crear filtros para sus operandos
                    JoinFiltersConfig? joinFilters = null;
                    if (filtersConfig?.DatasetFilters != null && filtersConfig.DatasetFilters.Any())
                    {
                        joinFilters = await CreateJoinFiltersFromReportFilters(sourceConfig.SourceId.Value, filtersConfig);
                    }
                    
                    rawData = await _joinConfigService.ExecuteJoinWithFiltersAsync(sourceConfig.SourceId.Value, joinFilters);
                    break;

                case "dataset":
                    if (!sourceConfig.SourceId.HasValue || !sourceConfig.SourceModule.HasValue || !sourceConfig.EntityName.HasValue) 
                    {
                        continue;
                    }
                    
                    
                    var operand = new JoinOperand
                    {
                        ModuleType = sourceConfig.SourceModule.Value,
                        DatasetId = sourceConfig.SourceId.Value,
                        EntityName = sourceConfig.EntityName.Value
                    };
                    var datasetData = await _apiDataService.GetDataForOperand(operand, username);
                    rawData = PrefixDatasetData(datasetData, sourceConfig.EntityName.Value.ToString());
                    
                    // Aplicar filtros específicos para este dataset si existen
                    if (filtersConfig?.DatasetFilters != null)
                    {
                        var datasetFilter = filtersConfig.DatasetFilters.FirstOrDefault(f => 
                            f.DatasetId == sourceConfig.SourceId.Value && 
                            f.ModuleType == sourceConfig.SourceModule.Value);
                        
                        if (datasetFilter?.Filters != null && datasetFilter.Filters.Any())
                        {
                    // Imprimir cada filtro individualmente
                    foreach (var f in datasetFilter.Filters)
                    {
                    }

                    rawData = ApiDataService.StaticFilterObjects(rawData, datasetFilter.Filters);
                        }
                    }
                    break;

                case "device":
                    if (!sourceConfig.SourceId.HasValue || !sourceConfig.DateFrom.HasValue || !sourceConfig.DateTo.HasValue)
                    {
                        continue;
                    }

                    
                    try
                    {
                        DateTime dateFrom = DateTime.ParseExact(sourceConfig.DateFrom.Value.ToString(), "yyyyMMddHHmm", CultureInfo.InvariantCulture);
                        DateTime dateTo = DateTime.ParseExact(sourceConfig.DateTo.Value.ToString(), "yyyyMMddHHmm", CultureInfo.InvariantCulture);

                        var deviceReadings = await _sondaIMService.GetDeviceDataByDate(sourceConfig.SourceId.Value, dateFrom, dateTo, username);
                        rawData = PrefixDatasetData(deviceReadings, "DeviceData");
                    }
                    catch (FormatException ex)
                    {
                        continue;
                    }
                    break;
                default:
                    continue;
            }

            if (rawData == null || !rawData.Any())
            {
                continue;
            }


            foreach (var rawRow in rawData)
            {
                var projectedRow = new ExpandoObject() as IDictionary<string, object>;
                var rowAsDictionary = ObjectToDictionary(rawRow);

                foreach (var column in (sourceConfig.Columns ?? Enumerable.Empty<ReportColumnConfig>()))
                {
                    if (rowAsDictionary.TryGetValue(column.Attribute, out object value))
                    {
                        projectedRow[column.As] = value;
                    }
                    else
                    {
                        projectedRow[column.As] = null!;
                    }
                }
                finalResults.Add(projectedRow);
            }
        }

        return finalResults;
    }






    private IEnumerable<dynamic> PrefixDatasetData(IEnumerable<dynamic> datasetData, string prefix)
    {
        if (datasetData == null || !datasetData.Any())
        {
            return new List<dynamic>();
        }

        var prefixedList = new List<dynamic>();
        foreach (var item in datasetData)
        {
            var expando = new ExpandoObject() as IDictionary<string, object>;
            foreach (var property in (item as object).GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                expando[$"{prefix}_{property.Name}"] = property.GetValue(item);
            }
            prefixedList.Add(expando);
        }
        return prefixedList;
    }

    private IDictionary<string, object> ObjectToDictionary(object obj)
    {
        if (obj == null) return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        // Si el objeto ya es un diccionario (como un ExpandoObject), lo usamos para crear
        // un nuevo diccionario estándar y normalizar el comportamiento.
        if (obj is IDictionary<string, object> dict)
        {
            return new Dictionary<string, object>(dict, StringComparer.OrdinalIgnoreCase);
        }

        // Si es una clase estándar (como Device), usamos reflexión para convertirlo en un diccionario.
        var dictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            dictionary[property.Name] = property.GetValue(obj) ?? default!;
        }
        return dictionary;
    }

    /// <summary>
    /// Crea filtros para un join específico basándose en los filtros del reporte y los operandos del join.
    /// </summary>
    private async Task<JoinFiltersConfig?> CreateJoinFiltersFromReportFilters(int joinId, ReportFiltersConfig reportFilters)
    {
        // 1. Obtener la configuración del join para conocer sus operandos
        var joinConfig = await _context.CrossModuleJoins
            .Include(j => j.LeftOperand)
            .Include(j => j.RightOperand)
            .FirstOrDefaultAsync(j => j.Id == joinId);

        if (joinConfig == null)
        {
            return null;
        }

        var joinFilters = new JoinFiltersConfig();

        // 2. Buscar filtros para el operando izquierdo
        var leftFilter = reportFilters.DatasetFilters.FirstOrDefault(f => 
            f.DatasetId == joinConfig.LeftOperand.DatasetId && 
            f.ModuleType == joinConfig.LeftOperand.ModuleType);
        
        if (leftFilter?.Filters != null && leftFilter.Filters.Any())
        {
            var clonedLeftFilters = CloneFiltersForJoinOperand(leftFilter.Filters, joinConfig.LeftOperand.EntityName);
            if (clonedLeftFilters.Any())
            {
                joinFilters.LeftOperandFilters = new OperandFilterConfig
                {
                    Filters = clonedLeftFilters
                };
            }
        }

        // 3. Buscar filtros para el operando derecho
        var rightFilter = reportFilters.DatasetFilters.FirstOrDefault(f => 
            f.DatasetId == joinConfig.RightOperand.DatasetId && 
            f.ModuleType == joinConfig.RightOperand.ModuleType);
        
        if (rightFilter?.Filters != null && rightFilter.Filters.Any())
        {
            var clonedRightFilters = CloneFiltersForJoinOperand(rightFilter.Filters, joinConfig.RightOperand.EntityName);
            if (clonedRightFilters.Any())
            {
                joinFilters.RightOperandFilters = new OperandFilterConfig
                {
                    Filters = clonedRightFilters
                };
            }
        }

        // 4. Solo devolver filtros si hay al menos uno
        if (joinFilters.LeftOperandFilters == null && joinFilters.RightOperandFilters == null)
        {
            return null;
        }

        return joinFilters;
    }

    private static List<FilterCondition> CloneFiltersForJoinOperand(IEnumerable<FilterCondition> filters, EntityName entity)
    {
        var cloned = new List<FilterCondition>();
        if (filters == null)
        {
            return cloned;
        }

        var prefix = entity.ToString() + "_";

        foreach (var filter in filters)
        {
            if (filter == null)
            {
                continue;
            }

            var attribute = filter.AttributeName ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(attribute) && attribute.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                attribute = attribute.Substring(prefix.Length);
            }

            if (string.IsNullOrWhiteSpace(attribute))
            {
                continue;
            }

            cloned.Add(new FilterCondition
            {
                AttributeName = attribute,
                Type = filter.Type,
                ValueType = filter.ValueType,
                Condition = CloneFilterConditionValue(filter.Condition)
            });
        }

        return cloned;
    }

    private static object CloneFilterConditionValue(object condition)
    {
        if (condition is JsonElement jsonElement)
        {
            return jsonElement.Clone();
        }

        return condition;
    }

    // ===================== PDF GENERATION METHODS =====================
    
    public async Task<byte[]> GenerateReportPdfAsync(int reportId, string username)
    {
        try
        {
            _logger.LogInformation($"Generando PDF para reporte {reportId}, usuario {username}");

            // 1. Obtener la definición del reporte
            var reportDefinition = await GetReportByIdAsync(reportId, username);
            if (reportDefinition == null)
                throw new ArgumentException($"Reporte {reportId} no encontrado para el usuario {username}");

            // 2. Ejecutar el reporte para obtener los datos
            var reportData = await ExecuteReportAsync(reportId, username);
            if (reportData == null || !reportData.Any())
                throw new InvalidOperationException($"No se pudieron obtener datos para el reporte {reportId}");

            // 3. Extraer las columnas de los datos
            var columns = ExtractColumnsFromDynamicData(reportData);

            // 4. Generar el PDF
            return GenerateReportPdfFromData(reportData, columns, reportDefinition.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error generando PDF para reporte {reportId}");
            throw;
        }
    }

    public byte[] GenerateReportPdfFromData(List<dynamic> reportData, List<string> columns, string reportTitle)
    {
        try
        {
            using (var memoryStream = new MemoryStream())
            {
                // Crear documento PDF usando iText7
                var writer = new iText.Kernel.Pdf.PdfWriter(memoryStream);
                var pdf = new iText.Kernel.Pdf.PdfDocument(writer);
                
                // Configurar página horizontal si hay muchas columnas
                var pageSize = columns.Count > 6 ? 
                    iText.Kernel.Geom.PageSize.A4.Rotate() : 
                    iText.Kernel.Geom.PageSize.A4;
                
                var document = new Document(pdf, pageSize);
                
                // Márgenes más pequeños para tablas grandes
                if (columns.Count > 6)
                {
                    document.SetMargins(20, 20, 20, 20);
                }

                // Header del reporte
                var title = new Paragraph(reportTitle)
                    .SetFontSize(20)
                    .SetBold()
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(20)
                    .SetFontColor(ColorConstants.DARK_GRAY);
                document.Add(title);

                // Información del reporte
                var info = new Paragraph()
                    .Add(new Text($"Fecha de generación: {DateTime.Now:dd/MM/yyyy HH:mm:ss}\n").SetBold())
                    .Add(new Text($"Total de registros: {reportData.Count:N0}\n"))
                    .Add(new Text("Sistema: OmniMonitor"))
                    .SetMarginBottom(20)
                    .SetFontSize(10)
                    .SetFontColor(ColorConstants.GRAY);
                document.Add(info);

                // Crear tabla con ancho relativo basado en el número de columnas
                float[] columnWidths;
                
                if (columns.Count > 10)
                {
                    // Para muchas columnas, usar anchos iguales y fuente más pequeña
                    columnWidths = Enumerable.Repeat(1f, columns.Count).ToArray();
                }
                else
                {
                    // Para pocas columnas, calcular anchos inteligentes
                    columnWidths = CalculateColumnWidths(columns, reportData);
                }
                
                var table = new Table(columnWidths)
                    .UseAllAvailableWidth()
                    .SetMarginBottom(20);

                // Ajustar fuente basado en número de columnas
                var headerFontSize = columns.Count > 8 ? 8f : 10f;
                var cellFontSize = columns.Count > 8 ? 7f : 9f;

                // Headers de la tabla
                foreach (var column in columns)
                {
                    var headerText = columns.Count > 10 ? 
                        TruncateText(column, 8) : 
                        column;
                        
                    var headerCell = new Cell()
                        .Add(new Paragraph(headerText)
                            .SetBold()
                            .SetFontColor(ColorConstants.WHITE)
                            .SetFontSize(headerFontSize))
                        .SetBackgroundColor(new DeviceRgb(44, 82, 130))
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetPadding(columns.Count > 8 ? 4 : 8);
                    table.AddHeaderCell(headerCell);
                }

                // Datos de la tabla
                bool isOddRow = false;
                foreach (var row in reportData)
                {
                    foreach (var column in columns)
                    {
                        var cellValue = GetValueFromDynamicObject(row, column);
                        var displayValue = FormatCellValue(cellValue);
                        
                        // Truncar texto si hay muchas columnas
                        if (columns.Count > 10)
                        {
                            displayValue = TruncateText(displayValue, 15);
                        }
                        else if (columns.Count > 6)
                        {
                            displayValue = TruncateText(displayValue, 25);
                        }
                        
                        var cell = new Cell()
                            .Add(new Paragraph(displayValue).SetFontSize(cellFontSize))
                            .SetPadding(columns.Count > 8 ? 3 : 6)
                            .SetTextAlignment(GetCellAlignment(cellValue));
                        
                        if (isOddRow)
                        {
                            cell.SetBackgroundColor(new DeviceRgb(247, 250, 252));
                        }
                        
                        table.AddCell(cell);
                    }
                    isOddRow = !isOddRow;
                }

                document.Add(table);

                // Footer
                var footer = new Paragraph("Generado por OmniMonitor - Sistema de Monitoreo Integral")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(8)
                    .SetFontColor(ColorConstants.GRAY)
                    .SetMarginTop(20);
                document.Add(footer);

                document.Close();
                return memoryStream.ToArray();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generando PDF desde datos");
            throw;
        }
    }

    public string GenerateReportHtml(List<dynamic> reportData, List<string> columns, string reportTitle)
    {
        var html = new StringBuilder();
        
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html>");
        html.AppendLine("<head>");
        html.AppendLine("    <meta charset='utf-8'>");
        html.AppendLine($"    <title>{reportTitle}</title>");
        html.AppendLine("    <style>");
        html.AppendLine("        body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 20px; background: #f9f9f9; }");
        html.AppendLine("        .header { background: #2c5282; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }");
        html.AppendLine("        .header h1 { margin: 0; font-size: 24px; }");
        html.AppendLine("        .info { background: #e2e8f0; padding: 15px; border-left: 4px solid #2c5282; margin: 20px 0; }");
        html.AppendLine("        .info p { margin: 5px 0; font-size: 14px; color: #4a5568; }");
        html.AppendLine("        table { width: 100%; border-collapse: collapse; background: white; box-shadow: 0 2px 10px rgba(0,0,0,0.1); border-radius: 8px; overflow: hidden; }");
        html.AppendLine("        th { background: #2c5282; color: white; padding: 12px; text-align: left; font-weight: 600; }");
        html.AppendLine("        td { padding: 10px 12px; border-bottom: 1px solid #e2e8f0; }");
        html.AppendLine("        tr:nth-child(even) { background: #f7fafc; }");
        html.AppendLine("        tr:hover { background: #edf2f7; }");
        html.AppendLine("        .footer { margin-top: 20px; text-align: center; font-size: 12px; color: #718096; }");
        html.AppendLine("        @media print { body { margin: 0; } .header { border-radius: 0; } }");
        html.AppendLine("    </style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        
        // Header
        html.AppendLine("    <div class='header'>");
        html.AppendLine($"        <h1>{System.Net.WebUtility.HtmlEncode(reportTitle)}</h1>");
        html.AppendLine("    </div>");
        
        // Info
        html.AppendLine("    <div class='info'>");
        html.AppendLine($"        <p><strong>Fecha de generación:</strong> {DateTime.Now:dd/MM/yyyy HH:mm:ss}</p>");
        html.AppendLine($"        <p><strong>Total de registros:</strong> {reportData.Count:N0}</p>");
        html.AppendLine($"        <p><strong>Sistema:</strong> OmniMonitor</p>");
        html.AppendLine("    </div>");
        
        // Table
        html.AppendLine("    <table>");
        
        // Headers
        html.AppendLine("        <thead>");
        html.AppendLine("            <tr>");
        foreach (var column in columns)
        {
            html.AppendLine($"                <th>{System.Net.WebUtility.HtmlEncode(column)}</th>");
        }
        html.AppendLine("            </tr>");
        html.AppendLine("        </thead>");
        
        // Data
        html.AppendLine("        <tbody>");
        foreach (var row in reportData)
        {
            html.AppendLine("            <tr>");
            foreach (var column in columns)
            {
                var cellValue = GetValueFromDynamicObject(row, column);
                var displayValue = FormatCellValue(cellValue);
                html.AppendLine($"                <td>{System.Net.WebUtility.HtmlEncode(displayValue)}</td>");
            }
            html.AppendLine("            </tr>");
        }
        html.AppendLine("        </tbody>");
        html.AppendLine("    </table>");
        
        // Footer
        html.AppendLine("    <div class='footer'>");
        html.AppendLine("        <p>Generado por OmniMonitor - Sistema de Monitoreo Integral</p>");
        html.AppendLine("    </div>");
        
        html.AppendLine("</body>");
        html.AppendLine("</html>");
        
        return html.ToString();
    }

    private List<string> ExtractColumnsFromDynamicData(List<dynamic> data)
    {
        if (data == null || !data.Any())
            return new List<string>();

        var firstRow = data.First();
        
        if (firstRow is ExpandoObject expandoObj)
        {
            return ((IDictionary<string, object>)expandoObj).Keys.ToList();
        }
        else
        {
            // Si es un objeto tipado, usar reflection
            var properties = firstRow.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance);
            
            var columnNames = new List<string>();
            foreach (var prop in properties)
            {
                columnNames.Add(prop.Name);
            }
            return columnNames;
        }
    }

    private object? GetValueFromDynamicObject(dynamic obj, string propertyName)
    {
        try
        {
            if (obj is ExpandoObject expandoObj)
            {
                var dict = (IDictionary<string, object>)expandoObj;
                return dict.ContainsKey(propertyName) ? dict[propertyName] : null;
            }
            else
            {
                // Objeto tipado
                var property = obj.GetType().GetProperty(propertyName);
                return property?.GetValue(obj);
            }
        }
        catch
        {
            return null;
        }
    }

    private string FormatCellValue(object? value)
    {
        if (value == null)
            return "";

        return value switch
        {
            DateTime dt => dt.ToString("dd/MM/yyyy HH:mm"),
            decimal dec => dec.ToString("N2"),
            double dbl => dbl.ToString("N2"),
            float flt => flt.ToString("N2"),
            bool boolean => boolean ? "Sí" : "No",
            _ => value.ToString() ?? ""
        };
    }

    private float[] CalculateColumnWidths(List<string> columns, List<dynamic> reportData)
    {
        var widths = new float[columns.Count];
        
        // Calcular ancho basado en el contenido
        for (int i = 0; i < columns.Count; i++)
        {
            var columnName = columns[i];
            var maxLength = columnName.Length;
            
            // Revisar algunas filas para estimar ancho
            var sampleRows = reportData.Take(Math.Min(10, reportData.Count));
            foreach (var row in sampleRows)
            {
                var cellValue = GetValueFromDynamicObject(row, columnName);
                var displayValue = FormatCellValue(cellValue);
                maxLength = Math.Max(maxLength, displayValue.Length);
            }
            
            // Asignar ancho relativo (mínimo 1, máximo 4)
            widths[i] = Math.Max(1f, Math.Min(4f, maxLength / 10f + 1f));
        }
        
        return widths;
    }

    private string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;
        
        return text.Substring(0, maxLength - 3) + "...";
    }

    private TextAlignment GetCellAlignment(object? value)
    {
        return value switch
        {
            decimal _ or double _ or float _ or int _ or long _ => TextAlignment.RIGHT,
            DateTime _ => TextAlignment.CENTER,
            bool _ => TextAlignment.CENTER,
            _ => TextAlignment.LEFT
        };
    }

    public async Task<int> CreateScheduledReportAsync(ScheduledReportRequest dto, string username)
    {
        if (dto.Recipients == null || dto.Recipients.Count == 0)
            throw new ArgumentException("Debe especificar al menos un destinatario.");

        if (dto.Recipients.Count > 50)
            throw new ArgumentException("No se permiten más de 50 destinatarios por programación.");

        foreach (var email in dto.Recipients)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
            }
            catch
            {
                throw new ArgumentException($"El correo '{email}' no es válido.");
            }
        }

        var validTypes = new[] { "DAILY", "WEEKLY", "MONTHLY", "ADVANCED" };
        if (!validTypes.Contains(dto.ScheduleType.ToUpper()))
            throw new ArgumentException("Tipo de programación no válido. Use DAILY, WEEKLY, MONTHLY o ADVANCED.");

        if (dto.ScheduleType != "ADVANCED" && string.IsNullOrWhiteSpace(dto.SendAtLocalTime))
            throw new ArgumentException("Debe indicar una hora para los tipos de programación diaria, semanal o mensual.");

        if (dto.ScheduleType == "ADVANCED" && string.IsNullOrWhiteSpace(dto.AdvancedRule))
            throw new ArgumentException("Debe especificar una regla avanzada si usa tipo ADVANCED.");

        // 3️⃣ Zona horaria
        if (string.IsNullOrWhiteSpace(dto.TimeZone))
            throw new ArgumentException("Debe especificar una zona horaria válida.");

        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(dto.TimeZone);
        }
        catch
        {
            throw new ArgumentException($"La zona horaria '{dto.TimeZone}' no es válida.");
        }

        var existing = await _context.ScheduledReports
            .AsNoTracking()
            .FirstOrDefaultAsync(r =>
                r.ReportId == dto.ReportId &&
                r.Username == username &&
                r.ScheduleType == dto.ScheduleType &&
                r.SendAtLocalTime == dto.SendAtLocalTime &&
                r.AdvancedRule == dto.AdvancedRule &&
                r.TimeZone == dto.TimeZone);

        if (existing != null)
            throw new ArgumentException("Ya existe una programación idéntica para este reporte.");

        int count = await _context.ScheduledReports.CountAsync(r => r.Username == username);
        if (count >= 100)
            throw new ArgumentException("Se alcanzó el límite máximo de programaciones permitidas (100).");

        var entity = new ScheduledReport
        {
            ReportId = dto.ReportId,
            Username = username,
            ScheduleType = dto.ScheduleType,
            IntervalMinutes = dto.IntervalMinutes,
            SendAtLocalTime = dto.SendAtLocalTime,
            AdvancedRule = dto.AdvancedRule,
            TimeZone = dto.TimeZone,
            RecipientsJson = JsonSerializer.Serialize(dto.Recipients),
            Subject = dto.Subject,
            Message = dto.Message,
            LastExecution = null,
            IsActive = true
        };

        _context.ScheduledReports.Add(entity);
        await _context.SaveChangesAsync();

        return entity.Id;
    }

    public async Task<List<ScheduledReport>> GetScheduledReports()
    {
        return await _context.ScheduledReports
            .AsNoTracking()
            .OrderBy(sr => sr.Id)
            .ToListAsync();
    }


    public async Task<List<ScheduledReportResponse>> GetScheduledReportsByUserAsync(string username)
    {
        var list = await _context.ScheduledReports
            .AsNoTracking()
            .Where(sr => sr.Username == username && sr.IsActive)
            .OrderBy(sr => sr.Id)
            .ToListAsync();

        return list
            .Select(sr => MapToResponse(sr))
            .ToList();
    }


    private ScheduledReportResponse MapToResponse(ScheduledReport entity)
    {
        return new ScheduledReportResponse
        {
            Id = entity.Id,
            ReportId = entity.ReportId,
            Username = entity.Username,
            ScheduleType = entity.ScheduleType,
            IntervalMinutes = entity.IntervalMinutes,
            SendAtLocalTime = entity.SendAtLocalTime,
            AdvancedRule = entity.AdvancedRule,
            TimeZone = entity.TimeZone,
            Recipients = string.IsNullOrWhiteSpace(entity.RecipientsJson)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(entity.RecipientsJson),
            Subject = entity.Subject,
            Message = entity.Message,
            LastExecution = entity.LastExecution,
            IsActive = entity.IsActive
        };
    }



    public async Task<ScheduledReportResponse?> GetScheduledReportByIdAsync(int id, string username)
    {
        var entity = await _context.ScheduledReports
            .AsNoTracking()
            .Where(sr => sr.Id == id && sr.Username == username && sr.IsActive)
            .FirstOrDefaultAsync();

        if (entity == null)
            return null;

        return MapToResponse(entity);
    }

    public async Task DeleteScheduledReportAsync(int id)
    {
        var report = await _context.ScheduledReports
            .FirstOrDefaultAsync(r => r.Id == id);

        if (report == null)
            throw new ArgumentException($"No se encontró la programación con Id {id}.");


        _context.ScheduledReports.Remove(report);
        await _context.SaveChangesAsync();
    }


    public ScheduledReportResponse MapToDto(ScheduledReport entity)
    {
        return new ScheduledReportResponse
        {
            Id = entity.Id,
            ReportId = entity.ReportId,
            Username = entity.Username,
            ScheduleType = entity.ScheduleType,
            IntervalMinutes = entity.IntervalMinutes,
            SendAtLocalTime = entity.SendAtLocalTime,
            AdvancedRule = entity.AdvancedRule,
            TimeZone = entity.TimeZone,
            Recipients = JsonSerializer.Deserialize<List<string>>(entity.RecipientsJson),
            Subject = entity.Subject,
            Message = entity.Message,
            IsActive = entity.IsActive,
            LastExecution = entity.LastExecution
        };
    }

    public async Task ProcessScheduledReportsAsync()
    {
        var utcNow = DateTime.UtcNow;

        var scheduledReports = await _context.ScheduledReports
            .Where(r => r.IsActive)
            .ToListAsync();

        foreach (var report in scheduledReports)
        {
            try
            {
                if (ShouldSend(report, utcNow))
                {
                    await sendScheduledReport(report);

                    report.LastExecution = utcNow;
                    _context.ScheduledReports.Update(report);
                }
            }
            catch (Exception ex)
            {
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task sendScheduledReport(ScheduledReport scheduled)
    {
        if (scheduled == null)
            return;

        try
        {

            // 2. Generar PDF
            var pdfBytes = await GenerateReportPdfAsync(scheduled.ReportId, scheduled.Username);

            // 3. Obtener destinatarios
            List<string>? recipients;

            try
            {
                recipients = JsonSerializer.Deserialize<List<string>>(scheduled.RecipientsJson)
                             ?? new List<string>();
            }
            catch
            {
                return;
            }

            if (recipients.Count == 0)
                return;

            // 4. Enviar email
            await _mailService.SendEmailAsync(
                recipients: recipients,
                subject: scheduled.Subject,
                message: scheduled.Message,
                pdfAttachment: pdfBytes,
                pdfName: $"Reporte_{scheduled.Id}.pdf"
            );
        }
        catch (Exception ex)
        {
        }
    }



    private async Task<byte[]> GenerateReportPdfAsync(Report report)
    {
        using var ms = new MemoryStream();

        var doc = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4);

        iTextSharp.text.pdf.PdfWriter.GetInstance(doc, ms);

        doc.Open();

        doc.Add(new iTextSharp.text.Paragraph("📄 Reporte generado automáticamente"));
        doc.Add(new iTextSharp.text.Paragraph($"Reporte ID: {report.Id}"));
        doc.Add(new iTextSharp.text.Paragraph($"Nombre: {report.Name}"));
        doc.Add(new iTextSharp.text.Paragraph($"Fecha: {DateTime.Now}"));
        doc.Add(new iTextSharp.text.Paragraph(" "));
        doc.Add(new iTextSharp.text.Paragraph("Este es un PDF dummy, la versión real será implementada después."));

        doc.Close();

        return await Task.FromResult(ms.ToArray());
    }



    public bool ShouldSend(ScheduledReport report, DateTime utcNow)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(report.TimeZone);
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, tz);

        if (!report.IsActive)
            return false;

        var scheduleType = (report.ScheduleType ?? "").Trim().ToUpperInvariant();

        if (report.LastExecution.HasValue)
        {
            var lastLocal = TimeZoneInfo.ConvertTimeFromUtc(report.LastExecution.Value, tz);

            if (scheduleType != "ADVANCED" && lastLocal.Date == localNow.Date)
                return false;
        }

        switch (scheduleType)
        {
            case "DAILY":
                return localNow.TimeOfDay >= TimeSpan.Parse(report.SendAtLocalTime);

            case "WEEKLY":
                return ShouldSendWeekly(report, localNow);

            case "MONTHLY":
                return ShouldSendMonthly(report, localNow);

            case "ADVANCED":
                return ShouldSendAdvanced(report, localNow);

            default:
                return false;
        }
    }

    private bool ShouldSendMonthly(ScheduledReport report, DateTime localNow)
    {
        if (report.LastExecution.HasValue)
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(report.TimeZone);
            var lastLocal = TimeZoneInfo.ConvertTimeFromUtc(report.LastExecution.Value, tz);

            if (lastLocal.Date == localNow.Date)
                return false;
        }

        if (string.IsNullOrWhiteSpace(report.AdvancedRule))
            return false;

        var obj = JsonSerializer.Deserialize<MonthlyRule>(report.AdvancedRule);
        var sendTime = TimeSpan.Parse(report.SendAtLocalTime);

        return localNow.Day == obj.day &&
               localNow.TimeOfDay >= sendTime;
    }


    public class MonthlyRule
    {
        public int day { get; set; }
    }

    private bool ShouldSendWeekly(ScheduledReport report, DateTime localNow)
    {

        if (report.LastExecution.HasValue)
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(report.TimeZone);
            var lastLocal = TimeZoneInfo.ConvertTimeFromUtc(report.LastExecution.Value, tz);

            if (lastLocal.Date == localNow.Date)
                return false;
        }

        var obj = JsonSerializer.Deserialize<WeeklyRule>(report.AdvancedRule);

        var dayOfWeek = ParseDayOfWeek(obj.day);
        var sendTime = TimeSpan.Parse(report.SendAtLocalTime);

        return localNow.DayOfWeek == dayOfWeek &&
               localNow.TimeOfDay >= sendTime;
    }

    public class WeeklyRule
    {
        public string day { get; set; }
    }

    private bool ShouldSendAdvanced(ScheduledReport report, DateTime localNow)
    {
        // { "days": ["MON","THU"], "time": "08:00" }
        var obj = JsonSerializer.Deserialize<AdvancedRule>(report.AdvancedRule);

        var targetDays = obj.days.Select(ParseDayOfWeek).ToList();
        var targetTime = TimeSpan.Parse(obj.time);

        return targetDays.Contains(localNow.DayOfWeek) &&
               localNow.TimeOfDay >= targetTime;
    }

    public class AdvancedRule
    {
        public string[] days { get; set; }
        public string time { get; set; }
    }


    // Helpers
    private DayOfWeek ParseDayOfWeek(string s)
    {
        return s.ToUpper() switch
        {
            "MON" => DayOfWeek.Monday,
            "TUE" => DayOfWeek.Tuesday,
            "WED" => DayOfWeek.Wednesday,
            "THU" => DayOfWeek.Thursday,
            "FRI" => DayOfWeek.Friday,
            "SAT" => DayOfWeek.Saturday,
            "SUN" => DayOfWeek.Sunday,
            _ => throw new Exception("Día inválido en regla avanzada")
        };
    }


    // Solo para simplificar ejemplo semanal
    private DayOfWeek DayOfWeekFromAdvancedRule(string rule) => ParseDayOfWeek(rule.Split(',')[0]);

    public async Task<ScheduledReportResponse?> UpdateScheduledReportAsync(
     int id,
     ScheduledReportRequest dto,
     string username)
    {
        var entity = await _context.ScheduledReports
            .FirstOrDefaultAsync(r => r.Id == id && r.Username == username);

        if (entity == null)
            return null;

        // Recipients
        if (dto.Recipients != null)
        {
            if (dto.Recipients.Count == 0)
                throw new ArgumentException("At least one recipient must be specified.");

            if (dto.Recipients.Count > 50)
                throw new ArgumentException("A maximum of 50 recipients is allowed.");

            foreach (var email in dto.Recipients)
            {
                try { var addr = new System.Net.Mail.MailAddress(email); }
                catch { throw new ArgumentException($"The email '{email}' is not valid."); }
            }

            entity.RecipientsJson = JsonSerializer.Serialize(dto.Recipients);
        }

        // ScheduleType
        if (!string.IsNullOrWhiteSpace(dto.ScheduleType))
        {
            var validTypes = new[] { "DAILY", "WEEKLY", "MONTHLY", "ADVANCED" };
            if (!validTypes.Contains(dto.ScheduleType.ToUpper()))
                throw new ArgumentException("Invalid schedule type. Use DAILY, WEEKLY, MONTHLY, or ADVANCED.");

            entity.ScheduleType = dto.ScheduleType;
        }

        // SendAtLocalTime
        if (!string.IsNullOrWhiteSpace(dto.SendAtLocalTime))
            entity.SendAtLocalTime = dto.SendAtLocalTime;

        // AdvancedRule
        if (!string.IsNullOrWhiteSpace(dto.AdvancedRule))
            entity.AdvancedRule = dto.AdvancedRule;

        // TimeZone
        if (!string.IsNullOrWhiteSpace(dto.TimeZone))
        {
            try { TimeZoneInfo.FindSystemTimeZoneById(dto.TimeZone); }
            catch { throw new ArgumentException($"The time zone '{dto.TimeZone}' is not valid."); }

            entity.TimeZone = dto.TimeZone;
        }

        // IntervalMinutes (nullable int)
        if (dto.IntervalMinutes.HasValue)
            entity.IntervalMinutes = dto.IntervalMinutes;

        // ReportId (solo si > 0)
        if (dto.ReportId > 0)
            entity.ReportId = dto.ReportId;

        if (!string.IsNullOrWhiteSpace(dto.Subject))
            entity.Subject = dto.Subject;

        if (!string.IsNullOrWhiteSpace(dto.Message))
            entity.Message = dto.Message;

        // Check duplicate
        var duplicate = await _context.ScheduledReports
            .AsNoTracking()
            .FirstOrDefaultAsync(r =>
                r.Id != id &&
                r.ReportId == entity.ReportId &&
                r.Username == username &&
                r.ScheduleType == entity.ScheduleType &&
                r.SendAtLocalTime == entity.SendAtLocalTime &&
                r.AdvancedRule == entity.AdvancedRule &&
                r.TimeZone == entity.TimeZone);

        if (duplicate != null)
            throw new ArgumentException("Another identical schedule already exists for this report.");

        _context.ScheduledReports.Update(entity);
        await _context.SaveChangesAsync();

        return new ScheduledReportResponse
        {
            Id = entity.Id,
            ReportId = entity.ReportId,
            Username = entity.Username,
            ScheduleType = entity.ScheduleType,
            IntervalMinutes = entity.IntervalMinutes,
            SendAtLocalTime = entity.SendAtLocalTime,
            AdvancedRule = entity.AdvancedRule,
            TimeZone = entity.TimeZone
        };
    }




}
