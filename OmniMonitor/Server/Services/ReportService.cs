using System.Dynamic;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

using iTextSharp.text.pdf;

using Microsoft.EntityFrameworkCore;

using OmniMonitor.Server.Context;
using OmniMonitor.Server.Services;
using OmniMonitor.Shared;
using OmniMonitor.Shared.Dtos;

public interface IReportService
{
    Task<Report> CreateReportAsync(CreateReportRequestDto request);
    Task<ReportJoin> CreateAndAddJoinToReportAsync(int reportId, CreateJoinRequestDto joinRequest, string username);
    Task<List<Report>> GetAllReportsByUsernameAsync(string username);
    Task<Report?> GetReportByIdAsync(int reportId, string username);
    Task<bool> DeleteReportAsync(int reportId, string username);
    Task<Report?> UpdateReportAsync(int reportId, string name, string descripcion, string username, string JSON_config);
    Task<bool> RemoveJoinFromReportAsync(int reportId, int joinId, string username);
    Task<List<dynamic>> ExecuteReportAsync(int reportId, string username);

    Task<int> CreateScheduledReportAsync(ScheduledReportRequest dto, string username);

    Task<List<ScheduledReport>> GetScheduledReports();
    Task<List<ScheduledReport>> GetScheduledReportsByUserAsync(string username);
    Task<ScheduledReport?> GetScheduledReportByIdAsync(int id, string username);
    
    Task DeleteScheduledReportAsync(int id);
    Task ProcessScheduledReportsAsync();


}

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _context;
    private readonly IJoinConfigurationService _joinConfigService;
    private readonly IApiDataService _apiDataService;
    private readonly ISondaAuthService _sondaAuthService;
    private readonly ISondaIMService _sondaIMService;
    private readonly IMailService _mailService;

    public ReportService(ApplicationDbContext context, IJoinConfigurationService JoinConfigurationService,
        IApiDataService ApiDataService, ISondaAuthService SondaAuthService, ISondaIMService SondaIMService, IMailService mailService)
    {
        _context = context;
        _joinConfigService = JoinConfigurationService;
        _apiDataService = ApiDataService;
        _sondaAuthService = SondaAuthService;
        _sondaIMService = SondaIMService;
        _mailService = mailService;
    }

    /// <summary>
    /// Creates a new report and returns the complete Report entity.
    /// </summary>
    public async Task<Report> CreateReportAsync(CreateReportRequestDto request)
    {
        var report = new Report
        {
            Name = request.Name,
            Description = request.Description,
            Username = request.Username,
            JSON_config = request.JSON_config
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

        // 3. Elimina el reporte. La base de datos se encargará de eliminar en cascada
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

    var finalResults = new List<dynamic>();

    var sources = config?.Sources ?? new List<ReportSourceConfig>();
    foreach (var sourceConfig in sources)
        {

            IEnumerable<dynamic> rawData;
            switch (sourceConfig.SourceType.ToLower())
            {
                case "join":
                    if (!sourceConfig.SourceId.HasValue) continue;
                    rawData = await _joinConfigService.ExecuteJoinAsync(sourceConfig.SourceId.Value);
                    break;

                case "dataset":
                    if (!sourceConfig.SourceId.HasValue || !sourceConfig.SourceModule.HasValue || !sourceConfig.EntityName.HasValue) continue;
                    var operand = new JoinOperand
                    {
                        ModuleType = sourceConfig.SourceModule.Value,
                        DatasetId = sourceConfig.SourceId.Value,
                        EntityName = sourceConfig.EntityName.Value
                    };
                    var datasetData = await _apiDataService.GetDataForOperand(operand, username);
                    rawData = PrefixDatasetData(datasetData, sourceConfig.EntityName.Value.ToString());
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
                    catch (FormatException)
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


    public async Task<List<ScheduledReport>> GetScheduledReportsByUserAsync(string username)
    {
        return await _context.ScheduledReports
            .AsNoTracking()
            .Where(sr => sr.Username == username && sr.IsActive)
            .OrderBy(sr => sr.Id)
            .ToListAsync();
    }

    public async Task<ScheduledReport?> GetScheduledReportByIdAsync(int id, string username)
    {
        return await _context.ScheduledReports
            .AsNoTracking()
            .Where(sr => sr.Id == id && sr.Username == username && sr.IsActive)
            .FirstOrDefaultAsync();
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
                Console.WriteLine($"Error procesando reporte {report.Id}: {ex.Message}");
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task sendScheduledReport(ScheduledReport scheduled)
    {
        Console.WriteLine($"sendScheduledReport llamado para ReportId={scheduled.ReportId}, Id={scheduled.Id}");

        // 1. Obtener el reporte asociado
        var report = await _context.Reports
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == scheduled.ReportId);

        if (report == null)
        {
            Console.WriteLine("Reporte no encontrado.");
            return;
        }

        // 2. Generar PDF (placeholder que después vas a implementar)
        var pdfBytes = await GenerateReportPdfAsync(report);

        // 3. Obtener destinatarios desde RecipientsJson
        var recipients = JsonSerializer.Deserialize<List<string>>(scheduled.RecipientsJson)
                        ?? new List<string>();

        if (recipients.Count == 0)
        {
            Console.WriteLine("No hay destinatarios para este scheduled report.");
            return;
        }

        // 4. Enviar email usando TU función exacta
        await _mailService.SendEmailAsync(
            recipients: recipients,
            subject: scheduled.Subject,
            message: scheduled.Message,
            pdfAttachment: pdfBytes,
            pdfName: $"Reporte_{report.Id}.pdf"
        );
    }


    private async Task<byte[]> GenerateReportPdfAsync(Report report)
    {
        using var ms = new MemoryStream();

        var doc = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4);

        PdfWriter.GetInstance(doc, ms);

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

    if (report.LastExecution.HasValue)
    {
        var lastLocal = TimeZoneInfo.ConvertTimeFromUtc(report.LastExecution.Value, tz);

        if (report.ScheduleType != "ADVANCED" && lastLocal.Date == localNow.Date)
            return false;
    }

    switch (report.ScheduleType)
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



}