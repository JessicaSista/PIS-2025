using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace OmniMonitor.Shared.Dtos
{
    public class VisualizationResponse
    {
        public string Type { get; set; } = string.Empty;
        public List<VisualizationValue> Values { get; set; } = new();
    }

    public class VisualizationValue
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; } 
    }


}
