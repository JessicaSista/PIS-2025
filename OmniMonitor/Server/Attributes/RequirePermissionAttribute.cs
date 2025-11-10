using Microsoft.AspNetCore.Authorization;

namespace OmniMonitor.Server.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class RequirePermissionAttribute : AuthorizeAttribute
    {
        public RequirePermissionAttribute(string permissionName) : base(permissionName)
        {
        }
    }
}