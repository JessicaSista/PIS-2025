using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OmniMonitor.Shared.Dtos.AM
{
    public class TaskTypeConditionsDto
    {
        [JsonPropertyName("assetTypeId")]
        public int AssetTypeId { get; set; }
        [JsonPropertyName("assetTypeName")]
        public string? AssetTypeName { get; set; }
        [JsonPropertyName("attributeId")]
        public int AttributeId { get; set; }
        [JsonPropertyName("fieldName")]
        public string? FieldName { get; set; }
        [JsonPropertyName("fieldVariationCondition")]
        public string? FieldVariationCondition { get; set; }
        [JsonPropertyName("typeOfAttribute")]
        public string? TypeOfAttribute { get; set; }
        [JsonPropertyName("daysDifference")]
        public int DaysDifference { get; set; }
        [JsonPropertyName("alertDaysBeforeStart")]
        public int AlertDaysBeforeStart { get; set; }
        [JsonPropertyName("durationOfTaskInHours")]
        public int DurationOfTaskInHours { get; set; }
        [JsonPropertyName("fieldToShowInSubject")]
        public int? FieldToShowInSubject { get; set; }
        [JsonPropertyName("fieldNameToShowInSubject")]
        public string? FieldNameToShowInSubject { get; set; }
        [JsonPropertyName("personalizedDescription")]
        public string? PersonalizedDescription { get; set; }
        [JsonPropertyName("users")]
        public List<UserDto>? Users { get; set; }
        [JsonPropertyName("groups")]
        public List<GroupDto>? Groups { get; set; }
        [JsonPropertyName("userIds")]
        public List<string>? UserIds { get; set; }
        [JsonPropertyName("groupIds")]
        public List<string>? GroupIds { get; set; }
        [JsonPropertyName("conditionalLastRegisteredValuesDto")]
        public List<ConditionalLastRegisteredValueDto>? ConditionalLastRegisteredValuesDto { get; set; }
    }
}