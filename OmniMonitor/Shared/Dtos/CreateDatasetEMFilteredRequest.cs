using System.Collections.Generic;

namespace OmniMonitor.Shared.Dtos
{
    public class CreateDatasetEMFilteredRequest
    {
        public CreateDatasetEMRequest DatasetRequest { get; set; } = new CreateDatasetEMRequest();
        public List<FilterCondition> Filters { get; set; } = new List<FilterCondition>();
    }
}
