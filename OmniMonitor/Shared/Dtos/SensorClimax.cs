using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniMonitor.Shared.Dtos
{
    public class SensorClimax
    {
        public int Id { get; set; }
        public int Temperatura { get; set; }
        public int Humedad { get; set; }
        public int Co2 { get; set; }
        public int Potencia { get; set; }
        public int NivleDeBrillo { get; set; }
        public int NivelDeRuido { get; set; }
        public int HumedadDelSuelo { get; set; }
        public int TemperaturaDelSuelo { get; set; }

    }
}
