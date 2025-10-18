using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.EntityFrameworkCore;
using OmniMonitor.Server.Context;
using OmniMonitor.Shared.Dtos;
using System.Text.Json.Serialization;

namespace OmniMonitor.Server.Services
{
    public interface IKpiService
    {
        Task<Kpi> CreateKpiAsync(KpiRequest request, string? username = null);
        Task<Kpi> GetKpiDefinitionAsync(int kpiId);
        Task<KpiResponse> CalculateKpiValueAsync(int kpiId, string username, string password);

        Task<List<KpiResponse>> CalculateAllKpisForUserAsync(string username, string password);
    }

    public class KpiService : IKpiService
    {
        private readonly ApplicationDbContext _context;
        private readonly IDatasetService _datasetService;
        private readonly ISondaEMService _sondaEMService;
        private readonly ISondaIMService _sondaIMService;
        private readonly ISondaAuthService _sondaAuthService;
        public KpiService(ApplicationDbContext context, IDatasetService datasetService, ISondaEMService sondaEMService, ISondaIMService sondaIMService, ISondaAuthService sondaAuthService)
        {
            _context = context;
            _datasetService = datasetService;
            _sondaEMService = sondaEMService;
            _sondaIMService = sondaIMService;
            _sondaAuthService = sondaAuthService;
        }

        public async Task<Kpi> CreateKpiAsync(KpiRequest request, string? username = null)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var newKpi = new Kpi
            {
                Name = request.Name,
                Description = request.Description,
                SourceModule = request.SourceModule,
                DatasetId = request.DatasetId,
                Unit = request.Unit,
                Metric = request.Metric,
                Multiplier = request.Multiplier,
                DefaultColor = request.DefaultColor,
                ColorRanges = request.ColorRanges,
                Username = username ?? string.Empty
            };

            _context.Kpi.Add(newKpi);
            await _context.SaveChangesAsync();

            return newKpi;
        }

        public async Task<Kpi> GetKpiDefinitionAsync(int kpiId)
        {
            var kpi = await _context.Kpi.FindAsync(kpiId);

            if (kpi == null)
                throw new ArgumentException($"No se encontró el KPI con ID {kpiId}");

            return kpi;
        }

        public async Task<KpiResponse> CalculateKpiValueAsync(int kpiId, string username, string password)
        {
            var kpi = await GetKpiDefinitionAsync(kpiId);

            KpiResponse? response = null;

            switch (kpi.SourceModule.ToUpper())
            {
                case "AM":
                    response = await CalculateAmKpiAsync(kpi, username, password);
                    break;

                case "EM":
                    response = await CalculateEmKpiAsync(kpi, username, password);
                    break;

                case "IM":
                    response = await CalculateImKpiAsync(kpi, username, password);
                    break;

                case "UM":
                    response = await CalculateUmKpiAsync(kpi, username, password);
                    break;

                default:
                    throw new ArgumentException($"SourceModule no soportado: {kpi.SourceModule}");
            }

            if (response == null)
                throw new Exception($"No se pudo calcular el KPI con ID {kpiId}");

            return response;
        }

        public async Task<List<KpiResponse>> CalculateAllKpisForUserAsync(string username, string password)
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
                    var response = await CalculateKpiValueAsync(kpi.Id, username, password);
                    results.Add(response);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error calculando KPI {kpi.Id}: {ex.Message}");
                }
            }

            return results;
        }

        private async Task<KpiResponse> CalculateImKpiAsync(Kpi kpi, string username, string password)
        {
            // Obtener dataset asociado al KPI
            var dataset = await _datasetService.GetDatasetIMByIdAsync(kpi.DatasetId, kpi.Username);

            if (dataset == null)
                throw new Exception($"No se encontró el dataset con ID {kpi.DatasetId} para el KPI {kpi.Name}");

            KpiResponse? response;

            switch (kpi.Metric?.ToLower())
            {
                case "lastvalue":
                    response = await CalculateLastValueIM(kpi, dataset, username, password);
                    break;

                case "average":
                    response = new KpiResponse
                    {
                        Name = kpi.Name,
                        Unit = kpi.Unit,
                        ActualColor = kpi.DefaultColor,
                        Value = "Pendiente de implementar (average)"
                    };
                    break;

                default:
                    throw new ArgumentException($"Métrica no soportada para IM: {kpi.Metric}");
            }

            return response;
        }


        // Funciones privadas por módulo
        private async Task<KpiResponse> CalculateAmKpiAsync(Kpi kpi, string username, string password)
        {
            // TODO: lógica de cálculo para AM
            return new KpiResponse
            {
                Name = kpi.Name,
                ActualColor = kpi.DefaultColor,
                Value = null
            };
        }

        private async Task<KpiResponse> CalculateEmKpiAsync(Kpi kpi, string username, string password)
        {
            // TODO: lógica de cálculo para EM
            return new KpiResponse
            {
                Name = kpi.Name,
                ActualColor = kpi.DefaultColor,
                Value = null
            };
        }


        private async Task<KpiResponse> CalculateUmKpiAsync(Kpi kpi, string username, string password)
        {
            // TODO: lógica de cálculo para UM
            return new KpiResponse
            {
                Name = kpi.Name,
                ActualColor = kpi.DefaultColor,
                Value = null
            };
        }


        private async Task<KpiResponse> CalculateLastValueIM(Kpi kpi, DatasetIM dataset, string username, string password)
        {
            string? rawValue = null;
            string? type = null;

            var source = await _sondaIMService.GetSourceById((int)dataset.Id_Source, username, password);
            if (source == null)
                throw new Exception($"No se encontró el source con ID {dataset.Id_Source}.");

            if (source.Devices == null || source.Devices.Count == 0)
                throw new Exception($"No se encontraron devices en el source {source.Id}.");

            foreach (var deviceSummary in source.Devices)
            {
                var device = await _sondaIMService.GetDeviceById(deviceSummary.Id, username, password);
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
                Name = kpi.Name,
                Description = kpi.Description,
                Type = type,
                Unit = kpi.Unit,
                ActualColor = finalColor,
                Value = finalValue
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
