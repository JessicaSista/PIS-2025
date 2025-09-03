namespace OmniMonitor.Shared.Dtos
{
    public class Item
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal Precio { get; set; }
        public string Categoria { get; set; } = string.Empty;
    }
}
