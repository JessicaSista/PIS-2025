
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Moq;
using Moq.Protected;
using OmniMonitor.Server.Configuration;
using OmniMonitor.Shared.Dtos;
using System.Net;
// using OmniMonitor.Server.Services;


public class SondaUMServiceTests
{
	private SondaUMService CreateService(HttpResponseMessage response, string token = "test-token")
	{
		var mockAuthService = new Mock<ISondaAuthService>();
		mockAuthService.Setup(x => x.GetUserTokenUMAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(token);
		var handlerMock = new Mock<HttpMessageHandler>();
		handlerMock.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>()
				)
			.ReturnsAsync(response);
		var httpClient = new HttpClient(handlerMock.Object);
		var httpClientFactoryMock = new Mock<IHttpClientFactory>();
		httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);
		var apiConfig = new ApiConfig
		{
			BaseUrl = new BaseUrlConfig { UrlUM = "http://localhost/api/" },
			EndpointsUM = new Dictionary<string, Dictionary<string, string>>
			{
				["Zone"] = new Dictionary<string, string>
				{
					["Zones"] = "zones",
					["GetById"] = "zones/get"
				},
				["News"] = new Dictionary<string, string>
				{
					["News"] = "news",
					["GetById"] = "news/get"
				},
				["Event"] = new Dictionary<string, string>
				{
					["Events"] = "events",
					["GetById"] = "events/get"
				}
			}
		};
		var options = Options.Create<ApiConfig>(apiConfig);
		return new SondaUMService(httpClientFactoryMock.Object, mockAuthService.Object, options);
	}

    private SondaUMService CreateService(HttpResponseMessage response, Func<HttpRequestMessage, bool> requestValidator, string token = "test-token")
    {
        var mockAuthService = new Mock<ISondaAuthService>();
        mockAuthService.Setup(x => x.GetUserTokenUMAsync(It.IsAny<string>(), It.IsAny<string>()))
                       .ReturnsAsync(token);

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => requestValidator(req)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var httpClient = new HttpClient(handlerMock.Object);
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var apiConfig = new ApiConfig
        {
            BaseUrl = new BaseUrlConfig { UrlUM = "http://localhost/api/" },
            EndpointsUM = new Dictionary<string, Dictionary<string, string>>
            {
                ["Zone"] = new Dictionary<string, string>
                {
                    ["Zones"] = "zones",
                    ["GetById"] = "zones/get"
                },
                ["News"] = new Dictionary<string, string>
                {
                    ["News"] = "news",
                    ["GetById"] = "news/get"
                },
                ["Event"] = new Dictionary<string, string>
                {
                    ["Events"] = "events",
                    ["GetById"] = "events/get"
                }
            }
        };

        var options = Options.Create<ApiConfig>(apiConfig);
        return new SondaUMService(httpClientFactoryMock.Object, mockAuthService.Object, options);
    }
    //Creamos un CreateService que devuelve un fake service
    private SondaUMService CreateServiceFake(Dictionary<string, object>? dataPerEndpoint = null, string token = "test-token")
    {
        var mockAuthService = new Mock<ISondaAuthService>();
        mockAuthService
            .Setup(x => x.GetUserTokenUMAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(token);

        var handler = new FakeHttpMessageHandler(req =>
        {
            // Ejemplo de AbsolutePath: "/api/news" -> segments -> ["api","news"] -> tomamos el último -> "news"
            var path = req.RequestUri!.AbsolutePath.Trim('/');
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var endpointKey = segments.Length > 0 ? segments.Last().ToLowerInvariant() : string.Empty; // e.g. "news"

            var queryParams = QueryHelpers.ParseQuery(req.RequestUri.Query);

            if (dataPerEndpoint != null && dataPerEndpoint.TryGetValue(endpointKey, out var data))
            {
                switch (data)
                {
                    case IEnumerable<News> newsList:
                        // Parseo robusto de parámetros
                        int startIndex = 1, count = 10;
                        if (queryParams.TryGetValue("startIndex", out StringValues siVals) && int.TryParse(siVals.FirstOrDefault(), out var si)) startIndex = si;
                        if (queryParams.TryGetValue("count", out StringValues cVals) && int.TryParse(cVals.FirstOrDefault(), out var c)) count = c;

                        string? queryString = queryParams.TryGetValue("queryString", out var qv) ? qv.FirstOrDefault() : null;
                        string? sort = queryParams.TryGetValue("sort", out var sv) ? sv.FirstOrDefault() : null;

                        var list = newsList.ToList();
                        if (!string.IsNullOrEmpty(queryString))
                            list = list.Where(n => n.Title?.Contains(queryString, StringComparison.OrdinalIgnoreCase) == true).ToList();

                        if (!string.IsNullOrEmpty(sort) && sort.Equals("title", StringComparison.OrdinalIgnoreCase))
                            list = list.OrderBy(n => n.Title).ToList();

                        var paged = list.Skip(Math.Max(0, startIndex - 1)).Take(Math.Max(0, count)).ToList();
                        var jsonNews = System.Text.Json.JsonSerializer.Serialize(new NewsResponse { results = paged });
                        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(jsonNews) };

                    case IEnumerable<Zone> zonesList:
                        var jsonZones = System.Text.Json.JsonSerializer.Serialize(zonesList);
                        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(jsonZones) };

                    case IEnumerable<Event> eventsList:
                        var jsonEvents = System.Text.Json.JsonSerializer.Serialize(eventsList);
                        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(jsonEvents) };

                    default:
                        return new HttpResponseMessage(HttpStatusCode.NotImplemented);
                }
            }

            // Si no encontramos el endpoint simulado, devolvemos 404
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var httpClient = new HttpClient(handler);
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var apiConfig = new ApiConfig
        {
            BaseUrl = new BaseUrlConfig { UrlUM = "http://localhost/api/" },
            EndpointsUM = new Dictionary<string, Dictionary<string, string>>
            {
                ["Zone"] = new Dictionary<string, string>
                {
                    ["Zones"] = "zones",
                    ["GetById"] = "zones/get"
                },
                ["News"] = new Dictionary<string, string>
                {
                    ["News"] = "news",
                    ["GetById"] = "news/get"
                },
                ["Event"] = new Dictionary<string, string>
                {
                    ["Events"] = "events",
                    ["GetById"] = "events/get"
                }
            }
        };

        return new SondaUMService(httpClientFactoryMock.Object, mockAuthService.Object, Options.Create(apiConfig));
    }

    public class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responder(request));
        }
    }
    // Tests para GetAllZones
    [Fact]
	public async Task GetAllZones_ReturnsZonesList_WhenResponseIsSuccessful()
	{
		// Arrange
		var zones = new List<Zone>
		{
			new Zone { Id = 1, Name = "Zone 1" },
			new Zone { Id = 2, Name = "Zone 2" }
		};
		var response = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(zones))
		};
		var service = CreateService(response);
		// Act
		var result = await service.GetAllZones("user", "pass");
		// Assert
		Assert.NotNull(result);
		Assert.Equal(2, result.Count);
		Assert.Equal("Zone 1", result[0].Name);
		Assert.Equal("Zone 2", result[1].Name);
	}

	[Fact]
	public async Task GetAllZones_ReturnsEmptyList_WhenResponseIsNotFound()
	{
		var service = CreateService(new HttpResponseMessage(HttpStatusCode.NotFound));
		var result = await service.GetAllZones("user", "pass");
		Assert.Empty(result);
	}

	[Fact]
	public async Task GetAllZones_ThrowsException_WhenResponseIsServerError()
	{
		var service = CreateService(new HttpResponseMessage(HttpStatusCode.InternalServerError));
		await Assert.ThrowsAsync<HttpRequestException>(async () => await service.GetAllZones("user", "pass"));
	}

    // Tests para GetZoneById
    [Fact]
	public async Task GetZoneById_ShouldReturnZone_WhenResponseIsSuccessful()
	{
		var zone = new Zone { Id = 1, Name = "Zone 1" };
		var response = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(zone))
		};
		var service = CreateService(response);
		var result = await service.GetZoneById(1, "user", "pass");
		Assert.NotNull(result);
		Assert.Equal(1, result.Id);
		Assert.Equal("Zone 1", result.Name);
	}

	[Fact]
	public async Task GetZoneById_ShouldReturnNull_WhenResponseIsNotFound()
	{
		var service = CreateService(new HttpResponseMessage(HttpStatusCode.NotFound));
		var result = await service.GetZoneById(1, "user", "pass");
		Assert.Null(result);
	}

	[Fact]
	public async Task GetZoneById_ShouldThrowException_WhenResponseIsServerError()
	{
		var service = CreateService(new HttpResponseMessage(HttpStatusCode.InternalServerError));
		await Assert.ThrowsAsync<HttpRequestException>(async () => await service.GetZoneById(1, "user", "pass"));
	}

    [Fact]
	public async Task GetZoneById_ShouldThrowException_WhenAuthFails()
	{
		var mockAuthService = new Mock<ISondaAuthService>();
		mockAuthService.Setup(x => x.GetUserTokenUMAsync(It.IsAny<string>(), It.IsAny<string>())).ThrowsAsync(new System.Exception("Auth failed"));
		var handlerMock = new Mock<HttpMessageHandler>();
		var httpClient = new HttpClient(handlerMock.Object);
		var httpClientFactoryMock = new Mock<IHttpClientFactory>();
		httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);
		var apiConfig = new ApiConfig
		{
			BaseUrl = new BaseUrlConfig { UrlUM = "http://localhost/api/" },
			EndpointsUM = new Dictionary<string, Dictionary<string, string>>
			{
				["Zone"] = new Dictionary<string, string>
				{
					["Zones"] = "zones",
					["GetById"] = "zones/get"
				},
				["News"] = new Dictionary<string, string>
				{
					["News"] = "news",
					["GetById"] = "news/get"
				},
				["Event"] = new Dictionary<string, string>
				{
					["Events"] = "events",
					["GetById"] = "events/get"
				}
			}
		};
		var options = Options.Create<ApiConfig>(apiConfig);
		var service = new SondaUMService(httpClientFactoryMock.Object, mockAuthService.Object, options);
		await Assert.ThrowsAsync<System.Exception>(async () => await service.GetZoneById(1, "user", "pass"));
	}

    // Tests para GetAllNews
    [Fact]
	public async Task GetAllNews_ReturnsNewsList_WhenResponseIsSuccessful()
	{
        // Arrange
        var news = new List<News>
        {
            new News { Id = 1, Title = "New 1" , Description = "Description 1"},
            new News { Id = 2, Title = "New 2" , Description = "Description 2"}
        };
		var newsResponse = new NewsResponse { results = news };
		var json = System.Text.Json.JsonSerializer.Serialize(newsResponse);
		var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent(json)
		};

        var service = CreateService(fakeResponse);
        // Act
        var result = await service.GetAllNews("user", "pass");
        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("New 1", result[0].Title);
        Assert.Equal("New 2", result[1].Title);
    }

	[Fact]
	public async Task GetAllNews_ReturnsEmptyList_WhenResponseIsNotFound()
	{
		var service = CreateService(new HttpResponseMessage(HttpStatusCode.NotFound));
		var result = await service.GetAllNews("user", "pass");
		Assert.Empty(result);
	}

	[Fact]
	public async Task GetAllNews_ThrowsException_WhenResponseIsServerError()
	{
		var service = CreateService(new HttpResponseMessage(HttpStatusCode.InternalServerError));
		await Assert.ThrowsAsync<HttpRequestException>(async () => await service.GetAllNews("user", "pass"));
    }

	[Fact]
	public async Task GetAllNews_ShouldRespectstartIndexAndcount()
	{
        var allNews = new List<News>();
        for (int i = 1; i <= 50; i++)
        {
            allNews.Add(new News { Id = i, Title = $"News {i}", Description = $"Description {i}" });
        }
        var pagedNews = allNews.Skip(10).Take(5).ToList();
		var newsResponse = new NewsResponse { results = pagedNews };
        var json = System.Text.Json.JsonSerializer.Serialize(newsResponse);
        var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json)
        };
        bool RequestValidator(HttpRequestMessage req)
        {
            var uri = req.RequestUri!.ToString();
            return uri.Contains("startIndex=11") && uri.Contains("count=5");
        }
        var service = CreateService(fakeResponse, RequestValidator);
        var result = await service.GetAllNews("user", "pass", startIndex: 11, count: 5);
        Assert.NotNull(result);
        Assert.Equal(5, result.Count);
        Assert.Equal(11, result[0].Id);
        Assert.Equal(15, result[4].Id);
    }

	[Fact]
	public async Task GetAllNews_ShouldApplyQueryStringFilter()
	{
        var filteredNews = new List<News>
    {
        new News { Id = 101, Title = "Filtered News 1" },
        new News { Id = 102, Title = "Filtered News 2" }
    };
        var newsResponse = new NewsResponse { results = filteredNews };
        var json = System.Text.Json.JsonSerializer.Serialize(newsResponse);
        var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json)
        };
        bool RequestValidator(HttpRequestMessage req) =>
            req.RequestUri!.Query.Contains("queryString=Filtered");
        var service = CreateService(fakeResponse, RequestValidator);
        var result = await service.GetAllNews("user", "pass", queryString: "Filtered");
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, n => Assert.Contains("Filtered", n.Title));
    }

	[Fact]
	public async Task GetAllNews_ShouldApplySortOrder()
	{
        // Arrange
        var sortedNews = new List<News>
    {
        new News { Id = 2, Title = "Zeta" },
        new News { Id = 1, Title = "Alpha" }
    };
        var newsResponse = new NewsResponse { results = sortedNews };
        var json = System.Text.Json.JsonSerializer.Serialize(newsResponse);
        var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json)
        };

        bool RequestValidator(HttpRequestMessage req) =>
            req.RequestUri!.Query.Contains("sort=title");

        var service = CreateService(fakeResponse, RequestValidator);

        // Act
        var result = await service.GetAllNews("user", "pass", sort: "title");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("Zeta", result[0].Title);
        Assert.Equal("Alpha", result[1].Title);
    }

    // Tests para GetNewsById
    [Fact]
	public async Task GetNewsById_ShouldReturnNews_WhenResponseIsSuccessful()
	{
		var news = new News { Id = 1, Title = "News 1" };
		var response = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(news))
		};
		var service = CreateService(response);
		var result = await service.GetNewsById(1, "user", "pass");
		Assert.NotNull(result);
		Assert.Equal(1, result.Id);
		Assert.Equal("News 1", result.Title);
    }
	[Fact]
	public async Task GetNewsById_ShouldReturnNull_WhenResponseIsNotFound()
	{
		var service = CreateService(new HttpResponseMessage(HttpStatusCode.NotFound));
		var result = await service.GetNewsById(1, "user", "pass");
		Assert.Null(result);
    }

	[Fact]
	public async Task GetNewsById_ShouldThrowException_WhenResponseIsServerError()
	{
		var service = CreateService(new HttpResponseMessage(HttpStatusCode.InternalServerError));
		await Assert.ThrowsAsync<HttpRequestException>(async () => await service.GetNewsById(1, "user", "pass"));
    }
	// Tests para GetAllEvents
	[Fact]
	public async Task GetAllEvents_ReturnsEventsList_WhenResponseIsSuccessful()
	{
		// Arrange
		var events = new List<Event>
		{
			new Event { Id = 1, Name = "Event 1" },
			new Event { Id = 2, Name = "Event 2" }
		};
		var response = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(events))
		};
		var service = CreateService(response);
		// Act
		var result = await service.GetAllEvents("user", "pass");
		// Assert
		Assert.NotNull(result);
		Assert.Equal(2, result.Count);
		Assert.Equal("Event 1", result[0].Name);
		Assert.Equal("Event 2", result[1].Name);
    }

	[Fact]
	public async Task GetAllEvents_ReturnsEmptyList_WhenResponseIsNotFound()
	{
		var service = CreateService(new HttpResponseMessage(HttpStatusCode.NotFound));
		var result = await service.GetAllEvents("user", "pass");
		Assert.Empty(result);
	}

	[Fact]
	public async Task GetAllEvents_ThrowsException_WhenResponseIsServerError()
	{
		var service = CreateService(new HttpResponseMessage(HttpStatusCode.InternalServerError));
		await Assert.ThrowsAsync<HttpRequestException>(async () => await service.GetAllEvents("user", "pass"));
    }
	// Tests para GetEventById
	[Fact]
	public async Task GetEventById_ShouldReturnEvent_WhenResponseIsSuccessful()
	{
		var events = new Event { Id = 1, Name = "Event 1" };
		var response = new HttpResponseMessage(HttpStatusCode.OK)
		{ 
			Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(events)) 
		};
		var service = CreateService(response);
		var result = await service.GetEventById(1, "user", "pass");
		Assert.NotNull(result);
		Assert.Equal(1, result.Id);
		Assert.Equal("Event 1", result.Name);
    }

	[Fact]
	public async Task GetEventById_ShouldReturnNull_WhenResponseIsNotFound()
	{
		var service = CreateService(new HttpResponseMessage(HttpStatusCode.NotFound));
		var result = await service.GetEventById(1, "user", "pass");
		Assert.Null(result);
    }

	[Fact]
	public async Task GetEventById_ShouldThrowException_WhenResponseIsServerError()
	{
		var service = CreateService(new HttpResponseMessage(HttpStatusCode.InternalServerError));
		await Assert.ThrowsAsync<HttpRequestException>(async () => await service.GetEventById(1, "user", "pass"));
    }

    // Tests de Integracion basicos con Fake Service
    [Fact]
	public async Task GetAllNews_ShouldRespectstartIndexAndCount_Fake()
	{
		var allNews = new List<News>();
		for (int i = 1; i <= 50; i++)
		{
			allNews.Add(new News { Id = i, Title = $"News {i}", Description = $"Description {i}" });
		}
		var dataPerEndpoint = new Dictionary<string, object>
		{
			{ "news", allNews }
		};
		var service = CreateServiceFake(dataPerEndpoint);
		var result = await service.GetAllNews("user", "pass", startIndex: 11, count: 5);
		Assert.NotNull(result);
		Assert.Equal(5, result.Count);
		Assert.Equal(11, result[0].Id);
		Assert.Equal(15, result[4].Id);
    }

	[Fact]
	public async Task GetAllNews_ShouldApplyQueryStringFilter_Fake()
	{
		var allNews = new List<News>
		{
			new News { Id = 1, Title = "Filtered News 1" },
			new News { Id = 2, Title = "Other News" },
			new News { Id = 3, Title = "Filtered News 2" }
		};
		var dataPerEndpoint = new Dictionary<string, object>
		{
			{ "news", allNews }
		};
		var service = CreateServiceFake(dataPerEndpoint);
		var result = await service.GetAllNews("user", "pass", queryString: "Filtered");
		Assert.NotNull(result);
		Assert.Equal(2, result.Count);
		Assert.All(result, n => Assert.Contains("Filtered", n.Title));
    }

	[Fact]
	public async Task GetAllNews_ShouldApplySortOrder_Fake()
	{
		var allNews = new List<News>
		{
			new News { Id = 1, Title = "Zeta" },
			new News { Id = 3, Title = "Beta" },
			new News { Id = 2, Title = "Alpha" }
		};
		var dataPerEndpoint = new Dictionary<string, object>
		{
			{ "news", allNews }
		};
		var service = CreateServiceFake(dataPerEndpoint);
		var result = await service.GetAllNews("user", "pass", sort: "title");
		Assert.NotNull(result);
		Assert.Equal(3, result.Count);
		Assert.Equal("Alpha", result[0].Title);
		Assert.Equal("Beta", result[1].Title);
		Assert.Equal("Zeta", result[2].Title);
    }

	[Fact]
	public async Task GetAllNews_ShouldApplyQueryStringFilter_SortOrderAndPagination_Fake()
	{
		var allNews = new List<News>();
		for (int i = 1; i <= 100; i++)
		{
			if (i % 2 == 0)
				if (i % 5 == 0)
					allNews.Add(new News { Id = i, Title = $"Filtered News {i}" });
				else
					allNews.Add(new News { Id = i, Title = $"Other News {i}" });

            else
					allNews.Add(new News { Id = i, Title = $"News {i}" });
		}
		var dataPerEndpoint = new Dictionary<string, object>
		{
			{ "news", allNews }
		};
		var service = CreateServiceFake(dataPerEndpoint);
		var result = await service.GetAllNews("user", "pass", startIndex: 2, count: 3, queryString: "Filtered", sort: "title");
		Assert.NotNull(result);
		Assert.Equal(3, result.Count);
		Assert.Equal("Filtered News 100", result[0].Title);
		Assert.Equal("Filtered News 20", result[1].Title);
		Assert.Equal("Filtered News 30", result[2].Title);
    }

}
