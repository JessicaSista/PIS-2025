using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

// Interfaz para la inyección de dependencias
public interface ISondaApiService
{
    Task<string> GetDevicesAsync();
}

public class SondaApiService : ISondaApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private string? _apiToken;

    public SondaApiService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    private async Task LoginAndStoreTokenAsync()
    {
        var credentials = new
        {
            email = "pis@pis.com",
            password = "PIS.sonda2025"
        };

        string loginUrl = "https://sondasmartplatform.com/internal/IoTMonitor/api/Account/Login";
        var client = _httpClientFactory.CreateClient();

        var content = new StringContent(JsonSerializer.Serialize(credentials), Encoding.UTF8, "application/json");
        var response = await client.PostAsync(loginUrl, content);

        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        Console.WriteLine(">>>> JSON CRUDO RECIBIDO: " + responseBody);
        // 1. Creamos opciones para ignorar mayúsculas/minúsculas en los nombres de propiedad
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        // 2. Usamos esas opciones al deserializar
        var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseBody, options);

        _apiToken = loginResponse?.Token;
        Console.WriteLine(">>>> VALOR DEL TOKEN ASIGNADO: " + _apiToken);
    }

    public async Task<string> GetDevicesAsync()
    {
        if (string.IsNullOrEmpty(_apiToken))
        {
            await LoginAndStoreTokenAsync();
            if (string.IsNullOrEmpty(_apiToken))
            {
                throw new InvalidOperationException("No se pudo obtener el token de la API de Sonda.");
            }
        }

        string getDataUrl = "https://sondasmartplatform.com/internal/IoTMonitor/api/Device/devices?page=-1";
        var client = _httpClientFactory.CreateClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiToken);

        var response = await client.GetAsync(getDataUrl);

        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            throw new HttpRequestException("Error 403: El usuario configurado no tiene permisos para este recurso.");
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }
}

// Clase auxiliar para deserializar la respuesta del login
public class LoginResponse
{
    public string? Token { get; set; }
}