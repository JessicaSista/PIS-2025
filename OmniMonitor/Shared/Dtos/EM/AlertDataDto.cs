namespace OmniMonitor.Shared.Dtos.EM
{
    public class AlertDataDto
    {
        public int Id { get; set; }
        public string Creator { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<object> FieldValues { get; set; } = new();
        public string UserName { get; set; } = string.Empty;
        public int UserId { get; set; }
    }
}
