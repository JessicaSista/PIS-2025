namespace OmniMonitor.Shared.Dtos
{
    /// <summary>
    /// Respuesta base para operaciones de dataset
    /// </summary>
    public class DatasetResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Dataset? Data { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }

    /// <summary>
    /// Respuesta para lista de datasets
    /// </summary>
    public class DatasetListResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<Dataset> Data { get; set; } = new List<Dataset>();
        public List<string> Errors { get; set; } = new List<string>();
    }

    /// <summary>
    /// Respuesta para opciones de dataset (módulos, sources, groups, sensores, dispositivos)
    /// </summary>
    public class DatasetOptionsResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public DatasetOptionsData? Data { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }

    /// <summary>
    /// Datos de opciones para el modal de nuevo dataset
    /// </summary>
    public class DatasetOptionsData
    {
        public List<string> Modules { get; set; } = new List<string>();
        public List<Source> Sources { get; set; } = new List<Source>();
        public List<DeviceGroup> DeviceGroups { get; set; } = new List<DeviceGroup>();
        public List<Sensor> Sensors { get; set; } = new List<Sensor>();
        public List<Device> Devices { get; set; } = new List<Device>();
    }

    /// <summary>
    /// Respuesta para validación de datos externos
    /// </summary>
    public class ExternalDataValidationResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> ValidSensorIds { get; set; } = new List<string>();
        public List<int> ValidDeviceIds { get; set; } = new List<int>();
        public List<int> ValidSourceIds { get; set; } = new List<int>();
        public List<int> ValidDeviceGroupIds { get; set; } = new List<int>();
    }
}
