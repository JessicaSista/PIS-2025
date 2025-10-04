namespace OmniMonitor.Shared.Dtos.EM
{
    public class UserStatusDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Color { get; set; }
        public string ChangeDate { get; set; } = string.Empty;
        public int AvaibleStatusId { get; set; }
        public string NotAvailableAllowedTime { get; set; } = string.Empty;
    }
}
