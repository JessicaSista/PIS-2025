using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OmniMonitor.Server.Configuration;
using OmniMonitor.Shared.Dtos;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>
/// Interfaz para el servicio de integración con Sonda IM.
/// </summary>
public interface ISondaIMService
{
    #region Devices

    /// <summary>
    /// Obtiene todos los dispositivos.
    /// </summary>
    /// <param name="username">Nombre de usuario.</param>
    /// <returns>Lista de dispositivos o null.</returns>
    Task<List<Device>?> GetAllDevices(string username);

    /// <summary>
    /// Obtiene un dispositivo por su ID.
    /// </summary>
    /// <param name="id">ID del dispositivo.</param>
    /// <param name="username">Nombre de usuario.</param>
    /// <returns>Dispositivo o null.</returns>
    Task<Device?> GetDeviceById(int id, string username);

    /// <summary>
    /// Obtiene todos los dispositivos de una fuente.
    /// </summary>
    /// <param name="id">ID de la fuente.</param>
    /// <param name="username">Nombre de usuario.</param>
    /// <returns>Lista de dispositivos o null.</returns>
    Task<List<Device>?> GetDeviceOfSource(int id, string username);

    /// <summary>
    /// Obtiene todos los dispositivos de un grupo.
    /// </summary>
    /// <param name="id">ID del grupo.</param>
    /// <param name="username">Nombre de usuario.</param>
    /// <returns>Lista de dispositivos o null.</returns>
    Task<List<Device>?> GetDeviceOfGroup(int id, string username);

    /// <summary>
    /// Obtiene los datos de un dispositivo por rango de fechas.
    /// </summary>
    /// <param name="deviceId">ID del dispositivo.</param>
    /// <param name="dateFrom">Fecha inicial.</param>
    /// <param name="dateTo">Fecha final.</param>
    /// <param name="username">Nombre de usuario.</param>
    /// <returns>Lista de datos del dispositivo o null.</returns>
    Task<List<DeviceData>?> GetDeviceDataByDate(int deviceId, DateTime dateFrom, DateTime dateTo, string username);

    #endregion

    #region Device Groups

    /// <summary>
    /// Obtiene todos los grupos de dispositivos.
    /// </summary>
    /// <param name="username">Nombre de usuario.</param>
    /// <returns>Lista de grupos de dispositivos.</returns>
    Task<List<DeviceGroup>> GetAllDeviceGroups(string username);

    /// <summary>
    /// Obtiene un grupo de dispositivos por su ID.
    /// </summary>
    /// <param name="id">ID del grupo.</param>
    /// <param name="username">Nombre de usuario.</param>
    /// <returns>Grupo de dispositivos o null.</returns>
    Task<DeviceGroup?> GetDeviceGroupById(int id, string username);

    #endregion

    #region Sources

    /// <summary>
    /// Obtiene todas las fuentes.
    /// </summary>
    /// <param name="username">Nombre de usuario.</param>
    /// <returns>Lista de fuentes.</returns>
    Task<List<Source>> GetAllSources(string username);

    /// <summary>
    /// Obtiene una fuente por su ID.
    /// </summary>
    /// <param name="id">ID de la fuente.</param>
    /// <param name="username">Nombre de usuario.</param>
    /// <returns>Fuente o null.</returns>
    Task<Source?> GetSourceById(int id, string username);

    #endregion

    #region Sensors

    /// <summary>
    /// Obtiene los datos de un sensor por rango de fechas.
    /// </summary>
    /// <param name="deviceId">ID del dispositivo.</param>
    /// <param name="sensorName">Nombre del sensor.</param>
    /// <param name="dateFrom">Fecha inicial.</param>
    /// <param name="dateTo">Fecha final.</param>
    /// <param name="username">Nombre de usuario.</param>
    /// <returns>Lista de datos del sensor o null.</returns>
    Task<List<SensorData>?> GetSensorDataByDate(int deviceId, string sensorName, DateTime dateFrom, DateTime dateTo, string username);
    Task<List<SensorData>?> GetSensorDataByDateSinToken(int deviceId, string sensorName, DateTime dateFrom, DateTime dateTo);
    //*****************************************

    #endregion

    #region System Status

    /// <summary>
    /// Obtiene la cantidad de dispositivos del sistema.
    /// </summary>
    /// <param name="username">Nombre de usuario.</param>
    /// <returns>Cantidad de dispositivos.</returns>
    Task<int> GetSSDeviceCount(string username);

    /// <summary>
    /// Obtiene el estado de los datos del sistema.
    /// </summary>
    /// <param name="username">Nombre de usuario.</param>
    /// <returns>Respuesta con el estado de los datos o null.</returns>
    Task<DeviceDataStatusResponse?> GetSSDataStatus(string username);

    #endregion
}

/// <summary>
/// Servicio de integración con Sonda IM.
/// </summary>
public class SondaIMService : ISondaIMService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISondaAuthService _sondaAuthService;
    private readonly ApiConfig _apiConfig;
    private readonly ILogger<SondaIMService> _logger;

    /// <summary>
    /// Constructor del servicio SondaIMService.
    /// </summary>
    /// <param name="httpClientFactory">Fábrica de HttpClient.</param>
    /// <param name="sondaAuthService">Servicio de autenticación.</param>
    /// <param name="apiConfigOptions">Opciones de configuración de la API.</param>
    /// <param name="logger">Logger para registrar información.</param>
    public SondaIMService(
        IHttpClientFactory httpClientFactory,
        ISondaAuthService sondaAuthService,
        IOptions<ApiConfig> apiConfigOptions,
        ILogger<SondaIMService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _sondaAuthService = sondaAuthService;
        _apiConfig = apiConfigOptions.Value;
        _logger = logger;
    }

    #region Devices

    /// <inheritdoc/>
    public async Task<List<Device>?> GetAllDevices(string username)
    {
        if (string.IsNullOrEmpty(username))
        {
            _logger.LogWarning("El parámetro 'username' está vacío o es nulo.");
            return null;
        }

        string baseUrl = _apiConfig.BaseUrl.UrlIM;
        string endpoint = _apiConfig.EndpointsIM["Device"]["GetAll"];
        string token = await _sondaAuthService.GetUserTokenImAsync(username);

        // Se actualizó la URL para usar page=-1 y obtener todos los dispositivos
        string getDataUrl = $"{baseUrl}{endpoint}?page=-1";

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync(getDataUrl);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();

        _logger.LogInformation("Response Body (All Devices): {ResponseBody}", responseBody);

        // La respuesta ahora es un arreglo directo de dispositivos, por lo que se deserializa a List<Device>
        var devices = JsonSerializer.Deserialize<List<Device>>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return devices;
    }

    /// <inheritdoc/>
    public async Task<Device?> GetDeviceById(int id, string username)
    {
        if (string.IsNullOrEmpty(username))
        {
            _logger.LogWarning("El parámetro 'username' está vacío o es nulo.");
            return null;
        }

        string baseUrl = _apiConfig.BaseUrl.UrlIM;
        string endpoint = _apiConfig.EndpointsIM["Device"]["GetById"];
        string getDataUrl = baseUrl + endpoint + "/" + id;

        string token = await _sondaAuthService.GetUserTokenImAsync(username);

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

        _logger.LogInformation("Response Body: {ResponseBody}", responseBody);

        var parsed = JsonSerializer.Deserialize<Device>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return parsed;
    }

    /// <inheritdoc/>
    public async Task<List<Device>?> GetDeviceOfSource(int id, string username)
    {
        if (string.IsNullOrEmpty(username))
        {
            _logger.LogWarning("El parámetro 'username' está vacío o es nulo.");
            return null;
        }

        string token = await _sondaAuthService.GetUserTokenImAsync(username);

        string baseUrl = _apiConfig.BaseUrl.UrlIM;
        string endpoint = _apiConfig.EndpointsIM["Device"]["DevicesOfSource"];

        string getDataUrl = baseUrl + endpoint + "/" + id;

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync(getDataUrl);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();

        _logger.LogInformation("Response Body (DevicesOfSource): {ResponseBody}", responseBody);

        // La respuesta ahora es un arreglo directo de dispositivos, por lo que se deserializa a List<Device>
        var devices = JsonSerializer.Deserialize<List<Device>>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return devices;
    }

    /// <inheritdoc/>
    public async Task<List<Device>?> GetDeviceOfGroup(int id, string username)
    {
        if (string.IsNullOrEmpty(username))
        {
            _logger.LogWarning("El parámetro 'username' está vacío o es nulo.");
            return null;
        }

        string token = await _sondaAuthService.GetUserTokenImAsync(username);

        string baseUrl = _apiConfig.BaseUrl.UrlIM;
        string endpoint = _apiConfig.EndpointsIM["Device"]["DevicesOfGroup"];

        string getDataUrl = baseUrl + endpoint + "/" + id;

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync(getDataUrl);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();

        _logger.LogInformation("Response Body (GetDeviceOfGroup): {ResponseBody}", responseBody);

        // La respuesta ahora es un arreglo directo de dispositivos, por lo que se deserializa a List<Device>
        var devices = JsonSerializer.Deserialize<List<Device>>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return devices;
    }

    /// <inheritdoc/>
    public async Task<List<DeviceData>?> GetDeviceDataByDate(int deviceId, DateTime dateFrom, DateTime dateTo, string username)
    {
        if (string.IsNullOrEmpty(username))
        {
            _logger.LogWarning("El parámetro 'username' está vacío o es nulo.");
            return null;
        }

        string token = await _sondaAuthService.GetUserTokenImAsync(username);
        string baseUrl = _apiConfig.BaseUrl.UrlIM;
        string endpoint = _apiConfig.EndpointsIM["Analytic"]["DeviceData"];

        // 1. Formatear las fechas al formato específico que requiere la API externa
        string formattedDateFrom = dateFrom.ToString("yyyy-MM-ddTHH:mm:ss");
        string formattedDateTo = dateTo.ToString("yyyy-MM-ddTHH:mm:ss");

        // 2. Unir con coma y luego codificar para la URL
        string datesParameter = $"{formattedDateFrom},{formattedDateTo}";
        string encodedDates = Uri.EscapeDataString(datesParameter);

        // 3. Construir la URL final para la API externa
        string url = $"{baseUrl}{endpoint}?deviceId={deviceId}&dates={encodedDates}";

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        _logger.LogInformation("URL de la API Externa (GetDeviceDataByDate): {Url}", url);

        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<List<DeviceData>>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    #endregion

    #region Device Groups

    /// <inheritdoc/>
    public async Task<List<DeviceGroup>> GetAllDeviceGroups(string username)
    {
        if (string.IsNullOrEmpty(username))
        {
            _logger.LogWarning("El parámetro 'username' está vacío o es nulo.");
            return new();
        }

        string baseUrl = _apiConfig.BaseUrl.UrlIM;
        string endpoint = _apiConfig.EndpointsIM["Group"]["Groups"];

        string token = await _sondaAuthService.GetUserTokenImAsync(username);

        string getDataUrl = baseUrl + endpoint;
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync(getDataUrl);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return new();
        }

        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();

        _logger.LogInformation("Response Body (DeviceGroups): {ResponseBody}", responseBody);

        var parsed = JsonSerializer.Deserialize<List<DeviceGroup>>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return parsed ?? new();
    }

    /// <inheritdoc/>
    public async Task<DeviceGroup?> GetDeviceGroupById(int id, string username)
    {
        if (string.IsNullOrEmpty(username))
        {
            _logger.LogWarning("El parámetro 'username' está vacío o es nulo.");
            return null;
        }

        string token = await _sondaAuthService.GetUserTokenImAsync(username);

        string baseUrl = _apiConfig.BaseUrl.UrlIM;
        string endpoint = _apiConfig.EndpointsIM["Group"]["GetById"];

        string getDataUrl = baseUrl + endpoint + "/" + id;
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync(getDataUrl);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();

        _logger.LogInformation("Response Body (DeviceGroup {Id}): {ResponseBody}", id, responseBody);

        var parsed = JsonSerializer.Deserialize<DeviceGroup>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return parsed;
    }

    #endregion

    #region Sources

    /// <inheritdoc/>
    public async Task<List<Source>> GetAllSources(string username)
    {
        if (string.IsNullOrEmpty(username))
        {
            _logger.LogWarning("El parámetro 'username' está vacío o es nulo.");
            return new();
        }

        string token = await _sondaAuthService.GetUserTokenImAsync(username);

        string baseUrl = _apiConfig.BaseUrl.UrlIM;
        string endpoint = _apiConfig.EndpointsIM["Source"]["Sources"];

        string getDataUrl = baseUrl + endpoint;
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync(getDataUrl);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return new();
        }
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();

        _logger.LogInformation("Response Body (Sources): {ResponseBody}", responseBody);

        var parsed = JsonSerializer.Deserialize<List<Source>>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return parsed ?? new();
    }

    /// <inheritdoc/>
    public async Task<Source?> GetSourceById(int id, string username)
    {
        if (string.IsNullOrEmpty(username))
        {
            _logger.LogWarning("El parámetro 'username' está vacío o es nulo.");
            return null;
        }

        string token = await _sondaAuthService.GetUserTokenImAsync(username);

        string baseUrl = _apiConfig.BaseUrl.UrlIM;
        string endpoint = _apiConfig.EndpointsIM["Source"]["GetById"];

        string getDataUrl = baseUrl + endpoint + "/" + id;
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync(getDataUrl);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        _logger.LogInformation("Response Body (Source {Id}): {ResponseBody}", id, responseBody);
        var parsed = JsonSerializer.Deserialize<Source>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        return parsed;
    }

    #endregion

    #region Sensors

    /// <inheritdoc/>
    public async Task<List<SensorData>?> GetSensorDataByDate(int deviceId, string sensorName, DateTime dateFrom, DateTime dateTo, string username)
    {
        if (string.IsNullOrEmpty(username))
        {
            _logger.LogWarning("El parámetro 'username' está vacío o es nulo.");
            return null;
        }

        string token = await _sondaAuthService.GetUserTokenImAsync(username);
        string baseUrl = _apiConfig.BaseUrl.UrlIM;
        string endpoint = _apiConfig.EndpointsIM["Analytic"]["TimeSerie"];

        string formattedDateFrom = dateFrom.ToString("yyyy-MM-ddTHH:mm:ss");
        string formattedDateTo = dateTo.ToString("yyyy-MM-ddTHH:mm:ss");

        string datesParameter = $"{formattedDateFrom},{formattedDateTo}";

        string encodedDates = Uri.EscapeDataString(datesParameter);

        string url = $"{baseUrl}{endpoint}?deviceId={deviceId}&sensorName={sensorName}&dates={encodedDates}";

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        _logger.LogInformation("URL GetSensorDataByDate: {Url}", url);

        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<List<SensorData>>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    public async Task<List<SensorData>?> GetSensorDataByDateSinToken(int deviceId, string sensorName, DateTime dateFrom, DateTime dateTo)
    {
        string token = await _sondaAuthService.GetUserTokenIMAsync("visitante");
        string baseUrl = _apiConfig.BaseUrl.UrlIM;
        string endpoint = _apiConfig.EndpointsIM["Analytic"]["TimeSerie"];

        string formattedDateFrom = dateFrom.ToString("yyyy-MM-ddTHH:mm:ss");
        string formattedDateTo = dateTo.ToString("yyyy-MM-ddTHH:mm:ss");

        string datesParameter = $"{formattedDateFrom},{formattedDateTo}";

        string encodedDates = Uri.EscapeDataString(datesParameter);

        string url = $"{baseUrl}{endpoint}?deviceId={deviceId}&sensorName={sensorName}&dates={encodedDates}";

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        Console.WriteLine($"URL GetSensorDataByDate: {url}");

        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<List<SensorData>>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }


    #endregion

    #region System Status

    /// <inheritdoc/>
    public async Task<int> GetSSDeviceCount(string username)
    {
        if (string.IsNullOrEmpty(username))
        {
            _logger.LogWarning("El parámetro 'username' está vacío o es nulo.");
            return 0;
        }

        string baseUrl = _apiConfig.BaseUrl.UrlIM;
        string endpoint = _apiConfig.EndpointsIM["SystemStatus"]["DeviceCount"];

        string token = await _sondaAuthService.GetUserTokenImAsync(username);

        string getDataUrl = baseUrl + endpoint;
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync(getDataUrl);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return 0;
        }

        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();

        _logger.LogInformation("Response Body (DeviceCount): {ResponseBody}", responseBody);

        var parsed = JsonSerializer.Deserialize<int>(responseBody);

        return parsed;
    }

    /// <inheritdoc/>
    public async Task<DeviceDataStatusResponse?> GetSSDataStatus(string username)
    {
        if (string.IsNullOrEmpty(username))
        {
            _logger.LogWarning("El parámetro 'username' está vacío o es nulo.");
            return null;
        }

        string baseUrl = _apiConfig.BaseUrl.UrlIM;
        string endpoint = _apiConfig.EndpointsIM["SystemStatus"]["DataStatus"];

        string token = await _sondaAuthService.GetUserTokenImAsync(username);

        string getDataUrl = baseUrl + endpoint;
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync(getDataUrl);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();

        _logger.LogInformation("Response Body (DataStatus): {ResponseBody}", responseBody);

        var parsed = JsonSerializer.Deserialize<DeviceDataStatusResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return parsed;
    }

    #endregion
}
