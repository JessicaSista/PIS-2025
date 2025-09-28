using OmniMonitor.Shared.Dtos;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OmniMonitor.Shared.Dtos
{
    public class News
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("datePublished")]
        public DateTime DatePublished { get; set; }


        //No es el mismo tipo de usuario
        //[JsonPropertyName("publisher")]
        //public User Publisher { get; set; } = new User();

        [JsonPropertyName("summary")]
        public string Summary { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("categories")]
        public List<Category>? Categories { get; set; }

        [JsonPropertyName("startDate")]
        public DateTime? StartDate { get; set; }

        [JsonPropertyName("endDate")]
        public DateTime? EndDate { get; set; }

        [JsonPropertyName("important")]
        public bool Important { get; set; }

        //[JsonPropertyName("images")]
        //public List<string>? Images { get; set; }

        //[JsonPropertyName("firstImage")]
        //public string? FirstImage { get; set; }

        //[JsonPropertyName("imagesRemoved")]
        //public List<string>? ImagesRemoved { get; set; }

        [JsonPropertyName("zone")]
        public Zone Zone { get; set; } = new Zone();
    }
}
