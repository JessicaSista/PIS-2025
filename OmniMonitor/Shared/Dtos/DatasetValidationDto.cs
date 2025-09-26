namespace OmniMonitor.Shared.Dtos
{
    public class DatasetValidationResultDto
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<int> InvalidDeviceIds { get; set; } = new List<int>();
        public List<int> InvalidSourceIds { get; set; } = new List<int>();
        public List<int> InvalidGroupIds { get; set; } = new List<int>();
        public List<int> InvalidSensorIds { get; set; } = new List<int>();
    }

    public class DatasetValidationRequestDto
    {
        public string TipoEntidad { get; set; } = string.Empty;
        public List<int>? IdDevices { get; set; }
        public int? IdSource { get; set; }
        public int? IdGroup { get; set; }
        public int IdSensor { get; set; }
    }
}
