using System.Text.Json;
using System.Text.Json.Serialization;

namespace OmniMonitor.Shared.Dtos
{
    public class PropiedadEntidadDto
    {
        public string Nombre { get; set; } = string.Empty;
        [JsonConverter(typeof(FilterValueTypeConverter))]
        public FilterValueType Tipo { get; set; }
    }

    public class FilterValueTypeConverter : JsonConverter<FilterValueType>
    {
        public override FilterValueType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var stringValue = reader.GetString();
                if (string.IsNullOrWhiteSpace(stringValue))
                {
                    return FilterValueType.String; // Default
                }

                // Intentar parsear como nombre del enum (case-insensitive)
                if (Enum.TryParse<FilterValueType>(stringValue, true, out var enumValue))
                {
                    return enumValue;
                }

                // Mapear valores comunes que pueden venir del backend
                var normalized = stringValue.Trim().ToLowerInvariant();
                return normalized switch
                {
                    "string" or "text" or "str" => FilterValueType.String,
                    "number" or "numeric" or "int" or "integer" or "double" or "decimal" or "float" => FilterValueType.Number,
                    "date" or "datetime" or "time" => FilterValueType.Date,
                    "enum" or "enumeration" => FilterValueType.Enum,
                    "boolean" or "bool" => FilterValueType.Boolean,
                    _ => FilterValueType.String // Default si no se reconoce
                };
            }

            if (reader.TokenType == JsonTokenType.Number)
            {
                var value = reader.GetInt32();
                if (Enum.IsDefined(typeof(FilterValueType), value))
                {
                    return (FilterValueType)value;
                }
                return FilterValueType.String; // Default
            }

            return FilterValueType.String; // Default
        }

        public override void Write(Utf8JsonWriter writer, FilterValueType value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}

