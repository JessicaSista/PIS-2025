using Microsoft.Extensions.Options;
using OmniMonitor.Server.Configuration;
using OmniMonitor.Shared.Dtos.EM;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

public interface ISondaEMService
{
    Task<EventDto?> GetEventById(int id, string username, string password);
    Task<AlertDto?> GetAlertById(int id, string username, string password);
    Task<List<AlertDto>> GetAlerts(int? page, int? pageSize,string? query,string? stateList,double? x,double? y,double? r,bool? forceGps,string? sort,string username,string password);
    Task<List<AlertDto>> GetStoredAlerts(
        int? page,
        int? pageSize,
        string? query,
        string? stateList,
        double? x,
        double? y,
        double? r,
        string? sort,
        string username,
        string password);
    Task<List<EventDto>> GetEvents(
        int? page,
        int? pageSize,
        string? sort,
        string? query,
        string username,
        string password);
    Task<List<EventTypeDto>> GetEventTypes(string username, string password);
    Task<ExtensionDtoDup?> GetExtensionById(int extensionId, string username, string password);
        Task<List<ExtensionDto>> GetExtensions(
        int? page,
        int? pageSize,
        string? sort,
        string? query,
        string? states,
        string? dates,
        string? priorities,
        string? categories,
        string? zones,
        string username,
        string password);
    Task<List<AttachmentDto>> GetAttachedItems(int extensionId, string username, string password);
        Task<ResourceDto?> GetResourceById(int id, string username, string password);
}


public class SondaEMService : ISondaEMService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISondaAuthService _sondaAuthService;
    private readonly ApiConfig _apiConfig;

    public SondaEMService(IHttpClientFactory httpClientFactory, ISondaAuthService sondaAuthService, IOptions<ApiConfig> apiConfigOptions)
    {
        _httpClientFactory = httpClientFactory;
        _sondaAuthService = sondaAuthService;
        _apiConfig = apiConfigOptions.Value;
    }

        public async Task<EventDto?> GetEventById(int eventId, string username, string password)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlEM;
        string endpoint = _apiConfig.EndpointsEM["Event"]["GetById"].Replace("{eventId}", eventId.ToString());
        if (eventId <= 0)
        {
            throw new ArgumentException("El eventId debe ser positivo.", nameof(eventId));
        }

        string token = await _sondaAuthService.GetUserTokenEMAsync(username, password);

        string getDataUrl = baseUrl + endpoint;
        Console.WriteLine($"SONDA API REQUEST: {getDataUrl}");
        Console.WriteLine($"SONDA API TOKEN: {token}");
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync(getDataUrl);
        //response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        Console.WriteLine("SONDA API RAW RESPONSE: " + responseBody);

        response.EnsureSuccessStatusCode();
        if (string.IsNullOrWhiteSpace(responseBody) || !responseBody.TrimStart().StartsWith("{"))
        {
            return null;
        }
        return JsonSerializer.Deserialize<EventDto>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    public async Task<AlertDto?> GetAlertById(int alertId, string username, string password)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlEM;
        string endpoint = _apiConfig.EndpointsEM["Alert"]["GetById"].Replace("{alertId}", alertId.ToString());
        if (alertId <= 0)
        {
            throw new ArgumentException("El alertId debe ser mayor que cero.", nameof(alertId));
        }

        string token = await _sondaAuthService.GetUserTokenEMAsync(username, password);

        string getDataUrl = baseUrl + endpoint;
        Console.WriteLine($"SONDA API REQUEST: {getDataUrl}");
        Console.WriteLine($"SONDA API TOKEN: {token}");
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync(getDataUrl);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        Console.WriteLine("SONDA API RAW RESPONSE: " + responseBody);
        if (string.IsNullOrWhiteSpace(responseBody) || !responseBody.TrimStart().StartsWith("{"))
        {
            return null;
        }
        return JsonSerializer.Deserialize<AlertDto>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    public async Task<List<AlertDto>> GetAlerts(
        int? page,
        int? pageSize,
        string? query,
        string? stateList,
        double? x,
        double? y,
        double? r,
        bool? forceGps,
        string? sort,
        string username,
        string password)
    {
        if (page.HasValue && page.Value <= 0)
        {
            throw new ArgumentException("El parámetro 'page' debe ser mayor que cero.");
        }
        string baseUrl = _apiConfig.BaseUrl.UrlEM;
        string endpoint = _apiConfig.EndpointsEM["Alert"]["GetAll"];
        var queryParams = new List<string>();
        if (page.HasValue) queryParams.Add($"page={page.Value}");
        if (pageSize.HasValue) queryParams.Add($"pageSize={pageSize.Value}");
        if (!string.IsNullOrEmpty(query)) queryParams.Add($"query={Uri.EscapeDataString(query)}");
        if (!string.IsNullOrEmpty(stateList)) queryParams.Add($"stateList={Uri.EscapeDataString(stateList)}");
        if (x.HasValue) queryParams.Add($"x={x.Value}");
        if (y.HasValue) queryParams.Add($"y={y.Value}");
        if (r.HasValue) queryParams.Add($"r={r.Value}");
        if (forceGps.HasValue) queryParams.Add($"forceGps={forceGps.Value.ToString().ToLower()}");
        if (!string.IsNullOrEmpty(sort)) queryParams.Add($"sort={Uri.EscapeDataString(sort)}");
        string queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;
        string getDataUrl = baseUrl + endpoint + queryString;
        Console.WriteLine($"SONDA API REQUEST: {getDataUrl}");
        string token = await _sondaAuthService.GetUserTokenEMAsync(username, password);
        Console.WriteLine($"SONDA API TOKEN: {token}");
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync(getDataUrl);
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        Console.WriteLine("SONDA API RAW RESPONSE: " + responseBody);
        if (string.IsNullOrWhiteSpace(responseBody) || !responseBody.TrimStart().StartsWith("{"))
        {
            return new List<AlertDto>();
        }
        var apiResponse = JsonSerializer.Deserialize<AlertApiResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return apiResponse?.Results ?? new List<AlertDto>();
    }

        public async Task<List<AlertDto>> GetStoredAlerts(
        int? page,
        int? pageSize,
        string? query,
        string? stateList,
        double? x,
        double? y,
        double? r,
        string? sort,
        string username,
        string password)
    {
        if (page.HasValue && page.Value <= 0)
        {
            throw new ArgumentException("El parámetro 'page' debe ser mayor que cero.");
        }
        string baseUrl = _apiConfig.BaseUrl.UrlEM;
        string endpoint = _apiConfig.EndpointsEM["Alert"]["GetStored"];
        var queryParams = new List<string>();
        if (page.HasValue) queryParams.Add($"page={page.Value}");
        if (pageSize.HasValue) queryParams.Add($"pageSize={pageSize.Value}");
        if (!string.IsNullOrEmpty(query)) queryParams.Add($"query={Uri.EscapeDataString(query)}");
        if (!string.IsNullOrEmpty(stateList)) queryParams.Add($"stateList={Uri.EscapeDataString(stateList)}");
        if (x.HasValue) queryParams.Add($"x={x.Value}");
        if (y.HasValue) queryParams.Add($"y={y.Value}");
        if (r.HasValue) queryParams.Add($"r={r.Value}");
        if (!string.IsNullOrEmpty(sort)) queryParams.Add($"sort={Uri.EscapeDataString(sort)}");
        string queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;
        string getDataUrl = baseUrl + endpoint + queryString;
        Console.WriteLine($"SONDA API REQUEST: {getDataUrl}");
        string token = await _sondaAuthService.GetUserTokenEMAsync(username, password);
        Console.WriteLine($"SONDA API TOKEN: {token}");
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync(getDataUrl);
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        Console.WriteLine("SONDA API RAW RESPONSE: " + responseBody);
        if (string.IsNullOrWhiteSpace(responseBody) || !responseBody.TrimStart().StartsWith("{"))
        {
            return new List<AlertDto>();
        }
        var apiResponse = JsonSerializer.Deserialize<AlertApiResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return apiResponse?.Results ?? new List<AlertDto>();
    }

        public async Task<List<EventDto>> GetEvents(
            int? page,
            int? pageSize,
            string? sort,
            string? query,
            string username,
            string password)
        {
            if (page.HasValue && page.Value < 0)
            {
                throw new ArgumentException("El parámetro 'page' debe ser mayor o igual que cero.");
            }
            string baseUrl = _apiConfig.BaseUrl.UrlEM;
            string endpoint = _apiConfig.EndpointsEM["Event"]["GetEvents"];
            var queryParams = new List<string>();
            if (page.HasValue) queryParams.Add($"page={page.Value}");
            if (pageSize.HasValue) queryParams.Add($"pageSize={pageSize.Value}");
            if (!string.IsNullOrEmpty(sort)) queryParams.Add($"sort={Uri.EscapeDataString(sort)}");
            if (!string.IsNullOrEmpty(query)) queryParams.Add($"query={Uri.EscapeDataString(query)}");
            string queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;
            string getDataUrl = baseUrl + endpoint + queryString;
            Console.WriteLine($"SONDA API REQUEST: {getDataUrl}");
            string token = await _sondaAuthService.GetUserTokenEMAsync(username, password);
            Console.WriteLine($"SONDA API TOKEN: {token}");
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await client.GetAsync(getDataUrl);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine("SONDA API RAW RESPONSE: " + responseBody);
            if (string.IsNullOrWhiteSpace(responseBody) || !responseBody.TrimStart().StartsWith("{"))
            {
                return new List<EventDto>();
            }
            var apiResponse = JsonSerializer.Deserialize<EventApiResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return apiResponse?.Results ?? new List<EventDto>();
        }

    public async Task<List<EventTypeDto>> GetEventTypes(string username, string password)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlEM;
        string endpoint = _apiConfig.EndpointsEM["EventType"]["GetEventTypes"];
        string getDataUrl = baseUrl + endpoint;
        Console.WriteLine($"SONDA API REQUEST: {getDataUrl}");
        string token = await _sondaAuthService.GetUserTokenEMAsync(username, password);
        Console.WriteLine($"SONDA API TOKEN: {token}");
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync(getDataUrl);
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        Console.WriteLine("SONDA API RAW RESPONSE: " + responseBody);
        if (string.IsNullOrWhiteSpace(responseBody) || !responseBody.TrimStart().StartsWith("["))
        {
            return new List<EventTypeDto>();
        }
        return JsonSerializer.Deserialize<List<EventTypeDto>>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<EventTypeDto>();
    }

    public async Task<ExtensionDtoDup?> GetExtensionById(int extensionId, string username, string password)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlEM;
        string endpoint = _apiConfig.EndpointsEM["Extension"]["GetById"].Replace("{extensionId}", extensionId.ToString());
        string getDataUrl = baseUrl + endpoint;
        Console.WriteLine($"SONDA API REQUEST: {getDataUrl}");
        string token = await _sondaAuthService.GetUserTokenEMAsync(username, password);
        Console.WriteLine($"SONDA API TOKEN: {token}");
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync(getDataUrl);
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        Console.WriteLine("SONDA API RAW RESPONSE: " + responseBody);
        if (string.IsNullOrWhiteSpace(responseBody) || !responseBody.TrimStart().StartsWith("{"))
        {
            return null;
        }
        return JsonSerializer.Deserialize<ExtensionDtoDup>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    public async Task<List<ExtensionDto>> GetExtensions(
            int? page,
            int? pageSize,
            string? sort,
            string? query,
            string? states,
            string? dates,
            string? priorities,
            string? categories,
            string? zones,
            string username,
            string password)
        {
            if (page.HasValue && page.Value < 0)
            {
                throw new ArgumentException("El parámetro 'page' debe ser mayor o igual que cero.");
            }
            string baseUrl = _apiConfig.BaseUrl.UrlEM;
            string endpoint = _apiConfig.EndpointsEM["Extension"]["GetAll"];
            var queryParams = new List<string>();
            if (page.HasValue) queryParams.Add($"page={page.Value}");
            if (pageSize.HasValue) queryParams.Add($"pageSize={pageSize.Value}");
            if (!string.IsNullOrEmpty(sort)) queryParams.Add($"sort={Uri.EscapeDataString(sort)}");
            if (!string.IsNullOrEmpty(query)) queryParams.Add($"query={Uri.EscapeDataString(query)}");
            if (!string.IsNullOrEmpty(states)) queryParams.Add($"states={Uri.EscapeDataString(states)}");
            if (!string.IsNullOrEmpty(dates)) queryParams.Add($"dates={Uri.EscapeDataString(dates)}");
            if (!string.IsNullOrEmpty(priorities)) queryParams.Add($"priorities={Uri.EscapeDataString(priorities)}");
            if (!string.IsNullOrEmpty(categories)) queryParams.Add($"categories={Uri.EscapeDataString(categories)}");
            if (!string.IsNullOrEmpty(zones)) queryParams.Add($"zones={Uri.EscapeDataString(zones)}");
            string queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;
            string getDataUrl = baseUrl + endpoint + queryString;
            Console.WriteLine($"SONDA API REQUEST: {getDataUrl}");
            string token = await _sondaAuthService.GetUserTokenEMAsync(username, password);
            Console.WriteLine($"SONDA API TOKEN: {token}");
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await client.GetAsync(getDataUrl);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine("SONDA API RAW RESPONSE: " + responseBody);
            if (string.IsNullOrWhiteSpace(responseBody) || !responseBody.TrimStart().StartsWith("{"))
            {
                return new List<ExtensionDto>();
            }
            var apiResponse = JsonSerializer.Deserialize<ExtensionApiResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return apiResponse?.Results ?? new List<ExtensionDto>();
        }

    public async Task<List<AttachmentDto>> GetAttachedItems(int extensionId, string username, string password)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlEM;
        string endpoint = _apiConfig.EndpointsEM["Extension"]["GetAttachedItems"].Replace("{extensionId}", extensionId.ToString());
        string getDataUrl = baseUrl + endpoint;
        Console.WriteLine($"SONDA API REQUEST: {getDataUrl}");
        string token = await _sondaAuthService.GetUserTokenEMAsync(username, password);
        Console.WriteLine($"SONDA API TOKEN: {token}");
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync(getDataUrl);
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        Console.WriteLine("SONDA API RAW RESPONSE: " + responseBody);
        if (string.IsNullOrWhiteSpace(responseBody) || !responseBody.TrimStart().StartsWith("["))
        {
            return new List<AttachmentDto>();
        }
        return JsonSerializer.Deserialize<List<AttachmentDto>>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<AttachmentDto>();
    }

    public async Task<ResourceDto?> GetResourceById(int id, string username, string password)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlEM;
        string endpoint =  _apiConfig.EndpointsEM["ResourceType"]["GetById"].Replace("{id}", id.ToString());
        string getDataUrl = baseUrl + endpoint;
        Console.WriteLine($"SONDA API REQUEST: {getDataUrl}");
        string token = await _sondaAuthService.GetUserTokenEMAsync(username, password);
        Console.WriteLine($"SONDA API TOKEN: {token}");
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync(getDataUrl);
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        Console.WriteLine("SONDA API RAW RESPONSE: " + responseBody);
        if (string.IsNullOrWhiteSpace(responseBody) || !responseBody.TrimStart().StartsWith("{"))
        {
            return null;
        }
        return JsonSerializer.Deserialize<ResourceDto>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

}
