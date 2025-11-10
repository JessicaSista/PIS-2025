using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace OmniMonitor.Server.Security
{
    /// <summary>
    /// Handler que verifica si el usuario tiene el permiso requerido en sus claims
    /// </summary>
    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly ILogger<PermissionAuthorizationHandler> _logger;

        public PermissionAuthorizationHandler(ILogger<PermissionAuthorizationHandler> logger)
        {
            _logger = logger;
        }

        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context, 
            PermissionRequirement requirement)
        {
            // Verificar si el usuario está autenticado
            if (context.User?.Identity?.IsAuthenticated != true)
            {
                _logger.LogWarning("Usuario no autenticado intentando acceder a recurso protegido");
                return Task.CompletedTask;
            }

            // Buscar el claim de permiso en el token JWT
            var hasPermission = context.User.HasClaim(c => 
                c.Type == "permission" && c.Value == requirement.PermissionName);

            if (hasPermission)
            {
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation(
                    "Usuario {UserId} autorizado con permiso {Permission}", 
                    userId, 
                    requirement.PermissionName);
                
                context.Succeed(requirement);
            }
            else
            {
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userPermissions = context.User.Claims
                    .Where(c => c.Type == "permission")
                    .Select(c => c.Value)
                    .ToList();

                _logger.LogWarning(
                    "Usuario {UserId} NO autorizado para {Permission}. Permisos actuales: {Permissions}", 
                    userId, 
                    requirement.PermissionName,
                    string.Join(", ", userPermissions));
            }

            return Task.CompletedTask;
        }
    }
}

