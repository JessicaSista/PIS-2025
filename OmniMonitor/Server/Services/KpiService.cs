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
        Task<List<KpiResponse>> CalculateAllKpisForUserAsync(string username);
        Task<List<MetricInfo>> GetMetricInfoListAsync(string sourceModule);
        Task DeleteKpiAsync(int kpiId, string? username = null);
        Task<Kpi> UpdateKpiAsync(int kpiId, KpiRequest request, string? username = null);
    Task<List<string>> GetFieldValuesAsync(int datasetId, string modulo, string campo, int choice, string username);

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

            switch (request.SourceModule.ToUpperInvariant())
            {
                case "IM":
                    await ValidateImKpiRequestAsync(request, username);
                    break;

               //default:
               //    throw new ArgumentException($"Unsupported SourceModule: {request.SourceModule}");
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
                Type = request.Type
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
                try
                {
                    // validamos que sea una lista de ColorRange válida
                    var ranges = JsonSerializer.Deserialize<List<ColorRange>>(request.ColorRanges);
                    if (ranges == null)
                        throw new ArgumentException("ColorRanges inválido o vacío.", nameof(request.ColorRanges));
                }
                catch (JsonException)
                {
                    throw new ArgumentException("ColorRanges no es JSON válido.", nameof(request.ColorRanges));
                }
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

            // --- APLICAR CAMBIOS (sólo si vinieron valores válidos; strings se trimmed)
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
                    Console.WriteLine($"Error calculando KPI {kpi.Id}: {ex.Message}");
                }
            }

            return results;
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
                return new KpiResponse
                {
                    Name = kpi.Name,
                    ActualColor = kpi.DefaultColor,
                    Value = "Dataset AM no encontrado"
                };
            }
            KpiResponse? response;
            if (kpi.Type == 1)
            {
                // Buscar assets relacionados al dataset
                var reducedAssets = await _datasetAmService.GetReducedAssetsByDatasetIdAsync(kpi.DatasetId, username);
                response = await _kpiAMService.CalculateAmKpiAsync(kpi, username, reducedAssets);
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
                Console.WriteLine("Lista de EventTaskInstanceDto enviada a CalculateAmKpiAsync:");
                foreach (var et in eventTasks)
                {
                    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(et));
                }*/
                var eventTasks = await _datasetAmService.GetReducedEventsByDatasetIdAsync(kpi.DatasetId, username);
                var result = await _kpiAMService.CalculateAmKpiAsync(kpi, username, eventTasks);
                // Imprimir el resultado devuelto
                Console.WriteLine("Resultado devuelto por CalculateAmKpiAsync:");
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(result));
                return result;
            }
            // Otros tipos o default
            return new KpiResponse
            {
                Name = kpi.Name,
                ActualColor = kpi.DefaultColor,
                Value = null
            };
        }

        private async Task<KpiResponse> CalculateEmKpiAsync(Kpi kpi, string username)
        {
            // Lógica para EM según Type
            var dataset = await _datasetEmService.GetDatasetEMByIdAsync(kpi.DatasetId, username);
            if (dataset == null)
            {
                return new KpiResponse
                {
                    Name = kpi.Name,
                    ActualColor = kpi.DefaultColor,
                    Value = "Dataset EM no encontrado"
                };
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
                var eventIds = dataset.DatasetEvents.Select(e => e.Id_event).ToList();
                        var eventDtos = new List<OmniMonitor.Shared.Dtos.EM.DatasetReducedEventEMDTO>();
                        foreach (var id in eventIds)
                        {
                            var evento = await _sondaEMService.GetEventById(id, username);
                            if (evento != null)
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
                return new KpiResponse
                {
                    Name = kpi.Name,
                    ActualColor = kpi.DefaultColor,
                    Value = "Dataset UM no encontrado"
                };
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

            var source = await _sondaIMService.GetSourceById((int)dataset.Id_Source, username);
            if (source == null)
                throw new Exception($"No se encontró el source con ID {dataset.Id_Source}.");

            if (source.Devices == null || source.Devices.Count == 0)
                throw new Exception($"No se encontraron devices en el source {source.Id}.");

            foreach (var deviceSummary in source.Devices)
            {
                var device = await _sondaIMService.GetDeviceById(deviceSummary.Id, username);
                if (device?.Sensors == null)
                    continue;

                var sensor = device.Sensors.FirstOrDefault(s => s.Name == dataset.SensorName);
                if (sensor != null)
                {
                    rawValue = sensor.LastValue;
                    type = sensor.Type;
                    break;
                }
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
                        Console.WriteLine($"Tipo de sensor desconocido: {type}");
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

            // 6. Calcular color según ColorRanges
            string actualColor = kpi.DefaultColor;
            if (!string.IsNullOrEmpty(kpi.ColorRanges))
            {
                try
                {
                    var ranges = JsonSerializer.Deserialize<List<ColorRange>>(kpi.ColorRanges);
                    if (ranges != null)
                    {
                        var matched = ranges.FirstOrDefault(r => average >= r.Min && average <= r.Max);
                        if (matched != null)
                            actualColor = matched.Color;
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
                Value = average.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
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
            string actualColor = kpi.DefaultColor;
            if (!string.IsNullOrEmpty(kpi.ColorRanges))
            {
                try
                {
                    var ranges = JsonSerializer.Deserialize<List<ColorRange>>(kpi.ColorRanges);
                    if (ranges != null)
                    {
                        var matched = ranges.FirstOrDefault(r => minValue >= r.Min && minValue <= r.Max);
                        if (matched != null)
                            actualColor = matched.Color;
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
                Value = minValue.Value.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
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
            string actualColor = kpi.DefaultColor;
            if (!string.IsNullOrEmpty(kpi.ColorRanges))
            {
                try
                {
                    var ranges = JsonSerializer.Deserialize<List<ColorRange>>(kpi.ColorRanges);
                    if (ranges != null)
                    {
                        var matched = ranges.FirstOrDefault(r => maxValue >= r.Min && maxValue <= r.Max);
                        if (matched != null)
                            actualColor = matched.Color;
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
                Value = maxValue.Value.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                Type = sensorType
            };
        }

        private string GetColorForValue(string colorRangesJson, double value, string defaultColor)
        {
            try
            {
                var ranges = System.Text.Json.JsonSerializer.Deserialize<List<ColorRange>>(colorRangesJson);
                if (ranges == null) return defaultColor;

                foreach (var range in ranges)
                {
                    if (value >= range.Min && value <= range.Max)
                        return range.Color;
                }
            }
            catch
            {
                return defaultColor;
            }

            return defaultColor;
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
                    var datasetAM = await _context.DatasetAM.FirstOrDefaultAsync(d => d.DatasetId == datasetId);
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
                        var eventIds = datasetEM.DatasetEvents.Select(e => e.Id_event).ToList();
                        var eventDtos = new List<OmniMonitor.Shared.Dtos.EM.DatasetReducedEventEMDTO>();
                        foreach (var id in eventIds)
                        {
                            var evento = await _sondaEMService.GetEventById(id, username);
                            if (evento != null)
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

        
        public class ColorRange
        {
            [JsonPropertyName("min")]
            public double Min { get; set; }

            [JsonPropertyName("max")]
            public double Max { get; set; }

            [JsonPropertyName("color")]
            public string Color { get; set; } = "#000000";
        }

    }

        }


