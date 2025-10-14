using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
    public interface IAuthService
    {
        Task<LoginResponse> LoginAsync(LoginRequest loginRequest);
        Task<bool> ValidateUserAsync(string username, string password);
    }

    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;


        // Inject IConfiguration to access appsettings.json for JWT settings
        public AuthService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest loginRequest)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Username == loginRequest.Username);

                if (user == null)
                {
                    return new LoginResponse { Success = false, Message = "Usuario no encontrado" };
                }

                // --- SECURITY WARNING ---
                // Storing and comparing passwords in plain text is highly insecure.
                // In a production application, you MUST hash passwords using a library like BCrypt.Net.
                if (user.Password != loginRequest.Password)
                {
                    return new LoginResponse { Success = false, Message = "Contraseña incorrecta" };
                }

                // --- TOKEN GENERATION LOGIC ---
                // 1. Create the claims for the token (user ID, name, roles)
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username)
                };

                foreach (var userRole in user.UserRoles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, userRole.Role.Name));
                }

                // 2. Get the secret key from appsettings.json
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

                // 3. Create the token object
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.UtcNow.AddDays(1), // Token is valid for 1 day
                    SigningCredentials = creds,
                    Issuer = _configuration["Jwt:Issuer"],
                    Audience = _configuration["Jwt:Audience"]
                };

                // 4. Create and write the token
                var tokenHandler = new JwtSecurityTokenHandler();
                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);

                user.SondaTokenOM = tokenString;
                user.TokenExpirationOM = tokenDescriptor.Expires;

                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                // 5. Return the successful response with the token included
                return new LoginResponse
                {
                    Success = true,
                    Message = "Login exitoso",
                    Token = tokenString, // The token is now included
                    UserId = user.Id,
                    Username = user.Username
                };
            }
            catch (Exception ex)
            {
                // Log the exception
                return new LoginResponse { Success = false, Message = $"Error interno: {ex.Message}" };
            }
        }

        public async Task<bool> ValidateUserAsync(string username, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            // Again, use password hashing in a real application
            return user != null && user.Password == password;
        }
    }
}
