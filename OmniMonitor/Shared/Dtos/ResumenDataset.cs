namespace OmniMonitor.Shared.Dtos
{
    public class ResumenDataset
    {
        public int ID_Table { get; set; }
        public int ID { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public DateTime? UltimaActualizacion { get; set; }
    }
}
