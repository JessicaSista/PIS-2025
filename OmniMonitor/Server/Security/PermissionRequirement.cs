using Microsoft.AspNetCore.Authorization;

namespace OmniMonitor.Server.Security
{
    /// <summary>
    /// Requirement para verificar permisos específicos en formato Module.Action
    /// </summary>
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public string PermissionName { get; }

        public PermissionRequirement(string permissionName)
        {
            PermissionName = permissionName;
        }
    }
}

