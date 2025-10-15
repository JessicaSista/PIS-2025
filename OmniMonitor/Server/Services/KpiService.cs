using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmniMonitor.Server.Context;
using OmniMonitor.Shared.Dtos;
using OmniMonitor.Shared.Dtos.KpiDtos;

namespace OmniMonitor.Server.Services
{
    /// <summary>
    /// Interfaz para el servicio de KPIs
    /// </summary>
    public interface IKpiService
    {
        Task<KpiResponse> CalculateKpiAsync(CalculateKpiRequest request, string username);
        Task<List<KpiResponse>> GetAllKpisAsync(string username);
        Task<KpiResponse?> GetKpiByIdAsync(int kpiId, string username);
    }

    /// <summary>
    /// Implementación del servicio de KPIs siguiendo el patrón simple de VisualizacionService
    /// </summary>
    public class KpiService : IKpiService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISondaIMService _sondaIMService;
        private readonly ISondaEMService _sondaEMService;
        private readonly ISondaUMService _sondaUMService;
        private readonly ILogger<KpiService> _logger;

        public KpiService(
            ApplicationDbContext context,
            ISondaIMService sondaIMService,
            ISondaEMService sondaEMService,
            ISondaUMService sondaUMService,
            ILogger<KpiService> logger)
        {
            _context = context;
            _sondaIMService = sondaIMService;
            _sondaEMService = sondaEMService;
            _sondaUMService = sondaUMService;
            _logger = logger;
        }

        /// <summary>
        /// Calcula un KPI para un dataset específico
        /// </summary>
        public async Task<KpiResponse> CalculateKpiAsync(CalculateKpiRequest request)
        {
            // Esta implementación por defecto asume username = "admin"
            return await CalculateKpiAsync(request, "admin");
        }

        /// <summary>
        /// Calcula un KPI para un dataset específico con username
        /// </summary>
        public async Task<KpiResponse> CalculateKpiAsync(CalculateKpiRequest request, string username)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(request.MetricType))
            {
                throw new ArgumentException("El nombre de usuario y el tipo de métrica son obligatorios.");
            }

            // Obtener el dataset
            var dataset = await _context.Datasets
                .FirstOrDefaultAsync(d => d.Id == request.DatasetId && d.Username == username);

            if (dataset == null)
            {
                throw new ArgumentException($"Dataset {request.DatasetId} no encontrado para el usuario {username}");
            }

            // Calcular el KPI según el tipo de dataset
            double value = await CalculateMetricAsync(dataset, request, username);

            var response = new KpiResponse
            {
                MetricType = request.MetricType,
                FieldName = request.FieldName,
                Value = value,
                FormatType = request.FormatType ?? "number",
                FormattedValue = FormatValue(value, request.FormatType ?? "number"),
                CalculatedAt = DateTime.UtcNow,
                DatasetInfo = new KpiDatasetInfo
                {
                    DatasetId = dataset.Id,
                    DatasetName = dataset.Name,
                    DatasetType = dataset.ContentType ?? "Generic",
                    Description = dataset.Description,
                    Username = dataset.Username,
                    TotalRecords = 0 // Se podría calcular si es necesario
                },
                Comparison = new KpiComparison
                {
                    PreviousValue = 0, // Se podría implementar
                    PercentageDifference = 0,
                    FormattedDifference = "stable"
                }
            };

            return response;
        }

        /// <summary>
        /// Obtiene todos los KPIs calculados para un usuario (simulado)
        /// </summary>
        public async Task<List<KpiResponse>> GetAllKpisAsync(string username)
        {
            // En una implementación real, esto podría obtener KPIs guardados
            // Por ahora devolvemos una lista vacía
            return new List<KpiResponse>();
        }

        /// <summary>
        /// Obtiene un KPI específico por ID (simulado)
        /// </summary>
        public async Task<KpiResponse?> GetKpiByIdAsync(int kpiId, string username)
        {
            // En una implementación real, esto obtendría un KPI guardado por su ID
            return null;
        }

        /// <summary>
        /// Calcula la métrica según el tipo de dataset y métrica solicitada
        /// </summary>
        private async Task<double> CalculateMetricAsync(Dataset dataset, CalculateKpiRequest request, string username)
        {
            try
            {
                var contentType = dataset.ContentType?.ToUpper();
                
                return contentType switch
                {
                    "IM" => await CalculateIMMetricAsync(dataset, request, username),
                    "EM" => await CalculateEMMetricAsync(dataset, request, username),
                    "UM" => await CalculateUMMetricAsync(dataset, request, username),
                    _ => await CalculateGenericMetricAsync(dataset, request)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculando métrica {MetricType} para dataset {DatasetId}", 
                    request.MetricType, dataset.Id);
                return 0;
            }
        }

        /// <summary>
        /// Calcula métricas para datasets de tipo IM (Industrial Monitoring)
        /// </summary>
        private async Task<double> CalculateIMMetricAsync(Dataset dataset, CalculateKpiRequest request, string username)
        {
            var password = "admin"; // En producción esto vendría de contexto seguro

            return request.MetricType.ToLower() switch
            {
                "count" => await GetDeviceCountAsync(username, password),
                "sensor_data_points" => await GetSensorDataPointsAsync(username, password),
                "active_devices" => await GetActiveDevicesAsync(username, password),
                _ => 0
            };
        }

        /// <summary>
        /// Calcula métricas para datasets de tipo EM (Event Management)
        /// </summary>
        private async Task<double> CalculateEMMetricAsync(Dataset dataset, CalculateKpiRequest request, string username)
        {
            var password = "admin"; // En producción esto vendría de contexto seguro

            return request.MetricType.ToLower() switch
            {
                "alert_count" => await GetAlertCountAsync(username, password),
                "event_count" => await GetEventCountAsync(username, password),
                "count" => await GetTotalEMCountAsync(username, password),
                _ => 0
            };
        }

        /// <summary>
        /// Calcula métricas para datasets de tipo UM (Urban Management)
        /// </summary>
        private async Task<double> CalculateUMMetricAsync(Dataset dataset, CalculateKpiRequest request, string username)
        {
            var password = "admin"; // En producción esto vendría de contexto seguro

            return request.MetricType.ToLower() switch
            {
                "zone_count" => await GetZoneCountAsync(username, password),
                "news_count" => await GetNewsCountAsync(username, password),
                "event_count" => await GetUMEventCountAsync(username, password),
                "count" => await GetTotalUMCountAsync(username, password),
                _ => 0
            };
        }

        /// <summary>
        /// Calcula métricas genéricas
        /// </summary>
        private async Task<double> CalculateGenericMetricAsync(Dataset dataset, CalculateKpiRequest request)
        {
            // Para datasets genéricos, devolvemos valores por defecto
            return request.MetricType.ToLower() switch
            {
                "count" => 1,
                _ => 0
            };
        }

        // Métodos auxiliares para obtener datos de las APIs Sonda
        private async Task<double> GetDeviceCountAsync(string username, string password)
        {
            try
            {
                var devices = await _sondaIMService.GetAllDevices(username, password);
                return devices?.Count ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        private async Task<double> GetSensorDataPointsAsync(string username, string password)
        {
            // Implementación simplificada
            return 100; // Valor por defecto
        }

        private async Task<double> GetActiveDevicesAsync(string username, string password)
        {
            try
            {
                var devices = await _sondaIMService.GetAllDevices(username, password);
                return devices?.Count ?? 0; // Simplificado: asumimos que todos están activos
            }
            catch
            {
                return 0;
            }
        }

        private async Task<double> GetAlertCountAsync(string username, string password)
        {
            try
            {
                var alerts = await _sondaEMService.GetAlerts(1, 1000, null, null, null, null, null, null, null, username, password);
                return alerts?.Count ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        private async Task<double> GetEventCountAsync(string username, string password)
        {
            try
            {
                var events = await _sondaEMService.GetEvents(1, 1000, null, null, username, password);
                return events?.Count ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        private async Task<double> GetTotalEMCountAsync(string username, string password)
        {
            var alerts = await GetAlertCountAsync(username, password);
            var events = await GetEventCountAsync(username, password);
            return alerts + events;
        }

        private async Task<double> GetZoneCountAsync(string username, string password)
        {
            try
            {
                var zones = await _sondaUMService.GetAllZones(username, password);
                return zones?.Count ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        private async Task<double> GetNewsCountAsync(string username, string password)
        {
            try
            {
                var news = await _sondaUMService.GetAllNews(username, password);
                return news?.Count ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        private async Task<double> GetUMEventCountAsync(string username, string password)
        {
            try
            {
                var events = await _sondaUMService.GetAllEvents(username, password);
                return events?.Count ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        private async Task<double> GetTotalUMCountAsync(string username, string password)
        {
            var zones = await GetZoneCountAsync(username, password);
            var news = await GetNewsCountAsync(username, password);
            var events = await GetUMEventCountAsync(username, password);
            return zones + news + events;
        }

        private static string FormatValue(double value, string formatType)
        {
            return formatType.ToLower() switch
            {
                "percentage" => $"{value:P2}",
                "currency" => value.ToString("C2"),
                _ => value.ToString("N0")
            };
        }
    }
}