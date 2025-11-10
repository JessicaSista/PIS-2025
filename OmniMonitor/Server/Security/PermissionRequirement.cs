using Microsoft.AspNetCore.Authorization;

namespace OmniMonitor.Server.Security
{
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public string PermissionName { get; }

        public PermissionRequirement(string permissionName)
        {
            PermissionName = permissionName;
        }
    }
}

