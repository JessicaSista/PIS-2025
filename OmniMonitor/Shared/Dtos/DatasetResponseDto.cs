namespace OmniMonitor.Shared.Dtos
{
    public class DatasetResponseDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string EsDataset { get; set; } = string.Empty;
        public int IdUsuario { get; set; }
        public string? GrupoDevice { get; set; }
        public int? IdSource { get; set; }
        public int? IdGroup { get; set; }
        public int IdSensor { get; set; }
        public string? TipoEntidad { get; set; }
        public string? Modulo { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaModificacion { get; set; }
        public int RecordCount { get; set; }
        public List<DeviceInfoDto>? Devices { get; set; }
    }

    public class DeviceInfoDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? GrupoDevice { get; set; }
    }
}
