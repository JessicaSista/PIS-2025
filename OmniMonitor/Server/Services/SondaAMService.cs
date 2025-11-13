using Microsoft.Extensions.Options;
using OmniMonitor.Server.Configuration;
using OmniMonitor.Shared.Dtos;
using System.Net.Http.Headers;
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
    Task<AssetDto?> GetAssetById(int id, string username);

    // GET all stock
    Task<List<StockDto>> GetAllStock(int? page, string? queryString, string? sort, int? pageSize, string? bundlesId, string username);

    // GET stock parameters by bundleId
    Task<BundleDto> GetStockParametersByBundleId(int bundleId, string username);

    // GET stock by id
    Task<StockDto?> GetStockById(int stockId, string username);

        // GET assets paginados y filtrados
    Task<List<AssetDto>> GetAssets(int? page, string? queryString, string? bundles, int? assetTypeId, string? sort, int? pageSize, string username);

     // GET assets basic data paginados y filtrados
    Task<List<AssetDto>> GetAssetsBasicData(int? page, string? queryString, int? pageSize, int? bundleId, string username);

      // GET linked assets paginados y filtrados
    Task<List<AssetDto>> GetLinkedAssets(int? page, string? queryString, string? sort, int? pageSize, string username);

        // GET asset relations paginados y filtrados
    Task<List<RelatedAssetDto>> GetAssetRelations(int assetId, int? page, int? pageSize, string username);

        // GET bundles paginados y filtrados
    Task<List<BundleDto>> GetBundles(int? page, string? queryString, string? sort, int? pageSize, string username);

        // GET asset history paginado y filtrado
    Task<List<AssetDto>> GetAssetHistory(int? page, string? queryString, string? sort, int? pageSize, string? bundlesId, string username);

        // GET event task instance by id
    Task<EventTaskInstanceDto?> GetEventTaskInstanceById(int eventTaskInstanceId, string username);

    // GET event tasks filtrados y paginados
    Task<List<EventTaskInstanceDto>> GetEventTaskInstances(string dates, int? page, string queryString, int? bundleId, string state, string sort, int? taskTypeId, int? groupId, int? pageSize, bool tasksAssignedToMe, bool tasksPendingApproval, string username);

        // GET actions for event task instance
    Task<List<OmniMonitor.Shared.Dtos.AM.EventTaskActionDto>> GetEventTaskInstanceActions(int taskInstanceId, string username);

    // GET stock for event task instance
    Task<List<EventTaskInstanceStockDto>> GetEventTaskInstanceStock(int taskInstanceId, string username);

    Task<List<int>> GetTypeDtoIdsFromEventTaskInstances(string username);
    Task<List<TaskTypeDto>> GetTaskTypeDtosFromEventTaskInstances(string username);

    // GET all asset types
    Task<List<AssetTypeDto>> GetAllAssetTypes(string username);
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

    public async Task<AssetDto?> GetAssetById(int id, string username)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlAM;
        string endpoint = _apiConfig.EndpointsAM["Asset"]["GetById"];
        if (id <= 0)
        {
            throw new ArgumentException("El ID debe ser positivo.", nameof(id));
        }

        string token = await _sondaAuthService.GetUserTokenAMAsync(username);
        // Console.WriteLine($"SONDA API TOKEN: {token}");
        string getDataUrl = baseUrl + endpoint + "?assetId=" + id;
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Respuesta de la API
        var response = await client.GetAsync(getDataUrl);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Traducir 404 de la API externa a null para que el Controller devuelva NotFound
            return null;
        }
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
        // Console.WriteLine("SONDA API RAW RESPONSE: " + responseBody);
        // Puedes revisar el log de consola para ver el JSON exacto que devuelve la API
        return JsonSerializer.Deserialize<AssetDto>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    public async Task<BundleDto> GetStockParametersByBundleId(int bundleId, string username)
    {
        if (bundleId <= 0)
            throw new ArgumentException("El parámetro 'bundleId' debe ser mayor que cero.", nameof(bundleId));
        string baseUrl = _apiConfig.BaseUrl.UrlAM;
        string endpoint = _apiConfig.EndpointsAM["Bundle"]["GetByBundleId"];
        string token = await _sondaAuthService.GetUserTokenAMAsync(username);

        string getDataUrl = $"{baseUrl}{endpoint}?bundleId={bundleId}";
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await client.GetAsync(getDataUrl);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                throw new Exception("No se encontraron stocksParameters  (404 NotFound).");
            
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

    public async Task<List<StockDto>> GetAllStock(int? page, string? queryString, string? sort, int? pageSize, string? bundlesId, string username)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlAM;
        string endpoint = _apiConfig.EndpointsAM["Stock"]["GetAll"];
        string token = await _sondaAuthService.GetUserTokenAMAsync(username);

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
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return new List<StockDto>();
        }
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            throw new Exception("No tienes permisos: token inválido o expirado (401 Unauthorized).");
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            throw new Exception("No tienes permisos para acceder a este recurso (403 Forbidden).");
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        Console.WriteLine("SONDA API RAW RESPONSE: " + responseBody);
        if (string.IsNullOrWhiteSpace(responseBody))
            return new List<StockDto>();

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

    public async Task<StockDto?> GetStockById(int stockId, string username)
    {
        if (stockId <= 0)
            throw new ArgumentException("El parámetro 'stockId' debe ser mayor que cero.", nameof(stockId));
        string baseUrl = _apiConfig.BaseUrl.UrlAM;
        string endpoint = _apiConfig.EndpointsAM["Stock"]["GetById"].Replace("{stockId}", stockId.ToString());
        string token = await _sondaAuthService.GetUserTokenAMAsync(username);

        string getDataUrl = baseUrl + endpoint;
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await client.GetAsync(getDataUrl);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            throw new Exception("No tienes permisos: token inválido o expirado (401 Unauthorized).");
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            throw new Exception("No tienes permisos para acceder a este recurso (403 Forbidden).");

        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        Console.WriteLine("SONDA API RAW RESPONSE: " + responseBody);

        if (string.IsNullOrWhiteSpace(responseBody) || !responseBody.TrimStart().StartsWith("{"))
            throw new Exception("La respuesta de la API es nula, vacía o no es un JSON válido.");
        try
        {
            return JsonSerializer.Deserialize<StockDto>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            throw new Exception("Error al deserializar la respuesta de la API: JSON inválido.", ex);
        }
    }

        public async Task<List<AssetDto>> GetAssets(int? page, string? queryString, string? bundles, int? assetTypeId, string? sort, int? pageSize, string username)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlAM;
        string endpoint = _apiConfig.EndpointsAM["Asset"]["GetAssets"];
        string token = await _sondaAuthService.GetUserTokenAMAsync(username);

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
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                throw new Exception("No se encontraron assets (404 NotFound).");
            
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
        {
            throw new Exception("La respuesta de la API está vacía.");
        }

        // Detecta si la respuesta es un objeto (con 'results') o una lista directa
        var trimmed = responseBody.TrimStart();
        if (trimmed.StartsWith("{"))
        {
            // La API devuelve un objeto con la lista en la propiedad "results"
            var apiResponse = JsonSerializer.Deserialize<OmniMonitor.Server.Models.AssetApiResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return apiResponse?.Results ?? new List<AssetDto>();
        }
        else if (trimmed.StartsWith("["))
        {
            // Si la API devuelve una lista directa
            var listResponse = JsonSerializer.Deserialize<List<AssetDto>>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return listResponse ?? new List<AssetDto>();
        }
        else
        {
            throw new Exception("La respuesta de la API no es JSON válido. Respuesta: " + responseBody);
        }
    }

     public async Task<List<AssetDto>> GetAssetsBasicData(int? page, string? queryString, int? pageSize, int? bundleId, string username)
    {
        // Validaciones de parámetros requeridos
        if (!page.HasValue)
        {
            throw new ArgumentException("El parámetro 'page' es requerido.", nameof(page));
        }
        if (!pageSize.HasValue)
        {
            throw new ArgumentException("El parámetro 'pageSize' es requerido.", nameof(pageSize));
        }
        if (!bundleId.HasValue)
        {
            throw new ArgumentException("El parámetro 'bundleId' es requerido.", nameof(bundleId));
        }

        string baseUrl = _apiConfig.BaseUrl.UrlAM;
        string endpoint = _apiConfig.EndpointsAM["Asset"]["GetAssetsBasicData"];
        string token = await _sondaAuthService.GetUserTokenAMAsync(username);

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
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                throw new Exception("No se encontro informacion (404 NotFound).");
            
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

    
    public async Task<List<AssetDto>> GetLinkedAssets(int? page, string? queryString, string? sort, int? pageSize, string username)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlAM;
        string endpoint = _apiConfig.EndpointsAM["Asset"]["GetLinkedAssets"];
        string token = await _sondaAuthService.GetUserTokenAMAsync(username);

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
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                throw new Exception("No se encontraron linked assets (404 NotFound).");
            
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

    
    public async Task<List<RelatedAssetDto>> GetAssetRelations(int assetId, int? page, int? pageSize, string username)
    {
        if (assetId <= 0)
        {
            throw new ArgumentException("El assetId debe ser mayor que cero.", nameof(assetId));
        }
        string baseUrl = _apiConfig.BaseUrl.UrlAM;
        string endpoint = _apiConfig.EndpointsAM["Relation"]["GetAssetRelations"];
        string token = await _sondaAuthService.GetUserTokenAMAsync(username);

        var queryParams = new List<string>();
        if (page.HasValue) queryParams.Add($"page={page.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
        if (pageSize.HasValue) queryParams.Add($"pageSize={pageSize.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
        string query = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;

        string getDataUrl = baseUrl + endpoint.Replace("{assetId}", assetId.ToString()) + query;
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await client.GetAsync(getDataUrl);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new Exception("AssetNotFound");
        }
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
        {
            // No hay relaciones, pero el asset existe
            return new List<RelatedAssetDto>();
        }

        var trimmedBody = responseBody.Trim();
        if (string.Equals(trimmedBody, "AssetNotFound", StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception("AssetNotFound");
        }

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

    
    public async Task<List<BundleDto>> GetBundles(int? page, string? queryString, string? sort, int? pageSize, string username)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlAM;
        string endpoint = _apiConfig.EndpointsAM["Bundle"]["GetBundles"];
        string token = await _sondaAuthService.GetUserTokenAMAsync(username);

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
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                throw new Exception("No se encontraron bundles (404 NotFound).");
            
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
        {
            throw new Exception("La respuesta de la API está vacía.");
        }

        // Detecta si la respuesta es un objeto (con 'results') o una lista directa
        var trimmed = responseBody.TrimStart();
        if (trimmed.StartsWith("{"))
        {
            // La API devuelve un objeto con la lista en la propiedad "results"
            var apiResponse = JsonSerializer.Deserialize<OmniMonitor.Server.Models.BundleApiResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return apiResponse?.Results ?? new List<BundleDto>();
        }
        else if (trimmed.StartsWith("["))
        {
            // Si la API devuelve una lista directa
            var listResponse = JsonSerializer.Deserialize<List<BundleDto>>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return listResponse ?? new List<BundleDto>();
        }
        else
        {
            throw new Exception("La respuesta de la API no es JSON válido. Respuesta: " + responseBody);
        }
    }

    public async Task<List<AssetDto>> GetAssetHistory(int? page, string? queryString, string? sort, int? pageSize, string? bundlesId, string username)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlAM;
        string endpoint = _apiConfig.EndpointsAM["Asset"]["History"];
        string token = await _sondaAuthService.GetUserTokenAMAsync(username);

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
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                throw new Exception("No se encontro historia para el asset (404 NotFound).");
            
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

    public async Task<EventTaskInstanceDto?> GetEventTaskInstanceById(int eventTaskInstanceId, string username)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlAM;
        string endpoint = _apiConfig.EndpointsAM["EventTaskInstance"]["GetById"].Replace("{eventTaskInstanceId}", eventTaskInstanceId.ToString());
        string token = await _sondaAuthService.GetUserTokenAMAsync(username);

        string getDataUrl = baseUrl + endpoint;
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        var response = await client.GetAsync(getDataUrl);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            throw new Exception("No tienes permisos: token inválido o expirado (401 Unauthorized).");
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            throw new Exception("No tienes permisos para acceder a este recurso (403 Forbidden).");
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        Console.WriteLine("SONDA API RAW RESPONSE: " + responseBody);
        if (string.IsNullOrWhiteSpace(responseBody) || !responseBody.TrimStart().StartsWith("{"))
            return null;
        return System.Text.Json.JsonSerializer.Deserialize<EventTaskInstanceDto>(responseBody, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    public async Task<List<EventTaskInstanceDto>> GetEventTaskInstances(string dates, int? page, string queryString, int? bundleId, string state, string sort, int? taskTypeId, int? groupId, int? pageSize, bool tasksAssignedToMe, bool tasksPendingApproval, string username)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlAM;
        string endpoint = _apiConfig.EndpointsAM["EventTaskInstance"]["GetAll"];
        string token = await _sondaAuthService.GetUserTokenAMAsync(username);

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
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            throw new Exception("No se encontraron event task instances (404 NotFound).");
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            throw new Exception("No tienes permisos: token inválido o expirado (401 Unauthorized).");
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            throw new Exception("No tienes permisos para acceder a este recurso (403 Forbidden).");

        var responseBody = await response.Content.ReadAsStringAsync();
        Console.WriteLine("SONDA API RAW RESPONSE: " + responseBody);
        response.EnsureSuccessStatusCode();
        if (string.IsNullOrWhiteSpace(responseBody))
            throw new Exception("La respuesta de la API está vacía.");

        // Detecta si la respuesta es un array o un objeto
        var trimmed = responseBody.TrimStart();
        if (trimmed.StartsWith("["))
        {
            // Es un array directo de instancias
            var list = JsonSerializer.Deserialize<List<EventTaskInstanceDto>>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return list ?? new List<EventTaskInstanceDto>();
        }
        else if (trimmed.StartsWith("{"))
        {
            // Es un objeto envolvente
            var apiResponse = JsonSerializer.Deserialize<OmniMonitor.Server.Models.EventTaskInstanceApiResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return apiResponse?.Results ?? new List<EventTaskInstanceDto>();
        }
        else
        {
            throw new Exception("La respuesta de la API no es JSON válido. Respuesta: " + responseBody);
        }
    }

    public async Task<List<OmniMonitor.Shared.Dtos.AM.EventTaskActionDto>> GetEventTaskInstanceActions(int taskInstanceId, string username)
    {
        if (taskInstanceId <= 0)
            throw new ArgumentException("El parámetro 'taskInstanceId' debe ser mayor que cero.", nameof(taskInstanceId));
        string baseUrl = _apiConfig.BaseUrl.UrlAM;
        // Asegúrate que la clave y endpoint existan en tu ApiConfig.json
        string endpoint = _apiConfig.EndpointsAM["EventTaskInstance"]["GetActions"].Replace("{taskInstanceId}", taskInstanceId.ToString());
        string token = await _sondaAuthService.GetUserTokenAMAsync(username);

        string getDataUrl = baseUrl + endpoint;
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        var response = await client.GetAsync(getDataUrl);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                throw new Exception("No se encontraron EventTaskActions (404 NotFound).");
            
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

        public async Task<List<EventTaskInstanceStockDto>> GetEventTaskInstanceStock(int taskInstanceId, string username)
        {
            if (taskInstanceId <= 0)
                throw new ArgumentException("El parámetro 'taskInstanceId' debe ser mayor que cero.", nameof(taskInstanceId));
            string baseUrl = _apiConfig.BaseUrl.UrlAM;
            string endpoint = _apiConfig.EndpointsAM["EventTaskInstance"]["GetStock"].Replace("{taskInstanceId}", taskInstanceId.ToString());
            string token = await _sondaAuthService.GetUserTokenAMAsync(username);

            string getDataUrl = baseUrl + endpoint;
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.GetAsync(getDataUrl);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                throw new Exception("No se encontraron stocks para el taskInstanceId proporcionado (404 NotFound).");
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

        // Devuelve una lista de IDs de los typeDto de cada EventTaskInstanceDto de la lista
public async Task<List<int>> GetTypeDtoIdsFromEventTaskInstances( string username)
{
    var eventTaskInstances = await GetEventTaskInstances(
        "1980-01-01T03:00:00,2050-10-31T03:00:00", // dates
        null,                                      // page (int?)
        "",                                        // queryString
        null,                                      // bundleId
        "",                                        // state
        "",                                        // sort
        null,                                      // taskTypeId
        null,                                      // groupId
        null,                                      // pageSize
        false,                                     // tasksAssignedToMe
        false,                                     // tasksPendingApproval
        username
    );

    var ids = new List<int>();
    if (eventTaskInstances == null)
        return ids;
    foreach (var instance in eventTaskInstances)
    {
        var typeDtoId = instance?.EventTaskDto?.TypeDto?.Id;
        if (typeDtoId != null)
            ids.Add(typeDtoId.Value);
    }
    return ids.Distinct().ToList();
}

public async Task<List<TaskTypeDto>> GetTaskTypeDtosFromEventTaskInstances(string username)
{
    var eventTaskInstances = await GetEventTaskInstances(
        "1980-01-01T03:00:00,2050-10-31T03:00:00", // dates
        null,                                      // page (int?)
        "",                                        // queryString
        null,                                      // bundleId
        "",                                        // state
        "",                                        // sort
        null,                                      // taskTypeId
        null,                                      // groupId
        null,                                      // pageSize
        false,                                     // tasksAssignedToMe
        false,                                     // tasksPendingApproval
        username
    );

    var typeDtos = new List<TaskTypeDto>();
    if (eventTaskInstances == null)
        return typeDtos;
    foreach (var instance in eventTaskInstances)
    {
        var typeDto = instance?.EventTaskDto?.TypeDto;
        if (typeDto != null)
            typeDtos.Add(typeDto);
    }
    // Devuelve solo los typeDto únicos por Id
    return typeDtos.GroupBy(t => t.Id).Select(g => g.First()).ToList();
}

public async Task<List<AssetTypeDto>> GetAllAssetTypes(string username)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlAM;
        string endpoint = _apiConfig.EndpointsAM["AssetType"]["GetAll"];
        string token = await _sondaAuthService.GetUserTokenAMAsync(username);

        string getDataUrl = baseUrl + endpoint;
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await client.GetAsync(getDataUrl);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return new List<AssetTypeDto>();
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            throw new Exception("No tienes permisos: token inválido o expirado (401 Unauthorized).");
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            throw new Exception("No tienes permisos para acceder a este recurso (403 Forbidden).");
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        Console.WriteLine("SONDA API RAW RESPONSE: " + responseBody);
        if (string.IsNullOrWhiteSpace(responseBody))
            return new List<AssetTypeDto>();

        var trimmed = responseBody.TrimStart();
        if (trimmed.StartsWith("{"))
        {
            var apiResponse = JsonSerializer.Deserialize<OmniMonitor.Server.Models.AssetTypeApiResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return apiResponse?.Results ?? new List<AssetTypeDto>();
        }
        else if (trimmed.StartsWith("["))
        {
            var listResponse = JsonSerializer.Deserialize<List<AssetTypeDto>>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return listResponse ?? new List<AssetTypeDto>();
        }
        else
        {
            throw new Exception("La respuesta de la API no es JSON válido. Respuesta: " + responseBody);
        }
    }
}