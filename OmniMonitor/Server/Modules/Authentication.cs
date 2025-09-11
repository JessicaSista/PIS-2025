using Microsoft.EntityFrameworkCore;
using OmniMonitor.Server.Context;
using System;
using System.Net.Http;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>
/// Defines the contract for the Sonda authentication service.
/// </summary>
public interface ISondaAuthService
{
    /// <summary>
    /// Gets a valid Sonda API token for a given user, handling validation, caching, and refreshing.
    /// </summary>
    /// <param name="Username">The user's email.</param>
    /// <param name="Password">The user's password.</param>
    /// <returns>A valid Bearer token.</returns>
    Task<string> GetUserTokenAsync(string username, string password);
}

/// <summary>
/// Handles user validation and Sonda API token management using a database.
/// </summary>
public class SondaAuthService : ISondaAuthService
{
    private readonly ApplicationDbContext _context; // Your EF DbContext
    private readonly IHttpClientFactory _httpClientFactory;

    public SondaAuthService(ApplicationDbContext context, IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string> GetUserTokenAsync(string username, string password)
    {
        // 1. Find the user in the database by their username.
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

        // 2. Validate user credentials.
        if (user == null || user.Password != password)
        {
            throw new AuthenticationException("Invalid email or password.");
        }

        // 3. Check if the stored token is still valid (exists and has not expired).
        // We check for 5 minutes in the future to be safe.
        if (!string.IsNullOrEmpty(user.SondaToken) && user.TokenExpiration > DateTime.UtcNow.AddMinutes(5))
        {
            Console.WriteLine(">>>> Returning cached token from DB for user: " + user.Username);
            return user.SondaToken;
        }

        // 4. If the token is invalid or missing, request a new one.
        Console.WriteLine(">>>> Token is invalid or expired. Requesting a new one for user: " + user.Username);
        return await RefreshAndStoreTokenAsync(user);
    }

    /// <summary>
    /// Calls the Sonda API to get a new token and saves it to the user's record in the database.
    /// </summary>
    private async Task<string> RefreshAndStoreTokenAsync(User user)
    {
        var credentials = new
        {
            email = "pis@pis.com",
            password = "PIS.sonda2025"
        };
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

        // 5. Update the user record with the new token and expiration date.
        user.SondaToken = loginResponse.Token;
        user.TokenExpiration = loginResponse.Expiration;

        // 6. Save changes to the database.
        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        Console.WriteLine(">>>> New token saved to the database for user: " + user.Username);

        return user.SondaToken;
    }
}

/// <summary>
/// Represents the structure of the JSON response from the Sonda Login API.
/// </summary>
file class SondaLoginResponse
{
    public string Token { get; set; }
    public DateTime Expiration { get; set; }
}
