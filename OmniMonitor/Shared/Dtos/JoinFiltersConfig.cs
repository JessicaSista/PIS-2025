using System.Collections.Generic;

namespace OmniMonitor.Shared.Dtos
{
    public class JoinFiltersConfig
    {
        public OperandFilterConfig? LeftOperandFilters { get; set; }
        public OperandFilterConfig? RightOperandFilters { get; set; }
    }

    public class OperandFilterConfig
    {
        public List<FilterCondition> Filters { get; set; } = new();
    }
}