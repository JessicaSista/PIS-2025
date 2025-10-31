using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmniMonitor.Server.Context;
using OmniMonitor.Shared.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OmniMonitor.Server.Services
{
    /// <summary>
    /// Servicio para la gestión y cálculo de KPIs.
    /// </summary>
    public interface IKpiService
    {
        /// <summary>
        /// Crea un nuevo KPI.
        /// </summary>
        /// <param name="request">Datos para la creación del KPI.</param>
        /// <param name="username">Nombre de usuario (opcional).</param>
        /// <returns>El KPI creado.</returns>
        Task<Kpi> CreateKpiAsync(KpiRequest request, string? username = null);

        /// <summary>
        /// Obtiene la definición de un KPI por su ID.
        /// </summary>
        /// <param name="kpiId">ID del KPI.</param>
        /// <returns>El KPI encontrado.</returns>
        Task<Kpi> GetKpiDefinitionAsync(int kpiId);

        /// <summary>
        /// Calcula el valor de un KPI para un usuario.
        /// </summary>
        /// <param name="kpiId">ID del KPI.</param>
        /// <param name="username">Nombre de usuario.</param>
        /// <returns>Respuesta con el resultado del KPI.</returns>
        Task<KpiResponse> CalculateKpiValueAsync(int kpiId, string username);

        /// <summary>
        /// Calcula todos los KPIs de un usuario.
        /// </summary>
        /// <param name="username">Nombre de usuario.</param>
        /// <returns>Lista de respuestas de KPIs.</returns>
        Task<List<KpiResponse>> CalculateAllKpisForUserAsync(string username);

        /// <summary>
        /// Obtiene la lista de métricas disponibles para un módulo.
        /// </summary>
        /// <param name="sourceModule">Nombre del módulo.</param>
        /// <returns>Lista de métricas.</returns>
        Task<List<MetricInfo>> GetMetricInfoListAsync(string sourceModule);

        /// <summary>
        /// Elimina un KPI.
        /// </summary>
        /// <param name="kpiId">ID del KPI.</param>
        /// <param name="username">Nombre de usuario (opcional).</param>
        Task DeleteKpiAsync(int kpiId, string? username = null);

        /// <summary>
        /// Actualiza un KPI existente.
        /// </summary>
        /// <param name="kpiId">ID del KPI.</param>
        /// <param name="request">Datos para la actualización.</param>
        /// <param name="username">Nombre de usuario (opcional).</param>
        /// <returns>El KPI actualizado.</returns>
        Task<Kpi> UpdateKpiAsync(int kpiId, KpiRequest request, string? username = null);
    }

    /// <summary>
    /// Implementación del servicio para la gestión y cálculo de KPIs.
    /// </summary>
    public class KpiService : IKpiService
    {
        #region Campos privados

        private readonly ApplicationDbContext _context;
        private readonly IDatasetService _datasetService;
        private readonly ISondaEMService _sondaEMService;
        private readonly ISondaIMService _sondaIMService;
        private readonly ISondaAuthService _sondaAuthService;
        private readonly IKpiAMService _kpiAMService;
        private readonly ILogger<KpiService> _logger;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor de KpiService.
        /// </summary>
        public KpiService(
            ApplicationDbContext context,
            IDatasetService datasetService,
            ISondaEMService sondaEMService,
            ISondaIMService sondaIMService,
            ISondaAuthService sondaAuthService,
            IKpiAMService kpiAMService,
            ILogger<KpiService> logger)
        {
            _context = context;
            _datasetService = datasetService;
            _sondaEMService = sondaEMService;
            _sondaIMService = sondaIMService;
            _kpiAMService = kpiAMService;
            _sondaAuthService = sondaAuthService;
            _logger = logger;
        }

        #endregion

        #region Métodos públicos

        /// <inheritdoc/>
        public async Task<Kpi> CreateKpiAsync(KpiRequest request, string? username = null)
        {
            try
            {
                _logger.LogInformation("Creando KPI '{Name}' para usuario {Username}", request.Name, username);

                if (request == null)
                {
                    throw new ArgumentNullException(nameof(request));
                }

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
                    ExtraInfo = request.ExtraInfo,
                    Username = string.IsNullOrEmpty(username) ? string.Empty : username,
                };

                _context.Kpi.Add(newKpi);
                await _context.SaveChangesAsync();

                _logger.LogInformation("KPI '{Name}' creado correctamente para usuario {Username}", request.Name, username);

                return newKpi;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando KPI '{Name}' para usuario {Username}", request?.Name, username);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task DeleteKpiAsync(int kpiId, string? username = null)
        {
            try
            {
                _logger.LogInformation("Eliminando KPI con ID {KpiId} para usuario {Username}", kpiId, username);

                var kpi = await _context.Kpi.FirstOrDefaultAsync(k => k.Id == kpiId);

                if (kpi == null)
                {
                    _logger.LogWarning("No se encontró el KPI con ID {KpiId}", kpiId);
                    throw new KeyNotFoundException($"No se encontró el KPI con ID {kpiId}.");
                }

                if (!string.IsNullOrEmpty(username) && !string.Equals(kpi.Username, username, StringComparison.Ordinal))
                {
                    _logger.LogWarning("Usuario {Username} no tiene permisos para eliminar el KPI con ID {KpiId}", username, kpiId);
                    throw new UnauthorizedAccessException("No tiene permisos para eliminar este KPI.");
                }

                _context.Kpi.Remove(kpi);
                await _context.SaveChangesAsync();

                _logger.LogInformation("KPI con ID {KpiId} eliminado correctamente para usuario {Username}", kpiId, username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando KPI con ID {KpiId} para usuario {Username}", kpiId, username);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Kpi> UpdateKpiAsync(int kpiId, KpiRequest request, string? username = null)
        {
            try
            {
                _logger.LogInformation("Actualizando KPI con ID {KpiId} para usuario {Username}", kpiId, username);

                if (request == null)
                {
                    throw new ArgumentNullException(nameof(request));
                }

                var existingKpi = await _context.Kpi.FindAsync(kpiId);
                if (existingKpi == null)
                {
                    _logger.LogWarning("No se encontró el KPI con ID {KpiId}", kpiId);
                    throw new KeyNotFoundException($"No se encontró el KPI con ID {kpiId}.");
                }

                if (!string.IsNullOrEmpty(username) && !string.Equals(existingKpi.Username, username, StringComparison.Ordinal))
                {
                    _logger.LogWarning("Usuario {Username} no tiene permisos para editar el KPI con ID {KpiId}", username, kpiId);
                    throw new UnauthorizedAccessException("No tiene permisos para editar este KPI.");
                }

                if (!string.IsNullOrEmpty(request.Name)) { existingKpi.Name = request.Name; }
                if (!string.IsNullOrEmpty(request.Description)) { existingKpi.Description = request.Description; }
                if (!string.IsNullOrEmpty(request.SourceModule)) { existingKpi.SourceModule = request.SourceModule; }
                if (request.DatasetId != null) { existingKpi.DatasetId = request.DatasetId; }
                if (!string.IsNullOrEmpty(request.Unit)) { existingKpi.Unit = request.Unit; }
                if (!string.IsNullOrEmpty(request.Metric)) { existingKpi.Metric = request.Metric; }
                if (request.Multiplier != null) { existingKpi.Multiplier = request.Multiplier; }
                if (!string.IsNullOrEmpty(request.DefaultColor)) { existingKpi.DefaultColor = request.DefaultColor; }
                if (!string.IsNullOrEmpty(request.ColorRanges)) { existingKpi.ColorRanges = request.ColorRanges; }
                if (!string.IsNullOrEmpty(request.ExtraInfo)) { existingKpi.ExtraInfo = request.ExtraInfo; }

                _context.Kpi.Update(existingKpi);
                await _context.SaveChangesAsync();

                _logger.LogInformation("KPI con ID {KpiId} actualizado correctamente para usuario {Username}", kpiId, username);

                return existingKpi;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando KPI con ID {KpiId} para usuario {Username}", kpiId, username);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Kpi> GetKpiDefinitionAsync(int kpiId)
        {
            try
            {
                _logger.LogInformation("Obteniendo definición de KPI con ID {KpiId}", kpiId);

                var kpi = await _context.Kpi.FindAsync(kpiId);

                if (kpi == null)
                {
                    _logger.LogWarning("No se encontró el KPI con ID {KpiId}", kpiId);
                    throw new ArgumentException($"No se encontró el KPI con ID {kpiId}");
                }

                return kpi;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo definición de KPI con ID {KpiId}", kpiId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<KpiResponse> CalculateKpiValueAsync(int kpiId, string username)
        {
            try
            {
                _logger.LogInformation("Calculando valor del KPI con ID {KpiId} para usuario {Username}", kpiId, username);

                var kpi = await GetKpiDefinitionAsync(kpiId);
                KpiResponse? response = null;

                switch (kpi.SourceModule.ToUpper())
                {
                    case "AM":
                        response = await _kpiAMService.CalculateAmKpiAsync(kpi, username);
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
                        _logger.LogWarning("SourceModule no soportado: {SourceModule}", kpi.SourceModule);
                        throw new ArgumentException($"SourceModule no soportado: {kpi.SourceModule}");
                }

                if (response == null)
                {
                    _logger.LogWarning("No se pudo calcular el KPI con ID {KpiId}", kpiId);
                    throw new Exception($"No se pudo calcular el KPI con ID {kpiId}");
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculando valor del KPI con ID {KpiId} para usuario {Username}", kpiId, username);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<List<KpiResponse>> CalculateAllKpisForUserAsync(string username)
        {
            try
            {
                _logger.LogInformation("Calculando todos los KPIs para usuario {Username}", username);

                var kpis = await _context.Kpi
                    .AsNoTracking()
                    .Where(k => string.Equals(k.Username, username, StringComparison.Ordinal))
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
                        _logger.LogError(ex, "Error calculando KPI {KpiId} para usuario {Username}", kpi.Id, username);
                    }
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculando todos los KPIs para usuario {Username}", username);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<List<MetricInfo>> GetMetricInfoListAsync(string sourceModule)
        {
            try
            {
                _logger.LogInformation("Obteniendo métricas para el módulo {SourceModule}", sourceModule);

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
                        // Agregar métricas para AM si corresponde
                        break;
                    case "EM":
                        // Agregar métricas para EM si corresponde
                        break;
                    case "UM":
                        // Agregar métricas para UM si corresponde
                        break;
                    default:
                        _logger.LogWarning("SourceModule no soportado: {SourceModule}", sourceModule);
                        throw new ArgumentException($"SourceModule no soportado: {sourceModule}");
                }

                await Task.CompletedTask;
                return metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo métricas para el módulo {SourceModule}", sourceModule);
                throw;
            }
        }

        #endregion

        #region Métodos privados por módulo

        private async Task<KpiResponse> CalculateImKpiAsync(Kpi kpi, string username)
        {
            var dataset = await _datasetService.GetDatasetIMByIdAsync(kpi.DatasetId, kpi.Username);

            if (dataset == null)
            {
                _logger.LogWarning("No se encontró el dataset con ID {DatasetId} para el KPI {KpiName}", kpi.DatasetId, kpi.Name);
                throw new Exception($"No se encontró el dataset con ID {kpi.DatasetId} para el KPI {kpi.Name}");
            }

            KpiResponse? response;

            switch (kpi.Metric?.ToLower())
            {
                case "lastvalue":
                    response = await CalculateLastValueIM(kpi, dataset, username);
                    break;
                case "average":
                    response = await CalculateAverageKpiIMAsync(kpi, dataset, username);
                    break;
                case "min":
                    response = await CalculateMinKpiIMAsync(kpi, dataset, username);
                    break;
                case "max":
                    response = await CalculateMaxKpiIMAsync(kpi, dataset, username);
                    break;
                default:
                    _logger.LogWarning("Métrica no soportada para IM: {Metric}", kpi.Metric);
                    throw new ArgumentException($"Métrica no soportada para IM: {kpi.Metric}");
            }

            return response;
        }

        private async Task<KpiResponse> CalculateAmKpiAsync(Kpi kpi, string username)
        {
            // TODO: lógica de cálculo para AM
            return new KpiResponse
            {
                Name = kpi.Name,
                ActualColor = kpi.DefaultColor,
                Value = null
            };
        }

        private async Task<KpiResponse> CalculateEmKpiAsync(Kpi kpi, string username)
        {
            // TODO: lógica de cálculo para EM
            return new KpiResponse
            {
                Name = kpi.Name,
                ActualColor = kpi.DefaultColor,
                Value = null
            };
        }

        private async Task<KpiResponse> CalculateUmKpiAsync(Kpi kpi, string username)
        {
            // TODO: lógica de cálculo para UM
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

        
        public class ColorRange
        {
            [JsonPropertyName("min")]
            public double Min { get; set; }

            [JsonPropertyName("max")]
            public double Max { get; set; }

            [JsonPropertyName("color")]
            public string Color { get; set; } = "#000000";
        }
        #endregion
    }
}
