using Microsoft.EntityFrameworkCore;
using OmniMonitor.Server.Context;
using OmniMonitor.Shared.Dtos;
using System.Dynamic;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OmniMonitor.Server.Services;

/// <summary>
/// Servicio para la gestión y ejecución de reportes.
/// </summary>
public interface IReportService
{
    /// <summary>
    /// Crea un nuevo reporte.
    /// </summary>
    /// <param name="request">Datos para la creación del reporte.</param>
    /// <returns>El reporte creado.</returns>
    Task<Report> CreateReportAsync(CreateReportRequestDto request);

    /// <summary>
    /// Crea y agrega un join a un reporte.
    /// </summary>
    /// <param name="reportId">ID del reporte.</param>
    /// <param name="joinRequest">Datos del join a agregar.</param>
    /// <param name="username">Nombre de usuario.</param>
    /// <returns>El join agregado al reporte.</returns>
    Task<ReportJoin> CreateAndAddJoinToReportAsync(int reportId, CreateJoinRequestDto joinRequest, string username);

    /// <summary>
    /// Obtiene todos los reportes de un usuario.
    /// </summary>
    /// <param name="username">Nombre de usuario.</param>
    /// <returns>Lista de reportes.</returns>
    Task<List<Report>> GetAllReportsByUsernameAsync(string username);

    /// <summary>
    /// Obtiene un reporte por ID y usuario.
    /// </summary>
    /// <param name="reportId">ID del reporte.</param>
    /// <param name="username">Nombre de usuario.</param>
    /// <returns>El reporte encontrado o null si no existe.</returns>
    Task<Report?> GetReportByIdAsync(int reportId, string username);

    /// <summary>
    /// Elimina un reporte.
    /// </summary>
    /// <param name="reportId">ID del reporte.</param>
    /// <param name="username">Nombre de usuario.</param>
    /// <returns>True si se eliminó, false si no existe o no pertenece al usuario.</returns>
    Task<bool> DeleteReportAsync(int reportId, string username);

    /// <summary>
    /// Actualiza un reporte.
    /// </summary>
    /// <param name="reportId">ID del reporte.</param>
    /// <param name="name">Nuevo nombre.</param>
    /// <param name="descripcion">Nueva descripción.</param>
    /// <param name="username">Nombre de usuario.</param>
    /// <param name="jsonConfig">Nueva configuración JSON.</param>
    /// <returns>El reporte actualizado o null si no existe.</returns>
    Task<Report?> UpdateReportAsync(int reportId, string name, string descripcion, string username, string jsonConfig);

    /// <summary>
    /// Elimina un join de un reporte.
    /// </summary>
    /// <param name="reportId">ID del reporte.</param>
    /// <param name="joinId">ID del join.</param>
    /// <param name="username">Nombre de usuario.</param>
    /// <returns>True si se eliminó, false si no existe o no pertenece al usuario.</returns>
    Task<bool> RemoveJoinFromReportAsync(int reportId, int joinId, string username);

    /// <summary>
    /// Ejecuta un reporte y devuelve los resultados.
    /// </summary>
    /// <param name="reportId">ID del reporte.</param>
    /// <param name="username">Nombre de usuario.</param>
    /// <returns>Lista de resultados dinámicos del reporte.</returns>
    Task<List<dynamic>> ExecuteReportAsync(int reportId, string username);
}

public class ReportService : IReportService
{
    #region Campos privados

    private readonly ApplicationDbContext _context;
    private readonly IJoinConfigurationService _joinConfigService;
    private readonly IApiDataService _apiDataService;
    private readonly ISondaAuthService _sondaAuthService;
    private readonly ISondaIMService _sondaIMService;

    #endregion

    #region Constructor

    /// <summary>
    /// Constructor de ReportService.
    /// </summary>
    /// <param name="context">Contexto de base de datos.</param>
    /// <param name="joinConfigurationService">Servicio de joins.</param>
    /// <param name="apiDataService">Servicio de datos dinámicos.</param>
    /// <param name="sondaAuthService">Servicio de autenticación.</param>
    /// <param name="sondaIMService">Servicio de IM.</param>
    public ReportService(
        ApplicationDbContext context,
        IJoinConfigurationService joinConfigurationService,
        IApiDataService apiDataService,
        ISondaAuthService sondaAuthService,
        ISondaIMService sondaIMService)
    {
        _context = context;
        _joinConfigService = joinConfigurationService;
        _apiDataService = apiDataService;
        _sondaAuthService = sondaAuthService;
        _sondaIMService = sondaIMService;
    }

    #endregion

    #region Métodos públicos

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public async Task<ReportJoin> CreateAndAddJoinToReportAsync(int reportId, CreateJoinRequestDto joinRequest, string username)
    {
        var report = await _context.Reports
            .FirstOrDefaultAsync(r => r.Id == reportId && r.Username.ToLower() == username.ToLower());

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

    /// <inheritdoc/>
    public async Task<List<Report>> GetAllReportsByUsernameAsync(string username)
    {
        return await _context.Reports
            .AsNoTracking()
            .Where(r => r.Username.ToLower() == username.ToLower())
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<Report?> GetReportByIdAsync(int reportId, string username)
    {
        return await _context.Reports
            .AsNoTracking()
            .Where(r => r.Id == reportId && r.Username.ToLower() == username.ToLower())
            .Include(r => r.ReportJoins)
                .ThenInclude(rj => rj.CrossModuleJoin)
                    .ThenInclude(j => j.LeftOperand)
            .Include(r => r.ReportJoins)
                .ThenInclude(rj => rj.CrossModuleJoin)
                    .ThenInclude(j => j.RightOperand)
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteReportAsync(int reportId, string username)
    {
        var reportToDelete = await _context.Reports
            .FirstOrDefaultAsync(r => r.Id == reportId && r.Username.ToLower() == username.ToLower());

        if (reportToDelete == null)
        {
            return false;
        }

        _context.Reports.Remove(reportToDelete);
        await _context.SaveChangesAsync();

        return true;
    }

    /// <inheritdoc/>
    public async Task<Report?> UpdateReportAsync(int reportId, string name, string descripcion, string username, string jsonConfig)
    {
        var reportToUpdate = await _context.Reports
            .FirstOrDefaultAsync(r => r.Id == reportId && string.Equals(r.Username, username, System.StringComparison.Ordinal));

        if (reportToUpdate == null)
        {
            return null;
        }

        reportToUpdate.Name = name;
        reportToUpdate.Description = descripcion;
        reportToUpdate.JSON_config = jsonConfig;

        await _context.SaveChangesAsync();

        return reportToUpdate;
    }

    /// <inheritdoc/>
    public async Task<bool> RemoveJoinFromReportAsync(int reportId, int joinId, string username)
    {
        var joinLinkToRemove = await _context.ReportJoins
            .FirstOrDefaultAsync(rj =>
                rj.ReportId == reportId &&
                rj.CrossModuleJoinId == joinId &&
                string.Equals(rj.Report.Username, username, System.StringComparison.Ordinal));

        if (joinLinkToRemove == null)
        {
            return false;
        }

        _context.ReportJoins.Remove(joinLinkToRemove);
        await _context.SaveChangesAsync();

        return true;
    }

    /// <inheritdoc/>
    public async Task<List<dynamic>> ExecuteReportAsync(int reportId, string username)
    {
        var report = await _context.Reports.FirstOrDefaultAsync(r => r.Id == reportId && r.Username.ToLower() == username.ToLower());
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
            IEnumerable<dynamic> rawData = null;
            switch (sourceConfig.SourceType.ToLower())
            {
                case "join":
                    if (!sourceConfig.SourceId.HasValue)
                    {
                        continue;
                    }
                    rawData = await _joinConfigService.ExecuteJoinAsync(sourceConfig.SourceId.Value);
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

    #endregion

    #region Métodos privados

    /// <summary>
    /// Agrega un prefijo a cada propiedad de los datos del dataset.
    /// </summary>
    /// <param name="datasetData">Datos del dataset.</param>
    /// <param name="prefix">Prefijo a agregar.</param>
    /// <returns>Lista de objetos dinámicos con prefijo.</returns>
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

    /// <summary>
    /// Convierte un objeto a un diccionario de propiedades.
    /// </summary>
    /// <param name="obj">Objeto a convertir.</param>
    /// <returns>Diccionario de propiedades.</returns>
    private IDictionary<string, object> ObjectToDictionary(object obj)
    {
        if (obj == null)
        {
            return new Dictionary<string, object>(System.StringComparer.OrdinalIgnoreCase);
        }

        if (obj is IDictionary<string, object> dict)
        {
            return new Dictionary<string, object>(dict, System.StringComparer.OrdinalIgnoreCase);
        }

        var dictionary = new Dictionary<string, object>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var property in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            dictionary[property.Name] = property.GetValue(obj) ?? default!;
        }
        return dictionary;
    }

    #endregion
}