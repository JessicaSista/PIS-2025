using System.Net.Http.Json;
using OmniMonitor.Shared.Dtos;

namespace OmniMonitor.Client.Services
{
    public interface IAuthService
    {
        User? CurrentUser { get; }
        List<string>? CurrentUserRoles { get; }
        bool IsAuthenticated { get; }
        Task<LoginResponse> LoginAsync(LoginRequest loginRequest);
        Task LogoutAsync();
        event Action? OnAuthenticationStateChanged;
    }

    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly IAuthorizationService _authorizationService;
        private readonly ILogger<AuthService> _logger;

        public User? CurrentUser { get; private set; }
        public List<string>? CurrentUserRoles { get; private set; }
        public bool IsAuthenticated => CurrentUser != null;

        public event Action? OnAuthenticationStateChanged;

        public AuthService(HttpClient httpClient, IAuthorizationService authorizationService, ILogger<AuthService> logger)
        {
            _httpClient = httpClient;
            _authorizationService = authorizationService;
            _logger = logger;
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest loginRequest)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/auth/login", loginRequest);
                
                if (response.IsSuccessStatusCode)
                {
                    var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
                    
                    if (loginResponse?.Success == true && loginResponse.User != null)
                    {
                        CurrentUser = loginResponse.User;
                        CurrentUserRoles = loginResponse.Roles ?? new List<string>();
                        
                        // Inicializar el servicio de autorización con la información del usuario
                        _authorizationService.InitializeUser(CurrentUser, CurrentUserRoles);
                        
                        OnAuthenticationStateChanged?.Invoke();
                        
                        _logger.LogInformation("Usuario {Username} autenticado exitosamente con roles: {Roles}", 
                            CurrentUser.Username, string.Join(", ", CurrentUserRoles));
                    }
                    
                    return loginResponse ?? new LoginResponse { Success = false, Message = "Error en la respuesta del servidor" };
                }
                else
                {
                    var errorResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
                    return errorResponse ?? new LoginResponse { Success = false, Message = "Error de autenticación" };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante el login");
                return new LoginResponse { Success = false, Message = "Error de conexión" };
            }
        }

        public async Task LogoutAsync()
        {
            try
            {
                CurrentUser = null;
                CurrentUserRoles = null;
                
                // Limpiar el servicio de autorización
                _authorizationService.ClearUser();
                
                OnAuthenticationStateChanged?.Invoke();
                
                _logger.LogInformation("Usuario deslogueado");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante el logout");
            }
        }
    }
}
