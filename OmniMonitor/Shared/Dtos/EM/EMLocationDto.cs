namespace OmniMonitor.Shared.Dtos.EM
{
    public class EMLocationDto
    {
        public double Lat { get; set; }
        public double Lng { get; set; }
        public string Type { get; set; } = string.Empty;

        public override string ToString() => Type;
    }
}
