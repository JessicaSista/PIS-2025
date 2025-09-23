using Microsoft.Extensions.Options;
using OmniMonitor.Server.Configuration;
using OmniMonitor.Shared.Dtos;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;


public interface ISondaUMService
{

}

public class SondaUMService : ISondaUMService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISondaAuthService _sondaAuthService;
    private readonly ApiConfig _apiConfig;
    public SondaUMService(IHttpClientFactory httpClientFactory, ISondaAuthService sondaAuthService, IOptions<ApiConfig> apiConfigOptions)
    {
        _httpClientFactory = httpClientFactory;
        _sondaAuthService = sondaAuthService;
        _apiConfig = apiConfigOptions.Value;
    }

   public async void testUMAPI (string username, string password)
    {
        string baseUrl = _apiConfig.BaseUrl.UrlIM;
        string endpoint = _apiConfig.EndpointsIM["Device"]["GetAll"];

        string token = await _sondaAuthService.GetUserTokenUMAsync(username, password);

        Console.Write("TOKEN RECIBIDO: " + token);
    }


}


