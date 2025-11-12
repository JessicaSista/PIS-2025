using System.Collections.Generic;

namespace OmniMonitor.Shared.Dtos
{
    public class ReportFiltersConfig
    {
        public List<DatasetFilterConfig> DatasetFilters { get; set; } = new();
    }

    public class DatasetFilterConfig
    {
        public int DatasetId { get; set; }
        public ModuleType ModuleType { get; set; }
        public List<FilterCondition> Filters { get; set; } = new();
    }
}