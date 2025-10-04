using OmniMonitor.Shared.Dtos.EM;
using OmniMonitor.Shared.Dtos.AM;

namespace OmniMonitor.Shared.Dtos.EM
{
    public class EMUserDto
    {
        public string? Id { get; set; }
        public UserStatusDto? StatusDto { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? Name { get; set; }
        public string? LastName { get; set; }
        public string? NewPassword { get; set; }
        public string? ConfirmPassword { get; set; }
        public string? OldPassword { get; set; }
        public string? Picture { get; set; }
        public string? Signature { get; set; }
        public List<RoleDto>? Roles { get; set; }
        public List<string>? RoleIds { get; set; }
        public bool CurrentlyInShift { get; set; }
    }
}
