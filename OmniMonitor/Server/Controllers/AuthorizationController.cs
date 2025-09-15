using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmniMonitor.Server.Attributes;
using OmniMonitor.Server.Context;
using OmniMonitor.Server.Services;
using OmniMonitor.Shared.Dtos;

namespace OmniMonitor.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthorizationController : ControllerBase
    {
        private readonly IAuthorizationService _authorizationService;
        private readonly ApplicationDbContext _context;

        public AuthorizationController(IAuthorizationService authorizationService, ApplicationDbContext context)
        {
            _authorizationService = authorizationService;
            _context = context;
        }

        /// <summary>
        /// Obtiene todos los roles disponibles
        /// </summary>
        [HttpGet("roles")]
        [RequirePermission("Roles", "Read")]
        public async Task<ActionResult<List<Role>>> GetRoles()
        {
            var roles = await _context.Roles.ToListAsync();
            return Ok(roles);
        }

        /// <summary>
        /// Obtiene todos los permisos disponibles
        /// </summary>
        [HttpGet("permissions")]
        [RequirePermission("Permissions", "Read")]
        public async Task<ActionResult<List<Permission>>> GetPermissions()
        {
            var permissions = await _context.Permissions.ToListAsync();
            return Ok(permissions);
        }

        /// <summary>
        /// Obtiene los roles de un usuario específico
        /// </summary>
        [HttpGet("users/{userId}/roles")]
        [RequirePermission("Users", "Read")]
        public async Task<ActionResult<List<string>>> GetUserRoles(int userId)
        {
            var roles = await _authorizationService.GetUserRolesAsync(userId);
            return Ok(roles);
        }

        /// <summary>
        /// Obtiene los permisos de un usuario específico
        /// </summary>
        [HttpGet("users/{userId}/permissions")]
        [RequirePermission("Users", "Read")]
        public async Task<ActionResult<List<Permission>>> GetUserPermissions(int userId)
        {
            var permissions = await _authorizationService.GetUserPermissionsAsync(userId);
            return Ok(permissions);
        }

        /// <summary>
        /// Verifica si un usuario tiene un permiso específico
        /// </summary>
        [HttpGet("users/{userId}/has-permission")]
        [RequirePermission("Ver Usuarios")]
        public async Task<ActionResult<bool>> HasPermission(int userId, [FromQuery] string permissionName)
        {
            var hasPermission = await _authorizationService.HasPermissionAsync(userId, permissionName);
            return Ok(hasPermission);
        }

        /// <summary>
        /// Verifica si un usuario tiene un rol específico
        /// </summary>
        [HttpGet("users/{userId}/has-role")]
        [RequirePermission("Users", "Read")]
        public async Task<ActionResult<bool>> HasRole(int userId, [FromQuery] string roleName)
        {
            var hasRole = await _authorizationService.HasRoleAsync(userId, roleName);
            return Ok(hasRole);
        }

        /// <summary>
        /// Obtiene los permisos de un rol específico
        /// </summary>
        [HttpGet("roles/{roleName}/permissions")]
        [RequirePermission("Roles", "Read")]
        public async Task<ActionResult<List<Permission>>> GetRolePermissions(string roleName)
        {
            var permissions = await _authorizationService.GetRolePermissionsAsync(roleName);
            return Ok(permissions);
        }
    }
}
