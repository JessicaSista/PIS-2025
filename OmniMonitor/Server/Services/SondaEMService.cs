using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using OmniMonitor.Server.Configuration;
using OmniMonitor.Shared.Dtos;
using OmniMonitor.Shared.Dtos.EM;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

public interface ISondaEMService
{
    Task<EventDto?> GetEventById(int id, string username);
    Task<AlertDto?> GetAlertById(int id, string username);
    Task<List<AlertDto>> GetAlerts(int? page, int? pageSize, string? query, string? stateList, double? x, double? y, double? r, bool? forceGps, string? sort, string username);
    Task<List<AlertDto>> GetStoredAlerts(int? page, int? pageSize, string? query, string? stateList, double? x, double? y, double? r, string? sort, string username);
    Task<List<EventDto>> GetEvents(int? page, int? pageSize, string? sort, string? query, string username);
    Task<List<EventTypeDto>> GetEventTypes(string username);
    Task<ExtensionDtoDup?> GetExtensionById(int extensionId, string username);
    Task<List<ExtensionDto>> GetExtensions(int? page, int? pageSize, string? sort, string? query, string? states, string? dates, string? priorities, string? categories, string? zones, string username);
    Task<List<AttachmentDto>> GetAttachedItems(int extensionId, string username);
    Task<List<ExtensionDtoDup>> GetExtensionByEventId(int eventId, string username);
    Task<List<CategoryDto>> GetCategory(int? page, int? pageSize, string? sort, string? query, string username);
    Task<List<AlertDto>> GetAlertsCategory(int? categoryId, int? page, int? pageSize, string? query, string? stateList, double? x, double? y, double? r, bool? forceGps, string? sort, string username);
    Task<List<EventDto>> GetEventsByCategory(int? categoryId, int? page, int? pageSize, string? query, string? sort, string username);
    Task<CategoryDto?> GetCategoryById(int categoryid, string username);
}

public class SondaEMService : ISondaEMService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISondaAuthService _sondaAuthService;
    private readonly ApiConfig _apiConfig;
    private readonly ILogger<SondaEMService> _logger;

    public SondaEMService(IHttpClientFactory httpClientFactory, ISondaAuthService sondaAuthService, IOptions<ApiConfig> apiConfigOptions, ILogger<SondaEMService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _sondaAuthService = sondaAuthService;
        _apiConfig = apiConfigOptions.Value;
        _logger = logger;
    }

    public async Task<CategoryDto?> GetCategoryById(int categoryid, string username)
    {
        if (categoryid <= 0)
        {
            _logger.LogWarning("El CategoryId debe ser mayor que cero. Valor recibido: {CategoryId}", categoryid);
            throw new ArgumentException("El CategoryId debe ser mayor que cero.", nameof(categoryid));
        }
        string baseUrl = _apiConfig.BaseUrl.UrlEM;
        string endpoint = _apiConfig.EndpointsEM["Category"]["GetById"].Replace("{id}", categoryid.ToString());
        string getDataUrl = baseUrl + endpoint;
        string token = await _sondaAuthService.GetUserTokenEmAsync(username);
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync(getDataUrl);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("No se encontró la categoría con ID {CategoryId}", categoryid);
            return null;
        }
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _logger.LogError("Token inválido o expirado para usuario {Username}", username);
            throw new Exception("No tienes permisos: token inválido o expirado (401 Unauthorized).");
        }
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            _logger.LogError("Acceso prohibido para usuario {Username}", username);
            throw new Exception("No tienes permisos para acceder a este recurso (403 Forbidden).");
        }
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(responseBody) || !responseBody.TrimStart().StartsWith("{"))
        {
            _logger.LogWarning("Respuesta vacía o no válida para categoría ID {CategoryId}", categoryid);
            return null;
        }
        return JsonSerializer.Deserialize<CategoryDto>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    public async Task<EventDto?> GetEventById(int eventId, string username)
    {
        if (eventId <= 0)
        {
            _logger.LogWarning("El eventId debe ser positivo. Valor recibido: {EventId}", eventId);
            throw new ArgumentException("El eventId debe ser positivo.", nameof(eventId));
        }
        string baseUrl = _apiConfig.BaseUrl.UrlEM;
        string endpoint = _apiConfig.EndpointsEM["Event"]["GetById"].Replace("{eventId}", eventId.ToString());
        string token = await _sondaAuthService.GetUserTokenEmAsync(username);
        string getDataUrl = baseUrl + endpoint;
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync(getDataUrl);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("No se encontró el evento con ID {EventId}", eventId);
            return null;
        }
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _logger.LogError("Token inválido o expirado para usuario {Username}", username);
            throw new Exception("No tienes permisos: token inválido o expirado (401 Unauthorized).");
        }
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            _logger.LogError("Acceso prohibido para usuario {Username}", username);
            throw new Exception("No tienes permisos para acceder a este recurso (403 Forbidden).");
        }
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(responseBody) || !responseBody.TrimStart().StartsWith("{"))
        {
            _logger.LogWarning("Respuesta vacía o no válida para evento ID {EventId}", eventId);
            return null;
        }
        return JsonSerializer.Deserialize<EventDto>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    public async Task<AlertDto?> GetAlertById(int alertId, string username)
    {
        if (alertId <= 0)
        {
            _logger.LogWarning("El alertId debe ser mayor que cero. Valor recibido: {AlertId}", alertId);
            throw new ArgumentException("El alertId debe ser mayor que cero.", nameof(alertId));
        }
        string baseUrl = _apiConfig.BaseUrl.UrlEM;
        string endpoint = _apiConfig.EndpointsEM["Alert"]["GetById"].Replace("{alertId}", alertId.ToString());
        string token = await _sondaAuthService.GetUserTokenEmAsync(username);
        string getDataUrl = baseUrl + endpoint;
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync(getDataUrl);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("No se encontró la alerta con ID {AlertId}", alertId);
            return null;
        }
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _logger.LogError("Token inválido o expirado para usuario {Username}", username);
            throw new Exception("No tienes permisos: token inválido o expirado (401 Unauthorized).");
        }
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            _logger.LogError("Acceso prohibido para usuario {Username}", username);
            throw new Exception("No tienes permisos para acceder a este recurso (403 Forbidden).");
        }
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(responseBody) || !responseBody.TrimStart().StartsWith("{"))
        {
            _logger.LogWarning("Respuesta vacía o no válida para alerta ID {AlertId}", alertId);
            return null;
        }
        return JsonSerializer.Deserialize<AlertDto>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    public async Task<List<AlertDto>> GetAlerts(int? page, int? pageSize, string? query, string? stateList, double? x, double? y, double? r, bool? forceGps, string? sort, string username)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlEM;
        string endpoint = _apiConfig.EndpointsEM["Alert"]["GetAll"];
        var queryParams = new List<string>();
        if (page.HasValue && page.Value > 0) { queryParams.Add($"page={page.Value.ToString(CultureInfo.InvariantCulture)}"); }
        if (pageSize.HasValue && pageSize.Value > 0) { queryParams.Add($"pageSize={pageSize.Value.ToString(CultureInfo.InvariantCulture)}"); }
        if (!string.IsNullOrEmpty(query)) { queryParams.Add($"query={Uri.EscapeDataString(query)}"); }
        if (!string.IsNullOrEmpty(stateList)) { queryParams.Add($"stateList={Uri.EscapeDataString(stateList)}"); }
        if (x.HasValue) { queryParams.Add($"x={x.Value.ToString(CultureInfo.InvariantCulture)}"); }
        if (y.HasValue) { queryParams.Add($"y={y.Value.ToString(CultureInfo.InvariantCulture)}"); }
        if (r.HasValue) { queryParams.Add($"r={r.Value.ToString(CultureInfo.InvariantCulture)}"); }
        if (forceGps.HasValue) { queryParams.Add($"forceGps={forceGps.Value.ToString().ToLowerInvariant()}"); }
        if (!string.IsNullOrEmpty(sort)) { queryParams.Add($"sort={Uri.EscapeDataString(sort)}"); }
        string queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;
        string getDataUrl = baseUrl + endpoint + queryString;
        string token = await _sondaAuthService.GetUserTokenEmAsync(username);
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync(getDataUrl);
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            _logger.LogWarning("La respuesta de la API está vacía para GetAlerts.");
            return new List<AlertDto>();
        }
        var trimmed = responseBody.TrimStart();
        if (trimmed.StartsWith("{"))
        {
            var apiResponse = JsonSerializer.Deserialize<AlertApiResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return apiResponse?.Results ?? new List<AlertDto>();
        }
        else if (trimmed.StartsWith("["))
        {
            var listResponse = JsonSerializer.Deserialize<List<AlertDto>>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return listResponse ?? new List<AlertDto>();
        }
        else
        {
            _logger.LogError("La respuesta de la API no es JSON válido. Respuesta: {ResponseBody}", responseBody);
            return new List<AlertDto>();
        }
    }

    public async Task<List<AlertDto>> GetStoredAlerts(int? page, int? pageSize, string? query, string? stateList, double? x, double? y, double? r, string? sort, string username)
    {
        if (!page.HasValue || page.Value <= 0)
        {
            _logger.LogWarning("El parámetro 'page' es requerido y debe ser mayor que cero.");
            throw new ArgumentException("El parámetro 'page' es requerido y debe ser mayor que cero.", nameof(page));
        }
        if (!pageSize.HasValue || pageSize.Value <= 0)
        {
            _logger.LogWarning("El parámetro 'pageSize' es requerido y debe ser mayor que cero.");
            throw new ArgumentException("El parámetro 'pageSize' es requerido y debe ser mayor que cero.", nameof(pageSize));
        }
        string baseUrl = _apiConfig.BaseUrl.UrlEM;
        string endpoint = _apiConfig.EndpointsEM["Alert"]["GetStored"];
        var queryParams = new List<string>();
        if (page.HasValue) { queryParams.Add($"page={page.Value.ToString(CultureInfo.InvariantCulture)}"); }
        if (pageSize.HasValue) { queryParams.Add($"pageSize={pageSize.Value.ToString(CultureInfo.InvariantCulture)}"); }
        if (!string.IsNullOrEmpty(query)) { queryParams.Add($"query={Uri.EscapeDataString(query)}"); }
        if (!string.IsNullOrEmpty(stateList)) { queryParams.Add($"stateList={Uri.EscapeDataString(stateList)}"); }
        if (x.HasValue) { queryParams.Add($"x={x.Value.ToString(CultureInfo.InvariantCulture)}"); }
        if (y.HasValue) { queryParams.Add($"y={y.Value.ToString(CultureInfo.InvariantCulture)}"); }
        if (r.HasValue) { queryParams.Add($"r={r.Value.ToString(CultureInfo.InvariantCulture)}"); }
        if (!string.IsNullOrEmpty(sort)) { queryParams.Add($"sort={Uri.EscapeDataString(sort)}"); }
        string queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;
        string getDataUrl = baseUrl + endpoint + queryString;
        string token = await _sondaAuthService.GetUserTokenEmAsync(username);
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync(getDataUrl);
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            _logger.LogWarning("La respuesta de la API está vacía para GetStoredAlerts.");
            return new List<AlertDto>();
        }
        var trimmed = responseBody.TrimStart();
        if (trimmed.StartsWith("{"))
        {
            var apiResponse = JsonSerializer.Deserialize<AlertApiResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return apiResponse?.Results ?? new List<AlertDto>();
        }
        else if (trimmed.StartsWith("["))
        {
            var listResponse = JsonSerializer.Deserialize<List<AlertDto>>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return listResponse ?? new List<AlertDto>();
        }
        else
        {
            _logger.LogError("La respuesta de la API no es JSON válido. Respuesta: {ResponseBody}", responseBody);
            return new List<AlertDto>();
        }
    }

    public async Task<List<EventDto>> GetEvents(int? page, int? pageSize, string? sort, string? query, string username)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlEM;
        string endpoint = _apiConfig.EndpointsEM["Event"]["GetEvents"];
        var queryParams = new List<string>();
        if (!string.IsNullOrEmpty(sort)) { queryParams.Add($"sort={Uri.EscapeDataString(sort)}"); }
        if (!string.IsNullOrEmpty(query)) { queryParams.Add($"query={Uri.EscapeDataString(query)}"); }
        string queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;
        string getDataUrl = baseUrl + endpoint + queryString;
        string token = await _sondaAuthService.GetUserTokenEmAsync(username);
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync(getDataUrl);
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            _logger.LogWarning("La respuesta de la API está vacía para GetEvents.");
            return new List<EventDto>();
        }
        var trimmed = responseBody.TrimStart();
        if (trimmed.StartsWith("{"))
        {
            var apiResponse = JsonSerializer.Deserialize<EventApiResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return apiResponse?.Results ?? new List<EventDto>();
        }
        else if (trimmed.StartsWith("["))
        {
            var listResponse = JsonSerializer.Deserialize<List<EventDto>>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return listResponse ?? new List<EventDto>();
        }
        else
        {
            _logger.LogError("La respuesta de la API no es JSON válido. Respuesta: {ResponseBody}", responseBody);
            return new List<EventDto>();
        }
    }

    public async Task<List<EventTypeDto>> GetEventTypes(string username)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlEM;
        string endpoint = _apiConfig.EndpointsEM["EventType"]["GetEventTypes"];
        string getDataUrl = baseUrl + endpoint;
        string token = await _sondaAuthService.GetUserTokenEmAsync(username);
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync(getDataUrl);
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(responseBody) || !responseBody.TrimStart().StartsWith("["))
        {
            _logger.LogWarning("La respuesta de la API está vacía o no es un array para GetEventTypes.");
            return new List<EventTypeDto>();
        }
        return JsonSerializer.Deserialize<List<EventTypeDto>>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<EventTypeDto>();
    }

    public async Task<ExtensionDtoDup?> GetExtensionById(int extensionId, string username)
    {
        if (extensionId <= 0)
        {
            _logger.LogWarning("El extensionId debe ser mayor que cero. Valor recibido: {ExtensionId}", extensionId);
            throw new ArgumentException("El extensionId debe ser mayor que cero.", nameof(extensionId));
        }
        string baseUrl = _apiConfig.BaseUrl.UrlEM;
        string endpoint = _apiConfig.EndpointsEM["Extension"]["GetById"].Replace("{extensionId}", extensionId.ToString());
        string getDataUrl = baseUrl + endpoint;
        string token = await _sondaAuthService.GetUserTokenEmAsync(username);
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync(getDataUrl);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("No se encontró la extensión con ID {ExtensionId}", extensionId);
            return null;
        }
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _logger.LogError("Token inválido o expirado para usuario {Username}", username);
            throw new Exception("No tienes permisos: token inválido o expirado (401 Unauthorized).");
        }
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            _logger.LogError("Acceso prohibido para usuario {Username}", username);
            throw new Exception("No tienes permisos para acceder a este recurso (403 Forbidden).");
        }
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(responseBody) || !responseBody.TrimStart().StartsWith("{"))
        {
            _logger.LogWarning("Respuesta vacía o no válida para extensión ID {ExtensionId}", extensionId);
            return null;
        }
        return JsonSerializer.Deserialize<ExtensionDtoDup>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    public async Task<List<ExtensionDto>> GetExtensions(int? page, int? pageSize, string? sort, string? query, string? states, string? dates, string? priorities, string? categories, string? zones, string username)
    {
        if (page.HasValue && page.Value < 0)
        {
            _logger.LogWarning("El parámetro 'page' debe ser mayor o igual que cero. Valor recibido: {Page}", page);
            throw new ArgumentException("El parámetro 'page' debe ser mayor o igual que cero.");
        }
        string baseUrl = _apiConfig.BaseUrl.UrlEM;
        string endpoint = _apiConfig.EndpointsEM["Extension"]["GetAll"];
        var queryParams = new List<string>();
        if (page.HasValue) { queryParams.Add($"page={page.Value}"); }
        if (pageSize.HasValue) { queryParams.Add($"pageSize={pageSize.Value}"); }
        if (!string.IsNullOrEmpty(sort)) { queryParams.Add($"sort={Uri.EscapeDataString(sort)}"); }
        if (!string.IsNullOrEmpty(query)) { queryParams.Add($"query={Uri.EscapeDataString(query)}"); }
        if (!string.IsNullOrEmpty(states)) { queryParams.Add($"states={Uri.EscapeDataString(states)}"); }
        if (!string.IsNullOrEmpty(dates)) { queryParams.Add($"dates={Uri.EscapeDataString(dates)}"); }
        if (!string.IsNullOrEmpty(priorities)) { queryParams.Add($"priorities={Uri.EscapeDataString(priorities)}"); }
        if (!string.IsNullOrEmpty(categories)) { queryParams.Add($"categories={Uri.EscapeDataString(categories)}"); }
        if (!string.IsNullOrEmpty(zones)) { queryParams.Add($"zones={Uri.EscapeDataString(zones)}"); }
        string queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;
        string getDataUrl = baseUrl + endpoint + queryString;
        string token = await _sondaAuthService.GetUserTokenEmAsync(username);
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync(getDataUrl);
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(responseBody) || !responseBody.TrimStart().StartsWith("{"))
        {
            _logger.LogWarning("La respuesta de la API está vacía o no es válida para GetExtensions.");
            return new List<ExtensionDto>();
        }
        var apiResponse = JsonSerializer.Deserialize<ExtensionApiResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return apiResponse?.Results ?? new List<ExtensionDto>();
    }

    public async Task<List<AttachmentDto>> GetAttachedItems(int extensionId, string username)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlEM;
        string endpoint = _apiConfig.EndpointsEM["Extension"]["GetAttachedItems"].Replace("{extensionId}", extensionId.ToString());
        string getDataUrl = baseUrl + endpoint;
        string token = await _sondaAuthService.GetUserTokenEmAsync(username);
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync(getDataUrl);
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(responseBody) || !responseBody.TrimStart().StartsWith("["))
        {
            _logger.LogWarning("La respuesta de la API está vacía o no es válida para GetAttachedItems.");
            return new List<AttachmentDto>();
        }
        return JsonSerializer.Deserialize<List<AttachmentDto>>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<AttachmentDto>();
    }
    

    public async Task<List<ExtensionDtoDup>> GetExtensionByEventId(int eventId, string username)
    {
        if (eventId <= 0)
        {
            _logger.LogWarning("El eventId debe ser mayor que cero. Valor recibido: {EventId}", eventId);
            throw new ArgumentException("El eventId debe ser mayor que cero.", nameof(eventId));
        }
        string baseUrl = _apiConfig.BaseUrl.UrlEM;
        string endpoint = _apiConfig.EndpointsEM["Event"]["Extensions"].Replace("{eventId}", eventId.ToString());
        string getDataUrl = baseUrl + endpoint;
        string token = await _sondaAuthService.GetUserTokenEmAsync(username);
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync(getDataUrl);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("No se encontraron extensiones para el evento con ID {EventId}", eventId);
            return null;
        }
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _logger.LogError("Token inválido o expirado para usuario {Username}", username);
            throw new Exception("No tienes permisos: token inválido o expirado (401 Unauthorized).");
        }
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            _logger.LogError("Acceso prohibido para usuario {Username}", username);
            throw new Exception("No tienes permisos para acceder a este recurso (403 Forbidden).");
        }
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            _logger.LogWarning("La respuesta de la API está vacía para GetExtensionByEventId.");
            return new List<ExtensionDtoDup>();
        }
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        List<ExtensionDtoDup> extensions = JsonSerializer.Deserialize<List<ExtensionDtoDup>>(responseBody, options);
        return extensions;

    }
    
    public async Task<List<CategoryDto>> GetCategory(
        int? page,
        int? pageSize,
        string? sort,
        string? query,
        string username)
    {
        // NOTE: The Sonda API's category endpoint does NOT support pagination parameters
        // Attempting to send page/pageSize results in 404 errors
        // This is an API design inconsistency that we need to work around
        string baseUrl = _apiConfig.BaseUrl.UrlEM;
        string endpoint = _apiConfig.EndpointsEM["Category"]["GetAll"];
        
        // Only include sort and query parameters if provided
        // DO NOT include page/pageSize as the API doesn't support them
        var queryParams = new List<string>();
        if (!string.IsNullOrEmpty(sort)) { queryParams.Add($"sort={Uri.EscapeDataString(sort)}"); }
        if (!string.IsNullOrEmpty(query)) { queryParams.Add($"query={Uri.EscapeDataString(query)}"); }
        
        string queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;
        string getDataUrl = baseUrl + endpoint + queryString;
        string token = await _sondaAuthService.GetUserTokenEmAsync(username);
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync(getDataUrl);
        response.EnsureSuccessStatusCode();
        
        var responseBody = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            _logger.LogWarning("La respuesta de la API está vacía para GetCategory.");
            return null;
        }

        var trimmed = responseBody.TrimStart();
        if (trimmed.StartsWith("{"))
        {
            // Si la respuesta es un objeto con la propiedad "results"
            var apiResponse = JsonSerializer.Deserialize<CategoryApiResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return apiResponse?.Results ?? new List<CategoryDto>();

        }
        else if (trimmed.StartsWith("["))
        {
            // Si la respuesta es un array directo (por si acaso)
            var listResponse = JsonSerializer.Deserialize<List<CategoryDto>>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return listResponse ?? new List<CategoryDto>();
        }
        else
        {
            _logger.LogError("La respuesta de la API no es JSON válido. Respuesta: {ResponseBody}", responseBody);
            return new List<CategoryDto>();
        }
    }

    public async Task<List<AlertDto>> GetAlertsCategory(
        int? categoryId,
        int? page,
        int? pageSize,
        string? query,
        string? stateList,
        double? x,
        double? y,
        double? r,
        bool? forceGps,
        string? sort,
        string username)
    {
        if (!categoryId.HasValue || categoryId.Value <= 0)
        {
            _logger.LogWarning("El parámetro 'categoryId' debe tener valor y ser mayor que cero. Valor recibido: {CategoryId}", categoryId);
            throw new ArgumentException("El categoryId debe tener valor y ser mayor que cero.", nameof(categoryId));
        }

        var allAlerts = await GetAlerts(page, pageSize, query, stateList, x, y, r, forceGps, sort, username);

        var filteredAlerts = allAlerts
            .Where(alert => alert.AlertCategory != null && alert.AlertCategory.Id == categoryId.Value)
            .ToList();

        if (filteredAlerts.Count == 0)
        {
            _logger.LogInformation("No se encontraron alertas para la categoría {CategoryId}", categoryId);
        }

        return filteredAlerts;
    }

    public async Task<List<EventDto>> GetEventsByCategory(
        int? categoryId,
        int? page,
        int? pageSize,
        string? query,
        string? sort,
        string username)
    {
        if (!categoryId.HasValue || categoryId.Value <= 0)
        {
            _logger.LogWarning("El parámetro 'categoryId' debe tener valor y ser mayor que cero. Valor recibido: {CategoryId}", categoryId);
            throw new ArgumentException("El categoryId debe tener valor y ser mayor que cero.", nameof(categoryId));
        }

        var allEvents = await GetEvents(page, pageSize, sort, query, username);

        var filteredEvents = allEvents
            .Where(ev => ev.Categories != null && ev.Categories.Any(cat => cat.Id == categoryId.Value))
            .ToList();

        if (filteredEvents.Count == 0)
        {
            _logger.LogInformation("No se encontraron eventos para la categoría {CategoryId}", categoryId);
        }

        return filteredEvents;
    }
}

