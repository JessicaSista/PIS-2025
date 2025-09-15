using Microsoft.EntityFrameworkCore;
using OmniMonitor.Server.Context;
using OmniMonitor.Shared.Dtos;

namespace OmniMonitor.Server.Services
{
    public interface IAuthorizationService
    {
        Task<bool> HasPermissionAsync(int userId, string permissionName);
        Task<bool> HasRoleAsync(int userId, string roleName);
        Task<List<string>> GetUserRolesAsync(int userId);
        Task<List<Permission>> GetUserPermissionsAsync(int userId);
        Task<List<Permission>> GetRolePermissionsAsync(string roleName);
    }

    public class AuthorizationService : IAuthorizationService
    {
        private readonly ApplicationDbContext _context;

        public AuthorizationService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Verifica si un usuario tiene un permiso específico
        /// </summary>
        public async Task<bool> HasPermissionAsync(int userId, string permissionName)
        {
            var hasPermission = await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .SelectMany(ur => ur.Role.RolePermissions)
                .Select(rp => rp.Permission)
                .Any(p => p.Name == permissionName);

            return hasPermission;
        }

        /// <summary>
        /// Verifica si un usuario tiene un rol específico
        /// </summary>
        public async Task<bool> HasRoleAsync(int userId, string roleName)
        {
            var hasRole = await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.Role)
                .Any(r => r.Name == roleName);

            return hasRole;
        }

        /// <summary>
        /// Obtiene todos los roles de un usuario
        /// </summary>
        public async Task<List<string>> GetUserRolesAsync(int userId)
        {
            var roles = await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.Role.Name)
                .ToListAsync();

            return roles;
        }

        /// <summary>
        /// Obtiene todos los permisos de un usuario
        /// </summary>
        public async Task<List<Permission>> GetUserPermissionsAsync(int userId)
        {
            var permissions = await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .SelectMany(ur => ur.Role.RolePermissions)
                .Select(rp => rp.Permission)
                .Distinct()
                .ToListAsync();

            return permissions;
        }

        /// <summary>
        /// Obtiene todos los permisos de un rol
        /// </summary>
        public async Task<List<Permission>> GetRolePermissionsAsync(string roleName)
        {
            var permissions = await _context.Roles
                .Where(r => r.Name == roleName)
                .SelectMany(r => r.RolePermissions)
                .Select(rp => rp.Permission)
                .ToListAsync();

            return permissions;
        }
    }
}
