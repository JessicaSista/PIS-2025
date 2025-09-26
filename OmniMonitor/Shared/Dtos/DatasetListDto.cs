namespace OmniMonitor.Shared.Dtos
{
    public class DatasetListDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string EsDataset { get; set; } = string.Empty;
        public string? TipoEntidad { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaModificacion { get; set; }
        public int RecordCount { get; set; }
    }

    public class DatasetListRequestDto
    {
        public string? EntityType { get; set; }
        public string? SearchText { get; set; }
        public string? OrderBy { get; set; } = "Nombre";
        public bool OrderDescending { get; set; } = false;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class DatasetListResponseDto
    {
        public List<DatasetListDto> Datasets { get; set; } = new List<DatasetListDto>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}
