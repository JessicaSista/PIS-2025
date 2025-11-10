using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniMonitor.Server.Attributes;

namespace OmniMonitor.Server.Controllers
{
    /// <summary>
    /// Controlador de ejemplo que demuestra cómo proteger endpoints con el sistema de permisos
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ExampleProtectedController : ControllerBase
    {
        private readonly ILogger<ExampleProtectedController> _logger;

        public ExampleProtectedController(ILogger<ExampleProtectedController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Endpoint de ejemplo protegido con permiso de visualización de usuarios
        /// Solo usuarios con el permiso "Users.View" pueden acceder
        /// </summary>
        [HttpGet("view-example")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [RequirePermission("Users.View")]
        public IActionResult ViewExample()
        {
            return Ok(new 
            { 
                message = "Has accedido exitosamente a un endpoint protegido con Users.View",
                timestamp = DateTime.UtcNow,
                user = User.Identity?.Name
            });
        }

        /// <summary>
        /// Endpoint de ejemplo protegido con permiso de creación de usuarios
        /// Solo usuarios con el permiso "Users.Create" pueden acceder
        /// </summary>
        [HttpPost("create-example")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [RequirePermission("Users.Create")]
        public IActionResult CreateExample()
        {
            return Ok(new 
            { 
                message = "Has accedido exitosamente a un endpoint protegido con Users.Create",
                timestamp = DateTime.UtcNow,
                user = User.Identity?.Name
            });
        }

        /// <summary>
        /// Endpoint de ejemplo protegido con permiso de edición de dashboards
        /// Solo usuarios con el permiso "Dashboards.Edit" pueden acceder
        /// </summary>
        [HttpPut("edit-dashboard-example")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [RequirePermission("Dashboards.Edit")]
        public IActionResult EditDashboardExample()
        {
            return Ok(new 
            { 
                message = "Has accedido exitosamente a un endpoint protegido con Dashboards.Edit",
                timestamp = DateTime.UtcNow,
                user = User.Identity?.Name
            });
        }

        /// <summary>
        /// Endpoint de ejemplo protegido con permiso de administración del sistema
        /// Solo usuarios con el permiso "System.ManageSettings" pueden acceder
        /// </summary>
        [HttpPost("admin-example")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [RequirePermission("System.ManageSettings")]
        public IActionResult AdminExample()
        {
            return Ok(new 
            { 
                message = "Has accedido exitosamente a un endpoint protegido con System.ManageSettings",
                timestamp = DateTime.UtcNow,
                user = User.Identity?.Name,
                note = "Este es un permiso de nivel administrador"
            });
        }

        /// <summary>
        /// Endpoint público - sin protección
        /// Cualquier usuario autenticado puede acceder (no requiere permisos específicos)
        /// </summary>
        [HttpGet("public-example")]
        public IActionResult PublicExample()
        {
            return Ok(new 
            { 
                message = "Este es un endpoint público (sin RequirePermission)",
                timestamp = DateTime.UtcNow,
                isAuthenticated = User.Identity?.IsAuthenticated ?? false,
                user = User.Identity?.Name ?? "Anonymous"
            });
        }

        /// <summary>
        /// Obtiene la información de permisos del usuario actual desde el JWT
        /// </summary>
        [HttpGet("my-permissions")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public IActionResult GetMyPermissions()
        {
            var permissions = User.Claims
                .Where(c => c.Type == "permission")
                .Select(c => c.Value)
                .ToList();

            var roles = User.Claims
                .Where(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                .Select(c => c.Value)
                .ToList();

            return Ok(new 
            { 
                userId = User.Identity?.Name,
                roles = roles,
                permissions = permissions,
                totalPermissions = permissions.Count
            });
        }

        /// <summary>
        /// DEBUG: Muestra TODOS los claims del token para debugging
        /// </summary>
        [HttpGet("debug-claims")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public IActionResult GetAllClaims()
        {
            var allClaims = User.Claims.Select(c => new 
            { 
                type = c.Type, 
                value = c.Value 
            }).ToList();

            return Ok(new 
            { 
                isAuthenticated = User.Identity?.IsAuthenticated ?? false,
                userName = User.Identity?.Name,
                totalClaims = allClaims.Count,
                claims = allClaims
            });
        }

        /// <summary>
        /// DEBUG: Muestra TODOS los headers que recibe el servidor
        /// </summary>
        [HttpGet("debug-headers")]
        public IActionResult GetAllHeaders()
        {
            var headers = Request.Headers.Select(h => new 
            { 
                name = h.Key, 
                value = h.Value.ToString() 
            }).ToList();

            var authHeader = Request.Headers["Authorization"].ToString();
            
            return Ok(new 
            { 
                totalHeaders = headers.Count,
                authorizationHeader = authHeader,
                hasAuthHeader = !string.IsNullOrEmpty(authHeader),
                allHeaders = headers,
                isAuthenticated = User.Identity?.IsAuthenticated ?? false
            });
        }
    }
}

