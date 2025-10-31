using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmniMonitor.Server.Context;
using OmniMonitor.Shared.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmniMonitor.Server.Services
{
    #region Interfaces

    /// <summary>
    /// Servicio para la gestión de autorización de usuarios, roles y permisos.
    /// </summary>
    public interface IAuthorizationService
    {
        /// <summary>
        /// Verifica si un usuario tiene un permiso específico.
        /// </summary>
        /// <param name="userId">ID del usuario.</param>
        /// <param name="permissionName">Nombre del permiso.</param>
        /// <returns>True si el usuario tiene el permiso, false en caso contrario.</returns>
        Task<bool> HasPermissionAsync(int userId, string permissionName);

        /// <summary>
        /// Verifica si un usuario tiene un rol específico.
        /// </summary>
        /// <param name="userId">ID del usuario.</param>
        /// <param name="roleName">Nombre del rol.</param>
        /// <returns>True si el usuario tiene el rol, false en caso contrario.</returns>
        Task<bool> HasRoleAsync(int userId, string roleName);

        /// <summary>
        /// Obtiene todos los roles de un usuario.
        /// </summary>
        /// <param name="userId">ID del usuario.</param>
        /// <returns>Lista de nombres de roles.</returns>
        Task<List<string>> GetUserRolesAsync(int userId);

        /// <summary>
        /// Obtiene todos los permisos de un usuario.
        /// </summary>
        /// <param name="userId">ID del usuario.</param>
        /// <returns>Lista de permisos.</returns>
        Task<List<Permission>> GetUserPermissionsAsync(int userId);

        /// <summary>
        /// Obtiene todos los permisos de un rol.
        /// </summary>
        /// <param name="roleName">Nombre del rol.</param>
        /// <returns>Lista de permisos.</returns>
        Task<List<Permission>> GetRolePermissionsAsync(string roleName);
    }

    #endregion

    #region Classes

    /// <summary>
    /// Implementación del servicio de autorización de usuarios, roles y permisos.
    /// </summary>
    public class AuthorizationService : IAuthorizationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuthorizationService> _logger;

        /// <summary>
        /// Constructor de AuthorizationService.
        /// </summary>
        /// <param name="context">Contexto de base de datos.</param>
        /// <param name="logger">Logger para registrar eventos.</param>
        public AuthorizationService(ApplicationDbContext context, ILogger<AuthorizationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<bool> HasPermissionAsync(int userId, string permissionName)
        {
            try
            {
                _logger.LogInformation("Verificando permiso '{PermissionName}' para usuario {UserId}", permissionName, userId);

                bool hasPermission = await _context.UserRoles
                    .AsNoTracking()
                    .Where(ur => ur.UserId == userId)
                    .SelectMany(ur => ur.Role.RolePermissions)
                    .AnyAsync(rp => string.Equals(rp.Permission.Name, permissionName, StringComparison.Ordinal));

                if (!hasPermission)
                {
                    _logger.LogWarning("El usuario {UserId} no tiene el permiso '{PermissionName}'", userId, permissionName);
                }

                return hasPermission;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando permiso '{PermissionName}' para usuario {UserId}", permissionName, userId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> HasRoleAsync(int userId, string roleName)
        {
            try
            {
                _logger.LogInformation("Verificando rol '{RoleName}' para usuario {UserId}", roleName, userId);

                bool hasRole = await _context.UserRoles
                    .AsNoTracking()
                    .Where(ur => ur.UserId == userId)
                    .AnyAsync(ur => string.Equals(ur.Role.Name, roleName, StringComparison.Ordinal));

                if (!hasRole)
                {
                    _logger.LogWarning("El usuario {UserId} no tiene el rol '{RoleName}'", userId, roleName);
                }

                return hasRole;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando rol '{RoleName}' para usuario {UserId}", roleName, userId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<List<string>> GetUserRolesAsync(int userId)
        {
            try
            {
                _logger.LogInformation("Obteniendo roles para usuario {UserId}", userId);

                List<string> roles = await _context.UserRoles
                    .AsNoTracking()
                    .Where(ur => ur.UserId == userId)
                    .Select(ur => ur.Role.Name)
                    .ToListAsync();

                if (!roles.Any())
                {
                    _logger.LogWarning("El usuario {UserId} no tiene roles asignados", userId);
                }

                return roles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo roles para usuario {UserId}", userId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<List<Permission>> GetUserPermissionsAsync(int userId)
        {
            try
            {
                _logger.LogInformation("Obteniendo permisos para usuario {UserId}", userId);

                List<Permission> permissions = await _context.UserRoles
                    .AsNoTracking()
                    .Where(ur => ur.UserId == userId)
                    .SelectMany(ur => ur.Role.RolePermissions)
                    .Select(rp => rp.Permission)
                    .Distinct()
                    .ToListAsync();

                if (!permissions.Any())
                {
                    _logger.LogWarning("El usuario {UserId} no tiene permisos asignados", userId);
                }

                return permissions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo permisos para usuario {UserId}", userId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<List<Permission>> GetRolePermissionsAsync(string roleName)
        {
            try
            {
                _logger.LogInformation("Obteniendo permisos para el rol '{RoleName}'", roleName);

                List<Permission> permissions = await _context.Roles
                    .AsNoTracking()
                    .Where(r => string.Equals(r.Name, roleName, StringComparison.Ordinal))
                    .SelectMany(r => r.RolePermissions)
                    .Select(rp => rp.Permission)
                    .ToListAsync();

                if (!permissions.Any())
                {
                    _logger.LogWarning("El rol '{RoleName}' no tiene permisos asignados", roleName);
                }

                return permissions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo permisos para el rol '{RoleName}'", roleName);
                throw;
            }
        }
    }

    #endregion
}
