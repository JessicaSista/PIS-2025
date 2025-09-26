using OmniMonitor.Shared.Dtos;

namespace OmniMonitor.Server.Services
{
    /// <summary>
    /// Interfaz para validadores de módulos de SONDA
    /// Cada módulo (IM, AM, UM, etc.) implementará su propio validador
    /// </summary>
    public interface IDatasetModuleValidator
    {
        /// <summary>
        /// Nombre del módulo (IM, AM, UM, etc.)
        /// </summary>
        string ModuleName { get; }

        /// <summary>
        /// Tipos de entidades soportadas por este módulo
        /// </summary>
        List<string> SupportedEntityTypes { get; }

        /// <summary>
        /// Valida los miembros de un dataset contra las APIs del módulo
        /// </summary>
        Task<DatasetValidationResultDto> ValidateDatasetMembersAsync(
            DatasetValidationRequestDto validationRequest, 
            string username, 
            string password);

        /// <summary>
        /// Obtiene información detallada de las entidades para el dataset
        /// </summary>
        Task<List<EntityInfoDto>> GetEntityInfoAsync(
            List<int> entityIds, 
            string entityType, 
            string username, 
            string password);
    }

    /// <summary>
    /// DTO genérico para información de entidades
    /// </summary>
    public class EntityInfoDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public Dictionary<string, object> AdditionalProperties { get; set; } = new Dictionary<string, object>();
    }
}
