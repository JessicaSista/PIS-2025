using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace OmniMonitor.Shared.Dtos
{
    /// <summary>
    /// Request para crear un nuevo dashboard
    /// </summary>
    public class CreateDashboardRequest
    {
        [Required(ErrorMessage = "El nombre del dashboard es obligatorio")]
        [MaxLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres")]
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(500, ErrorMessage = "La descripción no puede exceder los 500 caracteres")]
        public string? Descripcion { get; set; }

        [MaxLength(50, ErrorMessage = "El tema no puede exceder los 50 caracteres")]
        public string? Tema { get; set; }

        /// <summary>
        /// Layout inicial del dashboard (opcional, puede empezar vacío)
        /// </summary>
        public DashboardLayout? Layout { get; set; }
    }

    /// <summary>
    /// Representa el layout de un dashboard con sus tarjetas
    /// </summary>
    public class DashboardLayout
    {
        /// <summary>
        /// Lista de tarjetas en el dashboard
        /// </summary>
        public List<DashboardCard> Tarjetas { get; set; } = new List<DashboardCard>();

        /// <summary>
        /// Configuración general del layout
        /// </summary>
        public LayoutConfig? Configuracion { get; set; }
    }

    /// <summary>
    /// Representa una tarjeta en el dashboard (referencia a una visualización existente)
    /// </summary>
    public class DashboardCard
    {
        [Required(ErrorMessage = "El ID de la visualización es obligatorio")]
        public int CardId { get; set; }

        /// <summary>
        /// Tipo de tarjeta: 1=gráfica, 2=KPI, etc.
        /// </summary>
        public int TipoCard { get; set; }

        /// <summary>
        /// Propiedades de configuración específicas de la visualización en el dashboard
        /// </summary>
        public JsonElement? Props { get; set; }
    }

    /// <summary>
    /// Configuración general del layout - acepta cualquier JSON desde el frontend
    /// </summary>
    public class LayoutConfig
    {
        /// <summary>
        /// Configuración flexible como JSON - puede contener cualquier estructura
        /// que envíe el frontend sin importar su contenido
        /// </summary>
        public JsonElement? Configuracion { get; set; }
    }
}
