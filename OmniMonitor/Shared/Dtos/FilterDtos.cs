using System;
using System.Collections.Generic;

namespace OmniMonitor.Shared.Dtos
{
    public enum FilterType
    {
        Equals,
        NotEquals,
        GreaterThan,
        LessThan,
        Contains,
        StartsWith,
        EndsWith,
        In,
        Between
    }

    public enum FilterValueType
    {
        String,
        Number,
        Date,
        Enum,
        Boolean
    }

    public class FilterCondition
    {
        public string AttributeName { get; set; } = string.Empty;
        public FilterType Type { get; set; }
        public object Condition { get; set; } = null!;
        public FilterValueType ValueType { get; set; }
    }
}
