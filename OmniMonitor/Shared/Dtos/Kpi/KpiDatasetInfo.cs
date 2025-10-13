namespace OmniMonitor.Shared.Dtos.Kpi
{
    /// <summary>
    /// Información sobre el dataset utilizado para el cálculo del KPI
    /// </summary>
    public class KpiDatasetInfo
    {
        /// <summary>
        /// ID del dataset
        /// </summary>
        public int DatasetId { get; set; }

        /// <summary>
        /// Nombre del dataset
        /// </summary>
        public string DatasetName { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de dataset
        /// </summary>
        public string DatasetType { get; set; } = string.Empty;

        /// <summary>
        /// Descripción del dataset
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Usuario propietario del dataset
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Número total de registros procesados
        /// </summary>
        public int TotalRecords { get; set; }

        /// <summary>
        /// Número de registros que pasaron los filtros
        /// </summary>
        public int FilteredRecords { get; set; }

        /// <summary>
        /// Fuente de los datos: "cache", "sonda", "database"
        /// </summary>
        public string DataSource { get; set; } = "sonda";
    }
}