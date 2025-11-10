using Microsoft.AspNetCore.Authorization;

namespace OmniMonitor.Server.Attributes
{
    /// <summary>
    /// Atributo para verificar permisos específicos en formato Module.Action usando Authorization Policies
    /// Ejemplo de uso: [RequirePermission("Users.Create")]
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class RequirePermissionAttribute : AuthorizeAttribute
    {
        public RequirePermissionAttribute(string permissionName) : base(permissionName)
        {
            // La política se configura con el nombre del permiso
            // Las políticas se registran dinámicamente en Program.cs
        }
    }
}