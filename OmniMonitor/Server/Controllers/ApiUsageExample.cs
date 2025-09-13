using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

class ApiUsageExample
{
    public static async Task Main()
    {
        // Leer JSON de configuración
        var json = File.ReadAllText("ApiConfig.json");
        dynamic config = JsonSerializer.Deserialize<dynamic>(json);

        // Extraer BaseUrl y credenciales
        string baseUrl = config.BaseUrl;
        string email = config.Credentials.Email;
        string password = config.Credentials.Password;

        // Mostrar credenciales (solo para ejemplo, no usar en producción)
        Console.WriteLine("Usando credenciales:");
        Console.WriteLine($"Email: {email}");
        Console.WriteLine($"Password: {password}");

        // Construir URL completa del endpoint de login
        string loginEndpoint = config.Endpoints.Account.Login;
        string loginUrl = $"{baseUrl}{loginEndpoint}";

        using var client = new HttpClient();

        // Preparar payload con credenciales
        var payload = new
        {
            email = email,
            password = password
        };

        // Llamar endpoint POST de login
        var response = await client.PostAsJsonAsync(loginUrl, payload);
        var result = await response.Content.ReadAsStringAsync();

        Console.WriteLine("\nRespuesta del login:");
        Console.WriteLine(result);

        // Ejemplo de cómo llamar otro endpoint (GetAll Actions)
        string actionsEndpoint = config.Endpoints.Action.GetAll;
        string actionsUrl = $"{baseUrl}{actionsEndpoint}";

        var actionsResponse = await client.GetAsync(actionsUrl);
        var actionsResult = await actionsResponse.Content.ReadAsStringAsync();

        Console.WriteLine("\nLista de Actions:");
        Console.WriteLine(actionsResult);
    }
}
