using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmniMonitor.Server.Context;
using OmniMonitor.Shared.Dtos;
using OmniMonitor.Shared.Dtos.AM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmniMonitor.Server.Services
{
    /// <summary>
    /// Servicio para el cálculo de KPIs de Asset Manager (AM).
    /// </summary>
    public interface IKpiAMService
    {
        /// <summary>
        /// Calcula el KPI de AM según la configuración y usuario.
        /// </summary>
        /// <param name="kpi">Configuración del KPI.</param>
        /// <param name="username">Nombre de usuario.</param>
        /// <returns>Respuesta con el resultado del KPI.</returns>
        Task<KpiResponse> CalculateAmKpiAsync(Kpi kpi, string username);
    }

    /// <inheritdoc />
    public class KpiAMService : IKpiAMService
    {
        #region Campos privados

        private readonly ApplicationDbContext _context;
        private readonly ISondaAMService _sondaAMService;
        private readonly IDatasetAmService _datasetAmService;
        private readonly ILogger<KpiAMService> _logger;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor de KpiAMService.
        /// </summary>
        /// <param name="context">Contexto de base de datos.</param>
        /// <param name="sondaAMService">Servicio de Sonda AM.</param>
        /// <param name="datasetAmService">Servicio de datasets AM.</param>
        /// <param name="logger">Logger para registrar eventos.</param>
        public KpiAMService(
            ApplicationDbContext context,
            ISondaAMService sondaAMService,
            IDatasetAmService datasetAmService,
            ILogger<KpiAMService> logger)
        {
            _context = context;
            _sondaAMService = sondaAMService;
            _datasetAmService = datasetAmService;
            _logger = logger;
        }

        #endregion

        #region Métodos públicos

        /// <inheritdoc />
        public async Task<KpiResponse> CalculateAmKpiAsync(Kpi kpi, string username)
        {
            try
            {
                _logger.LogInformation("Calculando KPI AM '{KpiName}' para usuario {Username}", kpi.Name, username);

                var dataset = await _datasetAmService.GetDatasetAMByIdAsync(kpi.DatasetId, username);
                if (dataset == null)
                {
                    _logger.LogWarning("Dataset AM no encontrado para el KPI '{KpiName}' y usuario {Username}", kpi.Name, username);
                    throw new ArgumentException("Dataset AM no encontrado para el KPI proporcionado.");
                }

                KpiResponse response = null;

                if (dataset.Type_Dataset == 1)
                {
                    var eventTaskInstances = await _context.Set<DatasetEventTaskInstance>()
                        .AsNoTracking()
                        .Where(e => e.DatasetAMId == dataset.Id_Dataset)
                        .ToListAsync();

                    switch (kpi.Metric?.ToLower())
                    {
                        case "count":
                            response = await CountStateETI(kpi, eventTaskInstances, username);
                            break;
                        case "porcentaje":
                            response = await CalculateAverageETI(kpi, eventTaskInstances, username);
                            break;
                        case "state":
                            response = await StateETI(kpi, eventTaskInstances, username);
                            break;
                        case "count_stocks":
                            response = await CountStocksETI(kpi, eventTaskInstances, username);
                            break;
                        default:
                            _logger.LogWarning("Métrica no soportada para AM: {Metric}", kpi.Metric);
                            throw new ArgumentException($"Métrica no soportada para AM: {kpi.Metric}");
                    }
                }
                else if (dataset.Type_Dataset == 2)
                {
                    var assets = await _context.Set<DatasetAsset>()
                        .AsNoTracking()
                        .Where(a => a.DatasetAMId == dataset.Id_Dataset)
                        .ToListAsync();

                    switch (kpi.Metric?.ToLower())
                    {
                        case "count":
                            response = await CountStateAM(kpi, assets, username);
                            break;
                        case "porcentaje":
                            response = await CalculateAverageKpiAMAsync(kpi, assets, username);
                            break;
                        case "state":
                            response = await CalculateMinKpiAMAsync(kpi, assets, username);
                            break;
                        default:
                            _logger.LogWarning("Métrica no soportada para AM: {Metric}", kpi.Metric);
                            throw new ArgumentException($"Métrica no soportada para AM: {kpi.Metric}");
                    }
                }
                else
                {
                    _logger.LogWarning("Tipo de Dataset AM no soportado para KPI '{KpiName}'", kpi.Name);
                    throw new ArgumentException("Tipo de Dataset AM no soportado.");
                }
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculando KPI AM '{KpiName}' para usuario {Username}", kpi.Name, username);
                throw;
            }
        }

        #endregion

        #region Métodos privados

        /// <summary>
        /// Calcula el conteo de assets AM con el estado necesario.
        /// </summary>
        private async Task<KpiResponse> CountStateAM(Kpi kpi, List<DatasetAsset> assets, string username)
        {
            var estadoNecesario = kpi.ExtraInfo ?? string.Empty;
            var count = 0;
            foreach (var asset in assets)
            {
                if (int.TryParse(asset.Id_Asset, out var assetId))
                {
                    var assetDto = await _sondaAMService.GetAssetById(assetId, username);
                    if (assetDto != null && assetDto.StateDto != null &&
                        string.Equals(assetDto.StateDto.Name, estadoNecesario, StringComparison.Ordinal))
                    {
                        count++;
                    }
                }
            }
            return new KpiResponse
            {
                Name = kpi.Name,
                Description = kpi.Description,
                Type = "count",
                Value = count,
                Unit = null
            };
        }

        /// <summary>
        /// Calcula el porcentaje de assets con estado necesario sobre el total.
        /// </summary>
        private async Task<KpiResponse> CalculateAverageKpiAMAsync(Kpi kpi, List<DatasetAsset> assets, string username)
        {
            var estadoNecesario = kpi.ExtraInfo ?? string.Empty;
            var count = 0;
            foreach (var asset in assets)
            {
                if (int.TryParse(asset.Id_Asset, out var assetId))
                {
                    var assetDto = await _sondaAMService.GetAssetById(assetId, username);
                    if (assetDto != null && assetDto.StateDto != null &&
                        string.Equals(assetDto.StateDto.Name, estadoNecesario, StringComparison.Ordinal))
                    {
                        count++;
                    }
                }
            }
            var porcentaje = (assets.Count > 0) ? (double)count / assets.Count * 100.0 : 0.0;
            return new KpiResponse
            {
                Name = kpi.Name,
                Description = kpi.Description,
                Type = "average",
                Value = porcentaje,
                Unit = "%"
            };
        }

        /// <summary>
        /// Calcula el estado mínimo o el conteo de assets con el estado dado.
        /// </summary>
        private async Task<KpiResponse> CalculateMinKpiAMAsync(Kpi kpi, List<DatasetAsset> assets, string username)
        {
            var estadoNecesario = kpi.ExtraInfo ?? string.Empty;
            if (assets.Count == 1)
            {
                if (int.TryParse(assets[0].Id_Asset, out var assetId))
                {
                    var assetDto = await _sondaAMService.GetAssetById(assetId, username);
                    if (assetDto != null && assetDto.StateDto != null)
                    {
                        return new KpiResponse
                        {
                            Name = kpi.Name,
                            Description = kpi.Description,
                            Type = "state",
                            Value = assetDto.StateDto.Name,
                            Unit = null
                        };
                    }
                }
                return new KpiResponse
                {
                    Name = kpi.Name,
                    Description = kpi.Description,
                    Type = "state",
                    Value = "Desconocido",
                    Unit = null
                };
            }
            var count = 0;
            foreach (var asset in assets)
            {
                if (int.TryParse(asset.Id_Asset, out var assetId))
                {
                    var assetDto = await _sondaAMService.GetAssetById(assetId, username);
                    if (assetDto != null && assetDto.StateDto != null &&
                        string.Equals(assetDto.StateDto.Name, estadoNecesario, StringComparison.Ordinal))
                    {
                        count++;
                    }
                }
            }
            return new KpiResponse
            {
                Name = kpi.Name,
                Description = kpi.Description,
                Type = "state",
                Value = count,
                Unit = null
            };
        }

        /// <summary>
        /// Cuenta los eventTaskInstances con el estado igual a ExtraInfo.
        /// </summary>
        private async Task<KpiResponse> CountStateETI(Kpi kpi, List<DatasetEventTaskInstance> etis, string username)
        {
            var estadoNecesario = kpi.ExtraInfo ?? string.Empty;
            var count = 0;
            foreach (var eti in etis)
            {
                var etiDto = await _sondaAMService.GetEventTaskInstanceById(eti.Id_Event_Task_Instance, username);
                if (etiDto != null && string.Equals(etiDto.State, estadoNecesario, StringComparison.Ordinal))
                {
                    count++;
                }
            }
            return new KpiResponse
            {
                Name = kpi.Name,
                Description = kpi.Description,
                Type = "countETI",
                Value = count,
                Unit = null
            };
        }

        /// <summary>
        /// Calcula el porcentaje de eventTaskInstances con estado igual a ExtraInfo.
        /// </summary>
        private async Task<KpiResponse> CalculateAverageETI(Kpi kpi, List<DatasetEventTaskInstance> etis, string username)
        {
            var estadoNecesario = kpi.ExtraInfo ?? string.Empty;
            var count = 0;
            foreach (var eti in etis)
            {
                var etiDto = await _sondaAMService.GetEventTaskInstanceById(eti.Id_Event_Task_Instance, username);
                if (etiDto != null && string.Equals(etiDto.State, estadoNecesario, StringComparison.Ordinal))
                {
                    count++;
                }
            }
            var porcentaje = (etis.Count > 0) ? (double)count / etis.Count * 100.0 : 0.0;
            return new KpiResponse
            {
                Name = kpi.Name,
                Description = kpi.Description,
                Type = "averageETI",
                Value = porcentaje,
                Unit = "%"
            };
        }

        /// <summary>
        /// Si hay un solo eventTaskInstance, retorna su estado; si hay varios, cuenta los que tienen el estado dado.
        /// </summary>
        private async Task<KpiResponse> StateETI(Kpi kpi, List<DatasetEventTaskInstance> etis, string username)
        {
            if (etis.Count == 1)
            {
                var etiDto = await _sondaAMService.GetEventTaskInstanceById(etis[0].Id_Event_Task_Instance, username);
                return new KpiResponse
                {
                    Name = kpi.Name,
                    Description = kpi.Description,
                    Type = "stateETI",
                    Value = etiDto?.State ?? "Desconocido",
                    Unit = null
                };
            }
            var estadoNecesario = kpi.ExtraInfo ?? string.Empty;
            var count = 0;
            foreach (var eti in etis)
            {
                var etiDto = await _sondaAMService.GetEventTaskInstanceById(eti.Id_Event_Task_Instance, username);
                if (etiDto != null && string.Equals(etiDto.State, estadoNecesario, StringComparison.Ordinal))
                {
                    count++;
                }
            }
            return new KpiResponse
            {
                Name = kpi.Name,
                Description = kpi.Description,
                Type = "stateETI",
                Value = count,
                Unit = null
            };
        }

        /// <summary>
        /// Devuelve el stock del recurso indicado en ExtraInfo.
        /// </summary>
        private async Task<KpiResponse> CountStocksETI(Kpi kpi, List<DatasetEventTaskInstance> etis, string username)
        {
            var stockName = kpi.ExtraInfo ?? string.Empty;
            var totalQuantity = 0;
            foreach (var eti in etis)
            {
                var stocks = await _sondaAMService.GetEventTaskInstanceStock(eti.Id_Event_Task_Instance, username);
                if (stocks != null)
                {
                    foreach (var stock in stocks)
                    {
                        if (string.Equals(stock.Name, stockName, StringComparison.Ordinal))
                        {
                            totalQuantity += stock.Quantity;
                        }
                    }
                }
            }
            return new KpiResponse
            {
                Name = kpi.Name,
                Description = kpi.Description,
                Type = "countStocksETI",
                Value = totalQuantity,
                Unit = null
            };
        }

        #endregion
    }
}