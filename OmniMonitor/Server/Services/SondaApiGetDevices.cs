using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>
/// Defines the contract for a service that retrieves device data from the Sonda API.
/// </summary>
public interface ISondaApiGetDevicesService
{
    /// <summary>
    /// Gets all devices from the Sonda API for a specific user.
    /// </summary>
    /// <param name="username">The application username to authenticate with.</param>
    /// <param name="password">The application password for the user.</param>
    /// <returns>A JSON string containing the list of devices.</returns>
    Task<List<Device>?> GetAllDevicesAsync(string username, string password);
}

/// <summary>
/// Implements the logic to fetch device data from the Sonda API.
/// It relies on the ISondaAuthService to handle authentication and token management.
/// </summary>
public class SondaApiGetDevicesService : ISondaApiGetDevicesService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISondaAuthService _sondaAuthService;

    public SondaApiGetDevicesService(IHttpClientFactory httpClientFactory, ISondaAuthService sondaAuthService)
    {
        _httpClientFactory = httpClientFactory;
        _sondaAuthService = sondaAuthService;
    }

    public async Task<List<Device>?> GetAllDevicesAsync(string username, string password)
    {
        // 1. Get a valid token for the user.
        // The SondaAuthService will handle checking the database, validating, and refreshing the token if needed.
        string token = await _sondaAuthService.GetUserTokenAsync(username, password);

        // 2. Prepare and send the request to get the devices using the token.
        string getDataUrl = "https://sondasmartplatform.com/internal/IoTMonitor/api/Device/devices?page=-1";
        var client = _httpClientFactory.CreateClient();

        // Add the Bearer token to the Authorization header.
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync(getDataUrl);

        // This will throw an exception if the API returns an error status code (like 403 Forbidden or 404 Not Found).
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();

        // 3. Return the JSON content from the response.
        return JsonSerializer.Deserialize<List<Device>>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
}
