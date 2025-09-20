using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging; // Required for logging
using OmniMonitor.Server.Services;
using System.Linq; // Required for seeing all claims
using System.Security.Claims; // Required for ClaimTypes
using System.Threading.Tasks;
using System;

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
            // --- DEBUGGING CODE ---
            // Get the logger service to print debug information to the server console
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<RequirePermissionAttribute>>();
            logger.LogInformation("--- [RequirePermission] Authorization Check Started ---");
            logger.LogInformation("Required Permission: {Permission}", _permissionName);

            // Print all claims for the current user
            var allClaims = context.HttpContext.User.Claims.Select(c => $"{c.Type}: {c.Value}");
            logger.LogInformation("User Claims: {Claims}", string.Join(" | ", allClaims));
            // --- END OF DEBUGGING CODE ---

            var authorizationService = context.HttpContext.RequestServices.GetRequiredService<IAuthorizationService>();

            var userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                logger.LogWarning("Authorization FAILED: UserId claim not found or invalid. Claim was: {ClaimValue}", userIdClaim?.Value ?? "NULL");
                context.Result = new UnauthorizedResult(); // User is not authenticated or ID is missing
                return;
            }

            logger.LogInformation("Successfully parsed UserId: {UserId}", userId);

            var hasPermission = await authorizationService.HasPermissionAsync(userId, _permissionName);
            if (!hasPermission)
            {
                logger.LogWarning("Authorization FAILED: User {UserId} does not have permission '{Permission}'", userId, _permissionName);
                context.Result = new ForbidResult(); // User is authenticated but not authorized
                return;
            }

            logger.LogInformation("Authorization SUCCEEDED for User {UserId} with Permission '{Permission}'", userId, _permissionName);
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
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<RequireRoleAttribute>>();
            logger.LogInformation("--- [RequireRole] Authorization Check Started ---");
            logger.LogInformation("Required Role: {Role}", _roleName);

            var authorizationService = context.HttpContext.RequestServices.GetRequiredService<IAuthorizationService>();

            var userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                logger.LogWarning("Authorization FAILED: UserId claim not found or invalid.");
                context.Result = new UnauthorizedResult();
                return;
            }

            var hasRole = await authorizationService.HasRoleAsync(userId, _roleName);
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

