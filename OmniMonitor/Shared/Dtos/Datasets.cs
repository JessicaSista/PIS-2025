using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OmniMonitor.Shared.Dtos
{ 
    public class Datasets
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(256)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string NameDataset { get; set; }= string.Empty;

        [JsonConverter(typeof(ModuleTypeConverter))]
        public ModuleType TipoDataset { get; set; }

        // Relación con los DatasetsEM
        public virtual ICollection<DatasetEM> DatasetEM { get; set; } = new List<DatasetEM>();

        // Relación con los DatasetsUM
        public virtual ICollection<DatasetUM> DatasetUM { get; set; } = new List<DatasetUM>();
        // Relación con los DatasetAM
        public virtual ICollection<DatasetAM> DatasetAM { get; set; } = new List<DatasetAM>();

        // Relación con los DatasetIM
        public virtual ICollection<DatasetIM> DatasetIM { get; set; } = new List<DatasetIM>();
    }

    public class ModuleTypeConverter : JsonConverter<ModuleType>
    {
        public override ModuleType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return ModuleType.AssetManager; // Default
            }

            if (reader.TokenType == JsonTokenType.Number)
            {
                var value = reader.GetInt32();
                if (Enum.IsDefined(typeof(ModuleType), value))
                {
                    return (ModuleType)value;
                }
                return ModuleType.AssetManager; // Default
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                var stringValue = reader.GetString();
                if (string.IsNullOrWhiteSpace(stringValue))
                {
                    return ModuleType.AssetManager; // Default
                }

                // Intentar parsear como nombre del enum (case-insensitive)
                if (Enum.TryParse<ModuleType>(stringValue, true, out var enumValue))
                {
                    return enumValue;
                }

                // Mapear valores comunes que pueden venir del backend
                var normalized = stringValue.Trim().ToLowerInvariant();
                return normalized switch
                {
                    "insightmonitor" or "im" or "0" => ModuleType.InsightMonitor,
                    "urbanmonitor" or "um" or "1" => ModuleType.UrbanMonitor,
                    "eventmanager" or "em" or "2" => ModuleType.EventManager,
                    "assetmanager" or "am" or "3" => ModuleType.AssetManager,
                    _ => ModuleType.AssetManager // Default si no se reconoce
                };
            }

            return ModuleType.AssetManager; // Default
        }

        public override void Write(Utf8JsonWriter writer, ModuleType value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
