using Microsoft.Extensions.Options;
using OmniMonitor.Server.Configuration;
using OmniMonitor.Shared.Dtos;
using System.Net.Http.Headers;
using System.Text.Json;


public interface  ISondaUMService
{


    public Task<List<Zone>> GetAllZones(string username, string password);
    public Task<Zone?> GetZoneById(int id, string username, string password);

    public Task<List<News>> GetAllNews(string username, string password, int page = 1, string? queryString = null, string? sort = null, int pageSize = 10);
    public Task<News?> GetNewsById(int id, string username, string password);

    public Task<List<Event>> GetAllEvents(string username, string password);
    public Task<Event?> GetEventById(int id, string username, string password);


    public Task<string> TestUMAPI(string username, string password);
}

public class SondaUMService : ISondaUMService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISondaAuthService _sondaAuthService;
    private readonly ApiConfig _apiConfig;
    public SondaUMService(IHttpClientFactory httpClientFactory, ISondaAuthService sondaAuthService, IOptions<ApiConfig> apiConfigOptions)
    {
        _httpClientFactory = httpClientFactory;
        _sondaAuthService = sondaAuthService;
        _apiConfig = apiConfigOptions.Value;
    }

    public async Task<string> TestUMAPI(string username, string password)
    {

        string token = await _sondaAuthService.GetUserTokenUMAsync(username, password);

        Console.Write("TOKEN RECIBIDO: " + token);
        return token;
    }

    public async Task<List<Zone>> GetAllZones(string username, string password)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlUM;
        string endpoint = _apiConfig.EndpointsUM["Zone"]["Zones"];

        string token = await _sondaAuthService.GetUserTokenUMAsync(username, password);

        string getDataUrl = baseUrl + endpoint;
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync(getDataUrl);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return new List<Zone>();
        }

        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"Response Body (DeviceGroups): {responseBody}");

        var parsed = JsonSerializer.Deserialize<List<Zone>>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return parsed ?? new List<Zone>();


    }

    public async Task<Zone?> GetZoneById(int id, string username, string password)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlUM;
        string endpoint = _apiConfig.EndpointsUM["Zone"]["GetById"];
        string getDataUrl = baseUrl + endpoint + "/" + id;
        string token = await _sondaAuthService.GetUserTokenUMAsync(username, password);
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        // Respuesta de la API
        var response = await client.GetAsync(getDataUrl);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Response Body: {responseBody}");
        var parsed = JsonSerializer.Deserialize<Zone>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        return parsed;
    }

    public async Task<List<News>> GetAllNews(string username, string password, int startIndex = 1, string? queryString = null, string? sort = null, int count = 10)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlUM;
        string endpoint = _apiConfig.EndpointsUM["News"]["News"];

        // Construir query string
        var queryParams = new List<string>();
        queryParams.Add($"startIndex={startIndex}");
        queryParams.Add($"count={count}");
        if (!string.IsNullOrEmpty(queryString))
            queryParams.Add($"queryString={Uri.EscapeDataString(queryString)}");
        if (!string.IsNullOrEmpty(sort))
            queryParams.Add($"sort={Uri.EscapeDataString(sort)}");

        string getDataUrl = $"{baseUrl}{endpoint}?{string.Join("&", queryParams)}";

        // Obtener token
        string token = await _sondaAuthService.GetUserTokenUMAsync(username, password);
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Llamada HTTP
        var response = await client.GetAsync(getDataUrl);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return new List<News>();

        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();

        // Deserializar
        var parsed = JsonSerializer.Deserialize<NewsResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return parsed?.results ?? new List<News>();
    }

    public async Task<News?> GetNewsById(int id, string username, string password)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlUM;
        string endpoint = _apiConfig.EndpointsUM["News"]["GetById"];
        string getDataUrl = baseUrl + endpoint + "/" + id;
        string token = await _sondaAuthService.GetUserTokenUMAsync(username, password);
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        // Respuesta de la API
        var response = await client.GetAsync(getDataUrl);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Response Body: {responseBody}");
        var parsed = JsonSerializer.Deserialize<News>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        return parsed;
    }

    public async Task<List<Event>> GetAllEvents(string username, string password)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlUM;
        string endpoint = _apiConfig.EndpointsUM["Event"]["Events"];
        string token = await _sondaAuthService.GetUserTokenUMAsync(username, password);
        string getDataUrl = baseUrl + endpoint;
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync(getDataUrl);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return new List<Event>();
        }
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Response Body (DeviceGroups): {responseBody}");
        var parsed = JsonSerializer.Deserialize<List<Event>>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        return parsed ?? new List<Event>();
    }

    public async Task<Event?> GetEventById(int id, string username, string password)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlUM;
        string endpoint = _apiConfig.EndpointsUM["Event"]["GetById"];
        string getDataUrl = baseUrl + endpoint + "/" + id;
        string token = await _sondaAuthService.GetUserTokenUMAsync(username, password);
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        // Respuesta de la API
        var response = await client.GetAsync(getDataUrl);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Response Body: {responseBody}");
        var parsed = JsonSerializer.Deserialize<Event>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        return parsed;
    }
}

