using System.Text.Json.Serialization;

namespace OmniMonitor.Shared.Dtos.AM
{
    public class EventTaskInstanceDto
    {
    public int Id { get; set; }
    public EventTaskDto EventTaskDto { get; set; }
    public DateTime StartDate { get; set; }
    public string State { get; set; }
    public string Subject { get; set; }
    public DateTime? FinalizedDate { get; set; }
    public DateTime? TakenOn { get; set; }
    public UserDto TakenBy { get; set; }
    public DateTime? AlertTime { get; set; }
    public string EndingReason { get; set; }
    public List<TaskActionDto> ActionDtos { get; set; }
    public bool? Critical { get; set; }
    public List<StockDto> StockDtos { get; set; }
    public Dictionary<string, int> StockQuantities { get; set; }

    }
} 
