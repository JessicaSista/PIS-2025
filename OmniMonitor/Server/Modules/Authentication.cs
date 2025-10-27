using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OmniMonitor.Server.Configuration;
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

    // Para obtener el token de usuario del módulo IM
    Task<string> GetUserTokenIMAsync(string username);

    //Para obtener el token de usuario del módulo UM
    Task<string> GetUserTokenUMAsync(string username);

    //Para obtener el token de usuario del módulo AM
    Task<string> GetUserTokenAMAsync(string username);

    //Para obtener el token de usuario del módulo EM
    Task<string> GetUserTokenEMAsync(string username);

    Task<string> GetUserByTokenOMAsync(string token);

}

public class SondaAuthService : ISondaAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ApiConfig _apiConfig;

    public SondaAuthService(ApplicationDbContext context, IHttpClientFactory httpClientFactory, IOptions<ApiConfig> apiConfigOptions)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _apiConfig = apiConfigOptions.Value;
    }


    //**************************************** IM **************************************

    // Para obtener el token de un usuario
    public async Task<string> GetUserTokenIMAsync(string username)
    {
        // 1. Buscar usuario en DB
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);
        
        // 3. Verificar si el token es válido y no está cerca de expirar (5 minutos de margen).
        if (!string.IsNullOrEmpty(user.SondaTokenIM) && user.TokenExpirationIM > DateTime.UtcNow.AddMinutes(5))
        {
            Console.WriteLine(">>>> Returning cached token from DB for user: " + user.UserName);
            return user.SondaTokenIM;
        }

        // 4. Si el token no es válido o está por expirar, solicitar uno nuevo.
        Console.WriteLine(">>>> Token is invalid or expired. Requesting a new one for user: " + user.UserName);
        return await RefreshAndStoreTokenIMAsync(user);
    }

    // Para refrescar y almacenar el token IM
    private async Task<string> RefreshAndStoreTokenIMAsync(User user)
    {

        string baseUrl = _apiConfig.BaseUrl.UrlIM;
        string email = _apiConfig.Credentials.CredentialsIM.Email;
        string pass = _apiConfig.Credentials.CredentialsIM.Password;
        string endpoint = _apiConfig.EndpointsIM["Login"]["Login"];
        string loginUrl = baseUrl + endpoint;
        var credentials = new { email = email, password = pass };

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
        user.SondaTokenIM = loginResponse.Token;
        user.TokenExpirationIM = loginResponse.Expiration;

        // 6. Guardar los cambios en la base de datos.
        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        Console.WriteLine(">>>> New token saved to the database for user: " + user.UserName);

        return user.SondaTokenIM;
    }

    //**********************************************************************************

    //**************************************** UM **************************************

    // Para obtener el token de un usuario del módulo UM    
    public async Task<string> GetUserTokenUMAsync(string username)
    {
        // 1. Buscar usuario en DB
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);

        // 3. Verificar si el token es válido y no está cerca de expirar (5 minutos de margen).
        if (!string.IsNullOrEmpty(user.SondaTokenUM) && user.TokenExpirationUM > DateTime.UtcNow.AddMinutes(5))
        {
            Console.WriteLine(">>>> Returning cached token from DB for user: " + user.UserName);
            return user.SondaTokenUM;
        }

        // 4. Si el token no es válido o está por expirar, solicitar uno nuevo.
        Console.WriteLine(">>>> Token is invalid or expired. Requesting a new one for user: " + user.UserName);
        return await RefreshAndStoreTokenUMAsync(user);
    }


    // Para refrescar y almacenar el token UM
    private async Task<string> RefreshAndStoreTokenUMAsync(User user)
    {

        string baseUrl = _apiConfig.BaseUrl.UrlUM;
        string email = _apiConfig.Credentials.CredentialsUM.Email;
        string pass = _apiConfig.Credentials.CredentialsUM.Password;
        string endpoint = _apiConfig.EndpointsUM["Login"]["Login"];
        string loginUrl = baseUrl + endpoint;
        var credentials = new { username = email, password = pass };

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
        user.SondaTokenUM = loginResponse.Token;
        user.TokenExpirationUM = loginResponse.Expiration;

        // 6. Guardar los cambios en la base de datos.
        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        Console.WriteLine(">>>> New token saved to the database for user: " + user.UserName);

        return user.SondaTokenUM;
    }

    //************************************************************************************


    //**************************************** AM **************************************

    // Para obtener el token de un usuario del módulo AM    
    public async Task<string> GetUserTokenAMAsync(string username)
    {
        // 1. Buscar usuario en DB
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);

        // 3. Verificar si el token es válido y no está cerca de expirar (5 minutos de margen).
        if (!string.IsNullOrEmpty(user.SondaTokenAM) && user.TokenExpirationAM > DateTime.UtcNow.AddMinutes(5))
        {
            Console.WriteLine(">>>> Returning cached token from DB for user: " + user.UserName);
            return user.SondaTokenAM;
        }

        // 4. Si el token no es válido o está por expirar, solicitar uno nuevo.
        Console.WriteLine(">>>> Token is invalid or expired. Requesting a new one for user: " + user.UserName);
        return await RefreshAndStoreTokenAMAsync(user);
    }

    // Para refrescar y almacenar el token AM
    private async Task<string> RefreshAndStoreTokenAMAsync(User user)
    {

        string baseUrl = _apiConfig.BaseUrl.UrlAM;
        string email = _apiConfig.Credentials.CredentialsAM.Email;
        string pass = _apiConfig.Credentials.CredentialsAM.Password;
        string endpoint = _apiConfig.EndpointsAM["Login"]["Login"];
        string loginUrl = baseUrl + endpoint;
        var credentials = new { username = email, password = pass };

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
        user.SondaTokenAM = loginResponse.Token;
        user.TokenExpirationAM = loginResponse.Expiration;

        // 6. Guardar los cambios en la base de datos.
        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        Console.WriteLine(">>>> New token saved to the database for user: " + user.UserName);

        return user.SondaTokenAM;
    }

    //************************************************************************************

    //**************************************** EM **************************************

    // Para obtener el token de un usuario del módulo EM
    public async Task<string> GetUserTokenEMAsync(string username)
    {
        // 1. Buscar usuario en DB
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);

        // 3. Verificar si el token es válido y no está cerca de expirar (5 minutos de margen).
        if (!string.IsNullOrEmpty(user.SondaTokenEM) && user.TokenExpirationEM > DateTime.UtcNow.AddMinutes(5))
        {
            Console.WriteLine(">>>> Returning cached token from DB for user: " + user.UserName);
            return user.SondaTokenEM;
        }

        // 4. Si el token no es válido o está por expirar, solicitar uno nuevo.
        Console.WriteLine(">>>> Token is invalid or expired. Requesting a new one for user: " + user.UserName);
        return await RefreshAndStoreTokenEMAsync(user);
    }
    private async Task<string> RefreshAndStoreTokenEMAsync(User user)
    {

        string baseUrl = _apiConfig.BaseUrl.UrlEM;
        string email = _apiConfig.Credentials.CredentialsEM.Email;
        string pass = _apiConfig.Credentials.CredentialsEM.Password;
        string endpoint = _apiConfig.EndpointsEM["Login"]["Login"];
        string loginUrl = baseUrl + endpoint;
        var credentials = new { email = email, password = pass };

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
        user.SondaTokenEM = loginResponse.Token;
        user.TokenExpirationEM = loginResponse.Expiration;

        // 6. Guardar los cambios en la base de datos.
        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        Console.WriteLine(">>>> New token saved to the database for user: " + user.UserName);

        return user.SondaTokenEM;
    }

      public async Task<string> GetUserByTokenOMAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token must be provided.", nameof(token));

        // Buscar el usuario que tenga ese token OM
        var user = await _context.Users.FirstOrDefaultAsync(u => u.SondaTokenOM == token);
        if (user == null)
            throw new InvalidOperationException("No se encontró ningún usuario con ese token OM.");

        // Verificar expiración si la guardás
        if (user.TokenExpirationOM.HasValue && user.TokenExpirationOM.Value < DateTime.UtcNow)
            throw new InvalidOperationException("El token OM está expirado.");

        // Devolver username y password (normalmente hash)
        return user.UserName;
    }



}



// Respuesta del login de Sonda
file class SondaLoginResponse
{
    public string Token { get; set; }
    public DateTime Expiration { get; set; }
}
