using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniMonitor.Shared.Dtos
{
    public class ValidateSharePasswordRequestDto
    {
        [Required]
        public string Password { get; set; }
    }
}
