using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using OmniMonitor.Server.Configuration;
using OmniMonitor.Shared.Dtos;
using OmniMonitor.Shared.Dtos.EM;
using System;
using OmniMonitor.Server.Resources;
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
    Task<List<AlertDto>> GetStoredAlerts(
        int? page,
        int? pageSize,
        string? query,
        string? stateList,
        double? x,
        double? y,
        double? r,
        string? sort,
        string username);
    Task<List<EventDto>> GetEvents(
        int? page,
        int? pageSize,
        string? sort,
        string? query,
        string username);
    Task<List<EventTypeDto>> GetEventTypes(string username);
    Task<ExtensionDtoDup?> GetExtensionById(int extensionId, string username);
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
    string username);
    Task<List<AttachmentDto>> GetAttachedItems(int extensionId, string username);
    Task<List<ExtensionDtoDup>> GetExtensionByEventId(int eventId, string username);
    Task<List<CategoryDto>> GetCategory(
        int? page,
        int? pageSize,
        string? sort,
        string? query,
        string username);
    Task<List<AlertDto>> GetAlertsCategory(
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
        string username);
    Task<List<EventDto>> GetEventsByCategory(
        int? categoryId,
        int? page,
        int? pageSize,
        string? query,
        string? sort,
        string username);
    Task<CategoryDto?> GetCategoryById(int categoryid, string username);
}


public class SondaEMService : ISondaEMService
{
    #region Fields

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISondaAuthService _sondaAuthService;
    private readonly ApiConfig _apiConfig;

    #endregion

    #region Constructors

    public SondaEMService(IHttpClientFactory httpClientFactory, ISondaAuthService sondaAuthService, IOptions<ApiConfig> apiConfigOptions)
    {
        _httpClientFactory = httpClientFactory;
        _sondaAuthService = sondaAuthService;
        _apiConfig = apiConfigOptions.Value;
    }

    #endregion

    #region Methods

    public async Task<CategoryDto?> GetCategoryById(int categoryid, string username)
    {
        if (categoryid <= 0)
        {
            throw new ArgumentException("El CategoryId debe ser mayor que cero.", nameof(categoryid));
        }
        string baseUrl = _apiConfig.BaseUrl.UrlEM;
        string endpoint = _apiConfig.EndpointsEM["Category"]["GetById"].Replace("{id}", categoryid.ToString());
        string getDataUrl = baseUrl + endpoint;
        string token = await _sondaAuthService.GetUserTokenEMAsync(username);
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync(getDataUrl);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            throw new Exception(Language.ApiUnauthorized);
        }
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            throw new Exception(Language.ApiForbidden);
        }
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(responseBody) || !responseBody.TrimStart().StartsWith("{"))
        {
            return null;
        }
        return JsonSerializer.Deserialize<CategoryDto>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    public async Task<EventDto?> GetEventById(int eventId, string username)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlEM;
        string endpoint = _apiConfig.EndpointsEM["Event"]["GetById"].Replace("{eventId}", eventId.ToString());
        if (eventId <= 0)
        {
            throw new ArgumentException("El eventId debe ser positivo.", nameof(eventId));
        }
        string token = await _sondaAuthService.GetUserTokenEMAsync(username);
        string getDataUrl = baseUrl + endpoint;
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync(getDataUrl);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Traducir 404 a null para que el Controller devuelva NotFound
            return null;
        }
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            throw new Exception(Language.ApiUnauthorized);
        }
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            throw new Exception(Language.ApiForbidden);
        }

        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(responseBody) || !responseBody.TrimStart().StartsWith("{"))
        {
            return null;
        }
        return JsonSerializer.Deserialize<EventDto>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    public async Task<AlertDto?> GetAlertById(int alertId, string username)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlEM;
        string endpoint = _apiConfig.EndpointsEM["Alert"]["GetById"].Replace("{alertId}", alertId.ToString());
        if (alertId <= 0)
        {
            throw new ArgumentException("El alertId debe ser mayor que cero.", nameof(alertId));
        }

        string token = await _sondaAuthService.GetUserTokenEMAsync(username);

        string getDataUrl = baseUrl + endpoint;
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync(getDataUrl);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            throw new Exception(Language.ApiUnauthorized);
        }
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            throw new Exception(Language.ApiForbidden);
        }
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
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
        string username)
    {
        // NOTE: Making pagination optional like other endpoints
        // Some Sonda API endpoints may not support pagination properly
        string baseUrl = _apiConfig.BaseUrl.UrlEM;
        string endpoint = _apiConfig.EndpointsEM["Alert"]["GetAll"];

        var queryParams = new List<string>();
        // Only add pagination if provided
        if (page.HasValue && page.Value > 0) queryParams.Add($"page={page.Value.ToString(CultureInfo.InvariantCulture)}");
        if (pageSize.HasValue && pageSize.Value > 0) queryParams.Add($"pageSize={pageSize.Value.ToString(CultureInfo.InvariantCulture)}");
        if (!string.IsNullOrEmpty(query)) queryParams.Add($"query={Uri.EscapeDataString(query)}");
        if (!string.IsNullOrEmpty(stateList)) queryParams.Add($"stateList={Uri.EscapeDataString(stateList)}");
        if (x.HasValue) queryParams.Add($"x={x.Value.ToString(CultureInfo.InvariantCulture)}");
        if (y.HasValue) queryParams.Add($"y={y.Value.ToString(CultureInfo.InvariantCulture)}");
        if (r.HasValue) queryParams.Add($"r={r.Value.ToString(CultureInfo.InvariantCulture)}");
        if (forceGps.HasValue) queryParams.Add($"forceGps={forceGps.Value.ToString().ToLowerInvariant()}");
        if (!string.IsNullOrEmpty(sort)) queryParams.Add($"sort={Uri.EscapeDataString(sort)}");

        string queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;
        string getDataUrl = baseUrl + endpoint + queryString;
        string token = await _sondaAuthService.GetUserTokenEMAsync(username);
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync(getDataUrl);
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return new List<AlertDto>();
        }
        var trimmed = responseBody.TrimStart();
        if (trimmed.StartsWith("{"))
        {
            // La API devuelve un objeto con la lista en la propiedad "results"
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
            return new List<AlertDto>();
        }
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
        string username)
    {
        if (!page.HasValue)
        {
            throw new ArgumentException(string.Format(Language.ParameterRequired, "page"), nameof(page));
        }
        if (!pageSize.HasValue)
        {
            throw new ArgumentException(string.Format(Language.ParameterRequired, "pageSize"), nameof(pageSize));
        }
        if (page.Value <= 0)
        {
            throw new ArgumentException(string.Format(Language.ParameterMustBePositive, "page"), nameof(page));
        }
        if (pageSize.Value <= 0)
        {
            throw new ArgumentException(string.Format(Language.ParameterMustBePositive, "pageSize"), nameof(pageSize));
        }
        string baseUrl = _apiConfig.BaseUrl.UrlEM;
        string endpoint = _apiConfig.EndpointsEM["Alert"]["GetStored"];
        var queryParams = new List<string>();
        if (page.HasValue) queryParams.Add($"page={page.Value.ToString(CultureInfo.InvariantCulture)}");
        if (pageSize.HasValue) queryParams.Add($"pageSize={pageSize.Value.ToString(CultureInfo.InvariantCulture)}");
        if (!string.IsNullOrEmpty(query)) queryParams.Add($"query={Uri.EscapeDataString(query)}");
        if (!string.IsNullOrEmpty(stateList)) queryParams.Add($"stateList={Uri.EscapeDataString(stateList)}");
        if (x.HasValue) queryParams.Add($"x={x.Value.ToString(CultureInfo.InvariantCulture)}");
        if (y.HasValue) queryParams.Add($"y={y.Value.ToString(CultureInfo.InvariantCulture)}");
        if (r.HasValue) queryParams.Add($"r={r.Value.ToString(CultureInfo.InvariantCulture)}");
        if (!string.IsNullOrEmpty(sort)) queryParams.Add($"sort={Uri.EscapeDataString(sort)}");
        string queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;
        string getDataUrl = baseUrl + endpoint + queryString;
        string token = await _sondaAuthService.GetUserTokenEMAsync(username);
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync(getDataUrl);
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return new List<AlertDto>();
        }
        var trimmed = responseBody.TrimStart();
        if (trimmed.StartsWith("{"))
        {
            // La API devuelve un objeto con la lista en la propiedad "results"
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
            return new List<AlertDto>();
        }
    }

    public async Task<List<EventDto>> GetEvents(
        int? page,
        int? pageSize,
        string? sort,
        string? query,
        string username)
    {
        // NOTE: Similar to Categories, the Sonda API's events endpoint may not support pagination
        string baseUrl = _apiConfig.BaseUrl.UrlEM;
        string endpoint = _apiConfig.EndpointsEM["Event"]["GetEvents"];

        // Only include sort and query parameters if provided
        // DO NOT include page/pageSize as the API may not support them
        var queryParams = new List<string>();
        if (!string.IsNullOrEmpty(sort)) queryParams.Add($"sort={Uri.EscapeDataString(sort)}");
        if (!string.IsNullOrEmpty(query)) queryParams.Add($"query={Uri.EscapeDataString(query)}");

        string queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;
        string getDataUrl = baseUrl + endpoint + queryString;

        string token = await _sondaAuthService.GetUserTokenEMAsync(username);

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync(getDataUrl);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return new List<EventDto>();
        }
        var trimmed = responseBody.TrimStart();
        if (trimmed.StartsWith("{"))
        {
            // La API devuelve un objeto con la lista en la propiedad "results"
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
            return new List<EventDto>();
        }
    }

    public async Task<List<EventTypeDto>> GetEventTypes(string username)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlEM;
        string endpoint = _apiConfig.EndpointsEM["EventType"]["GetEventTypes"];
        string getDataUrl = baseUrl + endpoint;
        string token = await _sondaAuthService.GetUserTokenEMAsync(username);
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync(getDataUrl);
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(responseBody) || !responseBody.TrimStart().StartsWith("["))
        {
            return new List<EventTypeDto>();
        }
        return JsonSerializer.Deserialize<List<EventTypeDto>>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<EventTypeDto>();
    }

    public async Task<ExtensionDtoDup?> GetExtensionById(int extensionId, string username)
    {
        if (extensionId <= 0)
        {
            throw new ArgumentException("El extensionId debe ser mayor que cero.", nameof(extensionId));
        }
        string baseUrl = _apiConfig.BaseUrl.UrlEM;
        string endpoint = _apiConfig.EndpointsEM["Extension"]["GetById"].Replace("{extensionId}", extensionId.ToString());
        string getDataUrl = baseUrl + endpoint;
        string token = await _sondaAuthService.GetUserTokenEMAsync(username);
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync(getDataUrl);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            throw new Exception(Language.ApiUnauthorized);
        }
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            throw new Exception(Language.ApiForbidden);
        }
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
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
            string username)
    {
        if (page.HasValue && page.Value < 0)
        {
            throw new ArgumentException(string.Format(Language.ParameterMustBePositive, "page"));
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
        string token = await _sondaAuthService.GetUserTokenEMAsync(username);
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync(getDataUrl);
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(responseBody) || !responseBody.TrimStart().StartsWith("{"))
        {
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
        string token = await _sondaAuthService.GetUserTokenEMAsync(username);
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync(getDataUrl);
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(responseBody) || !responseBody.TrimStart().StartsWith("["))
        {
            return new List<AttachmentDto>();
        }
        return JsonSerializer.Deserialize<List<AttachmentDto>>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<AttachmentDto>();
    }


    public async Task<List<ExtensionDtoDup>> GetExtensionByEventId(int eventId, string username)
    {
        if (eventId <= 0)
        {
            throw new ArgumentException("El eventId debe ser mayor que cero.", nameof(eventId));
        }
        string baseUrl = _apiConfig.BaseUrl.UrlEM;
        string endpoint = _apiConfig.EndpointsEM["Event"]["Extensions"].Replace("{eventId}", eventId.ToString());
        string getDataUrl = baseUrl + endpoint;
        string token = await _sondaAuthService.GetUserTokenEMAsync(username);
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync(getDataUrl);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            throw new Exception(Language.ApiUnauthorized);
        }
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            throw new Exception(Language.ApiForbidden);
        }
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();

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
        if (!string.IsNullOrEmpty(sort)) queryParams.Add($"sort={Uri.EscapeDataString(sort)}");
        if (!string.IsNullOrEmpty(query)) queryParams.Add($"query={Uri.EscapeDataString(query)}");

        string queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;
        string getDataUrl = baseUrl + endpoint + queryString;

        string token = await _sondaAuthService.GetUserTokenEMAsync(username);

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync(getDataUrl);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(responseBody))
        {
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
            throw new ArgumentException("El categoryId debe tener valor y ser mayor que cero.", nameof(categoryId));
        }

        // Obtén todas las alertas según los parámetros
        var allAlerts = await GetAlerts(page, pageSize, query, stateList, x, y, r, forceGps, sort, username);

        // Filtra las alertas que tengan la categoría con el id solicitado
        var filteredAlerts = allAlerts
            .Where(alert => alert.AlertCategory != null && alert.AlertCategory.Id == categoryId)
            .ToList();

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
            throw new ArgumentException("El categoryId debe tener valor y ser mayor que cero.", nameof(categoryId));
        }
        // Obtén todos los eventos según los parámetros
        var allEvents = await GetEvents(page, pageSize, sort, query, username);

        // Filtra los eventos que tengan la categoría con el id solicitado
        var filteredEvents = allEvents
            .Where(ev => ev.Categories != null && ev.Categories.Any(cat => cat.Id == categoryId))
            .ToList();

        return filteredEvents;
    }

    #endregion
}

