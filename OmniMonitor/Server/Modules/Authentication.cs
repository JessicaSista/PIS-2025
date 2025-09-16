using Microsoft.EntityFrameworkCore;
using OmniMonitor.Server.Context;
using OmniMonitor.Shared.Dtos;
using System;
using System.Net.Http;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public interface ISondaAuthService
{

    // Para obtener el token de un usuario
    Task<string> GetUserTokenAsync(string username, string password);

}

public class SondaAuthService : ISondaAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;

    public SondaAuthService(ApplicationDbContext context, IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
    }


    // Para obtener el token de un usuario
    public async Task<string> GetUserTokenAsync(string username, string password)
    {
        // 1. Buscar usuario en DB
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        
        // 2. Validar credenciales
        if (user == null || user.Password != password)
        {
            throw new AuthenticationException("Invalid email or password.");
        }
        
        // 3. Verificar si el token es válido y no está cerca de expirar (5 minutos de margen).
        if (!string.IsNullOrEmpty(user.SondaToken) && user.TokenExpiration > DateTime.UtcNow.AddMinutes(5))
        {
            Console.WriteLine(">>>> Returning cached token from DB for user: " + user.Username);
            return user.SondaToken;
        }

        // 4. Si el token no es válido o está por expirar, solicitar uno nuevo.
        Console.WriteLine(">>>> Token is invalid or expired. Requesting a new one for user: " + user.Username);
        return await RefreshAndStoreTokenAsync(user);
    }


    // Para refrescar y almacenar el token
    private async Task<string> RefreshAndStoreTokenAsync(User user)
    {
        var credentials = new
        {
            email = "pis@pis.com",
            password = "PIS.sonda2025"
        };

        //URL de login de Sonda (IM)
        string loginUrl = "https://sondasmartplatform.com/internal/IoTMonitor/api/Account/Login";
        var client = _httpClientFactory.CreateClient();

        var content = new StringContent(JsonSerializer.Serialize(credentials), Encoding.UTF8, "application/json");
        var response = await client.PostAsync(loginUrl, content);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException("Failed to authenticate with Sonda API. Please check user credentials.");
        }

        var responseBody = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var loginResponse = JsonSerializer.Deserialize<SondaLoginResponse>(responseBody, options);

        if (loginResponse == null || string.IsNullOrEmpty(loginResponse.Token))
        {
            throw new InvalidOperationException("Sonda API returned a success status but did not provide a token.");
        }

        // 5. Actualizar el registro con la información del nuevo token y su expiración.
        user.SondaToken = loginResponse.Token;
        user.TokenExpiration = loginResponse.Expiration;

        // 6. Guardar los cambios en la base de datos.
        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        Console.WriteLine(">>>> New token saved to the database for user: " + user.Username);

        return user.SondaToken;
    }
}


// Respuesta del login de Sonda
file class SondaLoginResponse
{
    public string Token { get; set; }
    public DateTime Expiration { get; set; }
}
