using Microsoft.EntityFrameworkCore;
using OmniMonitor.Server.Context;
using OmniMonitor.Shared.Dtos;

namespace OmniMonitor.Server.Services
{
    public interface IPermissionService
    {
        Task<bool> HasPermissionAsync(int userId, string permissionName);
        Task<bool> HasRoleAsync(int userId, string roleName);
        Task<List<string>> GetUserRolesAsync(int userId);
        Task<List<Permission>> GetUserPermissionsAsync(int userId);
        Task<List<Permission>> GetRolePermissionsAsync(string roleName);
        Task<List<string>> GetUserPermissionClaimsAsync(int userId);
    }

    public class PermissionService : IPermissionService
    {
        private readonly ApplicationDbContext _context;

        public PermissionService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Verifica si un usuario tiene un permiso específico
        /// Considera tanto permisos heredados de roles como claims específicos del usuario
        /// </summary>
        public async Task<bool> HasPermissionAsync(int userId, string permissionName)
        {
            // Verificar si el usuario tiene un claim explícito que REVOCA este permiso
            var deniedClaim = await _context.UserClaims
                .Where(uc => uc.UserId == userId && uc.Permission.Name == permissionName && !uc.IsGranted)
                .AnyAsync();

            if (deniedClaim)
                return false;

            // Verificar si tiene el permiso por claims directos
            var hasDirectClaim = await _context.UserClaims
                .Where(uc => uc.UserId == userId && uc.Permission.Name == permissionName && uc.IsGranted)
                .AnyAsync();

            if (hasDirectClaim)
                return true;

            // Verificar si tiene el permiso por roles
            var hasPermissionFromRole = await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .SelectMany(ur => ur.Role.RolePermissions)
                .Select(rp => rp.Permission)
                .AnyAsync(p => p.Name == permissionName);

            return hasPermissionFromRole;
        }

        /// <summary>
        /// Verifica si un usuario tiene un rol específico
        /// </summary>
        public async Task<bool> HasRoleAsync(int userId, string roleName)
        {
            var hasRole = await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.Role)
                .AnyAsync(r => r.Name == roleName);

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
        /// Obtiene todos los permisos de un usuario (roles + claims específicos)
        /// </summary>
        public async Task<List<Permission>> GetUserPermissionsAsync(int userId)
        {
            // Permisos de roles
            var rolePermissions = await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .SelectMany(ur => ur.Role.RolePermissions)
                .Select(rp => rp.Permission)
                .ToListAsync();

            // Claims específicos del usuario (solo granted)
            var userClaimPermissions = await _context.UserClaims
                .Where(uc => uc.UserId == userId && uc.IsGranted)
                .Select(uc => uc.Permission)
                .ToListAsync();

            // Claims revocados del usuario
            var revokedPermissionIds = await _context.UserClaims
                .Where(uc => uc.UserId == userId && !uc.IsGranted)
                .Select(uc => uc.PermissionId)
                .ToListAsync();

            // Combinar y filtrar
            var allPermissions = rolePermissions.Concat(userClaimPermissions)
                .Where(p => !revokedPermissionIds.Contains(p.Id))
                .DistinctBy(p => p.Id)
                .ToList();

            return allPermissions;
        }

        /// <summary>
        /// Obtiene todos los permisos del usuario como claims en formato Module.Action
        /// </summary>
        public async Task<List<string>> GetUserPermissionClaimsAsync(int userId)
        {
            var permissions = await GetUserPermissionsAsync(userId);
            return permissions.Select(p => p.Name).ToList();
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
