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
        private readonly IPermissionService _permissionService;


        // Inject IConfiguration to access appsettings.json for JWT settings
        public AuthService(ApplicationDbContext context, IConfiguration configuration, UserManager<User> UserManager, SignInManager<User> SignInManager, IPermissionService permissionService)
        {
            _context = context;
            _configuration = configuration;
            _userManager = UserManager;
            _signInManager = SignInManager;
            _permissionService = permissionService;
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

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.UserName)
                };

                // Agregar roles personalizados (de la tabla Roles, no AspNetRoles)
                var roles = await _permissionService.GetUserRolesAsync(user.Id);
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                // Agregar permisos como claims (formato: permission:Module.Action)
                var permissions = await _permissionService.GetUserPermissionClaimsAsync(user.Id);
                foreach (var permission in permissions)
                {
                    claims.Add(new Claim("permission", permission));
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
