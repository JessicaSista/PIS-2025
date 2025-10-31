using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using OmniMonitor.Server.Context;
using OmniMonitor.Shared.Dtos;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace OmniMonitor.Server.Services
{
    #region Interfaces

    /// <summary>
    /// Servicio de autenticación de usuarios.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Realiza el login de un usuario.
        /// </summary>
        /// <param name="loginRequest">Datos de login.</param>
        /// <returns>Respuesta con el resultado del login.</returns>
        Task<LoginResponse> LoginAsync(LoginRequest loginRequest);
    }

    #endregion

    #region Classes

    /// <summary>
    /// Implementación del servicio de autenticación de usuarios.
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ILogger<AuthService> _logger;

        /// <summary>
        /// Constructor de AuthService.
        /// </summary>
        /// <param name="context">Contexto de base de datos.</param>
        /// <param name="configuration">Configuración de la aplicación.</param>
        /// <param name="userManager">Gestor de usuarios.</param>
        /// <param name="signInManager">Gestor de inicio de sesión.</param>
        /// <param name="logger">Logger para registrar eventos.</param>
        public AuthService(
            ApplicationDbContext context,
            IConfiguration configuration,
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            ILogger<AuthService> logger)
        {
            _context = context;
            _configuration = configuration;
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<LoginResponse> LoginAsync(LoginRequest loginRequest)
        {
            try
            {
                _logger.LogInformation("Intento de login para usuario {Username}", loginRequest.Username);

                if (string.IsNullOrEmpty(loginRequest.Username) || string.IsNullOrEmpty(loginRequest.Password))
                {
                    _logger.LogWarning("Usuario o contraseña vacíos.");
                    return new LoginResponse { Success = false, Message = "Usuario o contraseña vacíos." };
                }

                User? user = await _userManager.FindByNameAsync(loginRequest.Username);

                if (user == null)
                {
                    _logger.LogWarning("Usuario no encontrado: {Username}", loginRequest.Username);
                    return new LoginResponse { Success = false, Message = "Usuario no encontrado" };
                }

                SignInResult result = await _signInManager.CheckPasswordSignInAsync(user, loginRequest.Password, lockoutOnFailure: false);

                if (!result.Succeeded)
                {
                    _logger.LogWarning("Contraseña incorrecta para usuario {Username}", loginRequest.Username);
                    return new LoginResponse { Success = false, Message = "Contraseña incorrecta" };
                }

                List<Claim> claims = new()
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.UserName ?? string.Empty)
                };

                SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
                SigningCredentials creds = new(key, SecurityAlgorithms.HmacSha512Signature);

                SecurityTokenDescriptor tokenDescriptor = new()
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.UtcNow.AddDays(1),
                    SigningCredentials = creds,
                    Issuer = _configuration["Jwt:Issuer"],
                    Audience = _configuration["Jwt:Audience"]
                };

                JwtSecurityTokenHandler tokenHandler = new();
                SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);
                string tokenString = tokenHandler.WriteToken(token);

                user.SondaTokenOM = tokenString;
                user.TokenExpirationOM = tokenDescriptor.Expires;

                await _userManager.UpdateAsync(user);

                _logger.LogInformation("Login exitoso para usuario {Username}", loginRequest.Username);

                return new LoginResponse
                {
                    Success = true,
                    Message = "Login exitoso",
                    Token = tokenString,
                    UserId = user.Id,
                    Username = user.UserName,
                    User = user
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error interno durante el login para usuario {Username}", loginRequest.Username);
                return new LoginResponse { Success = false, Message = $"Error interno: {ex.Message}" };
            }
        }
    }

    #endregion
}
