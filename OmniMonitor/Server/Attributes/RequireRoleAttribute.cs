using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OmniMonitor.Server.Services;

namespace OmniMonitor.Server.Attributes
{
    /// <summary>
    /// Atributo para verificar roles específicos en controladores y acciones.
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
            ILogger<RequireRoleAttribute> logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<RequireRoleAttribute>>();
            IPermissionService permissionService = context.HttpContext.RequestServices.GetRequiredService<IPermissionService>();

            Claim? userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                logger.LogWarning("UserId claim no encontrado o inválido");
                context.Result = new UnauthorizedResult();
                return;
            }

            bool hasRole = await permissionService.HasRoleAsync(userId, _roleName);
            if (!hasRole)
            {
                logger.LogWarning("Usuario {UserId} no tiene rol '{Role}'", userId, _roleName);
                context.Result = new ForbidResult();
            }
        }
    }
}