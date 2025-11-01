using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniMonitor.Shared.Dtos
{
    public class CreateDatasetRequest
    {
        public CreateDatasetRequest(string name, string username, ModuleType Module)
        {
            Name = name;
            Username = username;
            TipoDataset = Module;
        }

        public string Name { get; set; }
        public string Username { get; set; }
        public ModuleType TipoDataset { get; set; }

    }
}
