using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OmniMonitor.Server.Services;

namespace OmniMonitor.Server.Attributes
{
    /// <summary>
    /// Atributo para verificar permisos específicos en controladores y acciones.
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
            ILogger<RequirePermissionAttribute> logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<RequirePermissionAttribute>>();
            logger.LogInformation("--- [RequirePermission] Authorization Check Started ---");
            logger.LogInformation("Required Permission: {Permission}", _permissionName);

            // Print all claims for the current user
            IEnumerable<string> allClaims = context.HttpContext.User.Claims.Select(c => $"{c.Type}: {c.Value}");
            logger.LogInformation("User Claims: {Claims}", string.Join(" | ", allClaims));

            // --- END OF DEBUGGING CODE ---
            IAuthorizationService authorizationService = context.HttpContext.RequestServices.GetRequiredService<IAuthorizationService>();

            Claim? userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                logger.LogWarning("Authorization FAILED: UserId claim not found or invalid. Claim was: {ClaimValue}", userIdClaim?.Value ?? "NULL");
                context.Result = new UnauthorizedResult(); // User is not authenticated or ID is missing
                return;
            }

            logger.LogInformation("Successfully parsed UserId: {UserId}", userId);

            bool hasPermission = await authorizationService.HasPermissionAsync(userId, _permissionName);
            if (!hasPermission)
            {
                logger.LogWarning("Authorization FAILED: User {UserId} does not have permission '{Permission}'", userId, _permissionName);
                context.Result = new ForbidResult(); // User is authenticated but not authorized
                return;
            }

            logger.LogInformation("Authorization SUCCEEDED for User {UserId} with Permission '{Permission}'", userId, _permissionName);
        }
    }
}