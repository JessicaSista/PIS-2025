using Microsoft.AspNetCore.Identity;
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
    }

    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;


        // Inject IConfiguration to access appsettings.json for JWT settings
        public AuthService(ApplicationDbContext context, IConfiguration configuration, UserManager<User> UserManager, SignInManager<User> SignInManager)
        {
            _context = context;
            _configuration = configuration;
            _userManager = UserManager;
            _signInManager = SignInManager;
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest loginRequest)
        {
            try
            {
                var user = await _userManager.FindByNameAsync(loginRequest.Username);

                if (user == null)
                {
                    return new LoginResponse { Success = false, Message = "Usuario no encontrado" };
                }

                var result = await _signInManager.CheckPasswordSignInAsync(user, loginRequest.Password, lockoutOnFailure: false);

                if (!result.Succeeded)
                {
                    return new LoginResponse { Success = false, Message = "Contraseña incorrecta" };
                }

                var roles = await _userManager.GetRolesAsync(user);

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    // Use UserName property from IdentityUser
                    new Claim(ClaimTypes.Name, user.UserName)
                };

                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                // 2. Get the secret key from appsettings.json
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

                // 3. Create the token object
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.UtcNow.AddDays(1),
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

                await _userManager.UpdateAsync(user);
                // 5. Return the successful response with the token included
                return new LoginResponse
                {
                    Success = true,
                    Message = "Login exitoso",
                    Token = tokenString, // The token is now included
                    UserId = user.Id,
                    Username = user.UserName
                };
            }
            catch (Exception ex)
            {
                // Log the exception
                return new LoginResponse { Success = false, Message = $"Error interno: {ex.Message}" };
            }
        }
    }
}
