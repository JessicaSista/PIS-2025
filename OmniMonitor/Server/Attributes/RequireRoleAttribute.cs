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
            logger.LogInformation("--- [RequireRole] Authorization Check Started ---");
            logger.LogInformation("Required Role: {Role}", _roleName);

            IAuthorizationService authorizationService = context.HttpContext.RequestServices.GetRequiredService<IAuthorizationService>();

            Claim? userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                logger.LogWarning("Authorization FAILED: UserId claim not found or invalid.");
                context.Result = new UnauthorizedResult();
                return;
            }

            bool hasRole = await authorizationService.HasRoleAsync(userId, _roleName);
            if (!hasRole)
            {
                logger.LogWarning("Authorization FAILED: User {UserId} does not have role '{Role}'", userId, _roleName);
                context.Result = new ForbidResult();
                return;
            }

            logger.LogInformation("Authorization SUCCEEDED for User {UserId} with Role '{Role}'", userId, _roleName);
        }
    }
}