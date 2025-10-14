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

        [Required(ErrorMessage = "La posición X es obligatoria")]
        [Range(0, 12, ErrorMessage = "La posición X debe estar entre 0 y 12")]
        public int PosicionX { get; set; }

        [Required(ErrorMessage = "La posición Y es obligatoria")]
        [Range(0, 100, ErrorMessage = "La posición Y debe estar entre 0 y 100")]
        public int PosicionY { get; set; }

        [Required(ErrorMessage = "El ancho es obligatorio")]
        [Range(1, 12, ErrorMessage = "El ancho debe estar entre 1 y 12")]
        public int Ancho { get; set; }

        [Required(ErrorMessage = "La altura es obligatoria")]
        [Range(1, 20, ErrorMessage = "La altura debe estar entre 1 y 20")]
        public int Alto { get; set; }

        /// <summary>
        /// Propiedades de configuración específicas de la visualización en el dashboard
        /// </summary>
        public Dictionary<string, object>? Props { get; set; }
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
