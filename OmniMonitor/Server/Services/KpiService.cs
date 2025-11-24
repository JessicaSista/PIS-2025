using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.EntityFrameworkCore;

using OmniMonitor.Server.Context;
using OmniMonitor.Shared.Dtos;
using OmniMonitor.Shared.Dtos.AM;
using OmniMonitor.Shared.Dtos.EM;

namespace OmniMonitor.Server.Services
{
    public interface IKpiService
    {
        Task<Kpi> CreateKpiAsync(KpiRequest request, string? username = null);
        Task<Kpi> GetKpiDefinitionAsync(int kpiId);
        Task<KpiResponse> CalculateKpiValueAsync(int kpiId, string username);
        Task<KpiResponse> CalculateKpiValueAsyncSinToken(int kpiId);
        Task<KpiResponse> CalculateKpiDataAsync(KpiRequest kpiData, string username);
        Task<List<KpiResponse>> CalculateAllKpisForUserAsync(string username);
        Task<List<Kpi>> GetAllKpisForUserAsync(string username);
        Task<List<MetricInfo>> GetMetricInfoListAsync(string sourceModule);
        Task DeleteKpiAsync(int kpiId, string? username = null);
        Task<Kpi> UpdateKpiAsync(int kpiId, KpiRequest request, string? username = null);
    Task<List<string>> GetFieldValuesAsync(int datasetId, string modulo, string campo, int choice, string username);
        Task<KpiSimplePaginatedResponse> GetAllKpisPaginatedAsync(string username, int page = 1, int pageSize = 10, string? query = null);

    }

    public class KpiService : IKpiService
    {
    private readonly ApplicationDbContext _context;
    private readonly IDatasetService _datasetService;
    private readonly ISondaEMService _sondaEMService;
    private readonly ISondaIMService _sondaIMService;
    private readonly ISondaAuthService _sondaAuthService;
    private readonly IKpiAMService _kpiAMService;
    private readonly IDatasetAmService _datasetAmService;
    private readonly IDatasetUMService _datasetUMService;
    private readonly ISondaUMService _sondaUMService;
    private readonly ISondaAMService _sondaAMService;
    private readonly IDatasetEMService _datasetEmService;

        public KpiService(ApplicationDbContext context, IDatasetService datasetService, ISondaEMService sondaEMService, ISondaIMService sondaIMService, ISondaAuthService sondaAuthService, IKpiAMService kpiAMService, IDatasetAmService datasetAmService, IDatasetUMService datasetUMService, ISondaUMService sondaUMService, ISondaAMService sondaAMService, IDatasetEMService datasetEmService)
        {
            _context = context;
            _datasetService = datasetService;
            _sondaEMService = sondaEMService;
            _sondaIMService = sondaIMService;
            _kpiAMService = kpiAMService;
            _sondaAuthService = sondaAuthService;
            _datasetAmService = datasetAmService;
            _datasetUMService = datasetUMService;
            _sondaUMService = sondaUMService;
            _sondaAMService = sondaAMService;
            _datasetEmService = datasetEmService;
        }

        private async Task ValidateDuplicateKpiNameAsync(string name, string username, int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(username))
            {
                return;
            }

            var normalizedName = name.Trim().ToLower();

            var query = _context.Kpi.AsQueryable()
                .Where(k => k.Username == username && k.Name.ToLower() == normalizedName);

            if (excludeId.HasValue)
            {
                query = query.Where(k => k.Id != excludeId.Value);
            }

            if (await query.AnyAsync())
            {
                throw new ArgumentException($"Ya existe un KPI con el nombre '{name}'.");
            }
        }

        private void ValidateColorRangesOrThrow(string? colorRanges)
        {
            if (string.IsNullOrWhiteSpace(colorRanges))
            {
                return;
            }

            try
            {
                var ranges = JsonSerializer.Deserialize<List<ColorRange>>(colorRanges);
                if (ranges == null || ranges.Count == 0)
                {
                    throw new ArgumentException("ColorRanges inválido o vacío.", nameof(colorRanges));
                }

                foreach (var range in ranges)
                {
                    if (range.min > range.max)
                    {
                        throw new ArgumentException("El mínimo debe ser menor o igual al máximo en cada rango de color.");
                    }
                }
            }
            catch (JsonException)
            {
                throw new ArgumentException("ColorRanges no es JSON válido.", nameof(colorRanges));
            }
        }

        public async Task<Kpi> CreateKpiAsync(KpiRequest request, string? username = null)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("KPI name is required.");

            if (string.IsNullOrWhiteSpace(request.SourceModule))
                throw new ArgumentException("SourceModule is required.");

            if (request.DatasetId == null)
                throw new ArgumentException("DatasetId is required.");

            await ValidateDuplicateKpiNameAsync(request.Name, username ?? string.Empty);
            ValidateColorRangesOrThrow(request.ColorRanges);

            switch (request.SourceModule.ToUpperInvariant())
            {
                case "IM":
                    await ValidateImKpiRequestAsync(request, username);
                    break;

               //default:
               //    throw new ArgumentException($"Unsupported SourceModule: {request.SourceModule}");
            }
            bool requestedLive = request.LiveEnabled ?? false;
            bool allowLive = requestedLive;
            if (requestedLive)
            {
                string? module = request.SourceModule?.Trim().ToUpperInvariant();
                if (module == "AM" || module == "UM")
                {
                    allowLive = false;
                }
            }

            var newKpi = new Kpi
            {
                Name = request.Name,
                Description = request.Description,
                SourceModule = request.SourceModule,
                DatasetId = request.DatasetId.Value,
                Unit = request.Unit,
                Metric = request.Metric,
                Multiplier = request.Multiplier,
                DefaultColor = request.DefaultColor,
                ColorRanges = request.ColorRanges,
                ExtraInfo = request.ExtraInfo,
                Atributo = string.IsNullOrWhiteSpace(request.Atributo) ? string.Empty : request.Atributo,
                Username = username ?? string.Empty,
                Type = request.Type,
                LiveEnabled = allowLive,
                Link = request.Link
            };

            _context.Kpi.Add(newKpi);
            await _context.SaveChangesAsync();

            return newKpi;
        }

        private async Task ValidateImKpiRequestAsync(KpiRequest request, string? username)
        {
            // 1. ExtraInfo required
            if (string.IsNullOrEmpty(request.ExtraInfo))
                throw new ArgumentException("ExtraInfo is required for IM KPIs.");

            // 2. Parse dates (accept both dateFrom/dateTo and startDate/endDate)
            DateTime dateFrom, dateTo;
            try
            {
                var extra = JsonSerializer.Deserialize<Dictionary<string, string>>(request.ExtraInfo);
                if (extra == null)
                    throw new FormatException("ExtraInfo could not be parsed.");

                if (extra.ContainsKey("dateFrom") && extra.ContainsKey("dateTo"))
                {
                    dateFrom = DateTime.Parse(extra["dateFrom"], null, System.Globalization.DateTimeStyles.RoundtripKind);
                    dateTo = DateTime.Parse(extra["dateTo"], null, System.Globalization.DateTimeStyles.RoundtripKind);
                }
                else
                {
                    throw new ArgumentException("ExtraInfo must contain dateFrom/dateTo or startDate/endDate.");
                }
            }
            catch (Exception ex) when (ex is JsonException || ex is FormatException || ex is ArgumentException)
            {
                throw new ArgumentException($"Invalid ExtraInfo date format: {ex.Message}", ex);
            }

            // 2.1 Validate ordering: dateFrom must be <= dateTo
            if (dateFrom > dateTo)
                throw new ArgumentException("Invalid date range: 'dateFrom' must be earlier than or equal to 'dateTo'.");

            // 3. Validate dataset exists (assumes DatasetIM is a DbSet in your context)
            var dataset = await _context.Set<DatasetIM>().FindAsync(request.DatasetId);
            if (dataset == null)
                throw new InvalidOperationException($"Dataset with ID {request.DatasetId} not found.");

            // 4. Validate source and devices
            var source = await _sondaIMService.GetSourceById((int)dataset.Id_Source, username);
            if (source == null)
                throw new InvalidOperationException($"Source with ID {dataset.Id_Source} not found.");

            if (source.Devices == null || source.Devices.Count == 0)
                throw new InvalidOperationException($"No devices found for source {dataset.Id_Source}.");

            // 5. Validate sensor presence
            bool sensorFound = false;
            foreach (var deviceSummary in source.Devices)
            {
                var device = await _sondaIMService.GetDeviceById(deviceSummary.Id, username);
                if (device?.Sensors == null) continue;
                if (device.Sensors.Any(s => s.Name.Equals(dataset.SensorName, StringComparison.OrdinalIgnoreCase)))
                {
                    sensorFound = true;
                    break;
                }
            }

            if (!sensorFound)
                throw new InvalidOperationException($"Sensor '{dataset.SensorName}' not found in source {dataset.Id_Source}.");

            // 6. Validate metric supported for IM
            var metric = request.Metric?.ToLowerInvariant();
            var supportedMetrics = new[] { "lastvalue", "average", "min", "max" };
            if (!supportedMetrics.Any(m => string.Equals(m, metric, StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException($"Unsupported metric '{request.Metric}' for IM KPIs.");
        }

        public async Task DeleteKpiAsync(int kpiId, string? username = null)
        {
            var kpi = await _context.Kpi.FirstOrDefaultAsync(k => k.Id == kpiId);

            if (kpi == null)
                throw new KeyNotFoundException($"No se encontró el KPI con ID {kpiId}.");

            //verificar que el usuario sea dueño del KPI
            if (!string.IsNullOrEmpty(username) && kpi.Username != username)
                throw new UnauthorizedAccessException("No tiene permisos para eliminar este KPI.");

            // Eliminar todas las referencias del KPI en los dashboards (GrupoVisualizacion)
            var dashboardReferences = await _context.GrupoVisualizaciones
                .Where(gv => gv.KpiId == kpiId)
                .ToListAsync();

            if (dashboardReferences.Any())
            {
                _context.GrupoVisualizaciones.RemoveRange(dashboardReferences);
            }

            _context.Kpi.Remove(kpi);
            await _context.SaveChangesAsync();
        }

        public async Task<Kpi> UpdateKpiAsync(int kpiId, KpiRequest request, string? username = null)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var existingKpi = await _context.Kpi.FindAsync(kpiId);
            if (existingKpi == null)
                throw new KeyNotFoundException($"No se encontró el KPI con ID {kpiId}.");

            if (!string.IsNullOrEmpty(username) && existingKpi.Username != username)
                throw new UnauthorizedAccessException("No tiene permisos para editar este KPI.");

            if (request.Name != null && string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Name provisto pero vacío.", nameof(request.Name));

            if (request.Name != null && !request.Name.Equals(existingKpi.Name, StringComparison.OrdinalIgnoreCase))
            {
                await ValidateDuplicateKpiNameAsync(request.Name, existingKpi.Username, existingKpi.Id);
            }

            if (request.SourceModule != null && string.IsNullOrWhiteSpace(request.SourceModule))
                throw new ArgumentException("SourceModule provisto pero vacío.", nameof(request.SourceModule));

            if (request.DatasetId.HasValue && request.DatasetId.Value <= 0)
                throw new ArgumentOutOfRangeException(nameof(request.DatasetId), "DatasetId debe ser mayor que 0.");

            if (request.Unit != null && string.IsNullOrWhiteSpace(request.Unit))
                throw new ArgumentException("Unit provisto pero vacío.", nameof(request.Unit));

            if (request.Metric != null && string.IsNullOrWhiteSpace(request.Metric))
                throw new ArgumentException("Metric provisto pero vacío.", nameof(request.Metric));

            if (request.Multiplier.HasValue && request.Multiplier.Value <= 0)
                throw new ArgumentOutOfRangeException(nameof(request.Multiplier), "Multiplier debe ser mayor que 0.");

            if (request.DefaultColor != null && !IsValidHexColor(request.DefaultColor))
                throw new ArgumentException("DefaultColor no es un color hex válido (ej. #RRGGBB).", nameof(request.DefaultColor));

            if (request.ColorRanges != null)
            {
                ValidateColorRangesOrThrow(request.ColorRanges);
            }

            // Si la métrica (efectiva) requiere extraInfo (rango de fechas), validamos que exista y sea correcto.
            var effectiveMetric = (request.Metric ?? existingKpi.Metric)?.Trim().ToLower();
            if (!string.IsNullOrEmpty(effectiveMetric) &&
                (effectiveMetric == "average" || effectiveMetric == "min" || effectiveMetric == "max" ||
                 effectiveMetric == "minvalue" || effectiveMetric == "maxvalue"))
            {
                // extraInfo puede venir en el request (si se está actualizando) o ya existir en el KPI
                var extraInfoToCheck = request.ExtraInfo ?? existingKpi.ExtraInfo;
                if (string.IsNullOrWhiteSpace(extraInfoToCheck))
                    throw new ArgumentException($"ExtraInfo requerida para la métrica '{effectiveMetric}'.", nameof(request.ExtraInfo));

                try
                {
                    var extra = JsonSerializer.Deserialize<Dictionary<string, string>>(extraInfoToCheck);
                    if (extra == null || !extra.ContainsKey("dateFrom") || !extra.ContainsKey("dateTo"))
                        throw new ArgumentException("ExtraInfo debe contener dateFrom y dateTo en formato ISO.", nameof(request.ExtraInfo));

                    // intentamos parsear fechas
                    DateTime.Parse(extra["dateFrom"], null, System.Globalization.DateTimeStyles.RoundtripKind);
                    DateTime.Parse(extra["dateTo"], null, System.Globalization.DateTimeStyles.RoundtripKind);
                }
                catch (JsonException)
                {
                    throw new ArgumentException("ExtraInfo no es JSON válido.", nameof(request.ExtraInfo));
                }
                catch (FormatException)
                {
                    throw new ArgumentException("Las fechas en ExtraInfo no tienen un formato válido (ISO).", nameof(request.ExtraInfo));
                }
            }

            if (request.Name != null) existingKpi.Name = request.Name.Trim();
            if (request.Description != null) existingKpi.Description = request.Description.Trim();
            if (request.SourceModule != null) existingKpi.SourceModule = request.SourceModule.Trim();
            if (request.DatasetId.HasValue) existingKpi.DatasetId = request.DatasetId.Value;
            if (request.Unit != null) existingKpi.Unit = request.Unit.Trim();
            if (request.Metric != null) existingKpi.Metric = request.Metric.Trim();
            if (request.Multiplier.HasValue) existingKpi.Multiplier = request.Multiplier.Value;
            if (request.DefaultColor != null) existingKpi.DefaultColor = request.DefaultColor.Trim();
            if (request.ColorRanges != null) existingKpi.ColorRanges = request.ColorRanges;
            if (request.ExtraInfo != null) existingKpi.ExtraInfo = request.ExtraInfo;
            // Link: update if provided (null means don't update, empty string means remove link)
            if (request.Link != null)
            {
                existingKpi.Link = string.IsNullOrWhiteSpace(request.Link) ? null : request.Link;
            }
            if (request.LiveEnabled.HasValue)
            {
                bool requestedLive = request.LiveEnabled.Value;
                string effectiveModule = (request.SourceModule ?? existingKpi.SourceModule)!.Trim().ToUpperInvariant();
                if (requestedLive && (effectiveModule == "AM" || effectiveModule == "UM"))
                {
                    existingKpi.LiveEnabled = false;
                }
                else
                {
                    existingKpi.LiveEnabled = requestedLive;
                }
            }

            _context.Kpi.Update(existingKpi);
            await _context.SaveChangesAsync();

            return existingKpi;
        }

        private string? NormalizeHexColor(string? color)
        {
            if (string.IsNullOrWhiteSpace(color)) return null;
            color = color.Trim();

            // #RGB -> #RRGGBB
            var mShort = Regex.Match(color, @"^#([0-9A-Fa-f]{3})$");
            if (mShort.Success)
            {
                var s = mShort.Groups[1].Value;
                return $"#{s[0]}{s[0]}{s[1]}{s[1]}{s[2]}{s[2]}".ToUpperInvariant();
            }

            // #RRGGBB -> ok
            var mLong = Regex.Match(color, @"^#([0-9A-Fa-f]{6})$");
            if (mLong.Success)
                return color.ToUpperInvariant();

            // #RRGGBBAA -> strip alpha and return #RRGGBB
            var mWithAlpha = Regex.Match(color, @"^#([0-9A-Fa-f]{8})$");
            if (mWithAlpha.Success)
            {
                var hex8 = mWithAlpha.Groups[1].Value;
                var rgb = hex8.Substring(0, 6);
                return $"#{rgb}".ToUpperInvariant();
            }

            return null;
        }

        private bool IsValidHexColor(string? color)
        {
            return NormalizeHexColor(color) != null;
        }

        public async Task<Kpi> GetKpiDefinitionAsync(int kpiId)
        {
            var kpi = await _context.Kpi.FindAsync(kpiId);

            if (kpi == null)
                throw new ArgumentException($"No se encontró el KPI con ID {kpiId}");

            return kpi;
        }

        public async Task<KpiResponse> CalculateKpiValueAsync(int kpiId, string username)
        {
            var kpi = await GetKpiDefinitionAsync(kpiId);

            KpiResponse? response = null;

            switch (kpi.SourceModule.ToUpper())
            {
                case "AM":
                    response = await CalculateAmKpiAsync(kpi, username);
                    break;

                case "EM":
                    response = await CalculateEmKpiAsync(kpi, username);
                    break;

                case "IM":
                    response = await CalculateImKpiAsync(kpi, username);
                    break;

                case "UM":
                    response = await CalculateUmKpiAsync(kpi, username);
                    break;

                default:
                    throw new ArgumentException($"SourceModule no soportado: {kpi.SourceModule}");
            }

            if (response == null)
                throw new Exception($"No se pudo calcular el KPI con ID {kpiId}");

            response.DatasetName = await GetDatasetNameFromModuleAsync(kpi.DatasetId, kpi.SourceModule, username);
            response.LiveEnabled = kpi.LiveEnabled;
            response.SourceModule = kpi.SourceModule;
            response.Link = kpi.Link;
            return response;
        }

        public async Task<KpiResponse> CalculateKpiValueAsyncSinToken(int kpiId)
        {
            var kpi = await GetKpiDefinitionAsync(kpiId);

            KpiResponse? response = null;

            switch (kpi.SourceModule.ToUpper())
            {
                case "AM":
                    response = await CalculateAmKpiAsync(kpi, kpi.Username);
                    break;

                case "EM":
                    response = await CalculateEmKpiAsync(kpi, kpi.Username);
                    break;

                case "IM":
                    response = await CalculateImKpiAsync(kpi, kpi.Username);
                    break;

                case "UM":
                    response = await CalculateUmKpiAsync(kpi, kpi.Username);
                    break;

                default:
                    throw new ArgumentException($"SourceModule no soportado: {kpi.SourceModule}");
            }

            if (response == null)
                throw new Exception($"No se pudo calcular el KPI con ID {kpiId}");

            response.DatasetName = await GetDatasetNameFromModuleAsync(kpi.DatasetId, kpi.SourceModule, kpi.Username ?? string.Empty);
            response.LiveEnabled = kpi.LiveEnabled;
            response.SourceModule = kpi.SourceModule;
            response.Link = kpi.Link;
            return response;
        }

        public async Task<KpiResponse> CalculateKpiDataAsync(KpiRequest kpiData, string username)
        {
            // Crear un objeto Kpi temporal a partir de los datos del request
            var tempKpi = new Kpi
            {
                Name = kpiData.Name ?? "Temp KPI",
                Description = kpiData.Description,
                SourceModule = kpiData.SourceModule ?? "IM",
                DatasetId = kpiData.DatasetId ?? 0,
                Unit = kpiData.Unit,
                Metric = kpiData.Metric,
                Multiplier = kpiData.Multiplier ?? 1.0,
                DefaultColor = kpiData.DefaultColor ?? "#000000",
                Atributo = kpiData.Atributo ?? "",
                ExtraInfo = kpiData.ExtraInfo,
                Type = kpiData.Type,
                Username = username
            };

            KpiResponse? response = null;

            switch (tempKpi.SourceModule.ToUpper())
            {
                case "AM":
                    response = await CalculateAmKpiAsync(tempKpi, username);
                    break;

                case "EM":
                    response = await CalculateEmKpiAsync(tempKpi, username);
                    break;

                case "IM":
                    response = await CalculateImKpiAsync(tempKpi, username);
                    break;

                case "UM":
                    response = await CalculateUmKpiAsync(tempKpi, username);
                    break;

                default:
                    throw new ArgumentException($"SourceModule no soportado: {tempKpi.SourceModule}");
            }

            if (response == null)
                throw new Exception($"No se pudo calcular el KPI con los datos proporcionados");

            return response;
        }

        public async Task<List<KpiResponse>> CalculateAllKpisForUserAsync(string username)
        {
            // Obtener todos los KPIs del usuario desde la base de datos
            var kpis = await _context.Kpi
                .Where(k => k.Username == username)
                .ToListAsync();

            var results = new List<KpiResponse>();

            foreach (var kpi in kpis)
            {
                try
                {
                    var response = await CalculateKpiValueAsync(kpi.Id, username);
                    results.Add(response);
                }
                catch (Exception ex)
                {
                    results.Add(BuildNoDataResponse(kpi, ex.Message));
                }
            }

            return results;
        }

        public async Task<List<Kpi>> GetAllKpisForUserAsync(string username)
        {
            // Obtener todos los KPIs del usuario desde la base de datos
            return await _context.Kpi
                .Where(k => k.Username == username)
                .ToListAsync();
        }

        private async Task<KpiResponse> CalculateImKpiAsync(Kpi kpi, string username)
        {
            // Obtener dataset asociado al KPI
            var dataset = await _datasetService.GetDatasetIMByIdAsync(kpi.DatasetId, kpi.Username);

            if (dataset == null)
                throw new Exception($"No se encontró el dataset con ID {kpi.DatasetId} para el KPI {kpi.Name}");

            KpiResponse? response;

            switch (kpi.Metric?.ToLower())
            {
                case "lastvalue":
                    response = await CalculateLastValueIM(kpi, dataset, username);
                    break;

                case "average":
                    response = await CalculateAverageKpiIMAsync(kpi,dataset, username);
                    break;

                case "min":
                    response = await CalculateMinKpiIMAsync(kpi, dataset, username);
                    break;

                case "max":
                    response = await CalculateMaxKpiIMAsync(kpi, dataset, username);
                    break;

                default:
                    throw new ArgumentException($"Métrica no soportada para IM: {kpi.Metric}");
            }

            return response;
        }


        // Funciones privadas por módulo
        private async Task<KpiResponse> CalculateAmKpiAsync(Kpi kpi, string username)
        {
            // Lógica para AM según Type
            var dataset = await _datasetAmService.GetDatasetAMByIdAsync(kpi.DatasetId, username);
            if (dataset == null)
            {
                return BuildNoDataResponse(kpi, "Dataset no encontrado");
            }
            KpiResponse? response;
            if (kpi.Type == 1)
            {
                // Buscar assets relacionados al dataset
                var reducedAssets = await _datasetAmService.GetReducedAssetsByDatasetIdAsync(kpi.DatasetId, username);
                response = await _kpiAMService.CalculateAmKpiAsync(kpi, username, reducedAssets);
                return response;
            }
            else if (kpi.Type == 2)
            {
                // Buscar event task instances relacionados al dataset
                /*var eventTasks = new List<EventTaskInstanceDto>();
                if (dataset.Grupo_Event_Task_Instance != null)
                {
                    foreach (var etiRef in dataset.Grupo_Event_Task_Instance)
                    {
                        var eventTask = await _sondaAMService.GetEventTaskInstanceById(etiRef.Id_Event_Task_Instance, username);
                        if (eventTask != null)
                            eventTasks.Add(eventTask);
                    }
                }
                // Imprimir la lista enviada
                foreach (var et in eventTasks)
                {
                }*/
                var eventTasks = await _datasetAmService.GetReducedEventsByDatasetIdAsync(kpi.DatasetId, username);
                var result = await _kpiAMService.CalculateAmKpiAsync(kpi, username, eventTasks);
                // Imprimir el resultado devuelto
                return result;
            } else if (kpi.Type == 3) // Stock
            {
                        var datasetAM = await _datasetAmService.GetDatasetAMByIdAsync(kpi.DatasetId, username);
                        var stockData = new List<OmniMonitor.Shared.Dtos.ReducedStockDatasetAM>();
                        if (datasetAM.Grupo_Stock != null)
                        {
                            foreach (var dsStock in datasetAM.Grupo_Stock)
                            {
                                var stockDto = await _sondaAMService.GetStockById(dsStock.Id_Stock, username);
                                if (stockDto != null)
                                {
                                    stockData.Add(new OmniMonitor.Shared.Dtos.ReducedStockDatasetAM
                                    {
                                        Nombre = stockDto.Name,
                                        Cantidad = stockDto.Quantity,
                                        Proveedor = stockDto.Provider?.Name ?? string.Empty,
                                        Sku = stockDto.Sku ?? string.Empty,
                                        Minimo = stockDto.Minimum,
                                        Bundle = stockDto.Bundle?.Name ?? stockDto.BundleId.ToString(),
                                        Supervisor = stockDto.Supervisor?.UserName ?? string.Empty
                                    });
                                }
                                else
                                {
                                }
                            }
                        }

                        if (kpi.Atributo.ToLower() != "cantidad" && kpi.Atributo.ToLower() != "minimo")
                        {
                            return await _kpiAMService.CalculateAmKpiAsync(kpi, username, stockData);
                        } else {
                            
                            var aux = ExtractFieldValuesFromLists(stockData, kpi.Atributo);
                            switch (kpi.Metric.ToLower())
                            {
                                case "count":
                                    if (aux is IEnumerable<float> floatList)
                                    {
                                        float suma = floatList.Sum();
                                        float sumaFinal = suma * (float)(kpi.Multiplier ?? 1);
                                        string color = kpi.DefaultColor;
                                        if (!string.IsNullOrEmpty(kpi.ColorRanges))
                                        {
                                            color = GetColorForValue(kpi.ColorRanges, sumaFinal, kpi.DefaultColor);
                                        }
                                        else
                                        {
                                        }
                                        return new KpiResponse
                                        {
                                            Id = kpi.Id,
                                            Name = kpi.Name,
                                            ActualColor = color,
                                            Value = sumaFinal
                                        };
                                    }
                                    return new KpiResponse
                                    {
                                        Id = kpi.Id,
                                        Name = kpi.Name,
                                        ActualColor = kpi.DefaultColor,
                                        Value = 0 * (kpi.Multiplier ?? 1)
                                    };
                                case "percentage":
                                    if (aux is IEnumerable<float> floatListAvg && floatListAvg.Any())
                                    {
                                        double promedio = floatListAvg.Average();
                                        double promedioFinal = promedio * (kpi.Multiplier ?? 1);
                                        string color = kpi.DefaultColor;
                                        if (!string.IsNullOrEmpty(kpi.ColorRanges))
                                        {
                                            color = GetColorForValue(kpi.ColorRanges, promedioFinal, kpi.DefaultColor);
                                        }
                                        else
                                        {
                                        }
                                        return new KpiResponse
                                        {
                                            Id = kpi.Id,
                                            Name = kpi.Name,
                                            ActualColor = color,
                                            Value = Math.Round(promedioFinal, 2)
                                        };
                                    }
                                    return new KpiResponse
                                    {
                                        Id = kpi.Id,
                                        Name = kpi.Name,
                                        ActualColor = kpi.DefaultColor,
                                        Value = 0 * (kpi.Multiplier ?? 1)
                                    };
                                default:
                                    return new KpiResponse
                                    {
                                        Id = kpi.Id,
                                        Name = kpi.Name,
                                        ActualColor = kpi.DefaultColor,
                                        Value = $"Atributo no soportado para Stock: {kpi.Atributo}"
                                    };
                            }
                        }
            } else {
                return new KpiResponse
                {
                    Name = kpi.Name,
                    ActualColor = kpi.DefaultColor,
                    Value = null
                };
            }
            // Otros tipos o default
            
        }

        private async Task<KpiResponse> CalculateEmKpiAsync(Kpi kpi, string username)
        {
            // Lógica para EM según Type
            var dataset = await _datasetEmService.GetDatasetEMByIdAsync(kpi.DatasetId, username);
            if (dataset == null)
            {
                return BuildNoDataResponse(kpi, "Dataset no encontrado");
            }

            if (kpi.Type == 1)
            {
                
                var alertIds = dataset.DatasetAlerts.Select(a => a.Id_alert).ToList();
                var alertDtos = new List<OmniMonitor.Shared.Dtos.EM.DatasetReducedAlertEMDTO>();
                foreach (var id in alertIds)
                        {
                            var alert = await _sondaEMService.GetAlertById(id, username);
                            if (alert != null)
                            {
                                alertDtos.Add(new OmniMonitor.Shared.Dtos.EM.DatasetReducedAlertEMDTO
                                {
                                    Nombre = alert.AlertName,
                                    Fuente = alert.SourceId.ToString(),
                                    Estado = alert.AlertState,
                                    SourceAddress = alert.SourceAddress
                                });
                            }
                        }


                return await _kpiAMService.CalculateAmKpiAsync(kpi, username, alertDtos);
            }
            else if (kpi.Type == 2)
            {
                // Buscar alertas relacionadas al dataset
                var eventIds = dataset.DatasetEvents.Select(e => e.Id_event).ToHashSet();
                var allEvents = await _sondaEMService.GetEvents(null, null, null, null, username);
                var eventDtos = new List<OmniMonitor.Shared.Dtos.EM.DatasetReducedEventEMDTO>();
                foreach (var evento in allEvents)
                {
                    if (eventIds.Contains(evento.Id))
                    {
                        eventDtos.Add(new OmniMonitor.Shared.Dtos.EM.DatasetReducedEventEMDTO
                        {
                            Nombre = evento.Name,
                            Origen = evento.Origin,
                            Estado = evento.State,
                            Direccion = evento.Address?.DisplayName
                        });
                    }
                }
                return await _kpiAMService.CalculateAmKpiAsync(kpi, username, eventDtos);
            }
            else if (kpi.Type == 3)
            {
                // Buscar extensiones relacionadas al dataset
                var extIds = dataset.DatasetExtensions.Select(x => x.Id_extension).ToList();
                        var extDtos = new List<OmniMonitor.Shared.Dtos.EM.DatasetReducedExtensionEMDTO>();
                        foreach (var id in extIds)
                        {
                            var extension = await _sondaEMService.GetExtensionById(id, username);
                            if (extension != null)
                            {
                                extDtos.Add(new OmniMonitor.Shared.Dtos.EM.DatasetReducedExtensionEMDTO
                                {
                                    Estado = extension.State,
                                    TakenBy = extension.TakenBy?.Name,
                                    CreatedBy = extension.CreatedBy?.Name,
                                    WorkZone = extension.WorkZoneName,
                                    Nombre = extension.EventName,
                                    Origen = extension.EventOrigin,
                                    Direccion = extension.Address?.DisplayName
                                });
                            }
                        }
                return await _kpiAMService.CalculateAmKpiAsync(kpi, username, extDtos);
            }
            // Otros tipos o default
            return new KpiResponse
            {
                Name = kpi.Name,
                ActualColor = kpi.DefaultColor,
                Value = null
            };
        }


        private async Task<KpiResponse> CalculateUmKpiAsync(Kpi kpi, string username)
        {
            // Lógica para UM según Type
            var datasetUM = await _datasetUMService.GetDatasetUMByIdAsync(kpi.DatasetId, username);
            if (datasetUM == null)
            {
                return BuildNoDataResponse(kpi, "Dataset no encontrado");
            }
            if (kpi.Type == 1)
            {
                // Buscar eventos por los IDs del dataset
                var eventIds = datasetUM.DatasetEvents.Select(e => e.Id_event).ToList();
                        var eventDtos = new List<OmniMonitor.Shared.Dtos.UM.DatasetReducedEventsUMDTO>();
                        foreach (var id in eventIds)
                        {
                            var evento = await _sondaUMService.GetEventById(id, username);
                            if (evento != null)
                            {
                                eventDtos.Add(new OmniMonitor.Shared.Dtos.UM.DatasetReducedEventsUMDTO
                                {
                                    Nombre = evento.Name,
                                    Descripcion = evento.Description,
                                    Tipo = evento.Type?.Name,
                                    Fecha = evento.Date?.ToString("yyyy-MM-dd HH:mm:ss"),
                                    Aprobacion = evento.ApprovalState == "Aprobado"
                                });
                            }
                        }
                return await _kpiAMService.CalculateAmKpiAsync(kpi, username, eventDtos);
            }
            else if (kpi.Type == 2)
            {
                // Buscar noticias por los IDs del dataset
                var newsIds = datasetUM.DatasetNews.Select(n => n.Id_news).ToList();
                        var newsDtos = new List<OmniMonitor.Shared.Dtos.UM.DatasetReducedNewsUMDTO>();
                        foreach (var id in newsIds)
                        {
                            var news = await _sondaUMService.GetNewsById(id, username);
                            if (news != null)
                            {
                                if (kpi.Atributo == "Categoria" && news.Categories != null)
                                {
                                    foreach (var category in news.Categories)
                                    {
                                        newsDtos.Add(new OmniMonitor.Shared.Dtos.UM.DatasetReducedNewsUMDTO
                                        {
                                            Titulo = news.Title,
                                            Resumen = news.Summary,
                                            Descripcion = news.Description,
                                            Categoria = category.Name
                                        });
                                    }
                                }
                                else
                                {
                                    newsDtos.Add(new OmniMonitor.Shared.Dtos.UM.DatasetReducedNewsUMDTO
                                    {
                                        Titulo = news.Title,
                                        Resumen = news.Summary,
                                        Descripcion = news.Description,
                                        Categoria = null
                                    });
                                }
                            }
                        }
                return await _kpiAMService.CalculateAmKpiAsync(kpi, username, newsDtos);
            }
            // Otros tipos o default
            return new KpiResponse
            {
                Name = kpi.Name,
                ActualColor = kpi.DefaultColor,
                Value = null
            };
        }


        private async Task<KpiResponse> CalculateLastValueIM(Kpi kpi, DatasetIM dataset, string username)
        {
            string? rawValue = null;
            string? type = null;
            var hasHistoricalRange = TryGetDateRange(kpi.ExtraInfo, out var rangeFrom, out var rangeTo);

            var source = await _sondaIMService.GetSourceById((int)dataset.Id_Source, username);
            if (source == null)
                throw new Exception($"No se encontró el source con ID {dataset.Id_Source}.");

            if (source.Devices == null || source.Devices.Count == 0)
                throw new Exception($"No se encontraron devices en el source {source.Id}.");

            // Buscar device que tenga el sensor
            int? deviceId = null;
            Sensor? foundSensor = null;
            foreach (var deviceSummary in source.Devices)
            {
                var device = await _sondaIMService.GetDeviceById(deviceSummary.Id, username);
                if (device?.Sensors == null)
                    continue;

                var sensor = device.Sensors.FirstOrDefault(s => s.Name.Equals(dataset.SensorName, StringComparison.OrdinalIgnoreCase));
                if (sensor != null)
                {
                    deviceId = device.Id;
                    type = sensor.Type;
                    foundSensor = sensor;
                    break;
                }
            }

            if (foundSensor == null || !deviceId.HasValue)
            {
                return new KpiResponse
                {
                    Id = kpi.Id,
                    Name = kpi.Name,
                    Description = kpi.Description,
                    Unit = kpi.Unit,
                    Type = null,
                    ActualColor = kpi.DefaultColor,
                    Value = null
                };
            }

            // Si hay rango de fechas histórico, buscar datos históricos en el device encontrado
            if (hasHistoricalRange)
            {
                var historicalData = await _sondaIMService.GetSensorDataByDate(deviceId.Value, dataset.SensorName, rangeFrom, rangeTo, username);
                if (historicalData != null && historicalData.Count > 0)
                {
                    var lastRecord = historicalData
                        .OrderBy(d => d.Time)
                        .Last();
                    rawValue = lastRecord.Data;
                }

                // Si no se encontraron datos históricos en el rango, devolver null
                if (string.IsNullOrEmpty(rawValue))
                {
                    return new KpiResponse
                    {
                        Id = kpi.Id,
                        Name = kpi.Name,
                        Description = kpi.Description,
                        Unit = kpi.Unit,
                        Type = type,
                        ActualColor = kpi.DefaultColor,
                        Value = null
                    };
                }
            }
            else
            {
                // Sin rango de fechas, usar directamente el LastValue del sensor
                rawValue = foundSensor.LastValue;
            }

            object? finalValue = null;

            if (!string.IsNullOrEmpty(rawValue) && !string.IsNullOrEmpty(type))
            {
                switch (type.ToLower())
                {
                    case "boolean":
                        if (bool.TryParse(rawValue, out var boolVal))
                            finalValue = boolVal;
                        break;

                    case "int":
                        if (int.TryParse(rawValue, out var intVal))
                            finalValue = intVal * (kpi.Multiplier ?? 1);
                        break;

                    case "double":
                        if (double.TryParse(rawValue, System.Globalization.NumberStyles.Any,
                                            System.Globalization.CultureInfo.InvariantCulture, out var doubleVal))
                            finalValue = doubleVal * (kpi.Multiplier ?? 1);
                        break;

                    case "text":
                    case "json":
                        finalValue = rawValue;
                        break;

                    default:
                        break;
                }
            }

            string finalColor = kpi.DefaultColor ?? "#000000";

            if (!string.IsNullOrEmpty(kpi.ColorRanges))
            {
                if (finalValue is int intValue)
                    finalColor = GetColorForValue(kpi.ColorRanges, intValue, finalColor);
                else if (finalValue is double doubleValue)
                    finalColor = GetColorForValue(kpi.ColorRanges, doubleValue, finalColor);
            }

            return new KpiResponse
            {
                Id = kpi.Id,
                Name = kpi.Name,
                Description = kpi.Description,
                Type = type,
                Unit = kpi.Unit,
                ActualColor = finalColor,
                Value = finalValue
            };
        }


        private async Task<KpiResponse> CalculateAverageKpiIMAsync(Kpi kpi, DatasetIM dataset, string username)
        {
            // Si no tiene extraInfo o dates, dejamos en pendiente
            if (string.IsNullOrEmpty(kpi.ExtraInfo))
            {
                return new KpiResponse
                {
                    Id = kpi.Id,
                    Name = kpi.Name,
                    Description = kpi.Description,
                    Unit = kpi.Unit,
                    ActualColor = kpi.DefaultColor,
                    Value = "Pendiente de implementar (average)",
                    Type = null
                };
            }

            // 1. Parsear fechas desde ExtraInfo
            DateTime dateFrom, dateTo;
            try
            {
                var extra = JsonSerializer.Deserialize<Dictionary<string, string>>(kpi.ExtraInfo);
                dateFrom = DateTime.Parse(extra["dateFrom"], null, System.Globalization.DateTimeStyles.RoundtripKind);
                dateTo = DateTime.Parse(extra["dateTo"], null, System.Globalization.DateTimeStyles.RoundtripKind);
            }
            catch
            {
                return new KpiResponse
                {
                    Id = kpi.Id,
                    Name = kpi.Name,
                    Description = kpi.Description,
                    Unit = kpi.Unit,
                    ActualColor = kpi.DefaultColor,
                    Value = "Pendiente de implementar (average) - fechas inválidas",
                    Type = null
                };
            }

            // 2. Obtener el source del dataset
            var source = await _sondaIMService.GetSourceById((int)dataset.Id_Source, username);
            if (source == null || source.Devices == null || source.Devices.Count == 0)
            {
                return new KpiResponse
                {
                    Id = kpi.Id,
                    Name = kpi.Name,
                    Description = kpi.Description,
                    Unit = kpi.Unit,
                    ActualColor = kpi.DefaultColor,
                    Value = "Pendiente de implementar (average) - sin devices",
                    Type = null
                };
            }

            // 3. Buscar device que tenga el sensor
            int? deviceId = null;
            string? sensorType = null;
            foreach (var deviceSummary in source.Devices)
            {
                var device = await _sondaIMService.GetDeviceById(deviceSummary.Id, username);
                if (device?.Sensors == null) continue;

                var sensor = device.Sensors.FirstOrDefault(s => s.Name.Equals(dataset.SensorName, StringComparison.OrdinalIgnoreCase));
                if (sensor != null)
                {
                    deviceId = device.Id;
                    sensorType = sensor.Type;
                    break;
                }
            }

            if (!deviceId.HasValue)
            {
                return new KpiResponse
                {
                    Id = kpi.Id,
                    Name = kpi.Name,
                    Description = kpi.Description,
                    Unit = kpi.Unit,
                    ActualColor = kpi.DefaultColor,
                    Value = "Pendiente de implementar (average) - sensor no encontrado",
                    Type = null
                };
            }

            // 4. Obtener datos del sensor (solo Data y Time)
            var sensorData = await _sondaIMService.GetSensorDataByDate(deviceId.Value, dataset.SensorName, dateFrom, dateTo, username);
            if (sensorData == null || sensorData.Count == 0)
            {
                return new KpiResponse
                {
                    Id = kpi.Id,
                    Name = kpi.Name,
                    Description = kpi.Description,
                    Unit = kpi.Unit,
                    ActualColor = kpi.DefaultColor,
                    Value = null,
                    Type = sensorType
                };
            }

            // 5. Filtrar solo valores numéricos y calcular promedio
            double sum = 0;
            int count = 0;
            foreach (var data in sensorData)
            {
                if (double.TryParse(data.Data, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var val))
                {
                    sum += val;
                    count++;
                }
            }

            if (count == 0)
            {
                return new KpiResponse
                {
                    Id = kpi.Id,
                    Name = kpi.Name,
                    Description = kpi.Description,
                    Unit = kpi.Unit,
                    ActualColor = kpi.DefaultColor,
                    Value = "Pendiente de implementar (average) - sin valores numéricos",
                    Type = sensorType
                };
            }

            var average = sum / count;

            var finalAverage = average * (kpi.Multiplier ?? 1);

            // 6. Calcular color según ColorRanges
            string actualColor = kpi.DefaultColor;
            if (!string.IsNullOrEmpty(kpi.ColorRanges))
            {
                try
                {
                    var ranges = JsonSerializer.Deserialize<List<ColorRange>>(kpi.ColorRanges);
                    if (ranges != null)
                    {
                        var matched = ranges.FirstOrDefault(r => finalAverage >= r.min && finalAverage <= r.max);
                        if (matched != null)
                            actualColor = matched.color;
                    }
                }
                catch
                {
                    // ignorar errores de parseo
                }
            }

            // 7. Devolver respuesta
            return new KpiResponse
            {
                Id = kpi.Id,
                Name = kpi.Name,
                Description = kpi.Description,
                Unit = kpi.Unit,
                ActualColor = actualColor,
                Value = finalAverage.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                Type = sensorType
            };
        }


        private async Task<KpiResponse> CalculateMinKpiIMAsync(Kpi kpi, DatasetIM dataset, string username)
        {
            // Si no tiene extraInfo o dates, dejamos en pendiente
            if (string.IsNullOrEmpty(kpi.ExtraInfo))
            {
                return new KpiResponse
                {
                    Id = kpi.Id,
                    Name = kpi.Name,
                    Description = kpi.Description,
                    Unit = kpi.Unit,
                    ActualColor = kpi.DefaultColor,
                    Value = "Pendiente de implementar (min)",
                    Type = null
                };
            }

            // 1. Parsear fechas desde ExtraInfo
            DateTime dateFrom, dateTo;
            try
            {
                var extra = JsonSerializer.Deserialize<Dictionary<string, string>>(kpi.ExtraInfo);
                dateFrom = DateTime.Parse(extra["dateFrom"], null, System.Globalization.DateTimeStyles.RoundtripKind);
                dateTo = DateTime.Parse(extra["dateTo"], null, System.Globalization.DateTimeStyles.RoundtripKind);
            }
            catch
            {
                return new KpiResponse
                {
                    Id = kpi.Id,
                    Name = kpi.Name,
                    Description = kpi.Description,
                    Unit = kpi.Unit,
                    ActualColor = kpi.DefaultColor,
                    Value = "Pendiente de implementar (min) - fechas inválidas",
                    Type = null
                };
            }

            // 2. Obtener el source del dataset
            var source = await _sondaIMService.GetSourceById((int)dataset.Id_Source, username);
            if (source == null || source.Devices == null || source.Devices.Count == 0)
            {
                return new KpiResponse
                {
                    Id = kpi.Id,
                    Name = kpi.Name,
                    Description = kpi.Description,
                    Unit = kpi.Unit,
                    ActualColor = kpi.DefaultColor,
                    Value = "Pendiente de implementar (min) - sin devices",
                    Type = null
                };
            }

            // 3. Buscar device que tenga el sensor
            int? deviceId = null;
            string? sensorType = null;
            foreach (var deviceSummary in source.Devices)
            {
                var device = await _sondaIMService.GetDeviceById(deviceSummary.Id, username);
                if (device?.Sensors == null) continue;

                var sensor = device.Sensors.FirstOrDefault(s => s.Name.Equals(dataset.SensorName, StringComparison.OrdinalIgnoreCase));
                if (sensor != null)
                {
                    deviceId = device.Id;
                    sensorType = sensor.Type;
                    break;
                }
            }

            if (!deviceId.HasValue)
            {
                return new KpiResponse
                {
                    Id = kpi.Id,
                    Name = kpi.Name,
                    Description = kpi.Description,
                    Unit = kpi.Unit,
                    ActualColor = kpi.DefaultColor,
                    Value = "Pendiente de implementar (min) - sensor no encontrado",
                    Type = null
                };
            }

            // 4. Obtener datos del sensor
            var sensorData = await _sondaIMService.GetSensorDataByDate(deviceId.Value, dataset.SensorName, dateFrom, dateTo, username);
            if (sensorData == null || sensorData.Count == 0)
            {
                return new KpiResponse
                {
                    Id = kpi.Id,
                    Name = kpi.Name,
                    Description = kpi.Description,
                    Unit = kpi.Unit,
                    ActualColor = kpi.DefaultColor,
                    Value = "Pendiente de implementar (min) - sin datos",
                    Type = sensorType
                };
            }

            // 5. Filtrar solo valores numéricos y encontrar el mínimo
            double? minValue = null;
            foreach (var data in sensorData)
            {
                if (double.TryParse(data.Data, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var val))
                {
                    if (!minValue.HasValue || val < minValue.Value)
                        minValue = val;
                }
            }

            if (!minValue.HasValue)
            {
                return new KpiResponse
                {
                    Id = kpi.Id,
                    Name = kpi.Name,
                    Description = kpi.Description,
                    Unit = kpi.Unit,
                    ActualColor = kpi.DefaultColor,
                    Value = "Pendiente de implementar (min) - sin valores numéricos",
                    Type = sensorType
                };
            }

            // 6. Calcular color según ColorRanges
            var finalMinValue = minValue.Value * (kpi.Multiplier ?? 1);
            string actualColor = kpi.DefaultColor;
            if (!string.IsNullOrEmpty(kpi.ColorRanges))
            {
                try
                {
                    var ranges = JsonSerializer.Deserialize<List<ColorRange>>(kpi.ColorRanges);
                    if (ranges != null)
                    {
                        var matched = ranges.FirstOrDefault(r => finalMinValue >= r.min && finalMinValue <= r.max);
                        if (matched != null)
                            actualColor = matched.color;
                    }
                }
                catch
                {
                    // ignorar errores de parseo
                }
            }

            // 7. Devolver respuesta
            return new KpiResponse
            {
                Id = kpi.Id,
                Name = kpi.Name,
                Description = kpi.Description,
                Unit = kpi.Unit,
                ActualColor = actualColor,
                Value = finalMinValue.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                Type = sensorType
            };
        }

        public async Task<List<MetricInfo>> GetMetricInfoListAsync(string sourceModule)
        {
            var metrics = new List<MetricInfo>();

            switch (sourceModule.ToUpper())
            {
                case "IM":
                    metrics.Add(new MetricInfo { Name = "lastValue", ExtraInfo = "none" });
                    metrics.Add(new MetricInfo { Name = "average", ExtraInfo = "requiresDateRange" });
                    metrics.Add(new MetricInfo { Name = "minValue", ExtraInfo = "requiresDateRange" });
                    metrics.Add(new MetricInfo { Name = "maxValue", ExtraInfo = "requiresDateRange" });
                    break;

                case "AM":
                    break;

                case "EM":
                    break;

                case "UM":
                    break;

                default:
                    throw new ArgumentException($"SourceModule no soportado: {sourceModule}");
            }

            await Task.CompletedTask;
            return metrics;
        }

        private async Task<KpiResponse> CalculateMaxKpiIMAsync(Kpi kpi, DatasetIM dataset, string username)
        {
            // Si no tiene extraInfo o dates, dejamos en pendiente
            if (string.IsNullOrEmpty(kpi.ExtraInfo))
            {
                return new KpiResponse
                {
                    Id = kpi.Id,
                    Name = kpi.Name,
                    Description = kpi.Description,
                    Unit = kpi.Unit,
                    ActualColor = kpi.DefaultColor,
                    Value = "Pendiente de implementar (max)",
                    Type = null
                };
            }

            // 1. Parsear fechas desde ExtraInfo
            DateTime dateFrom, dateTo;
            try
            {
                var extra = JsonSerializer.Deserialize<Dictionary<string, string>>(kpi.ExtraInfo);
                dateFrom = DateTime.Parse(extra["dateFrom"], null, System.Globalization.DateTimeStyles.RoundtripKind);
                dateTo = DateTime.Parse(extra["dateTo"], null, System.Globalization.DateTimeStyles.RoundtripKind);
            }
            catch
            {
                return new KpiResponse
                {
                    Id = kpi.Id,
                    Name = kpi.Name,
                    Description = kpi.Description,
                    Unit = kpi.Unit,
                    ActualColor = kpi.DefaultColor,
                    Value = "Pendiente de implementar (max) - fechas inválidas",
                    Type = null
                };
            }

            // 2. Obtener el source del dataset
            var source = await _sondaIMService.GetSourceById((int)dataset.Id_Source, username);
            if (source == null || source.Devices == null || source.Devices.Count == 0)
            {
                return new KpiResponse
                {
                    Id = kpi.Id,
                    Name = kpi.Name,
                    Description = kpi.Description,
                    Unit = kpi.Unit,
                    ActualColor = kpi.DefaultColor,
                    Value = "Pendiente de implementar (max) - sin devices",
                    Type = null
                };
            }

            // 3. Buscar device que tenga el sensor
            int? deviceId = null;
            string? sensorType = null;
            foreach (var deviceSummary in source.Devices)
            {
                var device = await _sondaIMService.GetDeviceById(deviceSummary.Id, username);
                if (device?.Sensors == null) continue;

                var sensor = device.Sensors.FirstOrDefault(s => s.Name.Equals(dataset.SensorName, StringComparison.OrdinalIgnoreCase));
                if (sensor != null)
                {
                    deviceId = device.Id;
                    sensorType = sensor.Type;
                    break;
                }
            }

            if (!deviceId.HasValue)
            {
                return new KpiResponse
                {
                    Id = kpi.Id,
                    Name = kpi.Name,
                    Description = kpi.Description,
                    Unit = kpi.Unit,
                    ActualColor = kpi.DefaultColor,
                    Value = "Pendiente de implementar (max) - sensor no encontrado",
                    Type = null
                };
            }

            // 4. Obtener datos del sensor
            var sensorData = await _sondaIMService.GetSensorDataByDate(deviceId.Value, dataset.SensorName, dateFrom, dateTo, username);
            if (sensorData == null || sensorData.Count == 0)
            {
                return new KpiResponse
                {
                    Id = kpi.Id,
                    Name = kpi.Name,
                    Description = kpi.Description,
                    Unit = kpi.Unit,
                    ActualColor = kpi.DefaultColor,
                    Value = "Pendiente de implementar (max) - sin datos",
                    Type = sensorType
                };
            }

            // 5. Filtrar solo valores numéricos y encontrar el máximo
            double? maxValue = null;
            foreach (var data in sensorData)
            {
                if (double.TryParse(data.Data, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var val))
                {
                    if (!maxValue.HasValue || val > maxValue.Value)
                        maxValue = val;
                }
            }

            if (!maxValue.HasValue)
            {
                return new KpiResponse
                {
                    Id = kpi.Id,
                    Name = kpi.Name,
                    Description = kpi.Description,
                    Unit = kpi.Unit,
                    ActualColor = kpi.DefaultColor,
                    Value = "Pendiente de implementar (max) - sin valores numéricos",
                    Type = sensorType
                };
            }

            // 6. Calcular color según ColorRanges
            var finalMaxValue = maxValue.Value * (kpi.Multiplier ?? 1);
            string actualColor = kpi.DefaultColor;
            if (!string.IsNullOrEmpty(kpi.ColorRanges))
            {
                try
                {
                    var ranges = JsonSerializer.Deserialize<List<ColorRange>>(kpi.ColorRanges);
                    if (ranges != null)
                    {
                        var matched = ranges.FirstOrDefault(r => finalMaxValue >= r.min && finalMaxValue <= r.max);
                        if (matched != null)
                            actualColor = matched.color;
                    }
                }
                catch
                {
                    // ignorar errores de parseo
                }
            }

            // 7. Devolver respuesta
            return new KpiResponse
            {
                Id = kpi.Id,
                Name = kpi.Name,
                Description = kpi.Description,
                Unit = kpi.Unit,
                ActualColor = actualColor,
                Value = finalMaxValue.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                Type = sensorType
            };
        }

        public string GetColorForValue(string colorRangesJson, double value, string defaultColor)
        {
            try
            {
                var options = new System.Text.Json.JsonSerializerOptions();
                options.Converters.Add(new FlexibleColorRangeConverter());
                var ranges = System.Text.Json.JsonSerializer.Deserialize<List<ColorRange>>(colorRangesJson, options);
                if (ranges == null)
                {
                    return defaultColor;
                }

                foreach (var range in ranges)
                {
                    if (value >= range.min && value <= range.max)
                    {
                        return range.color;
                    }
                }
            }
            catch (Exception ex)
            {
                return defaultColor;
            }
            return defaultColor;
        }

                // Custom converter para ColorRange que acepta min/max/color o Min/Max/Color
        public class FlexibleColorRangeConverter : System.Text.Json.Serialization.JsonConverter<ColorRange>
        {
            public override ColorRange Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
            {
                double min = 0, max = 0;
                string color = "#000000";
                if (reader.TokenType != System.Text.Json.JsonTokenType.StartObject)
                    throw new System.Text.Json.JsonException();
                while (reader.Read())
                {
                    if (reader.TokenType == System.Text.Json.JsonTokenType.EndObject)
                        break;
                    if (reader.TokenType == System.Text.Json.JsonTokenType.PropertyName)
                    {
                        string prop = reader.GetString();
                        reader.Read();
                        switch (prop.ToLower())
                        {
                            case "min": min = reader.GetDouble(); break;
                            case "max": max = reader.GetDouble(); break;
                            case "color": color = reader.GetString(); break;
                        }
                    }
                }
                return new ColorRange { min = min, max = max, color = color };
            }
            public override void Write(System.Text.Json.Utf8JsonWriter writer, ColorRange value, System.Text.Json.JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                writer.WriteNumber("min", value.min);
                writer.WriteNumber("max", value.max);
                writer.WriteString("color", value.color);
                writer.WriteEndObject();
            }
        }

        public async Task<List<string>> GetFieldValuesAsync(int datasetId, string modulo, string campo, int choice, string username)
        {
            if (datasetId <= 0)
                throw new ArgumentException("El ID del dataset debe ser mayor que 0.");

            if (string.IsNullOrWhiteSpace(modulo))
                throw new ArgumentException("El módulo no puede estar vacío.");

            if (string.IsNullOrWhiteSpace(campo))
                throw new ArgumentException("El campo no puede estar vacío.");

            List<string> fieldValues = new List<string>();

            switch (modulo.ToUpperInvariant())
            {
                case "AM":
                    var datasetAM = await _context.DatasetAM.FirstOrDefaultAsync(d => d.Id_Dataset == datasetId);
                    if (datasetAM == null)
                        throw new ArgumentException($"No se encontró un dataset AM con ID {datasetId}.");

                    if (choice == 1) // Assets
                    {
                        var assetsData = await _datasetAmService.GetReducedAssetsByDatasetIdAsync(datasetId, username);
                        fieldValues = await _kpiAMService.GetFieldValuesAsync(assetsData, campo);
                    }
                    else if (choice == 2) // Events
                    {
                        var eventsData = await _datasetAmService.GetReducedEventsByDatasetIdAsync(datasetId, username);
                        fieldValues = await _kpiAMService.GetFieldValuesAsync(eventsData, campo);
                    }
                    else if (choice == 3) // Stock
                    {
                        datasetAM = await _datasetAmService.GetDatasetAMByIdAsync(datasetId, username);
                        var stockData = new List<OmniMonitor.Shared.Dtos.ReducedStockDatasetAM>();
                        if (datasetAM.Grupo_Event_Task_Instance != null)
                        {
                            foreach (var eventTaskInstance in datasetAM.Grupo_Event_Task_Instance)
                            {
                                if (eventTaskInstance.Grupo_Stock != null)
                                {
                                    foreach (var dsStock in eventTaskInstance.Grupo_Stock)
                                    {
                                        var stockDto = await _sondaAMService.GetStockById(dsStock.Id_Stock, username);
                                        if (stockDto != null)
                                        {
                                            stockData.Add(new OmniMonitor.Shared.Dtos.ReducedStockDatasetAM
                                            {
                                                Nombre = stockDto.Name,
                                                Cantidad = stockDto.Quantity,
                                                Proveedor = stockDto.Provider?.Name ?? string.Empty,
                                                Sku = stockDto.Sku ?? string.Empty,
                                                Minimo = stockDto.Minimum,
                                                Bundle = stockDto.Bundle?.Name ?? stockDto.BundleId.ToString(),
                                                Supervisor = stockDto.Supervisor?.UserName ?? string.Empty
                                            });
                                        }
                                        else
                                        {
                                        }
                                    }
                                }
                            }
                        }
                        
                        
                        foreach (var s in stockData)
                        {
                        }
                        fieldValues = await _kpiAMService.GetFieldValuesAsync(stockData, campo);
                    }
                    break;

                case "EM":
                    var datasetEM = await _datasetEmService.GetDatasetEMByIdAsync(datasetId, username);
                    if (datasetEM == null)
                        throw new ArgumentException($"No se encontró un dataset EM con ID {datasetId}.");

                    // Alerts
                    if (choice == 1 && datasetEM.DatasetAlerts != null && datasetEM.DatasetAlerts.Any())
                    {
                        var alertIds = datasetEM.DatasetAlerts.Select(a => a.Id_alert).ToList();
                        var alertDtos = new List<OmniMonitor.Shared.Dtos.EM.DatasetReducedAlertEMDTO>();
                        foreach (var id in alertIds)
                        {
                            var alert = await _sondaEMService.GetAlertById(id, username);
                            if (alert != null)
                            {
                                alertDtos.Add(new OmniMonitor.Shared.Dtos.EM.DatasetReducedAlertEMDTO
                                {
                                    Nombre = alert.AlertName,
                                    Fuente = alert.SourceId.ToString(),
                                    Estado = alert.AlertState,
                                    SourceAddress = alert.SourceAddress
                                });
                            }
                        }
                        fieldValues = ExtractFieldValuesFromList(alertDtos, campo);
                    }
                    // Events
                    else if (choice == 2 && datasetEM.DatasetEvents != null && datasetEM.DatasetEvents.Any())
                    {
                        var eventIds = datasetEM.DatasetEvents.Select(e => e.Id_event).ToHashSet();
                        var allEvents = await _sondaEMService.GetEvents(null, null, null, null, username);
                        var eventDtos = new List<OmniMonitor.Shared.Dtos.EM.DatasetReducedEventEMDTO>();
                        foreach (var evento in allEvents)
                        {
                            if (eventIds.Contains(evento.Id))
                            {
                                eventDtos.Add(new OmniMonitor.Shared.Dtos.EM.DatasetReducedEventEMDTO
                                {
                                    Nombre = evento.Name,
                                    Origen = evento.Origin,
                                    Estado = evento.State,
                                    Direccion = evento.Address?.DisplayName
                                });
                            }
                        }
                        fieldValues = ExtractFieldValuesFromList(eventDtos, campo);
                    }
                    // Extensions
                    else if (choice == 3 && datasetEM.DatasetExtensions != null && datasetEM.DatasetExtensions.Any())
                    {
                        var extIds = datasetEM.DatasetExtensions.Select(x => x.Id_extension).ToList();
                        var extDtos = new List<OmniMonitor.Shared.Dtos.EM.DatasetReducedExtensionEMDTO>();
                        foreach (var id in extIds)
                        {
                            var extension = await _sondaEMService.GetExtensionById(id, username);
                            if (extension != null)
                            {
                                extDtos.Add(new OmniMonitor.Shared.Dtos.EM.DatasetReducedExtensionEMDTO
                                {
                                    Estado = extension.State,
                                    TakenBy = extension.TakenBy?.Name,
                                    CreatedBy = extension.CreatedBy?.Name,
                                    WorkZone = extension.WorkZoneName,
                                    Nombre = extension.EventName,
                                    Origen = extension.EventOrigin,
                                    Direccion = extension.Address?.DisplayName
                                });
                            }
                        }
                        fieldValues = ExtractFieldValuesFromList(extDtos, campo);
                    }
                    else
                    {
                        fieldValues = ExtractFieldValuesFromList(new List<DatasetEM> { datasetEM }, campo);
                    }
                    break;

                case "UM":
                    var datasetUM = await _datasetUMService.GetDatasetUMByIdAsync(datasetId, username);
                    if (datasetUM == null)
                        throw new ArgumentException($"No se encontró un dataset UM con ID {datasetId}.");

                    // Events
                    if (choice == 1 && datasetUM.DatasetEvents != null && datasetUM.DatasetEvents.Any())
                    {
                        var eventIds = datasetUM.DatasetEvents.Select(e => e.Id_event).ToList();
                        var eventDtos = new List<OmniMonitor.Shared.Dtos.UM.DatasetReducedEventsUMDTO>();
                        foreach (var id in eventIds)
                        {
                            var evento = await _sondaUMService.GetEventById(id, username);
                            if (evento != null)
                            {
                                eventDtos.Add(new OmniMonitor.Shared.Dtos.UM.DatasetReducedEventsUMDTO
                                {
                                    Nombre = evento.Name,
                                    Descripcion = evento.Description,
                                    Tipo = evento.Type?.Name,
                                    Fecha = evento.Date?.ToString("yyyy-MM-dd HH:mm:ss"),
                                    Aprobacion = evento.ApprovalState == "Aprobado"
                                });
                            }
                        }
                        fieldValues = ExtractFieldValuesFromList(eventDtos, campo);
                    }
                    // News
                    else if (choice == 2 && datasetUM.DatasetNews != null && datasetUM.DatasetNews.Any())
                    {
                        var newsIds = datasetUM.DatasetNews.Select(n => n.Id_news).ToList();
                        var newsDtos = new List<OmniMonitor.Shared.Dtos.UM.DatasetReducedNewsUMDTO>();
                        foreach (var id in newsIds)
                        {
                            var news = await _sondaUMService.GetNewsById(id, username);
                            if (news != null)
                            {
                                if (campo == "Categoria" && news.Categories != null)
                                {
                                    foreach (var category in news.Categories)
                                    {
                                        newsDtos.Add(new OmniMonitor.Shared.Dtos.UM.DatasetReducedNewsUMDTO
                                        {
                                            Titulo = news.Title,
                                            Resumen = news.Summary,
                                            Descripcion = news.Description,
                                            Categoria = category.Name
                                        });
                                    }
                                }
                                else
                                {
                                    newsDtos.Add(new OmniMonitor.Shared.Dtos.UM.DatasetReducedNewsUMDTO
                                    {
                                        Titulo = news.Title,
                                        Resumen = news.Summary,
                                        Descripcion = news.Description,
                                        Categoria = null
                                    });
                                }
                            }
                        }
                        fieldValues = ExtractFieldValuesFromList(newsDtos, campo);
                    }
                    else
                    {
                        fieldValues = ExtractFieldValuesFromList(new List<DatasetUM> { datasetUM }, campo);
                    }
                    break;
                
                case "IM":
                    throw new NotSupportedException("El módulo IM no soporta este tipo de consulta.");

                default:
                    throw new ArgumentException($"Módulo '{modulo}' no soportado.");
            }

            return fieldValues.Distinct().OrderBy(v => v).ToList();
        }

        private List<string> ExtractFieldValuesFromList<T>(List<T> dataList, string fieldName)
        {
            var values = new List<string>();

            if (dataList == null || !dataList.Any())
                return values;

            var type = typeof(T);
            var property = type.GetProperty(fieldName);

            if (property == null)
                throw new ArgumentException($"El campo '{fieldName}' no existe en el tipo {type.Name}.");

            foreach (var item in dataList)
            {
                var value = property.GetValue(item);
                if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
                {
                    values.Add(value.ToString()!);
                }
            }

            return values;
        }

        private List<float> ExtractFieldValuesFromLists<T>(List<T> dataList, string fieldName)
        {
            var values = new List<float>();

            if (dataList == null || !dataList.Any())
                return values;

            var type = typeof(T);
            var property = type.GetProperty(fieldName);

            if (property == null)
                throw new ArgumentException($"El campo '{fieldName}' no existe en el tipo {type.Name}.");

            foreach (var item in dataList)
            {
                var value = property.GetValue(item);
                if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
                {
                        values.Add(Convert.ToSingle(value));
                }
            }

            return values;
        }

        private bool TryGetDateRange(string? extraInfo, out DateTime dateFrom, out DateTime dateTo)
        {
            dateFrom = default;
            dateTo = default;

            if (string.IsNullOrWhiteSpace(extraInfo))
                return false;

            try
            {
                var extra = JsonSerializer.Deserialize<Dictionary<string, string>>(extraInfo);
                if (extra == null)
                    return false;

                if (extra.TryGetValue("dateFrom", out var fromRaw) && extra.TryGetValue("dateTo", out var toRaw))
                {
                    dateFrom = DateTime.Parse(fromRaw, null, System.Globalization.DateTimeStyles.RoundtripKind);
                    dateTo = DateTime.Parse(toRaw, null, System.Globalization.DateTimeStyles.RoundtripKind);
                    return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        public async Task<KpiSimplePaginatedResponse> GetAllKpisPaginatedAsync(string username, int page = 1, int pageSize = 10, string? query = null)
        {
            var kpisQuery = _context.Kpi.Where(k => k.Username == username);
            
            // Apply search filter if provided
            if (!string.IsNullOrWhiteSpace(query))
            {
                kpisQuery = kpisQuery.Where(k => 
                    k.Name.Contains(query) || 
                    (k.Description != null && k.Description.Contains(query)));
            }
            
            var totalCount = await kpisQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            if (totalPages == 0)
            {
                page = 1;
            }
            else if (page > totalPages)
            {
                page = totalPages;
            }
            
            var kpis = await kpisQuery
                .OrderBy(k => k.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var kpiDtos = new List<KpiSimpleDto>();
            foreach (var kpi in kpis)
            {
                var datasetName = await GetDatasetNameFromModuleAsync(kpi.DatasetId, kpi.SourceModule, kpi.Username ?? username);
                kpiDtos.Add(new KpiSimpleDto
                {
                    Id = kpi.Id,
                    Name = kpi.Name,
                    Description = kpi.Description,
                    DefaultColor = kpi.DefaultColor,
                    DatasetName = datasetName ?? string.Empty
                });
            }
            
            return new KpiSimplePaginatedResponse
            {
                Items = kpiDtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasPreviousPage = page > 1,
                HasNextPage = page < totalPages
            };
        }

        public class ColorRange
        {
            [JsonPropertyName("min")]
            public double min { get; set; }

            [JsonPropertyName("max")]
            public double max { get; set; }

            [JsonPropertyName("color")]
            public string color { get; set; } = "#000000";
        }

        private async Task<string?> GetDatasetNameAsync(int datasetId)
        {
            try
            {
                var dataset = await _context.Datasets
                    .FirstOrDefaultAsync(d => d.Id == datasetId);
                return dataset?.NameDataset;
            }
            catch
            {
                return null;
            }
        }

        private async Task<string?> GetDatasetNameFromModuleAsync(int moduleDatasetId, string sourceModule, string username)
        {
            try
            {
                switch (sourceModule.ToUpperInvariant())
                {
                    case "IM":
                        var datasetIM = await _datasetService.GetDatasetIMByIdAsync(moduleDatasetId, username);
                        return datasetIM?.Name;
                    case "AM":
                        var datasetAM = await _datasetAmService.GetDatasetAMByIdAsync(moduleDatasetId, username);
                        return datasetAM?.Nombre;
                    case "UM":
                        var datasetUM = await _datasetUMService.GetDatasetUMByIdAsync(moduleDatasetId, username);
                        return datasetUM?.Name;
                    case "EM":
                        var datasetEM = await _datasetEmService.GetDatasetEMByIdAsync(moduleDatasetId, username);
                        return datasetEM?.Name;
                    default:
                        return null;
                }
            }
            catch
            {
                return null;
            }
        }

        private static KpiResponse BuildNoDataResponse(Kpi kpi, string? reason = null)
        {
            _ = reason;
            return new KpiResponse
            {
                Id = kpi.Id,
                Name = kpi.Name,
                Description = kpi.Description,
                ActualColor = kpi.DefaultColor,
                Type = null,
                Unit = kpi.Unit,
                Value = null
            };
        }

    }

}
