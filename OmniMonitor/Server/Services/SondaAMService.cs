using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using OmniMonitor.Server.Configuration;
using OmniMonitor.Shared.Dtos;
using System.Net.Http.Headers;
using OmniMonitor.Shared.Dtos.AM;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Servicio para la gestión de activos y operaciones AM.
/// </summary>
public interface ISondaAMService
{
    #region Métodos públicos

    /// <summary>
    /// Obtiene un asset por su ID.
    /// </summary>
    /// <param name="id">ID del asset.</param>
    /// <param name="username">Nombre de usuario.</param>
    /// <returns>El asset encontrado o null si no existe.</returns>
    Task<AssetDto?> GetAssetById(int id, string username);

    /// <summary>
    /// Obtiene todos los stocks con filtros opcionales.
    /// </summary>
    /// <param name="page">Página.</param>
    /// <param name="queryString">Filtro de búsqueda.</param>
    /// <param name="sort">Orden.</param>
    /// <param name="pageSize">Tamaño de página.</param>
    /// <param name="bundlesId">IDs de bundles.</param>
    /// <param name="username">Nombre de usuario.</param>
    /// <returns>Lista de stocks.</returns>
    Task<List<StockDto>> GetAllStock(int? page, string? queryString, string? sort, int? pageSize, string? bundlesId, string username);

    /// <summary>
    /// Obtiene los parámetros de stock por bundleId.
    /// </summary>
    /// <param name="bundleId">ID del bundle.</param>
    /// <param name="username">Nombre de usuario.</param>
    /// <returns>BundleDto con los parámetros.</returns>
    Task<BundleDto> GetStockParametersByBundleId(int bundleId, string username);

    /// <summary>
    /// Obtiene un stock por su ID.
    /// </summary>
    /// <param name="stockId">ID del stock.</param>
    /// <param name="username">Nombre de usuario.</param>
    /// <returns>Stock encontrado o null si no existe.</returns>
    Task<StockDto?> GetStockById(int stockId, string username);

    /// <summary>
    /// Obtiene assets con filtros y paginación.
    /// </summary>
    /// <param name="page">Página.</param>
    /// <param name="queryString">Filtro de búsqueda.</param>
    /// <param name="bundles">Bundles.</param>
    /// <param name="assetTypeId">Tipo de asset.</param>
    /// <param name="sort">Orden.</param>
    /// <param name="pageSize">Tamaño de página.</param>
    /// <param name="username">Nombre de usuario.</param>
    /// <returns>Lista de assets.</returns>
    Task<List<AssetDto>> GetAssets(int? page, string? queryString, string? bundles, int? assetTypeId, string? sort, int? pageSize, string username);

    /// <summary>
    /// Obtiene datos básicos de assets con filtros y paginación.
    /// </summary>
    /// <param name="page">Página.</param>
    /// <param name="queryString">Filtro de búsqueda.</param>
    /// <param name="pageSize">Tamaño de página.</param>
    /// <param name="bundleId">ID del bundle.</param>
    /// <param name="username">Nombre de usuario.</param>
    /// <returns>Lista de assets.</returns>
    Task<List<AssetDto>> GetAssetsBasicData(int? page, string? queryString, int? pageSize, int? bundleId, string username);

    /// <summary>
    /// Obtiene assets vinculados con filtros y paginación.
    /// </summary>
    /// <param name="page">Página.</param>
    /// <param name="queryString">Filtro de búsqueda.</param>
    /// <param name="sort">Orden.</param>
    /// <param name="pageSize">Tamaño de página.</param>
    /// <param name="username">Nombre de usuario.</param>
    /// <returns>Lista de assets vinculados.</returns>
    Task<List<AssetDto>> GetLinkedAssets(int? page, string? queryString, string? sort, int? pageSize, string username);

    /// <summary>
    /// Obtiene relaciones de un asset.
    /// </summary>
    /// <param name="assetId">ID del asset.</param>
    /// <param name="page">Página.</param>
    /// <param name="pageSize">Tamaño de página.</param>
    /// <param name="username">Nombre de usuario.</param>
    /// <returns>Lista de relaciones.</returns>
    Task<List<RelatedAssetDto>> GetAssetRelations(int assetId, int? page, int? pageSize, string username);

    /// <summary>
    /// Obtiene bundles con filtros y paginación.
    /// </summary>
    /// <param name="page">Página.</param>
    /// <param name="queryString">Filtro de búsqueda.</param>
    /// <param name="sort">Orden.</param>
    /// <param name="pageSize">Tamaño de página.</param>
    /// <param name="username">Nombre de usuario.</param>
    /// <returns>Lista de bundles.</returns>
    Task<List<BundleDto>> GetBundles(int? page, string? queryString, string? sort, int? pageSize, string username);

    /// <summary>
    /// Obtiene el historial de un asset.
    /// </summary>
    /// <param name="page">Página.</param>
    /// <param name="queryString">Filtro de búsqueda.</param>
    /// <param name="sort">Orden.</param>
    /// <param name="pageSize">Tamaño de página.</param>
    /// <param name="bundlesId">IDs de bundles.</param>
    /// <param name="username">Nombre de usuario.</param>
    /// <returns>Lista de assets históricos.</returns>
    Task<List<AssetDto>> GetAssetHistory(int? page, string? queryString, string? sort, int? pageSize, string? bundlesId, string username);

    /// <summary>
    /// Obtiene una instancia de tarea de evento por ID.
    /// </summary>
    /// <param name="eventTaskInstanceId">ID de la instancia.</param>
    /// <param name="username">Nombre de usuario.</param>
    /// <returns>Instancia encontrada o null si no existe.</returns>
    Task<EventTaskInstanceDto?> GetEventTaskInstanceById(int eventTaskInstanceId, string username);

    /// <summary>
    /// Obtiene instancias de tareas de evento con filtros.
    /// </summary>
    /// <param name="dates">Fechas.</param>
    /// <param name="page">Página.</param>
    /// <param name="queryString">Filtro de búsqueda.</param>
    /// <param name="bundleId">ID del bundle.</param>
    /// <param name="state">Estado.</param>
    /// <param name="sort">Orden.</param>
    /// <param name="taskTypeId">Tipo de tarea.</param>
    /// <param name="groupId">Grupo.</param>
    /// <param name="pageSize">Tamaño de página.</param>
    /// <param name="tasksAssignedToMe">Solo asignadas a mí.</param>
    /// <param name="tasksPendingApproval">Solo pendientes de aprobación.</param>
    /// <param name="username">Nombre de usuario.</param>
    /// <returns>Lista de instancias.</returns>
    Task<List<EventTaskInstanceDto>> GetEventTaskInstances(string dates, int? page, string queryString, int? bundleId, string state, string sort, int? taskTypeId, int? groupId, int? pageSize, bool tasksAssignedToMe, bool tasksPendingApproval, string username);

    /// <summary>
    /// Obtiene acciones para una instancia de tarea de evento.
    /// </summary>
    /// <param name="taskInstanceId">ID de la instancia.</param>
    /// <param name="username">Nombre de usuario.</param>
    /// <returns>Lista de acciones.</returns>
    Task<List<EventTaskActionDto>> GetEventTaskInstanceActions(int taskInstanceId, string username);

    /// <summary>
    /// Obtiene stocks para una instancia de tarea de evento.
    /// </summary>
    /// <param name="taskInstanceId">ID de la instancia.</param>
    /// <param name="username">Nombre de usuario.</param>
    /// <returns>Lista de stocks.</returns>
    Task<List<EventTaskInstanceStockDto>> GetEventTaskInstanceStock(int taskInstanceId, string username);

    /// <summary>
    /// Obtiene los IDs de tipo de tarea de las instancias de evento.
    /// </summary>
    /// <param name="username">Nombre de usuario.</param>
    /// <returns>Lista de IDs de tipo de tarea.</returns>
    Task<List<int>> GetTypeDtoIdsFromEventTaskInstances(string username);

    /// <summary>
    /// Obtiene los tipos de tarea únicos de las instancias de evento.
    /// </summary>
    /// <param name="username">Nombre de usuario.</param>
    /// <returns>Lista de tipos de tarea.</returns>
    Task<List<TaskTypeDto>> GetTaskTypeDtosFromEventTaskInstances(string username);

    /// <summary>
    /// Obtiene todos los tipos de asset.
    /// </summary>
    /// <param name="username">Nombre de usuario.</param>
    /// <returns>Lista de tipos de asset.</returns>
    Task<List<AssetTypeDto>> GetAllAssetTypes(string username);

    #endregion
}

public class SondaAMService : ISondaAMService
{
    #region Campos privados

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISondaAuthService _sondaAuthService;
    private readonly ApiConfig _apiConfig;
    private readonly ILogger<SondaAMService> _logger;

    #endregion

    #region Constructor

    /// <summary>
    /// Constructor de SondaAMService.
    /// </summary>
    /// <param name="httpClientFactory">Fábrica de HttpClient.</param>
    /// <param name="sondaAuthService">Servicio de autenticación.</param>
    /// <param name="apiConfigOptions">Configuración de la API.</param>
    /// <param name="logger">Logger para registrar eventos.</param>
    public SondaAMService(
        IHttpClientFactory httpClientFactory,
        ISondaAuthService sondaAuthService,
        IOptions<ApiConfig> apiConfigOptions,
        ILogger<SondaAMService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _sondaAuthService = sondaAuthService;
        _apiConfig = apiConfigOptions.Value;
        _logger = logger;
    }

    #endregion

    #region Métodos públicos

    /// <inheritdoc/>
    public async Task<AssetDto?> GetAssetById(int id, string username)
    {
        try
        {
            _logger.LogInformation("Obteniendo asset por ID {Id} para usuario {Username}", id, username);

            string baseUrl = _apiConfig.BaseUrl.UrlAM;
            string endpoint = _apiConfig.EndpointsAM["Asset"]["GetById"];
            if (id <= 0)
            {
                _logger.LogWarning("ID inválido: {Id}", id);
                throw new ArgumentException("El ID debe ser positivo.", nameof(id));
            }

            string token = await _sondaAuthService.GetUserTokenAmAsync(username);
            string getDataUrl = baseUrl + endpoint + "?assetId=" + id;
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync(getDataUrl);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Asset no encontrado para ID {Id}", id);
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
                _logger.LogWarning("Respuesta vacía o no válida para asset ID {Id}", id);
                return null;
            }
            return JsonSerializer.Deserialize<AssetDto>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo asset por ID {Id} para usuario {Username}", id, username);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<BundleDto> GetStockParametersByBundleId(int bundleId, string username)
    {
        if (bundleId <= 0)
        {
            _logger.LogWarning("El parámetro 'bundleId' debe ser mayor que cero. Valor recibido: {BundleId}", bundleId);
            throw new ArgumentException("El parámetro 'bundleId' debe ser mayor que cero.", nameof(bundleId));
        }
        string baseUrl = _apiConfig.BaseUrl.UrlAM;
        string endpoint = _apiConfig.EndpointsAM["Bundle"]["GetByBundleId"];
        string token = await _sondaAuthService.GetUserTokenAmAsync(username);

        string getDataUrl = $"{baseUrl}{endpoint}?bundleId={bundleId}";
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await client.GetAsync(getDataUrl);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("No se encontraron stocksParameters para bundleId {BundleId}", bundleId);
            throw new Exception("No se encontraron stocksParameters  (404 NotFound).");
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
            _logger.LogError("La respuesta de la API no es JSON válido. Respuesta: {ResponseBody}", responseBody);
            throw new Exception("La respuesta de la API no es JSON válido. Respuesta: " + responseBody);
        }
        return JsonSerializer.Deserialize<BundleDto>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    /// <inheritdoc/>
    public async Task<List<StockDto>> GetAllStock(int? page, string? queryString, string? sort, int? pageSize, string? bundlesId, string username)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlAM;
        string endpoint = _apiConfig.EndpointsAM["Stock"]["GetAll"];
        string token = await _sondaAuthService.GetUserTokenAmAsync(username);

        var queryParams = new List<string>();
        if (page.HasValue) { queryParams.Add($"page={page.Value}"); }
        if (!string.IsNullOrEmpty(queryString)) { queryParams.Add($"queryString={Uri.EscapeDataString(queryString)}"); }
        if (!string.IsNullOrEmpty(sort)) { queryParams.Add($"sort={Uri.EscapeDataString(sort)}"); }
        if (pageSize.HasValue) { queryParams.Add($"pageSize={pageSize.Value}"); }
        if (!string.IsNullOrEmpty(bundlesId)) { queryParams.Add($"bundlesId={Uri.EscapeDataString(bundlesId)}"); }
        string query = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;

        string getDataUrl = baseUrl + endpoint + query;
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await client.GetAsync(getDataUrl);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("No se encontraron stocks para los filtros proporcionados.");
            return new();
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
            _logger.LogWarning("La respuesta de la API está vacía.");
            return new();
        }

        var trimmed = responseBody.TrimStart();
        if (trimmed.StartsWith("{"))
        {
            var apiResponse = JsonSerializer.Deserialize<OmniMonitor.Server.Models.StockApiResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return apiResponse?.Results ?? new();
        }
        else if (trimmed.StartsWith("["))
        {
            var listResponse = JsonSerializer.Deserialize<List<StockDto>>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return listResponse ?? new();
        }
        else
        {
            _logger.LogError("La respuesta de la API no es JSON válido. Respuesta: {ResponseBody}", responseBody);
            throw new Exception("La respuesta de la API no es JSON válido. Respuesta: " + responseBody);
        }
    }

    /// <inheritdoc/>
    public async Task<StockDto?> GetStockById(int stockId, string username)
    {
        if (stockId <= 0)
        {
            _logger.LogWarning("El parámetro 'stockId' debe ser mayor que cero. Valor recibido: {StockId}", stockId);
            throw new ArgumentException("El parámetro 'stockId' debe ser mayor que cero.", nameof(stockId));
        }
        string baseUrl = _apiConfig.BaseUrl.UrlAM;
        string endpoint = _apiConfig.EndpointsAM["Stock"]["GetById"].Replace("{stockId}", stockId.ToString());
        string token = await _sondaAuthService.GetUserTokenAmAsync(username);

        string getDataUrl = baseUrl + endpoint;
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await client.GetAsync(getDataUrl);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("No se encontró stock con ID {StockId}", stockId);
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
            _logger.LogError("La respuesta de la API es nula, vacía o no es un JSON válido. Respuesta: {ResponseBody}", responseBody);
            throw new Exception("La respuesta de la API es nula, vacía o no es un JSON válido.");
        }
        try
        {
            return JsonSerializer.Deserialize<StockDto>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error al deserializar la respuesta de la API para stockId {StockId}", stockId);
            throw new Exception("Error al deserializar la respuesta de la API: JSON inválido.", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<List<AssetDto>> GetAssets(int? page, string? queryString, string? bundles, int? assetTypeId, string? sort, int? pageSize, string username)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlAM;
        string endpoint = _apiConfig.EndpointsAM["Asset"]["GetAssets"];
        string token = await _sondaAuthService.GetUserTokenAmAsync(username);

        var queryParams = new List<string>();
        if (page.HasValue) { queryParams.Add($"page={page.Value}"); }
        if (!string.IsNullOrEmpty(queryString)) { queryParams.Add($"queryString={Uri.EscapeDataString(queryString)}"); }
        if (!string.IsNullOrEmpty(bundles)) { queryParams.Add($"bundles={Uri.EscapeDataString(bundles)}"); }
        if (assetTypeId.HasValue) { queryParams.Add($"assetTypeId={assetTypeId.Value}"); }
        if (!string.IsNullOrEmpty(sort)) { queryParams.Add($"sort={Uri.EscapeDataString(sort)}"); }
        if (pageSize.HasValue) { queryParams.Add($"pageSize={pageSize.Value}"); }
        string query = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;

        string getDataUrl = baseUrl + endpoint + query;
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await client.GetAsync(getDataUrl);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("No se encontraron assets para los filtros proporcionados.");
            throw new Exception("No se encontraron assets (404 NotFound).");
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
            _logger.LogWarning("La respuesta de la API está vacía.");
            throw new Exception("La respuesta de la API está vacía.");
        }

        var trimmed = responseBody.TrimStart();
        if (trimmed.StartsWith("{"))
        {
            var apiResponse = JsonSerializer.Deserialize<OmniMonitor.Server.Models.AssetApiResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return apiResponse?.Results ?? new();
        }
        else if (trimmed.StartsWith("["))
        {
            var listResponse = JsonSerializer.Deserialize<List<AssetDto>>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return listResponse ?? new();
        }
        else
        {
            _logger.LogError("La respuesta de la API no es JSON válido. Respuesta: {ResponseBody}", responseBody);
            throw new Exception("La respuesta de la API no es JSON válido. Respuesta: " + responseBody);
        }
    }

    /// <inheritdoc/>
    public async Task<List<AssetDto>> GetAssetsBasicData(int? page, string? queryString, int? pageSize, int? bundleId, string username)
    {
        // Validaciones de parámetros requeridos
        if (!page.HasValue)
        {
            _logger.LogWarning("El parámetro 'page' es requerido.");
            throw new ArgumentException("El parámetro 'page' es requerido.", nameof(page));
        }
        if (!pageSize.HasValue)
        {
            _logger.LogWarning("El parámetro 'pageSize' es requerido.");
            throw new ArgumentException("El parámetro 'pageSize' es requerido.", nameof(pageSize));
        }
        if (!bundleId.HasValue)
        {
            _logger.LogWarning("El parámetro 'bundleId' es requerido.");
            throw new ArgumentException("El parámetro 'bundleId' es requerido.", nameof(bundleId));
        }

        string baseUrl = _apiConfig.BaseUrl.UrlAM;
        string endpoint = _apiConfig.EndpointsAM["Asset"]["GetAssetsBasicData"];
        string token = await _sondaAuthService.GetUserTokenAmAsync(username);

        var queryParams = new List<string>();
        if (page.HasValue) { queryParams.Add($"page={page.Value}"); }
        if (!string.IsNullOrEmpty(queryString)) { queryParams.Add($"queryString={Uri.EscapeDataString(queryString)}"); }
        if (pageSize.HasValue) { queryParams.Add($"pageSize={pageSize.Value}"); }
        if (bundleId.HasValue) { queryParams.Add($"bundleId={bundleId.Value}"); }
        string query = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;

        string getDataUrl = baseUrl + endpoint + query;
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await client.GetAsync(getDataUrl);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("No se encontró información básica de assets para los filtros proporcionados.");
            throw new Exception("No se encontro informacion (404 NotFound).");
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
            _logger.LogError("La respuesta de la API no es JSON válido. Respuesta: {ResponseBody}", responseBody);
            throw new Exception("La respuesta de la API no es JSON válido. Respuesta: " + responseBody);
        }
        var apiResponse = JsonSerializer.Deserialize<OmniMonitor.Server.Models.AssetApiResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return apiResponse?.Results ?? new List<AssetDto>();
    }

    /// <inheritdoc/>
    public async Task<List<AssetDto>> GetLinkedAssets(int? page, string? queryString, string? sort, int? pageSize, string username)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlAM;
        string endpoint = _apiConfig.EndpointsAM["Asset"]["GetLinkedAssets"];
        string token = await _sondaAuthService.GetUserTokenAmAsync(username);

        var queryParams = new List<string>();
        if (page.HasValue) { queryParams.Add($"page={page.Value}"); }
        if (!string.IsNullOrEmpty(queryString)) { queryParams.Add($"queryString={Uri.EscapeDataString(queryString)}"); }
        if (!string.IsNullOrEmpty(sort)) { queryParams.Add($"sort={Uri.EscapeDataString(sort)}"); }
        if (pageSize.HasValue) { queryParams.Add($"pageSize={pageSize.Value}"); }
        string query = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;

        string getDataUrl = baseUrl + endpoint + query;
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await client.GetAsync(getDataUrl);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("No se encontraron linked assets para los filtros proporcionados.");
            throw new Exception("No se encontraron linked assets (404 NotFound).");
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
            _logger.LogError("La respuesta de la API no es JSON válido. Respuesta: {ResponseBody}", responseBody);
            throw new Exception("La respuesta de la API no es JSON válido. Respuesta: " + responseBody);
        }
        var apiResponse = JsonSerializer.Deserialize<OmniMonitor.Server.Models.AssetApiResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return apiResponse?.Results ?? new List<AssetDto>();
    }

    /// <inheritdoc/>
    public async Task<List<RelatedAssetDto>> GetAssetRelations(int assetId, int? page, int? pageSize, string username)
    {
        if (assetId <= 0)
        {
            _logger.LogWarning("El parámetro 'assetId' debe ser mayor que cero. Valor recibido: {AssetId}", assetId);
            throw new ArgumentException("El assetId debe ser mayor que cero.", nameof(assetId));
        }
        string baseUrl = _apiConfig.BaseUrl.UrlAM;
        string endpoint = _apiConfig.EndpointsAM["Relation"]["GetAssetRelations"];
        string token = await _sondaAuthService.GetUserTokenAmAsync(username);

        var queryParams = new List<string>();
        if (page.HasValue) { queryParams.Add($"page={page.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}"); }
        if (pageSize.HasValue) { queryParams.Add($"pageSize={pageSize.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}"); }
        string query = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;

        string getDataUrl = baseUrl + endpoint.Replace("{assetId}", assetId.ToString()) + query;
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await client.GetAsync(getDataUrl);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("No se encontró asset con ID {AssetId}", assetId);
            throw new Exception("AssetNotFound");
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
            // No hay relaciones, pero el asset existe
            return new List<RelatedAssetDto>();
        }

        var trimmedBody = responseBody.Trim();
        if (string.Equals(trimmedBody, "AssetNotFound", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("AssetNotFound para assetId {AssetId}", assetId);
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
            _logger.LogError("La respuesta de la API no es JSON válido. Respuesta: {ResponseBody}", responseBody);
            throw new Exception("La respuesta de la API no es JSON válido. Respuesta: " + responseBody);
        }
    }

    /// <inheritdoc/>
    public async Task<List<BundleDto>> GetBundles(int? page, string? queryString, string? sort, int? pageSize, string username)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlAM;
        string endpoint = _apiConfig.EndpointsAM["Bundle"]["GetBundles"];
        string token = await _sondaAuthService.GetUserTokenAmAsync(username);

        var queryParams = new List<string>();
        if (page.HasValue) { queryParams.Add($"page={page.Value}"); }
        if (!string.IsNullOrEmpty(queryString)) { queryParams.Add($"queryString={Uri.EscapeDataString(queryString)}"); }
        if (!string.IsNullOrEmpty(sort)) { queryParams.Add($"sort={Uri.EscapeDataString(sort)}"); }
        if (pageSize.HasValue) { queryParams.Add($"pageSize={pageSize.Value}"); }
        string query = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;

        string getDataUrl = baseUrl + endpoint + query;
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await client.GetAsync(getDataUrl);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("No se encontraron bundles para los filtros proporcionados.");
            throw new Exception("No se encontraron bundles (404 NotFound).");
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
            _logger.LogWarning("La respuesta de la API está vacía.");
            throw new Exception("La respuesta de la API está vacía.");
        }

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
            _logger.LogError("La respuesta de la API no es JSON válido. Respuesta: {ResponseBody}", responseBody);
            throw new Exception("La respuesta de la API no es JSON válido. Respuesta: " + responseBody);
        }
    }

    /// <inheritdoc/>
    public async Task<List<AssetDto>> GetAssetHistory(int? page, string? queryString, string? sort, int? pageSize, string? bundlesId, string username)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlAM;
        string endpoint = _apiConfig.EndpointsAM["Asset"]["History"];
        string token = await _sondaAuthService.GetUserTokenAmAsync(username);

        var queryParams = new List<string>();
        if (page.HasValue) { queryParams.Add($"page={page.Value}"); }
        if (!string.IsNullOrEmpty(queryString)) { queryParams.Add($"queryString={Uri.EscapeDataString(queryString)}"); }
        if (!string.IsNullOrEmpty(sort)) { queryParams.Add($"sort={Uri.EscapeDataString(sort)}"); }
        if (pageSize.HasValue) { queryParams.Add($"pageSize={pageSize.Value}"); }
        if (!string.IsNullOrEmpty(bundlesId)) { queryParams.Add($"bundlesId={Uri.EscapeDataString(bundlesId)}"); }
        string query = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;

        string getDataUrl = baseUrl + endpoint + query;
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await client.GetAsync(getDataUrl);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("No se encontró historia para el asset con los filtros proporcionados.");
            throw new Exception("No se encontro historia para el asset (404 NotFound).");
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
            _logger.LogError("La respuesta de la API no es JSON válido. Respuesta: {ResponseBody}", responseBody);
            throw new Exception("La respuesta de la API no es JSON válido. Respuesta: " + responseBody);
        }
        var apiResponse = JsonSerializer.Deserialize<OmniMonitor.Server.Models.AssetApiResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return apiResponse?.Results ?? new List<AssetDto>();
    }

    /// <inheritdoc/>
    public async Task<EventTaskInstanceDto?> GetEventTaskInstanceById(int eventTaskInstanceId, string username)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlAM;
        string endpoint = _apiConfig.EndpointsAM["EventTaskInstance"]["GetById"].Replace("{eventTaskInstanceId}", eventTaskInstanceId.ToString());
        string token = await _sondaAuthService.GetUserTokenAmAsync(username);

        string getDataUrl = baseUrl + endpoint;
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        var response = await client.GetAsync(getDataUrl);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("No se encontró EventTaskInstance con ID {EventTaskInstanceId}", eventTaskInstanceId);
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
            _logger.LogWarning("Respuesta vacía o no válida para EventTaskInstance ID {EventTaskInstanceId}", eventTaskInstanceId);
            return null;
        }
        return System.Text.Json.JsonSerializer.Deserialize<EventTaskInstanceDto>(responseBody, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    /// <inheritdoc/>
    public async Task<List<EventTaskInstanceDto>> GetEventTaskInstances(string dates, int? page, string queryString, int? bundleId, string state, string sort, int? taskTypeId, int? groupId, int? pageSize, bool tasksAssignedToMe, bool tasksPendingApproval, string username)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlAM;
        string endpoint = _apiConfig.EndpointsAM["EventTaskInstance"]["GetAll"];
        string token = await _sondaAuthService.GetUserTokenAmAsync(username);

        var queryParams = new List<string>();
        if (!string.IsNullOrEmpty(dates)) { queryParams.Add($"dates={Uri.EscapeDataString(dates)}"); }
        if (page.HasValue) { queryParams.Add($"page={page.Value}"); }
        if (!string.IsNullOrEmpty(queryString)) { queryParams.Add($"queryString={Uri.EscapeDataString(queryString)}"); }
        if (bundleId.HasValue) { queryParams.Add($"bundleId={bundleId.Value}"); }
        if (!string.IsNullOrEmpty(state)) { queryParams.Add($"state={Uri.EscapeDataString(state)}"); }
        if (!string.IsNullOrEmpty(sort)) { queryParams.Add($"sort={Uri.EscapeDataString(sort)}"); }
        if (taskTypeId.HasValue) { queryParams.Add($"taskTypeId={taskTypeId.Value}"); }
        if (groupId.HasValue) { queryParams.Add($"groupId={groupId.Value}"); }
        if (pageSize.HasValue) { queryParams.Add($"pageSize={pageSize.Value}"); }
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
        {
            _logger.LogWarning("No se encontraron event task instances para los filtros proporcionados.");
            throw new Exception("No se encontraron event task instances (404 NotFound).");
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

        var responseBody = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            _logger.LogWarning("La respuesta de la API está vacía.");
            throw new Exception("La respuesta de la API está vacía.");
        }

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
            _logger.LogError("La respuesta de la API no es JSON válido. Respuesta: {ResponseBody}", responseBody);
            throw new Exception("La respuesta de la API no es JSON válido. Respuesta: " + responseBody);
        }
    }

    /// <inheritdoc/>
    public async Task<List<EventTaskActionDto>> GetEventTaskInstanceActions(int taskInstanceId, string username)
    {
        if (taskInstanceId <= 0)
        {
            _logger.LogWarning("El parámetro 'taskInstanceId' debe ser mayor que cero. Valor recibido: {TaskInstanceId}", taskInstanceId);
            throw new ArgumentException("El parámetro 'taskInstanceId' debe ser mayor que cero.", nameof(taskInstanceId));
        }
        string baseUrl = _apiConfig.BaseUrl.UrlAM;
        string endpoint = _apiConfig.EndpointsAM["EventTaskInstance"]["GetActions"].Replace("{taskInstanceId}", taskInstanceId.ToString());
        string token = await _sondaAuthService.GetUserTokenAmAsync(username);

        string getDataUrl = baseUrl + endpoint;
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        var response = await client.GetAsync(getDataUrl);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("No se encontraron EventTaskActions para el taskInstanceId {TaskInstanceId}", taskInstanceId);
            throw new Exception("No se encontraron EventTaskActions (404 NotFound).");
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
            _logger.LogWarning("La respuesta de la API está vacía.");
            throw new Exception("La respuesta de la API está vacía.");
        }

        var actions = System.Text.Json.JsonSerializer.Deserialize<List<OmniMonitor.Shared.Dtos.AM.EventTaskActionDto>>(responseBody, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return actions ?? new List<OmniMonitor.Shared.Dtos.AM.EventTaskActionDto>();
    }

    /// <inheritdoc/>
    public async Task<List<EventTaskInstanceStockDto>> GetEventTaskInstanceStock(int taskInstanceId, string username)
    {
        if (taskInstanceId <= 0)
        {
            _logger.LogWarning("El parámetro 'taskInstanceId' debe ser mayor que cero. Valor recibido: {TaskInstanceId}", taskInstanceId);
            throw new ArgumentException("El parámetro 'taskInstanceId' debe ser mayor que cero.", nameof(taskInstanceId));
        }
        string baseUrl = _apiConfig.BaseUrl.UrlAM;
        string endpoint = _apiConfig.EndpointsAM["EventTaskInstance"]["GetStock"].Replace("{taskInstanceId}", taskInstanceId.ToString());
        string token = await _sondaAuthService.GetUserTokenAmAsync(username);

        string getDataUrl = baseUrl + endpoint;
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        var response = await client.GetAsync(getDataUrl);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("No se encontraron stocks para el taskInstanceId {TaskInstanceId}", taskInstanceId);
            throw new Exception("No se encontraron stocks para el taskInstanceId proporcionado (404 NotFound).");
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
            _logger.LogWarning("La respuesta de la API está vacía.");
            throw new Exception("La respuesta de la API está vacía.");
        }

        var stocks = System.Text.Json.JsonSerializer.Deserialize<List<EventTaskInstanceStockDto>>(responseBody, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return stocks ?? new List<EventTaskInstanceStockDto>();
    }

    // Devuelve una lista de IDs de los typeDto de cada EventTaskInstanceDto de la lista
    public async Task<List<int>> GetTypeDtoIdsFromEventTaskInstances(string username)
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
        {
            _logger.LogWarning("No se encontraron instancias de tareas de evento para el usuario {Username}", username);
            return ids;
        }
        foreach (var instance in eventTaskInstances)
        {
            var typeDtoId = instance?.EventTaskDto?.TypeDto?.Id;
            if (typeDtoId != null)
                ids.Add(typeDtoId.Value);
        }
        return ids.Distinct().ToList();
    }

    /// <inheritdoc/>
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
        {
            _logger.LogWarning("No se encontraron instancias de tareas de evento para el usuario {Username}", username);
            return typeDtos;
        }
        foreach (var instance in eventTaskInstances)
        {
            var typeDto = instance?.EventTaskDto?.TypeDto;
            if (typeDto != null)
            {
                typeDtos.Add(typeDto);
            }
        }
        // Devuelve solo los typeDto únicos por Id
        var result = typeDtos
            .Where(t => t != null)
            .GroupBy(t => t.Id)
            .Select(g => g.First())
            .ToList();

        if (result.Count == 0)
        {
            _logger.LogInformation("No se encontraron TaskTypeDto únicos para el usuario {Username}", username);
        }
        return result;
    }

    /// <inheritdoc/>
    public async Task<List<AssetTypeDto>> GetAllAssetTypes(string username)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlAM;
        string endpoint = _apiConfig.EndpointsAM["AssetType"]["GetAll"];
        string token = await _sondaAuthService.GetUserTokenAmAsync(username);

        string getDataUrl = baseUrl + endpoint;
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await client.GetAsync(getDataUrl);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("No se encontraron tipos de asset para el usuario {Username}", username);
            return new List<AssetTypeDto>();
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
            _logger.LogWarning("La respuesta de la API está vacía para tipos de asset.");
            return new List<AssetTypeDto>();
        }

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
            _logger.LogError("La respuesta de la API no es JSON válido. Respuesta: {ResponseBody}", responseBody);
            throw new Exception("La respuesta de la API no es JSON válido. Respuesta: " + responseBody);
        }
    }
    #endregion
}