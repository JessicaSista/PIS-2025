using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using OmniMonitor.Server.Configuration;
using OmniMonitor.Server.Context;
using OmniMonitor.Shared.Dtos;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>
/// Interface for Sonda authentication service.
/// </summary>
public interface ISondaAuthService
{
    /// <summary>
    /// Gets the IM module user token.
    /// </summary>
    /// <param name="username">The username to get the token for.</param>
    /// <returns>The IM module user token.</returns>
    Task<string> GetUserTokenImAsync(string username);

    /// <summary>
    /// Gets the UM module user token.
    /// </summary>
    /// <param name="username">The username to get the token for.</param>
    /// <returns>The UM module user token.</returns>
    Task<string> GetUserTokenUmAsync(string username);

    /// <summary>
    /// Gets the AM module user token.
    /// </summary>
    /// <param name="username">The username to get the token for.</param>
    /// <returns>The AM module user token.</returns>
    Task<string> GetUserTokenAmAsync(string username);

    /// <summary>
    /// Gets the EM module user token.
    /// </summary>
    /// <param name="username">The username to get the token for.</param>
    /// <returns>The EM module user token.</returns>
    Task<string> GetUserTokenEmAsync(string username);

    /// <summary>
    /// Gets the user by OM token.
    /// </summary>
    /// <param name="token">The OM token.</param>
    /// <returns>The username associated with the token.</returns>
    Task<string> GetUserByTokenOmAsync(string token);
}

/// <summary>
/// Sonda authentication service implementation.
/// </summary>
public class SondaAuthService : ISondaAuthService
{
    #region Fields

    private readonly ApplicationDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ApiConfig _apiConfig;
    private readonly ILogger<SondaAuthService> _logger;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="SondaAuthService"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="apiConfigOptions">The API configuration options.</param>
    /// <param name="logger">The logger instance.</param>
    public SondaAuthService(
        ApplicationDbContext context,
        IHttpClientFactory httpClientFactory,
        IOptions<ApiConfig> apiConfigOptions,
        ILogger<SondaAuthService> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _apiConfig = apiConfigOptions.Value;
        _logger = logger;
    }

    #endregion

    #region Methods

    /// <inheritdoc />
    public async Task<string> GetUserTokenImAsync(string username)
    {
        if (string.IsNullOrEmpty(username))
        {
            _logger.LogWarning(StringResources.UsernameNullOrEmpty);
            throw new ArgumentException(StringResources.UsernameNullOrEmpty, nameof(username));
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName==username);
        if (user == null)
        {
            _logger.LogWarning(StringResources.UserNotFound, username);
            throw new InvalidOperationException(string.Format(StringResources.UserNotFound, username));
        }

        if (!string.IsNullOrEmpty(user.SondaTokenIM) && user.TokenExpirationIM > DateTime.UtcNow.AddMinutes(5))
        {
            _logger.LogInformation(StringResources.ReturningCachedToken, user.UserName);
            return user.SondaTokenIM;
        }

        _logger.LogInformation(StringResources.RequestingNewToken, user.UserName);
        return await RefreshAndStoreTokenImAsync(user);
    }

    private async Task<string> RefreshAndStoreTokenImAsync(User user)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlIM;
        string email = _apiConfig.Credentials.CredentialsIM.Email;
        string password = _apiConfig.Credentials.CredentialsIM.Password;
        string endpoint = _apiConfig.EndpointsIM["Login"]["Login"];
        string loginUrl = $"{baseUrl}{endpoint}";
        var credentials = new { email, password };

        using var client = _httpClientFactory.CreateClient();
        var content = new StringContent(JsonSerializer.Serialize(credentials), Encoding.UTF8, "application/json");
        var response = await client.PostAsync(loginUrl, content);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(StringResources.AuthFailedIm);
            throw new HttpRequestException(StringResources.AuthFailedIm);
        }

        var responseBody = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var loginResponse = JsonSerializer.Deserialize<SondaLoginResponse>(responseBody, options);

        if (loginResponse == null || string.IsNullOrEmpty(loginResponse.Token))
        {
            _logger.LogError(StringResources.AuthNoTokenIm);
            throw new InvalidOperationException(StringResources.AuthNoTokenIm);
        }

        user.SondaTokenIM = loginResponse.Token;
        user.TokenExpirationIM = loginResponse.Expiration;

        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation(StringResources.NewTokenSaved, user.UserName);

        return user.SondaTokenIM;
    }

    /// <inheritdoc />
    public async Task<string> GetUserTokenUmAsync(string username)
    {
        if (string.IsNullOrEmpty(username))
        {
            _logger.LogWarning(StringResources.UsernameNullOrEmpty);
            throw new ArgumentException(StringResources.UsernameNullOrEmpty, nameof(username));
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName==username);
        if (user == null)
        {
            _logger.LogWarning(StringResources.UserNotFound, username);
            throw new InvalidOperationException(string.Format(StringResources.UserNotFound, username));
        }

        if (!string.IsNullOrEmpty(user.SondaTokenUM) && user.TokenExpirationUM > DateTime.UtcNow.AddMinutes(5))
        {
            _logger.LogInformation(StringResources.ReturningCachedToken, user.UserName);
            return user.SondaTokenUM;
        }

        _logger.LogInformation(StringResources.RequestingNewToken, user.UserName);
        return await RefreshAndStoreTokenUmAsync(user);
    }

    private async Task<string> RefreshAndStoreTokenUmAsync(User user)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlUM;
        string email = _apiConfig.Credentials.CredentialsUM.Email;
        string password = _apiConfig.Credentials.CredentialsUM.Password;
        string endpoint = _apiConfig.EndpointsUM["Login"]["Login"];
        string loginUrl = $"{baseUrl}{endpoint}";
        var credentials = new { username = email, password };

        using var client = _httpClientFactory.CreateClient();
        var content = new StringContent(JsonSerializer.Serialize(credentials), Encoding.UTF8, "application/json");
        var response = await client.PostAsync(loginUrl, content);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(StringResources.AuthFailedUm);
            throw new HttpRequestException(StringResources.AuthFailedUm);
        }

        var responseBody = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var loginResponse = JsonSerializer.Deserialize<SondaLoginResponse>(responseBody, options);

        if (loginResponse == null || string.IsNullOrEmpty(loginResponse.Token))
        {
            _logger.LogError(StringResources.AuthNoTokenUm);
            throw new InvalidOperationException(StringResources.AuthNoTokenUm);
        }

        user.SondaTokenUM = loginResponse.Token;
        user.TokenExpirationUM = loginResponse.Expiration;

        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation(StringResources.NewTokenSaved, user.UserName);

        return user.SondaTokenUM;
    }

    /// <inheritdoc />
    public async Task<string> GetUserTokenAmAsync(string username)
    {
        if (string.IsNullOrEmpty(username))
        {
            _logger.LogWarning(StringResources.UsernameNullOrEmpty);
            throw new ArgumentException(StringResources.UsernameNullOrEmpty, nameof(username));
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);
        if (user == null)
        {
            _logger.LogWarning(StringResources.UserNotFound, username);
            throw new InvalidOperationException(string.Format(StringResources.UserNotFound, username));
        }

        if (!string.IsNullOrEmpty(user.SondaTokenAM) && user.TokenExpirationAM > DateTime.UtcNow.AddMinutes(5))
        {
            _logger.LogInformation(StringResources.ReturningCachedToken, user.UserName);
            return user.SondaTokenAM;
        }

        _logger.LogInformation(StringResources.RequestingNewToken, user.UserName);
        return await RefreshAndStoreTokenAmAsync(user);
    }

    private async Task<string> RefreshAndStoreTokenAmAsync(User user)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlAM;
        string email = _apiConfig.Credentials.CredentialsAM.Email;
        string password = _apiConfig.Credentials.CredentialsAM.Password;
        string endpoint = _apiConfig.EndpointsAM["Login"]["Login"];
        string loginUrl = $"{baseUrl}{endpoint}";
        var credentials = new { username = email, password };

        using var client = _httpClientFactory.CreateClient();
        var content = new StringContent(JsonSerializer.Serialize(credentials), Encoding.UTF8, "application/json");
        var response = await client.PostAsync(loginUrl, content);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(StringResources.AuthFailedAm);
            throw new HttpRequestException(StringResources.AuthFailedAm);
        }

        var responseBody = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var loginResponse = JsonSerializer.Deserialize<SondaLoginResponse>(responseBody, options);

        if (loginResponse == null || string.IsNullOrEmpty(loginResponse.Token))
        {
            _logger.LogError(StringResources.AuthNoTokenAm);
            throw new InvalidOperationException(StringResources.AuthNoTokenAm);
        }

        user.SondaTokenAM = loginResponse.Token;
        user.TokenExpirationAM = loginResponse.Expiration;

        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation(StringResources.NewTokenSaved, user.UserName);

        return user.SondaTokenAM;
    }

    /// <inheritdoc />
    public async Task<string> GetUserTokenEmAsync(string username)
    {
        if (string.IsNullOrEmpty(username))
        {
            _logger.LogWarning(StringResources.UsernameNullOrEmpty);
            throw new ArgumentException(StringResources.UsernameNullOrEmpty, nameof(username));
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);
        if (user == null)
        {
            _logger.LogWarning(StringResources.UserNotFound, username);
            throw new InvalidOperationException(string.Format(StringResources.UserNotFound, username));
        }

        if (!string.IsNullOrEmpty(user.SondaTokenEM) && user.TokenExpirationEM > DateTime.UtcNow.AddMinutes(5))
        {
            _logger.LogInformation(StringResources.ReturningCachedToken, user.UserName);
            return user.SondaTokenEM;
        }

        _logger.LogInformation(StringResources.RequestingNewToken, user.UserName);
        return await RefreshAndStoreTokenEmAsync(user);
    }

    private async Task<string> RefreshAndStoreTokenEmAsync(User user)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlEM;
        string email = _apiConfig.Credentials.CredentialsEM.Email;
        string password = _apiConfig.Credentials.CredentialsEM.Password;
        string endpoint = _apiConfig.EndpointsEM["Login"]["Login"];
        string loginUrl = $"{baseUrl}{endpoint}";
        var credentials = new { email, password };

        using var client = _httpClientFactory.CreateClient();
        var content = new StringContent(JsonSerializer.Serialize(credentials), Encoding.UTF8, "application/json");
        var response = await client.PostAsync(loginUrl, content);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(StringResources.AuthFailedEm);
            throw new HttpRequestException(StringResources.AuthFailedEm);
        }

        var responseBody = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var loginResponse = JsonSerializer.Deserialize<SondaLoginResponse>(responseBody, options);

        if (loginResponse == null || string.IsNullOrEmpty(loginResponse.Token))
        {
            _logger.LogError(StringResources.AuthNoTokenEm);
            throw new InvalidOperationException(StringResources.AuthNoTokenEm);
        }

        user.SondaTokenEM = loginResponse.Token;
        user.TokenExpirationEM = loginResponse.Expiration;

        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation(StringResources.NewTokenSaved, user.UserName);

        return user.SondaTokenEM;
    }

    /// <inheritdoc />
    public async Task<string> GetUserByTokenOmAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning(StringResources.TokenNotProvided);
            throw new ArgumentException(StringResources.TokenNotProvided, nameof(token));
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.SondaTokenOM == token);
        if (user == null)
        {
            _logger.LogWarning(StringResources.UserNotFoundByToken);
            throw new InvalidOperationException(StringResources.UserNotFoundByToken);
        }

        if (user.TokenExpirationOM.HasValue && user.TokenExpirationOM.Value < DateTime.UtcNow)
        {
            _logger.LogWarning(StringResources.TokenExpired, user.UserName);
            throw new InvalidOperationException(StringResources.TokenExpired);
        }

        return user.UserName;
    }

    #endregion
}

/// <summary>
/// Sonda login response.
/// </summary>
file class SondaLoginResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime Expiration { get; set; }
}

/// <summary>
/// Centralized string resources for messages and logs.
/// </summary>
public static class StringResources
{
    public const string UsernameNullOrEmpty = "The username must be provided.";
    public const string UserNotFound = "User not found: {0}";
    public const string ReturningCachedToken = "Returning cached token for user: {0}";
    public const string RequestingNewToken = "Token is invalid or expired. Requesting a new one for user: {0}";
    public const string AuthFailedIm = "Failed to authenticate with Sonda API (IM). Please check user credentials.";
    public const string AuthNoTokenIm = "Sonda API (IM) returned a success status but did not provide a token.";
    public const string AuthFailedUm = "Failed to authenticate with Sonda API (UM). Please check user credentials.";
    public const string AuthNoTokenUm = "Sonda API (UM) returned a success status but did not provide a token.";
    public const string AuthFailedAm = "Failed to authenticate with Sonda API (AM). Please check user credentials.";
    public const string AuthNoTokenAm = "Sonda API (AM) returned a success status but did not provide a token.";
    public const string AuthFailedEm = "Failed to authenticate with Sonda API (EM). Please check user credentials.";
    public const string AuthNoTokenEm = "Sonda API (EM) returned a success status but did not provide a token.";
    public const string NewTokenSaved = "New token saved to the database for user: {0}";
    public const string TokenNotProvided = "Token must be provided.";
    public const string UserNotFoundByToken = "No user found with the provided OM token.";
    public const string TokenExpired = "The OM token is expired.";
}
