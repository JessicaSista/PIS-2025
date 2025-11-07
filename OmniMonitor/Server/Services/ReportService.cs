using Microsoft.EntityFrameworkCore;
using OmniMonitor.Server.Context;
using OmniMonitor.Shared.Dtos;
using System.Dynamic;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

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
    Task<List<Report>> GetAllReportsPaginatedAsync(string username, int page = 1, int pageSize = 10, string? query = null);
    Task<int> GetReportsCountAsync(string username, string? query = null);

}

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _context;
    private readonly IJoinConfigurationService _joinConfigService;
    private readonly IApiDataService _apiDataService;
    private readonly ISondaAuthService _sondaAuthService;
    private readonly ISondaIMService _sondaIMService;

    public ReportService(ApplicationDbContext context, IJoinConfigurationService JoinConfigurationService,
        IApiDataService ApiDataService, ISondaAuthService SondaAuthService, ISondaIMService SondaIMService)
    {
        _context = context;
        _joinConfigService = JoinConfigurationService;
        _apiDataService = ApiDataService;
        _sondaAuthService = SondaAuthService;
        _sondaIMService = SondaIMService;
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
}