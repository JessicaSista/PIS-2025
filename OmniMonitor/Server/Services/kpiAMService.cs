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
    public interface IKpiAMService
    {
        Task<KpiResponse> CalculateAmKpiAsync<T>(Kpi kpi, string username, List<T> items);
        Task<List<string>> GetFieldValuesAsync<T>(List<T> items, string fieldName);
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
                // Obtiene el valor de un campo de DatasetReducedAMDTO por nombre usando reflexión
        private string? GetAssetFieldValue(object asset, string fieldName)
        {
            var prop = asset.GetType().GetProperty(fieldName);
            return prop?.GetValue(asset)?.ToString();
        }

        public async Task<List<string>> GetFieldValuesAsync<T>(List<T> items, string fieldName)
        {
            List<string> values = new List<string>();
            if (items == null || items.Count == 0)
                return values;
            foreach (var item in items)
            {
                var value = GetAssetFieldValue(item, fieldName);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    values.Add(value);
                }
            }
            return values.Distinct().OrderBy(v => v).ToList();
        }

        public async Task<KpiResponse> CalculateAmKpiAsync<T>(Kpi kpi, string username, List<T> items)
        {
            KpiResponse response = null;
            if (items == null || items.Count == 0)
                throw new ArgumentException("No se proporcionó la lista de objetos para el KPI AM.");

            switch (kpi.Metric?.ToLower())
            {
                case "count":
                    response = await CountStateGeneric(kpi, items, username);
                    break;
                case "porcentaje":
                    response = await CalculateAverageKpiGeneric(kpi, items, username);
                    break;
                case "state":
                    response = await CalculateMinKpiGeneric(kpi, items, username);
                    break;
                default:
                    throw new ArgumentException($"Métrica no soportada para AM: {kpi.Metric}");
            }
            return response;
        }

        // Métodos genéricos para operar sobre List<object>
        private async Task<KpiResponse> CountStateGeneric<T>(Kpi kpi, List<T> items, string username)
        {
            var estadoNecesario = kpi.ExtraInfo ?? "";
            var atributo = kpi.Atributo ?? "";
                int count = items.Count(a => GetAssetFieldValue(a, atributo) == estadoNecesario);
            return new KpiResponse
            {
                Name = kpi.Name,
                Description = kpi.Description,
                Type = "count",
                Value = count,
                Unit = null
            };
        }

        private async Task<KpiResponse> CalculateAverageKpiGeneric<T>(Kpi kpi, List<T> items, string username)
        {
            var estadoNecesario = kpi.ExtraInfo ?? "";
            var atributo = kpi.Atributo ?? "";
                int count = items.Count(a => GetAssetFieldValue(a, atributo) == estadoNecesario);
            double porcentaje = (items.Count > 0) ? (double)count / items.Count * 100.0 : 0.0;
            return new KpiResponse
            {
                Name = kpi.Name,
                Description = kpi.Description,
                Type = "average",
                Value = porcentaje,
                Unit = "%"
            };
        }

        private async Task<KpiResponse> CalculateMinKpiGeneric<T>(Kpi kpi, List<T> items, string username)
        {
            var estadoNecesario = kpi.ExtraInfo ?? "";
            var atributo = kpi.Atributo ?? "";
            if (items.Count == 1)
            {
                return new KpiResponse
                {
                    Name = kpi.Name,
                    Description = kpi.Description,
                    Type = "state",
                        Value = GetAssetFieldValue(items[0], atributo) ?? "Desconocido",
                    Unit = null
                };
            }
                int count = items.Count(a => GetAssetFieldValue(a, atributo) == estadoNecesario);
            return new KpiResponse
            {
                Name = kpi.Name,
                Description = kpi.Description,
                Type = "state",
                Value = count,
                Unit = null
            };
        }
    }
}   
        