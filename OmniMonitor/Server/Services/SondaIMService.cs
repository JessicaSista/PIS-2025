using Microsoft.Extensions.Options;
using OmniMonitor.Server.Configuration;
using OmniMonitor.Shared.Dtos;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

public interface ISondaIMService
{

    //****************DEVICES***************
    // GET all devices
    Task<List<Device>?> GetAllDevices(string username, string password);

    // GET device by ID
    Task<Device?> GetDeviceById(int id, string username, string password);

    // Obtener todos los devices pertenecientes a una source
    Task<List<Device>?> GetDeviceOfSource(int id, string username, string password);

    // Obtener todos los devices pertenecientes a un grupo
    Task<List<Device>?> GetDeviceOfGroup(int id, string username, string password);

    Task<List<DeviceData>?> GetDeviceDataByDate(int deviceId, DateTime dateFrom, DateTime dateTo, string username, string password);
    //***************************************

    //***************DEVICE GROUPS*************
    // GET all device groups
    Task<List<DeviceGroup>> GetAllDeviceGroups(string username, string password);
    
    // GET device group by ID
    Task<DeviceGroup?> GetDeviceGroupById(int id, string username, string password);
    //*****************************************

    //****************SOURCES*****************
    // GET all sources
    Task<List<Source>> GetAllSources(string username, string password);

    // GET source by ID
    Task<Source?> GetSourceById(int id, string username, string password);
    //*****************************************

    //****************SENSORS*****************
    Task<List<SensorData>?> GetSensorDataByDate(int deviceId, string sensorName, DateTime dateFrom, DateTime dateTo, string username, string password);
    //*****************************************


    //****************SYSTEM STATUS*****************
    Task<int> GetSSDeviceCount(string username, string password);
    Task<DeviceDataStatusResponse?> GetSSDataStatus(string username, string password);

}


public class SondaIMService : ISondaIMService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISondaAuthService _sondaAuthService;
    private readonly ApiConfig _apiConfig;

    public SondaIMService(IHttpClientFactory httpClientFactory, ISondaAuthService sondaAuthService, IOptions<ApiConfig> apiConfigOptions)
    {
        _httpClientFactory = httpClientFactory;
        _sondaAuthService = sondaAuthService;
        _apiConfig = apiConfigOptions.Value;
    }

    public async Task<List<Device>?> GetAllDevices(string username, string password)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlIM;
        string endpoint = _apiConfig.EndpointsIM["Device"]["GetAll"];
        string token = await _sondaAuthService.GetUserTokenIMAsync(username, password);

        // Se actualizó la URL para usar page=-1 y obtener todos los dispositivos
        string getDataUrl = $"{baseUrl}{endpoint}?page=-1";

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync(getDataUrl);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"Response Body (All Devices): {responseBody}");

        // La respuesta ahora es un arreglo directo de dispositivos, por lo que se deserializa a List<Device>
        var devices = JsonSerializer.Deserialize<List<Device>>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return devices;
    }

    public async Task<Device?> GetDeviceById(int id, string username, string password)
    {

        string baseUrl = _apiConfig.BaseUrl.UrlIM;
        string endpoint = _apiConfig.EndpointsIM["Device"]["GetById"];
        string getDataUrl = baseUrl + endpoint + "/" + id;

        string token = await _sondaAuthService.GetUserTokenIMAsync(username, password);

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

        var parsed = JsonSerializer.Deserialize<Device>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return parsed;
    }


    public async Task<List<DeviceGroup>> GetAllDeviceGroups(string username, string password)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlIM;
        string endpoint = _apiConfig.EndpointsIM["Group"]["Groups"];

        string token = await _sondaAuthService.GetUserTokenIMAsync(username, password);

        string getDataUrl = baseUrl + endpoint;
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync(getDataUrl);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return new List<DeviceGroup>(); // devolver lista vacía si no hay grupos
        }

        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"Response Body (DeviceGroups): {responseBody}");

        var parsed = JsonSerializer.Deserialize<List<DeviceGroup>>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return parsed ?? new List<DeviceGroup>();
    }

    public async Task<DeviceGroup?> GetDeviceGroupById(int id, string username, string password)
    {
        string token = await _sondaAuthService.GetUserTokenIMAsync(username, password);

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

        Console.WriteLine($"Response Body (DeviceGroup {id}): {responseBody}");

        var parsed = JsonSerializer.Deserialize<DeviceGroup>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return parsed;
    }

    public async Task<List<Device>> GetDeviceOfSource(int id, string username, string password)
    {
        string token = await _sondaAuthService.GetUserTokenIMAsync(username, password);

        string baseUrl = _apiConfig.BaseUrl.UrlIM;
        string endpoint = _apiConfig.EndpointsIM["Device"]["DevicesOfSource"];

        string getDataUrl = baseUrl + endpoint + "/" + id;

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync(getDataUrl);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"Response Body (DevicesOfSource): {responseBody}");

        // La respuesta ahora es un arreglo directo de dispositivos, por lo que se deserializa a List<Device>
        var devices = JsonSerializer.Deserialize<List<Device>>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return devices;
    }

    // Obtener todos los devices pertenecientes a un grupo
    public async Task<List<Device>> GetDeviceOfGroup(int id, string username, string password)
    {
        string token = await _sondaAuthService.GetUserTokenIMAsync(username, password);

        string baseUrl = _apiConfig.BaseUrl.UrlIM;
        string endpoint = _apiConfig.EndpointsIM["Device"]["DevicesOfGroup"];

        string getDataUrl = baseUrl + endpoint + "/" + id;

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync(getDataUrl);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"Response Body (GetDeviceOfGroup): {responseBody}");

        // La respuesta ahora es un arreglo directo de dispositivos, por lo que se deserializa a List<Device>
        var devices = JsonSerializer.Deserialize<List<Device>>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return devices;
    }

    public async Task<List<DeviceData>?> GetDeviceDataByDate(int deviceId, DateTime dateFrom, DateTime dateTo, string username, string password)
    {
        string token = await _sondaAuthService.GetUserTokenIMAsync(username, password);
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

        Console.WriteLine($"URL de la API Externa (GetDeviceDataByDate): {url}");

        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<List<DeviceData>>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    public async Task<List<Source>> GetAllSources(string username, string password)
    {
        string token = await _sondaAuthService.GetUserTokenIMAsync(username, password);

        string baseUrl = _apiConfig.BaseUrl.UrlIM;
        string endpoint = _apiConfig.EndpointsIM["Source"]["Sources"];

        string getDataUrl = baseUrl + endpoint;
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync(getDataUrl);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return new List<Source>();
        }
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"Response Body (Sources): {responseBody}");

        var parsed = JsonSerializer.Deserialize<List<Source>>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return parsed ?? new List<Source>();
    }

    public async Task<Source?> GetSourceById(int id, string username, string password)
    {
        string token = await _sondaAuthService.GetUserTokenIMAsync(username, password);

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
        Console.WriteLine($"Response Body (Source {id}): {responseBody}");
        var parsed = JsonSerializer.Deserialize<Source>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        return parsed;
    }

    public async Task<List<SensorData>?> GetSensorDataByDate(int deviceId, string sensorName, DateTime dateFrom, DateTime dateTo, string username, string password)
    {
        string token = await _sondaAuthService.GetUserTokenIMAsync(username, password);
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
    public async Task<int> GetSSDeviceCount(string username, string password)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlIM;
        string endpoint = _apiConfig.EndpointsIM["SystemStatus"]["DeviceCount"];

        string token = await _sondaAuthService.GetUserTokenIMAsync(username, password);

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

        Console.WriteLine($"Response Body (DeviceCount): {responseBody}");


        var parsed = JsonSerializer.Deserialize<int>(responseBody);

        return parsed;
    }

    public async Task<DeviceDataStatusResponse?> GetSSDataStatus(string username, string password)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlIM;
        string endpoint = _apiConfig.EndpointsIM["SystemStatus"]["DataStatus"];

        string token = await _sondaAuthService.GetUserTokenIMAsync(username, password);

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

        Console.WriteLine($"Response Body (DataStatus): {responseBody}");

        var parsed = JsonSerializer.Deserialize<DeviceDataStatusResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return parsed;
    }






}
