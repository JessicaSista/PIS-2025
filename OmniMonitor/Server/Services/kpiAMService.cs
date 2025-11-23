using Microsoft.EntityFrameworkCore;

using OmniMonitor.Server.Context;
using OmniMonitor.Server.Resources;
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

    public class KpiAMService : IKpiAMService
    {
        #region Fields

        private readonly ApplicationDbContext _context;
        private readonly ISondaAMService _sondaAmService;
        private readonly IDatasetAmService _datasetAmService;

        #endregion

        #region Constructors

        public KpiAMService(ApplicationDbContext context, ISondaAMService sondaAmService, IDatasetAmService datasetAmService)
        {
            _context = context;
            _sondaAmService = sondaAmService;
            _datasetAmService = datasetAmService;
        }

        #endregion

        #region Methods

        private string? GetAssetFieldValue(object asset, string fieldName)
        {
            var prop = asset.GetType().GetProperty(fieldName);
            return prop?.GetValue(asset)?.ToString();
        }

        public async Task<List<string>> GetFieldValuesAsync<T>(List<T> items, string fieldName)
        {
            List<string> values = new();
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
            KpiResponse? response = null;
            if (items == null || items.Count == 0)
                return BuildEmptyResponse(kpi);

            switch (kpi.Metric?.ToLower())
            {
                case "count":
                    response = await CountStateGeneric(kpi, items, username);
                    break;
                case "percentage":
                    response = await CalculateAverageKpiGeneric(kpi, items, username);
                    break;
                case "state":
                    response = await CalculateMinKpiGeneric(kpi, items, username);
                    break;
                default:
                    throw new ArgumentException(string.Format(Language.MetricNotSupportedAM, kpi.Metric));
            }
            return response;
        }

        // Métodos genéricos para operar sobre List<object>
        private async Task<KpiResponse> CountStateGeneric<T>(Kpi kpi, List<T> items, string username)
        {
            var estadoNecesario = kpi.ExtraInfo ?? string.Empty;
            var atributo = kpi.Atributo ?? string.Empty;
            var coincidencias = items.Count(a => GetAssetFieldValue(a, atributo) == estadoNecesario);
            var multiplier = kpi.Multiplier ?? 1d;
            var valorFinal = Math.Round(coincidencias * multiplier, 2);

            var color = kpi.DefaultColor;
            if (!string.IsNullOrEmpty(kpi.ColorRanges))
            {
                color = GetColorForValue(kpi.ColorRanges, valorFinal, kpi.DefaultColor);
            }

            return new KpiResponse
            {
                Id = kpi.Id,
                Name = kpi.Name,
                Description = kpi.Description,
                Type = "count",
                Value = valorFinal,
                Unit = null,
                ActualColor = color
            };
        }

        private async Task<KpiResponse> CalculateAverageKpiGeneric<T>(Kpi kpi, List<T> items, string username)
        {
            var estadoNecesario = kpi.ExtraInfo ?? string.Empty;
            var atributo = kpi.Atributo ?? string.Empty;
            var coincidencias = items.Count(a => GetAssetFieldValue(a, atributo) == estadoNecesario);

            double porcentajeBase = items.Count > 0 ? (double)coincidencias / items.Count * 100.0 : 0.0;
            double porcentajeFormateado = Math.Round(porcentajeBase, 2);
            double porcentajeFinal = Math.Round(porcentajeFormateado * (kpi.Multiplier ?? 1d), 2);

            var color = kpi.DefaultColor;
            if (!string.IsNullOrEmpty(kpi.ColorRanges))
            {
                color = GetColorForValue(kpi.ColorRanges, porcentajeFinal, kpi.DefaultColor);
            }

            return new KpiResponse
            {
                Id = kpi.Id,
                Name = kpi.Name,
                Description = kpi.Description,
                Type = "average",
                Value = porcentajeFinal,
                Unit = "%",
                ActualColor = color
            };
        }

        private async Task<KpiResponse> CalculateMinKpiGeneric<T>(Kpi kpi, List<T> items, string username)
        {
            var estadoNecesario = kpi.ExtraInfo ?? string.Empty;
            var atributo = kpi.Atributo ?? string.Empty;

            if (items.Count == 1)
            {
                var colorUnico = kpi.DefaultColor;
                if (!string.IsNullOrEmpty(kpi.ColorRanges))
                {
                    var valorTexto = GetAssetFieldValue(items[0], atributo);
                    if (double.TryParse(valorTexto, out var numericValue))
                    {
                        colorUnico = GetColorForValue(kpi.ColorRanges, numericValue, kpi.DefaultColor);
                    }
                }

                return new KpiResponse
                {
                    Id = kpi.Id,
                    Name = kpi.Name,
                    Description = kpi.Description,
                    Type = "state",
                    Value = GetAssetFieldValue(items[0], atributo) ?? "Desconocido",
                    Unit = null,
                    ActualColor = colorUnico
                };
            }

            var coincidencias = items.Count(a => GetAssetFieldValue(a, atributo) == estadoNecesario);
            var multiplier = kpi.Multiplier ?? 1d;
            var valorFinal = Math.Round(coincidencias * multiplier, 2);

            var color = kpi.DefaultColor;
            if (!string.IsNullOrEmpty(kpi.ColorRanges))
            {
                color = GetColorForValue(kpi.ColorRanges, valorFinal, kpi.DefaultColor);
            }

            return new KpiResponse
            {
                Id = kpi.Id,
                Name = kpi.Name,
                Description = kpi.Description,
                Type = "state",
                Value = valorFinal,
                Unit = null,
                ActualColor = color
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
            catch (Exception)
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

        public class ColorRange
        {
            public double min { get; set; }
            public double max { get; set; }
            public string color { get; set; }
        }

        private static KpiResponse BuildEmptyResponse(Kpi kpi)
        {
            return new KpiResponse
            {
                Id = kpi.Id,
                Name = kpi.Name,
                Description = kpi.Description,
                ActualColor = kpi.DefaultColor,
                Type = null,
                Unit = null,
                Value = null
            };
        }

        #endregion
    }
}
