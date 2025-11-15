using System.Collections.Generic;

namespace OmniMonitor.Shared.Dtos
{
    public class FiltrarDatosRequest
    {
        public string Modulo { get; set; } = string.Empty;
        public int EntidadId { get; set; }
        public List<FilterCondition> Filtros { get; set; } = new();
    }
}
