namespace OmniMonitor.Shared.Dtos
{
    public class ReporteDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public int RecordCount { get; set; } = 0;
    }
}
