using System;
using System.Collections.Generic;

using OmniMonitor.Shared.Dtos;

namespace OmniMonitor.Client.Services
{
    public class VisualizationDraftService
    {
        private CreateVisualizationDraft? _draft;

        public void Save(CreateVisualizationDraft draft)
        {
            _draft = draft ?? throw new ArgumentNullException(nameof(draft));
        }

        public bool TryGet(out CreateVisualizationDraft draft)
        {
            if (_draft is not null)
            {
                draft = _draft;
                return true;
            }
            draft = default!;
            return false;
        }

        public void Clear()
        {
            _draft = null;
        }
    }

    public class CreateVisualizationDraft
    {
        public System.Int32 VisualizationId { get; set; }
        public string VisualizationName { get; set; } = string.Empty;
        public string SelectedChartType { get; set; } = "line";
        public DateTime? BeginDate { get; set; }
        public TimeSpan? BeginHour { get; set; }
        public DateTime? EndDate { get; set; }
        public TimeSpan? EndHour { get; set; }
        public List<DatasetSelectionDraft> SelectedDatasets { get; set; } = new();
    }

    public class DatasetSelectionDraft
    {
        public DatasetDtoGenerico Dataset { get; set; }
        public System.String DisplayName { get; set; }
        public System.String Entity { get; set; }
        public System.String EntityAttribute { get; set; }
        public decimal Multiplier { get; set; } = 1.0M;
        public string Color { get; set; } = "#00FFE7";
    }
}
