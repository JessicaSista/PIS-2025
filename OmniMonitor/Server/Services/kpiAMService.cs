using Microsoft.EntityFrameworkCore;
using OmniMonitor.Server.Context;
using OmniMonitor.Shared.Dtos;
using OmniMonitor.Shared.Dtos.AM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmniMonitor.Server.Services
{
    public interface IKpiAMService
    {
        Task<KpiResponse> CalculateAmKpiAsync(Kpi kpi, string username);
        
    }

    public class KpiAMService : IKpiAMService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISondaAMService _sondaAMService;
        private readonly IDatasetAmService _datasetAmService;


        public KpiAMService(ApplicationDbContext context, ISondaAMService sondaAMService, IDatasetAmService datasetAmService)
        {
            _context = context;
            _sondaAMService = sondaAMService;
            _datasetAmService = datasetAmService;

        }

        public async Task<KpiResponse> CalculateAmKpiAsync(Kpi kpi, string username)
        {
            // Lógica para crear un KPI AM
            var dataset = await _datasetAmService.GetDatasetAMByIdAsync(kpi.DatasetId, username);
            if (dataset == null)
                throw new ArgumentException("Dataset AM no encontrado para el KPI proporcionado.");

            KpiResponse response = null;

            if (dataset.Type_Dataset == 1) {
                // ... lógica para Type_Dataset 1 ...
                 var eventTaskInstances = await _context.Set<DatasetEventTaskInstance>()
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
                        throw new ArgumentException($"Métrica no soportada para AM: {kpi.Metric}");
                }
                
                
            } else if (dataset.Type_Dataset == 2) {
                var assets = await _context.Set<DatasetAsset>()
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
                        throw new ArgumentException($"Métrica no soportada para AM: {kpi.Metric}");
                }
            } else {
                throw new ArgumentException("Tipo de Dataset AM no soportado.");
            }
            return response;
        }

        // Calcula el último valor para los assets AM
        private async Task<KpiResponse> CountStateAM(Kpi kpi, List<DatasetAsset> assets, string username)
        {
            var estadoNecesario = kpi.ExtraInfo ?? "";
            int count = 0;
            foreach (var asset in assets)
            {
                if (int.TryParse(asset.Id_Asset, out int assetId))
                {
                    var assetDto = await _sondaAMService.GetAssetById(assetId, username);
                    if (assetDto != null && assetDto.StateDto != null && assetDto.StateDto.Name == estadoNecesario)
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

        // Calcula el porcentaje de assets con estado necesario sobre el total
        private async Task<KpiResponse> CalculateAverageKpiAMAsync(Kpi kpi, List<DatasetAsset> assets, string username)
        {
            var estadoNecesario = kpi.ExtraInfo ?? "";
            int count = 0;
            foreach (var asset in assets)
            {
                if (int.TryParse(asset.Id_Asset, out int assetId))
                {
                    var assetDto = await _sondaAMService.GetAssetById(assetId, username);
                    if (assetDto != null && assetDto.StateDto != null && assetDto.StateDto.Name == estadoNecesario)
                    {
                        count++;
                    }
                }
            }
            double porcentaje = (assets.Count > 0) ? (double)count / assets.Count * 100.0 : 0.0;
            return new KpiResponse
            {
                Name = kpi.Name,
                Description = kpi.Description,
                Type = "average",
                Value = porcentaje,
                Unit = "%"
            };
        }

        // Calcula el mínimo para los assets AM (cuenta los que tienen el estado dado en ExtraInfo o retorna el estado si es uno solo)
        private async Task<KpiResponse> CalculateMinKpiAMAsync(Kpi kpi, List<DatasetAsset> assets, string username)
        {
            var estadoNecesario = kpi.ExtraInfo ?? "";
            if (assets.Count == 1)
            {
                if (int.TryParse(assets[0].Id_Asset, out int assetId))
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
            int count = 0;
            foreach (var asset in assets)
            {
                if (int.TryParse(asset.Id_Asset, out int assetId))
                {
                    var assetDto = await _sondaAMService.GetAssetById(assetId, username);
                    if (assetDto != null && assetDto.StateDto != null && assetDto.StateDto.Name == estadoNecesario)
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

        // Cuenta los eventTaskInstances con el estado igual a ExtraInfo usando el endpoint
        private async Task<KpiResponse> CountStateETI(Kpi kpi, List<DatasetEventTaskInstance> etis, string username)
        {
            var estadoNecesario = kpi.ExtraInfo ?? "";
            int count = 0;
            foreach (var eti in etis)
            {
                var etiDto = await _sondaAMService.GetEventTaskInstanceById(eti.Id_Event_Task_Instance, username);
                if (etiDto != null && etiDto.State == estadoNecesario)
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

        // Calcula el porcentaje de eventTaskInstances con estado igual a ExtraInfo usando el endpoint
        private async Task<KpiResponse> CalculateAverageETI(Kpi kpi, List<DatasetEventTaskInstance> etis, string username)
        {
            var estadoNecesario = kpi.ExtraInfo ?? "";
            int count = 0;
            foreach (var eti in etis)
            {
                var etiDto = await _sondaAMService.GetEventTaskInstanceById(eti.Id_Event_Task_Instance, username);
                if (etiDto != null && etiDto.State == estadoNecesario)
                {
                    count++;
                }
            }
            double porcentaje = (etis.Count > 0) ? (double)count / etis.Count * 100.0 : 0.0;
            return new KpiResponse
            {
                Name = kpi.Name,
                Description = kpi.Description,
                Type = "averageETI",
                Value = porcentaje,
                Unit = "%"
            };
        }

        // Si hay un solo eventTaskInstance, retorna su estado usando el endpoint
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
            var estadoNecesario = kpi.ExtraInfo ?? "";
            int count = 0;
            foreach (var eti in etis)
            {
                var etiDto = await _sondaAMService.GetEventTaskInstanceById(eti.Id_Event_Task_Instance, username);
                if (etiDto != null && etiDto.State == estadoNecesario)
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

        // Devuelve el stock del resource indicado en ExtraInfo usando el endpoint
        private async Task<KpiResponse> CountStocksETI(Kpi kpi, List<DatasetEventTaskInstance> etis, string username)
        {
            var stockName = kpi.ExtraInfo ?? "";
            int totalQuantity = 0;
            foreach (var eti in etis)
            {
                var stocks = await _sondaAMService.GetEventTaskInstanceStock(eti.Id_Event_Task_Instance, username);
                if (stocks != null)
                {
                    foreach (var stock in stocks)
                    {
                        if (stock.Name == stockName)
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
    }
}