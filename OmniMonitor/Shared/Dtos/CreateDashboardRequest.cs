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

        [Required(ErrorMessage = "El nombre de usuario es obligatorio")]
        [MaxLength(256, ErrorMessage = "El nombre de usuario no puede exceder los 256 caracteres")]
        public string Username { get; set; } = string.Empty;

        [MaxLength(500, ErrorMessage = "La descripción no puede exceder los 500 caracteres")]
        public string? Descripcion { get; set; }

        [MaxLength(50, ErrorMessage = "El tema no puede exceder los 50 caracteres")]
        public string? Tema { get; set; }

        /// <summary>
        /// Initial dashboard layout (optional, can start empty)
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
        /// General layout configuration
        /// </summary>
        public LayoutConfig? Configuracion { get; set; }
    }

    /// <summary>
    /// Represents a card in the dashboard (reference to an existing visualization)
    /// </summary>
    public class DashboardCard
    {
        [Required(ErrorMessage = "El ID de la visualización es obligatorio")]
        public int CardId { get; set; }

        /// <summary>
        /// Card type: 1=chart, 2=KPI, etc.
        /// </summary>
        public int TipoCard { get; set; }

        /// <summary>
        /// Configuration properties specific to the visualization in the dashboard
        /// </summary>
        public JsonElement? Props { get; set; }
    }

    /// <summary>
    /// General layout configuration - accepts any JSON from the frontend
    /// </summary>
    public class LayoutConfig
    {
        /// <summary>
        /// Flexible configuration as JSON - can contain any structure
        /// sent by the frontend regardless of its content
        /// </summary>
        public JsonElement? Configuracion { get; set; }
    }
}
