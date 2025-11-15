using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using OmniMonitor.Server.Services;
using OmniMonitor.Shared.Dtos;

namespace OmniMonitor.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest loginRequest)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new LoginResponse
                    {
                        Success = false,
                        Message = "Datos de entrada inválidos",
                    });
                }

                _logger.LogInformation($"Intento de login para usuario: {loginRequest.Username}");

                LoginResponse result = await _authService.LoginAsync(loginRequest);

                if (result.Success)
                {
                    _logger.LogInformation($"Login exitoso para usuario: {loginRequest.Username}");

                    return Ok(result);
                }
                else
                {
                    _logger.LogWarning($"Login fallido para usuario: {loginRequest.Username} - {result.Message}");
                    return Unauthorized(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error durante el login para usuario: {loginRequest.Username}");
                return StatusCode(500, new LoginResponse
                {
                    Success = false,
                    Message = "Error interno del servidor",
                });
            }
        }
    }
}
