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
    Task<List<Device>?> GetAllDevicesByPage(int page, string username, string password);

    // GET device by ID
    Task<Device?> GetDeviceById(int id, string username, string password);
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

    public async Task<List<Device>?> GetAllDevicesByPage(int page, string username, string password)
    {
        string baseUrl = _apiConfig.BaseUrl;
        string endpoint = _apiConfig.Endpoints["Device"]["GetAll"];

        if (page <= 0)
        {
            throw new ArgumentException("El número de página debe ser positivo.", nameof(page));
        }

        string token = await _sondaAuthService.GetUserTokenAsync(username, password);

        string getDataUrl = baseUrl + endpoint + "?page=" + page;
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Respuesta de la API
        var response = await client.GetAsync(getDataUrl);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        
        var pagedResponse = JsonSerializer.Deserialize<PagedDeviceResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return pagedResponse?.PagedData;
    }

    public async Task<Device?> GetDeviceById(int id, string username, string password)
    {

        string baseUrl = _apiConfig.BaseUrl;
        string endpoint = _apiConfig.Endpoints["Device"]["GetById"];
        string getDataUrl = baseUrl + endpoint + "/" + id;

        string token = await _sondaAuthService.GetUserTokenAsync(username, password);

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
        string baseUrl = _apiConfig.BaseUrl;
        string endpoint = _apiConfig.Endpoints["Group"]["Groups"];

        string token = await _sondaAuthService.GetUserTokenAsync(username, password);

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
        string token = await _sondaAuthService.GetUserTokenAsync(username, password);

        string baseUrl = _apiConfig.BaseUrl;
        string endpoint = _apiConfig.Endpoints["Group"]["GetById"];

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


    public async Task<List<Source>> GetAllSources(string username, string password)
    {
        string token = await _sondaAuthService.GetUserTokenAsync(username, password);

        string baseUrl = _apiConfig.BaseUrl;
        string endpoint = _apiConfig.Endpoints["Source"]["Sources"];

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
        string token = await _sondaAuthService.GetUserTokenAsync(username, password);

        string baseUrl = _apiConfig.BaseUrl;
        string endpoint = _apiConfig.Endpoints["Source"]["GetById"];

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




}
