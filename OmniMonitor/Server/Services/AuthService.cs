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
                // Buscar usuario en la base de datos
                var user = await _context.Users
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

                // Login exitoso sin token
                return new LoginResponse
                {
                    Success = true,
                    Message = "Login exitoso",
                    User = user
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
