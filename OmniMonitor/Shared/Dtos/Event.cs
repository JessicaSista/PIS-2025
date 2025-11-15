using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OmniMonitor.Shared.Dtos
{
    public class Event
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("date")]
        public DateTime? Date { get; set; }

        [JsonPropertyName("type")]
        public EventType? Type { get; set; }

        [JsonPropertyName("typeId")]
        public int TypeId { get; set; }

        [JsonPropertyName("location")]
        public Location? Location { get; set; }

        [JsonPropertyName("fieldValues")]
        public List<FieldValue>? FieldValues { get; set; }



        [JsonPropertyName("approvalState")]
        public string? ApprovalState { get; set; }

        //[JsonPropertyName("creator")]
        //public User? Creator { get; set; }

        //[JsonPropertyName("reviewedBy")]
        //public User? ReviewedBy { get; set; }

        [JsonPropertyName("reviewedAt")]
        public DateTime? ReviewedAt { get; set; }

        //[JsonPropertyName("comments")]
        //public List<ExtraInfo>? Comments { get; set; }

        //[JsonPropertyName("ratings")]
        //public List<EventRating>? Ratings { get; set; }

        [JsonPropertyName("firstImage")]
        public string? FirstImage { get; set; }
    }
}
