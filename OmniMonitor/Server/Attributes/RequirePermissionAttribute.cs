using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OmniMonitor.Server.Services;

namespace OmniMonitor.Server.Attributes
{
    /// <summary>
    /// Atributo para verificar permisos específicos en controladores y acciones
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequirePermissionAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly string _permissionName;

        public RequirePermissionAttribute(string permissionName)
        {
            _permissionName = permissionName;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            // Obtener el servicio de autorización
            var authorizationService = context.HttpContext.RequestServices.GetRequiredService<OmniMonitor.Server.Services.IAuthorizationService>();

            // Obtener el ID del usuario desde el contexto (asumiendo que se almacena en Claims)
            var userIdClaim = context.HttpContext.User.FindFirst("UserId");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            // Verificar si el usuario tiene el permiso requerido
            var hasPermission = await authorizationService.HasPermissionAsync(userId, _permissionName);
            if (!hasPermission)
            {
                context.Result = new ForbidResult();
                return;
            }
        }
    }

    /// <summary>
    /// Atributo para verificar roles específicos en controladores y acciones
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireRoleAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly string _roleName;

        public RequireRoleAttribute(string roleName)
        {
            _roleName = roleName;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            // Obtener el servicio de autorización
            var authorizationService = context.HttpContext.RequestServices.GetRequiredService<OmniMonitor.Server.Services.IAuthorizationService>();

            // Obtener el ID del usuario desde el contexto
            var userIdClaim = context.HttpContext.User.FindFirst("UserId");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            // Verificar si el usuario tiene el rol requerido
            var hasRole = await authorizationService.HasRoleAsync(userId, _roleName);
            if (!hasRole)
            {
                context.Result = new ForbidResult();
                return;
            }
        }
    }
}
