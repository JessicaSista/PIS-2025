using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
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
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class AuthorizationController : ControllerBase
    {
        private readonly IPermissionService _permissionService;
        private readonly ApplicationDbContext _context;

        public AuthorizationController(IPermissionService permissionService, ApplicationDbContext context)
        {
            _permissionService = permissionService;
            _context = context;
        }

        /// <summary>
        /// Obtiene todos los roles disponibles.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpGet("roles")]
        [RequirePermission("System.ViewRoles")]
        public async Task<ActionResult<List<Role>>> GetRoles()
        {
            List<Role> roles = await _context.Roles.ToListAsync();
            return Ok(roles);
        }

        /// <summary>
        /// Obtiene todos los permisos disponibles.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpGet("permissions")]
        [RequirePermission("System.ViewPermissions")]
        public async Task<ActionResult<List<Permission>>> GetPermissions()
        {
            List<Permission> permissions = await _context.Permissions.ToListAsync();
            return Ok(permissions);
        }

        /// <summary>
        /// Gets the roles of a specific user.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpGet("users/{userId}/roles")]
        [RequirePermission("Users.View")]
        public async Task<ActionResult<List<string>>> GetUserRoles(int userId)
        {
            List<string> roles = await _permissionService.GetUserRolesAsync(userId);
            return Ok(roles);
        }

        /// <summary>
        /// Gets the permissions of a specific user.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpGet("users/{userId}/permissions")]
        [RequirePermission("Users.View")]
        public async Task<ActionResult<List<Permission>>> GetUserPermissions(int userId)
        {
            List<Permission> permissions = await _permissionService.GetUserPermissionsAsync(userId);
            return Ok(permissions);
        }

        /// <summary>
        /// Verifies if a user has a specific permission.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpGet("users/{userId}/has-permission")]
        [RequirePermission("Users.View")]
        public async Task<ActionResult<bool>> HasPermission(int userId, [FromQuery] string permissionName)
        {
            bool hasPermission = await _permissionService.HasPermissionAsync(userId, permissionName);
            return Ok(hasPermission);
        }

        /// <summary>
        /// Verifies if a user has a specific role.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpGet("users/{userId}/has-role")]
        [RequirePermission("Users.View")]
        public async Task<ActionResult<bool>> HasRole(int userId, [FromQuery] string roleName)
        {
            bool hasRole = await _permissionService.HasRoleAsync(userId, roleName);
            return Ok(hasRole);
        }

        /// <summary>
        /// Gets the permissions of a specific role.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpGet("roles/{roleName}/permissions")]
        [RequirePermission("System.ViewPermissions")]
        public async Task<ActionResult<List<Permission>>> GetRolePermissions(string roleName)
        {
            List<Permission> permissions = await _permissionService.GetRolePermissionsAsync(roleName);
            return Ok(permissions);
        }
    }
}
