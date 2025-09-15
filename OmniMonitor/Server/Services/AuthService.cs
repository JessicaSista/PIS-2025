using Microsoft.EntityFrameworkCore;
using OmniMonitor.Server.Context;
using OmniMonitor.Shared.Dtos;

namespace OmniMonitor.Server.Services
{
    public interface IAuthService
    {
        Task<LoginResponse> LoginAsync(LoginRequest loginRequest);
        Task<bool> ValidateUserAsync(string username, string password);
    }

    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;

        public AuthService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest loginRequest)
        {
            try
            {
                // Buscar usuario en la base de datos con sus roles
                var user = await _context.Users
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Username == loginRequest.Username);

                if (user == null)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Usuario no encontrado"
                    };
                }

                // Verificar contraseña (comparación directa por simplicidad)
                if (user.Password != loginRequest.Password)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Contraseña incorrecta"
                    };
                }

                // Obtener roles del usuario
                var userRoles = user.UserRoles
                    .Select(ur => ur.Role.Name)
                    .ToList();

                // Login exitoso con información de roles
                return new LoginResponse
                {
                    Success = true,
                    Message = "Login exitoso",
                    User = user,
                    Roles = userRoles
                };
            }
            catch (Exception ex)
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = $"Error interno: {ex.Message}"
                };
            }
        }

        public async Task<bool> ValidateUserAsync(string username, string password)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            return user != null && user.Password == password;
        }
    }
}
