namespace OmniMonitor.Shared.Dtos
{
    public class DatasetDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty; 
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public int RecordCount { get; set; } = 0;
        public string Module { get; set; } = "Insight Monitor"; // "Insight Monitor", "Asset Manager", "Urban Monitor"
    }

    public class DatasetDtoGenerico
    {
        public int Id { get; set; }
        public int IdGenerico { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public int RecordCount { get; set; } = 0;
        public string Module { get; set; } = "Insight Monitor";
    }
}
