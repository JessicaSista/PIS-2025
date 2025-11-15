using System.Collections.Generic;

namespace OmniMonitor.Shared.Dtos
{
    public class CreateDatasetUMFilteredRequest
    {
        public CreateDatasetUMRequest DatasetRequest { get; set; } = new CreateDatasetUMRequest();
        public List<FilterCondition> Filters { get; set; } = new List<FilterCondition>();
    }
}

