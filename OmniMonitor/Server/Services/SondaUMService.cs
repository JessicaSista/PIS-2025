using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OmniMonitor.Server.Configuration;
using OmniMonitor.Shared.Dtos;
using System.Net.Http.Headers;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

/// <summary>
/// Interfaz para el servicio de integración con Sonda UM.
/// </summary>
public interface ISondaUMService
{
    #region Zonas

    /// <summary>
    /// Obtiene todas las zonas.
    /// </summary>
    /// <param name="username">Nombre de usuario.</param>
    /// <returns>Lista de zonas.</returns>
    Task<List<Zone>> GetAllZones(string username);

    /// <summary>
    /// Obtiene una zona por su ID.
    /// </summary>
    /// <param name="id">ID de la zona.</param>
    /// <param name="username">Nombre de usuario.</param>
    /// <returns>Zona o null.</returns>
    Task<Zone?> GetZoneById(int id, string username);

    #endregion

    #region Noticias

    /// <summary>
    /// Obtiene todas las noticias.
    /// </summary>
    /// <param name="username">Nombre de usuario.</param>
    /// <param name="page">Página.</param>
    /// <param name="queryString">Filtro de búsqueda.</param>
    /// <param name="sort">Orden.</param>
    /// <param name="pageSize">Tamaño de página.</param>
    /// <returns>Lista de noticias.</returns>
    Task<List<News>> GetAllNews(string username, int page = 1, string? queryString = null, string? sort = null, int pageSize = 10);

    /// <summary>
    /// Obtiene una noticia por su ID.
    /// </summary>
    /// <param name="id">ID de la noticia.</param>
    /// <param name="username">Nombre de usuario.</param>
    /// <returns>Noticia o null.</returns>
    Task<News?> GetNewsById(int id, string username);

    /// <summary>
    /// Obtiene noticias por zona.
    /// </summary>
    /// <param name="zoneId">ID de la zona.</param>
    /// <param name="username">Nombre de usuario.</param>
    /// <param name="startIndex">Índice inicial.</param>
    /// <param name="queryString">Filtro de búsqueda.</param>
    /// <param name="sort">Orden.</param>
    /// <param name="count">Cantidad.</param>
    /// <returns>Lista de noticias.</returns>
    Task<List<News>> GetNewsByZoneId(int zoneId, string username, int startIndex = 1, string? queryString = null, string? sort = null, int count = 10);

    #endregion

    #region Eventos

    /// <summary>
    /// Obtiene todos los eventos.
    /// </summary>
    /// <param name="username">Nombre de usuario.</param>
    /// <returns>Lista de eventos.</returns>
    Task<List<Event>> GetAllEvents(string username);

    /// <summary>
    /// Obtiene un evento por su ID.
    /// </summary>
    /// <param name="id">ID del evento.</param>
    /// <param name="username">Nombre de usuario.</param>
    /// <returns>Evento o null.</returns>
    Task<Event?> GetEventById(int id, string username);

    /// <summary>
    /// Obtiene eventos por zona.
    /// </summary>
    /// <param name="zoneId">ID de la zona.</param>
    /// <param name="username">Nombre de usuario.</param>
    /// <returns>Lista de eventos.</returns>
    Task<List<Event>> GetEventsByZoneId(int zoneId, string username);

    #endregion
}

/// <summary>
/// Servicio de integración con Sonda UM.
/// </summary>
public class SondaUMService : ISondaUMService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISondaAuthService _sondaAuthService;
    private readonly ApiConfig _apiConfig;
    private readonly ILogger<SondaUMService> _logger;

    /// <summary>
    /// Constructor del servicio SondaUMService.
    /// </summary>
    public SondaUMService(
        IHttpClientFactory httpClientFactory,
        ISondaAuthService sondaAuthService,
        IOptions<ApiConfig> apiConfigOptions,
        ILogger<SondaUMService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _sondaAuthService = sondaAuthService;
        _apiConfig = apiConfigOptions.Value;
        _logger = logger;
    }

    #region Zonas

    /// <inheritdoc/>
    public async Task<List<Zone>> GetAllZones(string username)
    {
        if (string.IsNullOrEmpty(username))
        {
            _logger.LogWarning("El parámetro 'username' está vacío o es nulo.");
            return new();
        }

        string baseUrl = _apiConfig.BaseUrl.UrlUM;
        string endpoint = _apiConfig.EndpointsUM["Zone"]["Zones"];
        string token = await _sondaAuthService.GetUserTokenUmAsync(username);

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

        _logger.LogInformation("Response Body (Zones): {ResponseBody}", responseBody);

        var parsed = JsonSerializer.Deserialize<List<Zone>>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return parsed ?? new();
    }

    /// <inheritdoc/>
    public async Task<Zone?> GetZoneById(int id, string username)
    {
        if (string.IsNullOrEmpty(username))
        {
            _logger.LogWarning("El parámetro 'username' está vacío o es nulo.");
            return null;
        }

        string baseUrl = _apiConfig.BaseUrl.UrlUM;
        string endpoint = _apiConfig.EndpointsUM["Zone"]["GetById"];
        string getDataUrl = baseUrl + endpoint + "/" + id;
        string token = await _sondaAuthService.GetUserTokenUmAsync(username);
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync(getDataUrl);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        _logger.LogInformation("Response Body (Zone): {ResponseBody}", responseBody);
        var parsed = JsonSerializer.Deserialize<Zone>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        return parsed;
    }

    #endregion

    #region Noticias

    /// <inheritdoc/>
    public async Task<List<News>> GetAllNews(string username, int page = 1, string? queryString = null, string? sort = null, int pageSize = 10)
    {
        if (string.IsNullOrEmpty(username))
        {
            _logger.LogWarning("El parámetro 'username' está vacío o es nulo.");
            return new();
        }

        string baseUrl = _apiConfig.BaseUrl.UrlUM;
        string endpoint = _apiConfig.EndpointsUM["News"]["News"];

        var queryParams = new List<string>
        {
            $"startIndex={page}",
            $"count={pageSize}"
        };
        if (!string.IsNullOrEmpty(queryString))
        {
            queryParams.Add($"queryString={Uri.EscapeDataString(queryString)}");
        }
        if (!string.IsNullOrEmpty(sort))
        {
            queryParams.Add($"sort={Uri.EscapeDataString(sort)}");
        }

        string getDataUrl = $"{baseUrl}{endpoint}?{string.Join("&", queryParams)}";
        string token = await _sondaAuthService.GetUserTokenUmAsync(username);
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync(getDataUrl);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return new();
        }

        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();

        _logger.LogInformation("Response Body (News): {ResponseBody}", responseBody);

        var parsed = JsonSerializer.Deserialize<NewsResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return parsed?.results ?? new();
    }

    /// <inheritdoc/>
    public async Task<News?> GetNewsById(int id, string username)
    {
        if (string.IsNullOrEmpty(username))
        {
            _logger.LogWarning("El parámetro 'username' está vacío o es nulo.");
            return null;
        }

        string baseUrl = _apiConfig.BaseUrl.UrlUM;
        string endpoint = _apiConfig.EndpointsUM["News"]["GetById"];
        string getDataUrl = baseUrl + endpoint + "/" + id;
        string token = await _sondaAuthService.GetUserTokenUmAsync(username);
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync(getDataUrl);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        _logger.LogInformation("Response Body (News): {ResponseBody}", responseBody);
        var parsed = JsonSerializer.Deserialize<News>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        return parsed;
    }

    /// <inheritdoc/>
    public async Task<List<News>> GetNewsByZoneId(int zoneId, string username, int startIndex = 1, string? queryString = null, string? sort = null, int count = 10)
    {
        if (string.IsNullOrEmpty(username))
        {
            _logger.LogWarning("El parámetro 'username' está vacío o es nulo.");
            return new();
        }
        if (startIndex <= 0)
        {
            throw new ArgumentException("startIndex debe ser mayor a 0");
        }
        if (count <= 0)
        {
            throw new ArgumentException("count debe ser mayor a 0");
        }

        var zone = await GetZoneById(zoneId, username);
        if (zone == null)
        {
            throw new ArgumentException($"No existe una zona con id {zoneId}");
        }

        var allNews = await GetAllNews(username, startIndex, queryString, sort, count);
        var filteredNews = new List<News>();

        foreach (var news in allNews)
        {
            var newsDetail = await GetNewsById(news.Id, username);
            if (newsDetail != null && newsDetail.Zone != null && newsDetail.Zone.Id == zoneId)
            {
                filteredNews.Add(newsDetail);
            }
        }

        return filteredNews;
    }

    #endregion

    #region Eventos

    /// <inheritdoc/>
    public async Task<List<Event>> GetAllEvents(string username)
    {
        if (string.IsNullOrEmpty(username))
        {
            _logger.LogWarning("El parámetro 'username' está vacío o es nulo.");
            return new();
        }

        string baseUrl = _apiConfig.BaseUrl.UrlUM;
        string endpoint = _apiConfig.EndpointsUM["Event"]["Events"];
        string token = await _sondaAuthService.GetUserTokenUmAsync(username);
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
        _logger.LogInformation("Response Body (Events): {ResponseBody}", responseBody);
        var parsed = JsonSerializer.Deserialize<List<Event>>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        return parsed ?? new();
    }

    /// <inheritdoc/>
    public async Task<Event?> GetEventById(int id, string username)
    {
        if (string.IsNullOrEmpty(username))
        {
            _logger.LogWarning("El parámetro 'username' está vacío o es nulo.");
            return null;
        }

        string baseUrl = _apiConfig.BaseUrl.UrlUM;
        string endpoint = _apiConfig.EndpointsUM["Event"]["GetById"];
        string getDataUrl = baseUrl + endpoint + "/" + id;
        string token = await _sondaAuthService.GetUserTokenUmAsync(username);
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync(getDataUrl);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        _logger.LogInformation("Response Body (Event): {ResponseBody}", responseBody);
        var parsed = JsonSerializer.Deserialize<Event>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        return parsed;
    }

    /// <inheritdoc/>
    public async Task<List<Event>> GetEventsByZoneId(int zoneId, string username)
    {
        if (string.IsNullOrEmpty(username))
        {
            _logger.LogWarning("El parámetro 'username' está vacío o es nulo.");
            return new();
        }

        var zone = await GetZoneById(zoneId, username);
        if (zone == null)
        {
            return new();
        }

        var allEvents = await GetAllEvents(username);
        if (allEvents == null || !allEvents.Any())
        {
            return new();
        }

        var eventsInZone = new List<Event>();

        foreach (var eventItem in allEvents)
        {
            if (eventItem.Location != null &&
                GeometryHelper.IsPointInZone(eventItem.Location, zone.Areas))
            {
                eventsInZone.Add(eventItem);
            }
        }

        return eventsInZone;
    }

    #endregion
}

