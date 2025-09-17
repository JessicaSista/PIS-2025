using System.ComponentModel.DataAnnotations;

namespace OmniMonitor.Shared.Dtos
{
    /// <summary>
    /// Request para validar datos externos (sensores, dispositivos, fuentes y grupos)
    /// </summary>
    public class ValidateExternalDataRequest
    {
        public List<string> SensorIds { get; set; } = new List<string>();
        public List<int> DeviceIds { get; set; } = new List<int>();
        public List<int> SourceIds { get; set; } = new List<int>();
        public List<int> DeviceGroupIds { get; set; } = new List<int>();
    }
}
