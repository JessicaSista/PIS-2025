using Microsoft.Extensions.Options;
using OmniMonitor.Server.Configuration;
using OmniMonitor.Shared.Dtos;
using OmniMonitor.Shared.Dtos.AM;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

public interface ISondaAMService
{

    //****************DEVICES***************
    // GET all devices
    Task<AssetDto> GetAssetById(int id, string username, string password);

    // GET all stock
    Task<List<StockDto>> GetAllStock(int? page, string? queryString, string? sort, int? pageSize, string? bundlesId, string username, string password);

    // GET stock parameters by bundleId
    Task<BundleDto> GetStockParametersByBundleId(int bundleId, string username, string password);

    // GET stock by id
    Task<StockDto> GetStockById(int stockId, string username, string password);

        // GET assets paginados y filtrados
    Task<List<AssetDto>> GetAssets(int? page, string? queryString, string? bundles, int? assetTypeId, string? sort, int? pageSize, string username, string password);

     // GET assets basic data paginados y filtrados
    Task<List<AssetDto>> GetAssetsBasicData(int? page, string? queryString, int? pageSize, int? bundleId, string username, string password);

      // GET linked assets paginados y filtrados
    Task<List<AssetDto>> GetLinkedAssets(int? page, string? queryString, string? sort, int? pageSize, string username, string password);

        // GET asset relations paginados y filtrados
    Task<List<RelatedAssetDto>> GetAssetRelations(int assetId, int? page, int? pageSize, string username, string password);

        // GET bundles paginados y filtrados
    Task<List<BundleDto>> GetBundles(int? page, string? queryString, string? sort, int? pageSize, string username, string password);

        // GET asset history paginado y filtrado
    Task<List<AssetDto>> GetAssetHistory(int? page, string? queryString, string? sort, int? pageSize, string? bundlesId, string username, string password);

        // GET event task instance by id
    Task<EventTaskInstanceDto> GetEventTaskInstanceById(int eventTaskInstanceId, string username, string password);

    // GET event tasks filtrados y paginados
    Task<List<EventTaskInstanceDto>> GetEventTaskInstances(string dates, int? page, string queryString, int? bundleId, string state, string sort, int? taskTypeId, int? groupId, int? pageSize, bool tasksAssignedToMe, bool tasksPendingApproval, string username, string password);

        // GET actions for event task instance
    Task<List<OmniMonitor.Shared.Dtos.AM.EventTaskActionDto>> GetEventTaskInstanceActions(int taskInstanceId, string username, string password);

    // GET stock for event task instance
    Task<List<EventTaskInstanceStockDto>> GetEventTaskInstanceStock(int taskInstanceId, string username, string password);

}


public class SondaAMService : ISondaAMService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISondaAuthService _sondaAuthService;
    private readonly ApiConfig _apiConfig;

    public SondaAMService(IHttpClientFactory httpClientFactory, ISondaAuthService sondaAuthService, IOptions<ApiConfig> apiConfigOptions)
    {
        _httpClientFactory = httpClientFactory;
        _sondaAuthService = sondaAuthService;
        _apiConfig = apiConfigOptions.Value;
    }

    public async Task<AssetDto> GetAssetById(int id, string username, string password)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlAM;
        string endpoint = _apiConfig.EndpointsAM["Asset"]["GetById"];
        if (id <= 0)
        {
            throw new ArgumentException("El ID debe ser positivo.", nameof(id));
        }

        string token = await _sondaAuthService.GetUserTokenAMAsync(username, password);

        string getDataUrl = baseUrl + endpoint + "?assetId=" + id;
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Respuesta de la API
        var response = await client.GetAsync(getDataUrl);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        Console.WriteLine("SONDA API RAW RESPONSE: " + responseBody);
        // Puedes revisar el log de consola para ver el JSON exacto que devuelve la API
        return JsonSerializer.Deserialize<AssetDto>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    public async Task<BundleDto> GetStockParametersByBundleId(int bundleId, string username, string password)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlAM;
        string endpoint = _apiConfig.EndpointsAM["Bundle"]["GetByBundleId"];
        string token = await _sondaAuthService.GetUserTokenAMAsync(username, password);

        string getDataUrl = $"{baseUrl}{endpoint}?bundleId={bundleId}";
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await client.GetAsync(getDataUrl);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            throw new Exception("No tienes permisos: token inválido o expirado (401 Unauthorized).");
        }
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            throw new Exception("No tienes permisos para acceder a este recurso (403 Forbidden).");
        }
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        Console.WriteLine("SONDA API RAW RESPONSE: " + responseBody);
        if (string.IsNullOrWhiteSpace(responseBody) || !responseBody.TrimStart().StartsWith("{"))
        {
            throw new Exception("La respuesta de la API no es JSON válido. Respuesta: " + responseBody);
        }
        return JsonSerializer.Deserialize<BundleDto>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    public async Task<List<StockDto>> GetAllStock(int? page, string? queryString, string? sort, int? pageSize, string? bundlesId, string username, string password)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlAM;
        string endpoint = _apiConfig.EndpointsAM["Stock"]["GetAll"];
        string token = await _sondaAuthService.GetUserTokenAMAsync(username, password);

        var queryParams = new List<string>();
        if (page.HasValue) queryParams.Add($"page={page.Value}");
        if (!string.IsNullOrEmpty(queryString)) queryParams.Add($"queryString={Uri.EscapeDataString(queryString)}");
        if (!string.IsNullOrEmpty(sort)) queryParams.Add($"sort={Uri.EscapeDataString(sort)}");
        if (pageSize.HasValue) queryParams.Add($"pageSize={pageSize.Value}");
        if (!string.IsNullOrEmpty(bundlesId)) queryParams.Add($"bundlesId={Uri.EscapeDataString(bundlesId)}");
        string query = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;

        string getDataUrl = baseUrl + endpoint + query;
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await client.GetAsync(getDataUrl);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            throw new Exception("No tienes permisos: token inválido o expirado (401 Unauthorized).");
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            throw new Exception("No tienes permisos para acceder a este recurso (403 Forbidden).");
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        Console.WriteLine("SONDA API RAW RESPONSE: " + responseBody);
        if (string.IsNullOrWhiteSpace(responseBody))
            throw new Exception("La respuesta de la API está vacía.");

        // Detecta si la respuesta es un objeto (con 'results') o una lista directa
        var trimmed = responseBody.TrimStart();
        if (trimmed.StartsWith("{"))
        {
            var apiResponse = JsonSerializer.Deserialize<OmniMonitor.Server.Models.StockApiResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return apiResponse?.Results ?? new List<StockDto>();
        }
        else if (trimmed.StartsWith("["))
        {
            var listResponse = JsonSerializer.Deserialize<List<StockDto>>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return listResponse ?? new List<StockDto>();
        }
        else
        {
            throw new Exception("La respuesta de la API no es JSON válido. Respuesta: " + responseBody);
        }
    }

    public async Task<StockDto> GetStockById(int stockId, string username, string password)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlAM;
        string endpoint = _apiConfig.EndpointsAM["Stock"]["GetById"].Replace("{stockId}", stockId.ToString());
        string token = await _sondaAuthService.GetUserTokenAMAsync(username, password);

        string getDataUrl = baseUrl + endpoint;
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await client.GetAsync(getDataUrl);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            throw new Exception("No tienes permisos: token inválido o expirado (401 Unauthorized).");
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            throw new Exception("No tienes permisos para acceder a este recurso (403 Forbidden).");
        //response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        Console.WriteLine("SONDA API RAW RESPONSE: " + responseBody);

        response.EnsureSuccessStatusCode();

        if (string.IsNullOrWhiteSpace(responseBody) || !responseBody.TrimStart().StartsWith("{"))
            throw new Exception("La respuesta de la API no es JSON válido. Respuesta: " + responseBody);
        return JsonSerializer.Deserialize<StockDto>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

        public async Task<List<AssetDto>> GetAssets(int? page, string? queryString, string? bundles, int? assetTypeId, string? sort, int? pageSize, string username, string password)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlAM;
        string endpoint = _apiConfig.EndpointsAM["Asset"]["GetAssets"];
        string token = await _sondaAuthService.GetUserTokenAMAsync(username, password);

        var queryParams = new List<string>();
        if (page.HasValue) queryParams.Add($"page={page.Value}");
        if (!string.IsNullOrEmpty(queryString)) queryParams.Add($"queryString={Uri.EscapeDataString(queryString)}");
        if (!string.IsNullOrEmpty(bundles)) queryParams.Add($"bundles={Uri.EscapeDataString(bundles)}");
        if (assetTypeId.HasValue) queryParams.Add($"assetTypeId={assetTypeId.Value}");
        if (!string.IsNullOrEmpty(sort)) queryParams.Add($"sort={Uri.EscapeDataString(sort)}");
        if (pageSize.HasValue) queryParams.Add($"pageSize={pageSize.Value}");
        string query = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;

        string getDataUrl = baseUrl + endpoint + query;
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await client.GetAsync(getDataUrl);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            throw new Exception("No tienes permisos: token inválido o expirado (401 Unauthorized).");
        }
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            throw new Exception("No tienes permisos para acceder a este recurso (403 Forbidden).");
        }
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        Console.WriteLine("SONDA API RAW RESPONSE: " + responseBody);
        if (string.IsNullOrWhiteSpace(responseBody) || !responseBody.TrimStart().StartsWith("{"))
        {
            throw new Exception("La respuesta de la API no es JSON válido. Respuesta: " + responseBody);
        }
    // La API devuelve un objeto con la lista en la propiedad "results":
    var apiResponse = JsonSerializer.Deserialize<OmniMonitor.Server.Models.AssetApiResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    return apiResponse?.Results ?? new List<AssetDto>();
    }

     public async Task<List<AssetDto>> GetAssetsBasicData(int? page, string? queryString, int? pageSize, int? bundleId, string username, string password)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlAM;
        string endpoint = _apiConfig.EndpointsAM["Asset"]["GetAssetsBasicData"];
        string token = await _sondaAuthService.GetUserTokenAMAsync(username, password);

        var queryParams = new List<string>();
        if (page.HasValue) queryParams.Add($"page={page.Value}");
        if (!string.IsNullOrEmpty(queryString)) queryParams.Add($"queryString={Uri.EscapeDataString(queryString)}");
        if (pageSize.HasValue) queryParams.Add($"pageSize={pageSize.Value}");
        if (bundleId.HasValue) queryParams.Add($"bundleId={bundleId.Value}");
        string query = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;

        string getDataUrl = baseUrl + endpoint + query;
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await client.GetAsync(getDataUrl);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            throw new Exception("No tienes permisos: token inválido o expirado (401 Unauthorized).");
        }
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            throw new Exception("No tienes permisos para acceder a este recurso (403 Forbidden).");
        }
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        Console.WriteLine("SONDA API RAW RESPONSE: " + responseBody);
        if (string.IsNullOrWhiteSpace(responseBody) || !responseBody.TrimStart().StartsWith("{"))
        {
            throw new Exception("La respuesta de la API no es JSON válido. Respuesta: " + responseBody);
        }
        var apiResponse = JsonSerializer.Deserialize<OmniMonitor.Server.Models.AssetApiResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return apiResponse?.Results ?? new List<AssetDto>();
    }

    
    public async Task<List<AssetDto>> GetLinkedAssets(int? page, string? queryString, string? sort, int? pageSize, string username, string password)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlAM;
        string endpoint = _apiConfig.EndpointsAM["Asset"]["GetLinkedAssets"];
        string token = await _sondaAuthService.GetUserTokenAMAsync(username, password);

        var queryParams = new List<string>();
        if (page.HasValue) queryParams.Add($"page={page.Value}");
        if (!string.IsNullOrEmpty(queryString)) queryParams.Add($"queryString={Uri.EscapeDataString(queryString)}");
        if (!string.IsNullOrEmpty(sort)) queryParams.Add($"sort={Uri.EscapeDataString(sort)}");
        if (pageSize.HasValue) queryParams.Add($"pageSize={pageSize.Value}");
        string query = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;

        string getDataUrl = baseUrl + endpoint + query;
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await client.GetAsync(getDataUrl);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            throw new Exception("No tienes permisos: token inválido o expirado (401 Unauthorized).");
        }
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            throw new Exception("No tienes permisos para acceder a este recurso (403 Forbidden).");
        }
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        Console.WriteLine("SONDA API RAW RESPONSE: " + responseBody);
        if (string.IsNullOrWhiteSpace(responseBody) || !responseBody.TrimStart().StartsWith("{"))
        {
            throw new Exception("La respuesta de la API no es JSON válido. Respuesta: " + responseBody);
        }
        var apiResponse = JsonSerializer.Deserialize<OmniMonitor.Server.Models.AssetApiResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return apiResponse?.Results ?? new List<AssetDto>();
    }

    
    public async Task<List<RelatedAssetDto>> GetAssetRelations(int assetId, int? page, int? pageSize, string username, string password)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlAM;
        string endpoint = _apiConfig.EndpointsAM["Relation"]["GetAssetRelations"];
        string token = await _sondaAuthService.GetUserTokenAMAsync(username, password);

        var queryParams = new List<string>();
        if (page.HasValue) queryParams.Add($"page={page.Value}");
        if (pageSize.HasValue) queryParams.Add($"pageSize={pageSize.Value}");
        string query = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;

        string getDataUrl = baseUrl + endpoint.Replace("{assetId}", assetId.ToString()) + query;
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await client.GetAsync(getDataUrl);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            throw new Exception("No tienes permisos: token inválido o expirado (401 Unauthorized).");
        }
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            throw new Exception("No tienes permisos para acceder a este recurso (403 Forbidden).");
        }
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        Console.WriteLine("SONDA API RAW RESPONSE: " + responseBody);
        if (string.IsNullOrWhiteSpace(responseBody))
            throw new Exception("La respuesta de la API está vacía.");

        var trimmed = responseBody.TrimStart();
        if (trimmed.StartsWith("{"))
        {
            // La API devuelve un objeto con la lista en la propiedad "results"
            using var doc = JsonDocument.Parse(responseBody);
            var results = doc.RootElement.GetProperty("results");
            var relatedAssets = new List<RelatedAssetDto>();
            foreach (var item in results.EnumerateArray())
            {
                relatedAssets.Add(new RelatedAssetDto
                {
                    AssetId = item.GetProperty("assetId").GetInt32(),
                    AssetName = item.GetProperty("assetName").GetString(),
                    Type = item.GetProperty("type").GetString()
                });
            }
            return relatedAssets;
        }
        else if (trimmed.StartsWith("["))
        {
            // Si la API devuelve una lista directa
            var listResponse = JsonSerializer.Deserialize<List<RelatedAssetDto>>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return listResponse ?? new List<RelatedAssetDto>();
        }
        else
        {
            throw new Exception("La respuesta de la API no es JSON válido. Respuesta: " + responseBody);
        }
    }

    
    public async Task<List<BundleDto>> GetBundles(int? page, string? queryString, string? sort, int? pageSize, string username, string password)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlAM;
        string endpoint = _apiConfig.EndpointsAM["Bundle"]["GetBundles"];
        string token = await _sondaAuthService.GetUserTokenAMAsync(username, password);

        var queryParams = new List<string>();
        if (page.HasValue) queryParams.Add($"page={page.Value}");
        if (!string.IsNullOrEmpty(queryString)) queryParams.Add($"queryString={Uri.EscapeDataString(queryString)}");
        if (!string.IsNullOrEmpty(sort)) queryParams.Add($"sort={Uri.EscapeDataString(sort)}");
        if (pageSize.HasValue) queryParams.Add($"pageSize={pageSize.Value}");
        string query = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;

        string getDataUrl = baseUrl + endpoint + query;
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await client.GetAsync(getDataUrl);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            throw new Exception("No tienes permisos: token inválido o expirado (401 Unauthorized).");
        }
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            throw new Exception("No tienes permisos para acceder a este recurso (403 Forbidden).");
        }
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        Console.WriteLine("SONDA API RAW RESPONSE: " + responseBody);
        if (string.IsNullOrWhiteSpace(responseBody) || !responseBody.TrimStart().StartsWith("{"))
        {
            throw new Exception("La respuesta de la API no es JSON válido. Respuesta: " + responseBody);
        }
        var apiResponse = JsonSerializer.Deserialize<OmniMonitor.Server.Models.BundleApiResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return apiResponse?.Results ?? new List<BundleDto>();
    }

    public async Task<List<AssetDto>> GetAssetHistory(int? page, string? queryString, string? sort, int? pageSize, string? bundlesId, string username, string password)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlAM;
        string endpoint = _apiConfig.EndpointsAM["Asset"]["History"];
        string token = await _sondaAuthService.GetUserTokenAMAsync(username, password);

        var queryParams = new List<string>();
        if (page.HasValue) queryParams.Add($"page={page.Value}");
        if (!string.IsNullOrEmpty(queryString)) queryParams.Add($"queryString={Uri.EscapeDataString(queryString)}");
        if (!string.IsNullOrEmpty(sort)) queryParams.Add($"sort={Uri.EscapeDataString(sort)}");
        if (pageSize.HasValue) queryParams.Add($"pageSize={pageSize.Value}");
        if (!string.IsNullOrEmpty(bundlesId)) queryParams.Add($"bundlesId={Uri.EscapeDataString(bundlesId)}");
        string query = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;

        string getDataUrl = baseUrl + endpoint + query;
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await client.GetAsync(getDataUrl);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            throw new Exception("No tienes permisos: token inválido o expirado (401 Unauthorized).");
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            throw new Exception("No tienes permisos para acceder a este recurso (403 Forbidden).");
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        Console.WriteLine("SONDA API RAW RESPONSE: " + responseBody);
        if (string.IsNullOrWhiteSpace(responseBody) || !responseBody.TrimStart().StartsWith("{"))
            throw new Exception("La respuesta de la API no es JSON válido. Respuesta: " + responseBody);
        var apiResponse = JsonSerializer.Deserialize<OmniMonitor.Server.Models.AssetApiResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return apiResponse?.Results ?? new List<AssetDto>();
    }

    public async Task<EventTaskInstanceDto> GetEventTaskInstanceById(int eventTaskInstanceId, string username, string password)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlAM;
        string endpoint = _apiConfig.EndpointsAM["EventTaskInstance"]["GetById"].Replace("{eventTaskInstanceId}", eventTaskInstanceId.ToString());
        string token = await _sondaAuthService.GetUserTokenAMAsync(username, password);

        string getDataUrl = baseUrl + endpoint;
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        var response = await client.GetAsync(getDataUrl);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            throw new Exception("No tienes permisos: token inválido o expirado (401 Unauthorized).");
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            throw new Exception("No tienes permisos para acceder a este recurso (403 Forbidden).");
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        Console.WriteLine("SONDA API RAW RESPONSE: " + responseBody);
        if (string.IsNullOrWhiteSpace(responseBody) || !responseBody.TrimStart().StartsWith("{"))
            throw new Exception("La respuesta de la API no es JSON válido. Respuesta: " + responseBody);
        return System.Text.Json.JsonSerializer.Deserialize<EventTaskInstanceDto>(responseBody, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    public async Task<List<EventTaskInstanceDto>> GetEventTaskInstances(string dates, int? page, string queryString, int? bundleId, string state, string sort, int? taskTypeId, int? groupId, int? pageSize, bool tasksAssignedToMe, bool tasksPendingApproval, string username, string password)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlAM;
        string endpoint = _apiConfig.EndpointsAM["EventTaskInstance"]["GetAll"];
        string token = await _sondaAuthService.GetUserTokenAMAsync(username, password);

        var queryParams = new List<string>();
        if (!string.IsNullOrEmpty(dates)) queryParams.Add($"dates={Uri.EscapeDataString(dates)}");
        if (page.HasValue) queryParams.Add($"page={page.Value}");
        if (!string.IsNullOrEmpty(queryString)) queryParams.Add($"queryString={Uri.EscapeDataString(queryString)}");
        if (bundleId.HasValue) queryParams.Add($"bundleId={bundleId.Value}");
        if (!string.IsNullOrEmpty(state)) queryParams.Add($"state={Uri.EscapeDataString(state)}");
        if (!string.IsNullOrEmpty(sort)) queryParams.Add($"sort={Uri.EscapeDataString(sort)}");
        if (taskTypeId.HasValue) queryParams.Add($"taskTypeId={taskTypeId.Value}");
        if (groupId.HasValue) queryParams.Add($"groupId={groupId.Value}");
        if (pageSize.HasValue) queryParams.Add($"pageSize={pageSize.Value}");
        queryParams.Add($"tasksAssignedToMe={tasksAssignedToMe.ToString().ToLower()}");
        queryParams.Add($"tasksPendingApproval={tasksPendingApproval.ToString().ToLower()}");
        string query = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;

        string getDataUrl = baseUrl + endpoint + query;
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await client.GetAsync(getDataUrl);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            throw new Exception("No tienes permisos: token inválido o expirado (401 Unauthorized).");
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            throw new Exception("No tienes permisos para acceder a este recurso (403 Forbidden).");
        //response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        Console.WriteLine("SONDA API RAW RESPONSE: " + responseBody);
        response.EnsureSuccessStatusCode();
        if (string.IsNullOrWhiteSpace(responseBody) || !responseBody.TrimStart().StartsWith("{"))
            throw new Exception("La respuesta de la API no es JSON válido. Respuesta: " + responseBody);
        var apiResponse = JsonSerializer.Deserialize<OmniMonitor.Server.Models.EventTaskInstanceApiResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return apiResponse?.Results ?? new List<EventTaskInstanceDto>();
    }

    public async Task<List<OmniMonitor.Shared.Dtos.AM.EventTaskActionDto>> GetEventTaskInstanceActions(int taskInstanceId, string username, string password)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlAM;
        // Asegúrate que la clave y endpoint existan en tu ApiConfig.json
        string endpoint = _apiConfig.EndpointsAM["EventTaskInstance"]["GetActions"].Replace("{taskInstanceId}", taskInstanceId.ToString());
        string token = await _sondaAuthService.GetUserTokenAMAsync(username, password);

        string getDataUrl = baseUrl + endpoint;
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        var response = await client.GetAsync(getDataUrl);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            throw new Exception("No tienes permisos: token inválido o expirado (401 Unauthorized).");
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            throw new Exception("No tienes permisos para acceder a este recurso (403 Forbidden).");
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        Console.WriteLine("SONDA API RAW RESPONSE: " + responseBody);
        if (string.IsNullOrWhiteSpace(responseBody))
            throw new Exception("La respuesta de la API está vacía.");

        // Asume que la respuesta es una lista directa de acciones
        var actions = System.Text.Json.JsonSerializer.Deserialize<List<OmniMonitor.Shared.Dtos.AM.EventTaskActionDto>>(responseBody, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return actions ?? new List<OmniMonitor.Shared.Dtos.AM.EventTaskActionDto>();
    }

        public async Task<List<EventTaskInstanceStockDto>> GetEventTaskInstanceStock(int taskInstanceId, string username, string password)
        {
            string baseUrl = _apiConfig.BaseUrl.UrlAM;
            string endpoint = _apiConfig.EndpointsAM["EventTaskInstance"]["GetStock"].Replace("{taskInstanceId}", taskInstanceId.ToString());
            string token = await _sondaAuthService.GetUserTokenAMAsync(username, password);

            string getDataUrl = baseUrl + endpoint;
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.GetAsync(getDataUrl);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                throw new Exception("No tienes permisos: token inválido o expirado (401 Unauthorized).");
            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                throw new Exception("No tienes permisos para acceder a este recurso (403 Forbidden).");
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine("SONDA API RAW RESPONSE: " + responseBody);
            if (string.IsNullOrWhiteSpace(responseBody))
                throw new Exception("La respuesta de la API está vacía.");

            var stocks = System.Text.Json.JsonSerializer.Deserialize<List<EventTaskInstanceStockDto>>(responseBody, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return stocks ?? new List<EventTaskInstanceStockDto>();
        }
}