namespace OmniMonitor.Shared.Dtos
{
    /// <summary>
    /// Response para obtener un dashboard completo
    /// </summary>
    public class DashboardResponse
    {
        public int IdDashboard { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public int? GrupoVisualizacion { get; set; }
        public string? JsonDiseno { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaModificacion { get; set; }
        
        /// <summary>
        /// Layout completo del dashboard con todas las tarjetas
        /// </summary>
        public DashboardLayout? Layout { get; set; }
        
        /// <summary>
        /// Lista de visualizaciones (tarjetas) con sus configuraciones
        /// </summary>
        public List<DashboardCardResponse> Tarjetas { get; set; } = new List<DashboardCardResponse>();
    }

    /// <summary>
    /// Response para una visualización específica en el dashboard
    /// </summary>
    public class DashboardCardResponse
    {
    public int IdGrupoVisualizacion { get; set; }
    public int CardId { get; set; } // IdVisualizacion
    public int TipoCard { get; set; }
    public string? PropsConfiguracion { get; set; }
    public DateTime FechaAgregado { get; set; }
    /// <summary>
    /// Información básica de la visualización asociada
    /// </summary>
    public VisualizacionInfo? Visualizacion { get; set; }
    }

    /// <summary>
    /// Información básica de una visualización
    /// </summary>
    public class VisualizacionInfo
    {
        public int IdVisualizacion { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public DateTime FechaDesde { get; set; }
        public DateTime FechaHasta { get; set; }
        public string? JsonDesign { get; set; }
    }

    /// <summary>
    /// Response simplificado para listar dashboards
    /// </summary>
    public class DashboardSummaryResponse
    {
        public int IdDashboard { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaModificacion { get; set; }
        public int CantidadTarjetas { get; set; }
    }
}
