using System.Net.Http.Json;
using OmniMonitor.Shared.Dtos;

namespace OmniMonitor.Client.Services
{
    public interface IAuthorizationService
    {
        Task<bool> HasPermissionAsync(string permissionName);
        Task<bool> HasRoleAsync(string roleName);
        Task<List<string>> GetUserRolesAsync();
        Task<List<Permission>> GetUserPermissionsAsync();
        Task<List<Role>> GetRolesAsync();
        Task<List<Permission>> GetPermissionsAsync();
    }

    public class AuthorizationService : IAuthorizationService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AuthorizationService> _logger;
        private User? _currentUser;
        private List<string>? _userRoles;
        private List<Permission>? _userPermissions;

        public AuthorizationService(HttpClient httpClient, ILogger<AuthorizationService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        /// <summary>
        /// Inicializa el servicio con la información del usuario actual
        /// </summary>
        public void InitializeUser(User user, List<string>? roles = null)
        {
            _currentUser = user;
            _userRoles = roles ?? new List<string>();
            _userPermissions = null; // Se cargarán bajo demanda
        }

        /// <summary>
        /// Limpia la información del usuario actual
        /// </summary>
        public void ClearUser()
        {
            _currentUser = null;
            _userRoles = null;
            _userPermissions = null;
        }

        /// <summary>
        /// Verifica si el usuario actual tiene un permiso específico
        /// </summary>
        public async Task<bool> HasPermissionAsync(string permissionName)
        {
            if (_currentUser == null)
                return false;

            try
            {
                var response = await _httpClient.GetAsync($"api/authorization/users/{_currentUser.Id}/has-permission?permissionName={permissionName}");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<bool>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando permiso {PermissionName}", permissionName);
            }

            return false;
        }

        /// <summary>
        /// Verifica si el usuario actual tiene un rol específico
        /// </summary>
        public async Task<bool> HasRoleAsync(string roleName)
        {
            if (_currentUser == null)
                return false;

            try
            {
                var response = await _httpClient.GetAsync($"api/authorization/users/{_currentUser.Id}/has-role?roleName={roleName}");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<bool>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando rol {RoleName}", roleName);
            }

            return false;
        }

        /// <summary>
        /// Obtiene los roles del usuario actual
        /// </summary>
        public async Task<List<string>> GetUserRolesAsync()
        {
            if (_userRoles != null)
                return _userRoles;

            if (_currentUser == null)
                return new List<string>();

            try
            {
                var response = await _httpClient.GetAsync($"api/authorization/users/{_currentUser.Id}/roles");
                if (response.IsSuccessStatusCode)
                {
                    _userRoles = await response.Content.ReadFromJsonAsync<List<string>>() ?? new List<string>();
                    return _userRoles;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo roles del usuario");
            }

            return new List<string>();
        }

        /// <summary>
        /// Obtiene los permisos del usuario actual
        /// </summary>
        public async Task<List<Permission>> GetUserPermissionsAsync()
        {
            if (_userPermissions != null)
                return _userPermissions;

            if (_currentUser == null)
                return new List<Permission>();

            try
            {
                var response = await _httpClient.GetAsync($"api/authorization/users/{_currentUser.Id}/permissions");
                if (response.IsSuccessStatusCode)
                {
                    _userPermissions = await response.Content.ReadFromJsonAsync<List<Permission>>() ?? new List<Permission>();
                    return _userPermissions;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo permisos del usuario");
            }

            return new List<Permission>();
        }

        /// <summary>
        /// Obtiene todos los roles disponibles
        /// </summary>
        public async Task<List<Role>> GetRolesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/authorization/roles");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<Role>>() ?? new List<Role>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo roles");
            }

            return new List<Role>();
        }

        /// <summary>
        /// Obtiene todos los permisos disponibles
        /// </summary>
        public async Task<List<Permission>> GetPermissionsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/authorization/permissions");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<Permission>>() ?? new List<Permission>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo permisos");
            }

            return new List<Permission>();
        }

    }
}
