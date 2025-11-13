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

        public async Task<bool> HasPermissionAsync(int userId, string permissionName)
        {
            var deniedClaim = await _context.UserClaims
                .Where(uc => uc.UserId == userId && uc.Permission.Name == permissionName && !uc.IsGranted)
                .AnyAsync();

            if (deniedClaim)
                return false;

            var hasDirectClaim = await _context.UserClaims
                .Where(uc => uc.UserId == userId && uc.Permission.Name == permissionName && uc.IsGranted)
                .AnyAsync();

            if (hasDirectClaim)
                return true;

            var hasPermissionFromRole = await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .SelectMany(ur => ur.Role.RolePermissions)
                .Select(rp => rp.Permission)
                .AnyAsync(p => p.Name == permissionName);

            return hasPermissionFromRole;
        }

        public async Task<bool> HasRoleAsync(int userId, string roleName)
        {
            var hasRole = await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.Role)
                .AnyAsync(r => r.Name == roleName);

            return hasRole;
        }

        public async Task<List<string>> GetUserRolesAsync(int userId)
        {
            var roles = await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.Role.Name)
                .ToListAsync();

            return roles;
        }

        public async Task<List<Permission>> GetUserPermissionsAsync(int userId)
        {
            var rolePermissions = await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .SelectMany(ur => ur.Role.RolePermissions)
                .Select(rp => rp.Permission)
                .ToListAsync();

            var userClaimPermissions = await _context.UserClaims
                .Where(uc => uc.UserId == userId && uc.IsGranted)
                .Select(uc => uc.Permission)
                .ToListAsync();

            var revokedPermissionIds = await _context.UserClaims
                .Where(uc => uc.UserId == userId && !uc.IsGranted)
                .Select(uc => uc.PermissionId)
                .ToListAsync();

            var allPermissions = rolePermissions.Concat(userClaimPermissions)
                .Where(p => !revokedPermissionIds.Contains(p.Id))
                .DistinctBy(p => p.Id)
                .ToList();

            return allPermissions;
        }

        public async Task<List<string>> GetUserPermissionClaimsAsync(int userId)
        {
            var permissions = await GetUserPermissionsAsync(userId);
            return permissions.Select(p => p.Name).ToList();
        }

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
