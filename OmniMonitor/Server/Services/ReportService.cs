using Microsoft.EntityFrameworkCore;
using OmniMonitor.Server.Context;
using System.Dynamic;
using System.Reflection;
using System.Text.Json;

public interface IReportService
{
    Task<Report> CreateReportAsync(CreateReportRequestDto request);
    Task<ReportJoin> AddJoinToReportAsync(int reportId, ReportJoinItemDto joinRequest, string username);
    Task<List<Report>> GetAllReportsByUsernameAsync(string username);
    Task<Report?> GetReportByIdAsync(int reportId, string username);
    Task<bool> DeleteReportAsync(int reportId, string username);
    Task<Report?> UpdateReportAsync(int reportId, string name, string descripcion, string username, string JSON_config);
    Task<bool> RemoveJoinFromReportAsync(int reportId, int joinId, string username);
    Task<DatasetReports> AddDatasetToReportAsync(int reportId, ModuleType moduleType, int id_dataset, string username);
    Task<bool> RemoveDatasetFromReportAsync(int reportId, ModuleType moduleType, int id_dataset, string username);
    Task<List<dynamic>> ExecuteReportAsync(int reportId, string username);
}

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _context;
    private readonly IJoinConfigurationService _joinConfigService;
    private readonly IApiDataService _apiDataService;

    public ReportService(ApplicationDbContext context, IJoinConfigurationService JoinConfigurationService, IApiDataService ApiDataService)
    {
        _context = context;
        _joinConfigService = JoinConfigurationService;
        _apiDataService = ApiDataService;
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
            Username = request.Username
        };
        _context.Reports.Add(report);
        await _context.SaveChangesAsync();

        return report;
    }

    public async Task<ReportJoin> AddJoinToReportAsync(int reportId, ReportJoinItemDto joinRequest, string username)
    {
        // 1. Verificar que el reporte existe y pertenece al usuario.
        var report = await _context.Reports
            .FirstOrDefaultAsync(r => r.Id == reportId && r.Username == username);

        if (report == null)
        {
            // Si el reporte no se encuentra o no pertenece al usuario, lanzamos una excepción.
            throw new KeyNotFoundException($"El reporte con ID {reportId} no fue encontrado para este usuario.");
        }

        // 2. (Opcional pero recomendado) Verificar que el Join existe.
        var joinExists = await _context.CrossModuleJoins.AnyAsync(j => j.Id == joinRequest.CrossModuleJoinId);
        if (!joinExists)
        {
            throw new KeyNotFoundException($"La configuración de Join con ID {joinRequest.CrossModuleJoinId} no existe.");
        }

        // 3. Crear y añadir la nueva entrada en la tabla de enlace.
        var newReportJoin = new ReportJoin
        {
            ReportId = reportId,
            CrossModuleJoinId = joinRequest.CrossModuleJoinId,
            ExecutionOrder = joinRequest.ExecutionOrder
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

    public async Task<DatasetReports> AddDatasetToReportAsync(int reportId, ModuleType moduleType, int id_dataset, string username)
    {
        var report = await _context.Reports.FirstOrDefaultAsync(r => r.Id == reportId && r.Username == username);
        if (report == null)
        {
            throw new KeyNotFoundException($"El reporte con ID {reportId} no fue encontrado para este usuario.");
        }

        var datasetInfo = new DatasetsOfReports
        {
            ModuleType = moduleType,
            id_dataset = id_dataset
        };
        _context.DatasetsOfReports.Add(datasetInfo);
        await _context.SaveChangesAsync();

        var reportLink = new DatasetReports
        {
            ReportId = report.Id,
            DatasetsOfReportsId = datasetInfo.Id
        };
        _context.DatasetReports.Add(reportLink);
        await _context.SaveChangesAsync();

        return reportLink;
    }

    public async Task<bool> RemoveDatasetFromReportAsync(int reportId, ModuleType moduleType, int id_dataset, string username)
    {
        var linkToRemove = await _context.DatasetReports
            .FirstOrDefaultAsync(link =>
                link.ReportId == reportId &&
                link.Report.Username == username &&
                link.DatasetsOfReports.ModuleType == moduleType &&
                link.DatasetsOfReports.id_dataset == id_dataset);

        if (linkToRemove == null)
        {
            return false;
        }

        _context.DatasetReports.Remove(linkToRemove);
        await _context.SaveChangesAsync();

        return true;
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

        var config = JsonSerializer.Deserialize<ReportJsonConfig>(report.JSON_config, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var finalResults = new List<dynamic>();

        
        foreach (var sourceConfig in config.Sources ?? new List<ReportSourceConfig>())
        {

            IEnumerable<dynamic> rawData;
            switch (sourceConfig.SourceType.ToLower())
            {
                case "join":
                    if (!sourceConfig.SourceId.HasValue) continue;
                    rawData = await _joinConfigService.ExecuteJoinAsync(sourceConfig.SourceId.Value);
                    break;

                case "dataset":
                    if (!sourceConfig.SourceId.HasValue || !sourceConfig.SourceModule.HasValue) continue;
                    var operand = new JoinOperand
                    {
                        ModuleType = sourceConfig.SourceModule.Value,
                        DatasetId = sourceConfig.SourceId.Value,
                        EntityName = sourceConfig.EntityName.Value
                    };
                    rawData = await _apiDataService.GetDataForOperand(operand, username);
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

                foreach (var column in sourceConfig.Columns)
                {
                    if (rowAsDictionary.TryGetValue(column.Attribute, out object value))
                    {
                        projectedRow[column.As] = value;
                    }
                    else
                    {
                        projectedRow[column.As] = null;
                    }
                }
                finalResults.Add(projectedRow);
            }
        }

        return finalResults;
    }

    private IDictionary<string, object> ObjectToDictionary(object obj)
    {
        if (obj == null) return new Dictionary<string, object>();

        // Si el objeto ya es un diccionario (como un ExpandoObject), lo retornamos directamente.
        if (obj is IDictionary<string, object> dict)
        {
            return dict;
        }

        // Si es una clase estándar (como Device), usamos reflexión para convertirlo en un diccionario.
        var dictionary = new Dictionary<string, object>();
        foreach (var property in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            dictionary[property.Name] = property.GetValue(obj);
        }
        return dictionary;
    }
}