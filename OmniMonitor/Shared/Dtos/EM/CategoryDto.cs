using System.Text.Json;
using System.Text.Json.Serialization;

namespace OmniMonitor.Shared.Dtos.EM
{
    public class CategoryDto
    {
      
        public int Id { get; set; }
        public bool Active { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Priority { get; set; } = string.Empty;
        public List<CategoryGroupDto>? Groups { get; set; }
        public List<int>? GroupIds { get; set; }
        public string? Protocol { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("CategoryId")]
        public int CategoryId { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("CategoryState")]
        public string CategoryState { get; set; } = string.Empty;
        [System.Text.Json.Serialization.JsonPropertyName("typeCategoryEvent")]
        [JsonConverter(typeof(NullableTypeEventCategoryConverter))]
        public TypeEventCategory? TypeCategoryEvent { get; set; }
        public bool Selected { get; set; }
        public List<object>? ActionsDtos { get; set; }
        public List<object>? WorkZones { get; set; }
    }

    public enum TypeEventCategory
    {
        Type0 = 0,
        Type1 = 1
    }

    public class NullableTypeEventCategoryConverter : JsonConverter<TypeEventCategory?>
    {
        public override TypeEventCategory? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            if (reader.TokenType == JsonTokenType.Number)
            {
                var value = reader.GetInt32();
                if (Enum.IsDefined(typeof(TypeEventCategory), value))
                {
                    return (TypeEventCategory)value;
                }
                // Si el valor no está en el enum, retornar null en lugar de lanzar excepción
                return null;
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                var stringValue = reader.GetString();
                if (int.TryParse(stringValue, out var intValue) && Enum.IsDefined(typeof(TypeEventCategory), intValue))
                {
                    return (TypeEventCategory)intValue;
                }
                if (Enum.TryParse<TypeEventCategory>(stringValue, true, out var enumValue))
                {
                    return enumValue;
                }
                return null;
            }

            return null;
        }

        public override void Write(Utf8JsonWriter writer, TypeEventCategory? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
            {
                writer.WriteNumberValue((int)value.Value);
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }
}
