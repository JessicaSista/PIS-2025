using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniMonitor.Shared.Dtos
{
    public class ValidateSharePasswordResponseDto
    {
        public bool IsValid { get; set; }
        public int? DashboardId { get; set; }
    }
}
