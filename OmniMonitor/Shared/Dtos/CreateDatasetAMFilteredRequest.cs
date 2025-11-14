using System.Collections.Generic;

namespace OmniMonitor.Shared.Dtos
{
    public class CreateDatasetAMFilteredRequest
    {
        public CreateDatasetAMRequest DatasetRequest { get; set; } = new CreateDatasetAMRequest();
        public string Token { get; set; } = string.Empty;
        public List<FilterCondition> Filters { get; set; } = new List<FilterCondition>();
    }
}
