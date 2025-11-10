
using Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Identity.Client;
using Moq;
using Moq.Protected;
using OmniMonitor.Server.Configuration;
using OmniMonitor.Server.Models;
using OmniMonitor.Shared.Dtos;
using OmniMonitor.Shared.Dtos.AM;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
// using OmniMonitor.Server.Services;


public class SondaAMServiceTests
{
    private SondaAMService CreateService(HttpResponseMessage response, string token = "test-token")
    {
        var mockAuthService = new Mock<ISondaAuthService>();
        mockAuthService.Setup(x => x.GetUserTokenAMAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(token);
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
            BaseUrl = new BaseUrlConfig { UrlAM = "http://localhost/api/" },
            EndpointsAM = new Dictionary<string, Dictionary<string, string>>
            {
                ["Asset"] = new Dictionary<string, string>
                {
                    ["GetAssets"] = "assets",
                    ["GetById"] = "assets/get",
                    ["GetAssetsBasicData"] = "assets/basic",
                    ["GetLinkedAssets"] = "assets/linked",
                    ["History"] = "assets/history"
                },
                ["Relation"] = new Dictionary<string, string>
                {
                    ["GetAssetRelations"] = "relation/{assetId}"
                },
                ["Bundle"] = new Dictionary<string, string>
                {
                    ["GetBundles"] = "bundles",
                    ["GetByBundleId"] = "bundles/get"
                },
                ["Stock"] = new Dictionary<string, string>
                {
                    ["GetAll"] = "stock",
                    ["GetById"] = "stock/{stockId}/get"
                },
                ["EventTaskInstance"] = new Dictionary<string, string>
                {
                    ["GetById"] = "eventtaskinstance/{eventTaskInstanceId}/get",
                    ["GetAll"] = "eventtaskinstance",
                    ["GetActions"] = "eventtaskinstance/{taskInstanceId}/actions",
                    ["GetStock"] = "eventtaskinstance/{taskInstanceId}/stock"
                },
            }
        };
        var options = Options.Create<ApiConfig>(apiConfig);
        return new SondaAMService(httpClientFactoryMock.Object, mockAuthService.Object, options);
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
    //Tests para GetAssetById
    [Fact]
    public async Task GetAssetById_ShouldReturnAsset_WhenResponseIsSuccessful()
    {
        var asset = new AssetDto { Id = "1", Name = "Asset 1" };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(asset))
        };
        var service = CreateService(response);
        var result = await service.GetAssetById(1, "user", "pass");
        Assert.NotNull(result);
        Assert.Equal("1", result.Id);
        Assert.Equal("Asset 1", result.Name);
    }

    [Fact]
    public async Task GetAssetById_ShouldReturnNull_WhenResponseIsNotFound()
    {
        var asset = new AssetDto { Id = "1", Name = "Asset 1" };
        var service = CreateService(new HttpResponseMessage(HttpStatusCode.NotFound));
        var result = await service.GetAssetById(1, "user", "pass");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAssetById_ShouldThrowException_WhenBodyIsInvalidJson()
    {
        var service = CreateService(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("Invalid JSON")
        });
        await Assert.ThrowsAsync<System.Text.Json.JsonException>(async () => await service.GetAssetById(1, "user", "pass"));
    }

    [Fact]
    public async Task GetAssetById_CallCorrectUrl()
    {
        var asset = new AssetDto { Id = "1", Name = "Asset 1" };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(asset))
        };
        string? capturedUrl = null;
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) =>
            {
                capturedUrl = req.RequestUri?.ToString();
            })
            .ReturnsAsync(response);
        var httpClient = new HttpClient(handlerMock.Object);
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var mockAuthService = new Mock<ISondaAuthService>();
        mockAuthService.Setup(x => x.GetUserTokenAMAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("test-token");
        var apiConfig = new ApiConfig
        {
            BaseUrl = new BaseUrlConfig { UrlAM = "http://localhost/api/" },
            EndpointsAM = new Dictionary<string, Dictionary<string, string>>
            {
                ["Asset"] = new Dictionary<string, string>
                {
                    ["GetAssets"] = "assets",
                    ["GetById"] = "assets/get"
                }
            }
        };
        var options = Options.Create<ApiConfig>(apiConfig);
        var service = new SondaAMService(httpClientFactoryMock.Object, mockAuthService.Object, options);
        var result = await service.GetAssetById(1, "user", "pass");
        Assert.Equal("http://localhost/api/assets/get?assetId=1", capturedUrl);
    }

    [Fact]
    public async Task GetAssetById_IncludesAuthorizationHeader()
    {
        var asset = new AssetDto { Id = "1", Name = "Asset 1" };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(asset))
        };
        AuthenticationHeaderValue? capturedAuthHeader = null;
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) =>
            {
                capturedAuthHeader = req.Headers.Authorization;
            })
            .ReturnsAsync(response);
        var httpClient = new HttpClient(handlerMock.Object);
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var mockAuthService = new Mock<ISondaAuthService>();
        mockAuthService.Setup(x => x.GetUserTokenAMAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("test-token");
        var apiConfig = new ApiConfig
        {
            BaseUrl = new BaseUrlConfig { UrlAM = "http://localhost/api/" },
            EndpointsAM = new Dictionary<string, Dictionary<string, string>>
            {
                ["Asset"] = new Dictionary<string, string>
                {
                    ["GetAssets"] = "assets",
                    ["GetById"] = "assets/get"
                }
            }
        };
        var options = Options.Create<ApiConfig>(apiConfig);
        var service = new SondaAMService(httpClientFactoryMock.Object, mockAuthService.Object, options);
        var result = await service.GetAssetById(1, "user", "pass");
        Assert.NotNull(capturedAuthHeader);
        Assert.Equal("Bearer", capturedAuthHeader!.Scheme);
        Assert.Equal("test-token", capturedAuthHeader.Parameter);
    }

    [Fact]
    public async Task GetAssetById_ShouldThrowException_WhenResponseIsServerError()
    {
        var service = CreateService(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        await Assert.ThrowsAsync<HttpRequestException>(async () => await service.GetAssetById(1, "user", "pass"));
    }

    [Fact]
    public async Task GetAssetById_ShouldThrowArgumentException_WhenIdIsInvalid()
    {
        var service = CreateService(new HttpResponseMessage(HttpStatusCode.OK));
        await Assert.ThrowsAsync<ArgumentException>(async () => await service.GetAssetById(-1, "user", "pass"));
    }

    // Tests para GetAssets

    [Fact]
    public async Task GetAssets_ShouldReturnAssetsList_WhenResponseIsSuccessful()
    {
        var assets = new List<AssetDto>
        {
            new AssetDto { Id = "1", Name = "Asset 1" },
            new AssetDto { Id = "2", Name = "Asset 2" }
        };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(assets))
        };
        var service = CreateService(response);
        var result = await service.GetAssets(null, null, null, null, null, null, "user", "pass");
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("Asset 1", result[0].Name);
        Assert.Equal("Asset 2", result[1].Name);
    }

    [Fact]
    public async Task GetAssets_ShouldReturnNull_WhenResponseIsNotFound()
    {
        var assets = new List<AssetDto>
        {
            new AssetDto { Id = "1", Name = "Asset 1" },
            new AssetDto { Id = "2", Name = "Asset 2" }
        };
        var service = CreateService(new HttpResponseMessage(HttpStatusCode.NotFound));
        var result = await service.GetAssets(null, null, null, null, null, null, "user", "pass");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAssets_HandlesQueryParameters()
    {
        var asserts = new List<AssetDto>
        {
            new AssetDto { Id = "1", Name = "Asset A" },
            new AssetDto { Id = "2", Name = "Asset B" },
            new AssetDto { Id = "3", Name = "Asset C" }
        };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(asserts))
        };
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
        .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        )
        .Callback<HttpRequestMessage, CancellationToken>((req, ct) =>
        {
            // Verificar query string
            Assert.Contains("page=2", req.RequestUri!.Query);
            Assert.Contains("queryString=foo", req.RequestUri.Query);
            Assert.Contains("bundles=bundle123", req.RequestUri.Query);
            Assert.Contains("assetTypeId=5", req.RequestUri.Query);
            Assert.Contains("sort=name", req.RequestUri.Query);
            Assert.Contains("pageSize=10", req.RequestUri.Query);

            // Verificar header Authorization
            Assert.NotNull(req.Headers.Authorization);
            Assert.Equal("Bearer", req.Headers.Authorization!.Scheme);
            Assert.Equal("test-token", req.Headers.Authorization.Parameter);
        })
        .ReturnsAsync(response);
        var httpClient = new HttpClient(handlerMock.Object);
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var mockAuthService = new Mock<ISondaAuthService>();
        mockAuthService.Setup(x => x.GetUserTokenAMAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("test-token");
        var apiConfig = new ApiConfig
        {
            BaseUrl = new BaseUrlConfig { UrlAM = "http://localhost/api/" },
            EndpointsAM = new Dictionary<string, Dictionary<string, string>>
            {
                ["Asset"] = new Dictionary<string, string>
                {
                    ["GetAssets"] = "assets",
                    ["GetById"] = "assets/get"
                }
            }
        };
        var options = Options.Create<ApiConfig>(apiConfig);
        var service = new SondaAMService(httpClientFactoryMock.Object, mockAuthService.Object, options);
        var result = await service.GetAssets(2, "foo", "bundle123", 5, "name", 10, "user", "pass");
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal("Asset A", result[0].Name);
        Assert.Equal("Asset B", result[1].Name);
        Assert.Equal("Asset C", result[2].Name);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "No tienes permisos: token inválido o expirado (401 Unauthorized).")]
    [InlineData(HttpStatusCode.Forbidden, "No tienes permisos para acceder a este recurso (403 Forbidden).")]
    public async Task GetAssets_ThrowsException_WhenAuthFails(HttpStatusCode statusCode, string expectedMessage)
    {
        var service = CreateService(new HttpResponseMessage(statusCode));
        var ex = await Assert.ThrowsAsync<Exception>(() => service.GetAssets(null, null, null, null, null, null, "user", "pass"));
        Assert.Equal(expectedMessage, ex.Message);
    }

    // Tests para GetAllStock

    [Fact]
    public async Task GetAllStock_shouldReturnStockList_WhenResponseIsSuccessful()
    {
        var stockItems = new List<StockDto>
        {
            new StockDto { Id = 1, Name = "Stock 1" },
            new StockDto { Id = 2, Name = "Stock 2" }
        };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(stockItems))
        };
        var service = CreateService(response);
        var result = await service.GetAllStock(null, null, null, null, null, "user", "pass");
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("Stock 1", result[0].Name);
        Assert.Equal("Stock 2", result[1].Name);
    }

    [Fact]
    public async Task GetAllStock_ShouldReturnEmptyList_WhenResponseIsNotFound()
    {
        var service = CreateService(new HttpResponseMessage(HttpStatusCode.NotFound));
        var result = await service.GetAllStock(null, null, null, null, null, "user", "pass");
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllStock_HandlesQueryParameters()
    {
        var stockItems = new List<StockDto>
        {
            new StockDto { Id = 1, Name = "Stock A" },
            new StockDto { Id = 2, Name = "Stock B" },
            new StockDto { Id = 3, Name = "Stock C" }
        };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(stockItems))
        };
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        )
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) =>
            {
                // Verificar query string
                Assert.Contains("page=2", req.RequestUri!.Query);
                Assert.Contains("queryString=foo", req.RequestUri.Query);
                Assert.Contains("sort=name", req.RequestUri.Query);
                Assert.Contains("pageSize=10", req.RequestUri.Query);
                Assert.Contains("bundlesId=2", req.RequestUri.Query);

                // Verificar header Authorization
                Assert.NotNull(req.Headers.Authorization);
                Assert.Equal("Bearer", req.Headers.Authorization!.Scheme);
                Assert.Equal("test-token", req.Headers.Authorization.Parameter);
            })
            .ReturnsAsync(response);
        var httpClient = new HttpClient(handlerMock.Object);
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var mockAuthService = new Mock<ISondaAuthService>();
        mockAuthService.Setup(x => x.GetUserTokenAMAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("test-token");
        var apiConfig = new ApiConfig
        {
            BaseUrl = new BaseUrlConfig { UrlAM = "http://localhost/api/" },
            EndpointsAM = new Dictionary<string, Dictionary<string, string>>
            {
                ["Stock"] = new Dictionary<string, string>
                {
                    ["GetAll"] = "stock",
                    ["GetById"] = "stock/{stockId}/get"
                }
            }
        };
        var options = Options.Create<ApiConfig>(apiConfig);
        var service = new SondaAMService(httpClientFactoryMock.Object, mockAuthService.Object, options);
        var result = await service.GetAllStock(2, "foo", "name", 10, "2", "user", "pass");
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal("Stock A", result[0].Name);
        Assert.Equal("Stock B", result[1].Name);
        Assert.Equal("Stock C", result[2].Name);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "No tienes permisos: token inválido o expirado (401 Unauthorized).")]
    [InlineData(HttpStatusCode.Forbidden, "No tienes permisos para acceder a este recurso (403 Forbidden).")]
    public async Task GetAllStock_ThrowsException_WhenAuthFails(HttpStatusCode statusCode, string expectedMessage)
    {
        var service = CreateService(new HttpResponseMessage(statusCode));
        var ex = await Assert.ThrowsAsync<Exception>(() => service.GetAllStock(null, null, null, null, null, "user", "pass"));
        Assert.Equal(expectedMessage, ex.Message);
    }

    // Tests para GetStockParametersByBoundleId

    [Fact]
    public async Task GetStockParametersByBundleId_shouldReturnBundle_WhenResponseIsSuccessful()
    {
        var bundle = new BundleDto { Id = 1, Name = "Bundle 1" };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(bundle))
        };
        var service = CreateService(response);
        var result = await service.GetStockParametersByBundleId(1, "user", "pass");
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Bundle 1", result.Name);
    }

    [Fact]
    public async Task GetStockParametersByBundleId_shouldReturnNull_WhenResponseIsNotFound()
    {
        var bundle = new BundleDto { Id = 1, Name = "Bundle 1" };
        var service = CreateService(new HttpResponseMessage(HttpStatusCode.NotFound));
        var result = await service.GetStockParametersByBundleId(1, "user", "pass");
        Assert.Null(result);
    }

    [Theory]
    [InlineData("Invalid JSON")]
    [InlineData("")]
    public async Task GetStockParametersByBundleId_ShouldThrowException_WhenBodyIsInvalidOrEmptyJson(string content)
    {
        var service = CreateService(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(content)
        });
        var ex = await Assert.ThrowsAsync<Exception>(() => service.GetStockParametersByBundleId(1, "user", "pass"));
        Assert.Equal("La respuesta de la API no es JSON válido. Respuesta: " + content, ex.Message);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "No tienes permisos: token inválido o expirado (401 Unauthorized).")]
    [InlineData(HttpStatusCode.Forbidden, "No tienes permisos para acceder a este recurso (403 Forbidden).")]
    public async Task GetStockParametersByBundleId_ThrowsException_WhenAuthFails(HttpStatusCode statusCode, string expectedMessage)
    {
        var service = CreateService(new HttpResponseMessage(statusCode));
        var ex = await Assert.ThrowsAsync<Exception>(() => service.GetStockParametersByBundleId(1, "user", "pass"));
        Assert.Equal(expectedMessage, ex.Message);
    }

    [Fact]
    public async Task GetStockParametersByBundleId_ShouldThrowArgumentException_WhenIdIsInvalid()
    {
        var service = CreateService(new HttpResponseMessage(HttpStatusCode.OK));
        await Assert.ThrowsAsync<ArgumentException>(async () => await service.GetStockParametersByBundleId(-1, "user", "pass"));
    }

    // Tests para GetStockById

    [Fact]
    public async Task GetStockById_ShouldReturnStock_WhenResponseIsSuccessful()
    {
        var stock = new StockDto { Id = 1, Name = "Stock 1" };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(stock))
        };
        var service = CreateService(response);
        var result = await service.GetStockById(1, "user", "pass");
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Stock 1", result.Name);
    }

    [Fact]
    public async Task GetStockById_ShouldReturnNull_WhenResponseIsNotFound()
    {
        var service = CreateService(new HttpResponseMessage(HttpStatusCode.NotFound));
        var result = await service.GetStockById(1, "user", "pass");
        Assert.Null(result);
    }

    [Theory]
    [InlineData("Invalid JSON")]
    [InlineData("")]
    public async Task GetStockById_ShouldThrowException_WhenBodyIsInvalidorEmptyJson(string content)
    {
        var service = CreateService(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(content)
        });
        var ex = await Assert.ThrowsAsync<Exception>(() => service.GetStockById(1, "user", "pass"));
        Assert.Equal("La respuesta de la API no es JSON válido. Respuesta: " + content, ex.Message);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "No tienes permisos: token inválido o expirado (401 Unauthorized).")]
    [InlineData(HttpStatusCode.Forbidden, "No tienes permisos para acceder a este recurso (403 Forbidden).")]
    public async Task GetStockById_ThrowsException_WhenAuthFails(HttpStatusCode statusCode, string expectedMessage)
    {
        var service = CreateService(new HttpResponseMessage(statusCode));
        var ex = await Assert.ThrowsAsync<Exception>(() => service.GetStockById(1, "user", "pass"));
        Assert.Equal(expectedMessage, ex.Message);
    }

    [Fact]
    public async Task GetStockById_CallCorrectUrl()
    {
        var stock = new StockDto { Id = 1, Name = "Stock 1" };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(stock))
        };
        string? capturedUrl = null;
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) =>
            {
                capturedUrl = req.RequestUri?.ToString();
            })
            .ReturnsAsync(response);
        var httpClient = new HttpClient(handlerMock.Object);
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var mockAuthService = new Mock<ISondaAuthService>();
        mockAuthService.Setup(x => x.GetUserTokenAMAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("test-token");
        var apiConfig = new ApiConfig
        {
            BaseUrl = new BaseUrlConfig { UrlAM = "http://localhost/api/" },
            EndpointsAM = new Dictionary<string, Dictionary<string, string>>
            {
                ["Stock"] = new Dictionary<string, string>
                {
                    ["GetAll"] = "stock",
                    ["GetById"] = "stock/{stockId}/get"
                }
            }
        };
        var options = Options.Create<ApiConfig>(apiConfig);
        var service = new SondaAMService(httpClientFactoryMock.Object, mockAuthService.Object, options);
        var result = await service.GetStockById(1, "user", "pass");
        Assert.Equal("http://localhost/api/stock/1/get", capturedUrl);
    }

    [Fact]
    public async Task GetStockById_DeserializesCorrectly()
    {
        var stock = new StockDto { Id = 1, Name = "Stock 1" };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(stock))
        };
        var service = CreateService(response);
        var result = await service.GetStockById(1, "user", "pass");
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Stock 1", result.Name);
    }

    [Fact]
    public async Task GetStockById_ShouldThrowArgumentException_WhenIdIsInvalid()
    {
        var service = CreateService(new HttpResponseMessage(HttpStatusCode.BadRequest));
        await Assert.ThrowsAsync<ArgumentException > (async () => await service.GetStockById(-1, "user", "pass"));
    }

    // Tests para GetAssetRelation

    [Fact]
    public async Task ThrowsArgumentException_WhenAssetIdIsInvalid()
    {
        var service = CreateService(new HttpResponseMessage(HttpStatusCode.OK));
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetAssetRelations(0, 1, 10, "user", "pass"));
    }

    [Fact]
    public async Task ReturnsList_WhenApiReturnsObjectWithResults()
    {
        var json = @"{ ""results"": [
        { ""assetId"": 10, ""assetName"": ""Sensor A"", ""type"": ""Linked"" },
        { ""assetId"": 11, ""assetName"": ""Sensor B"", ""type"": ""Linked"" }
    ] }";

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var service = CreateService(response);
        var result = await service.GetAssetRelations(123, 1, 10, "user", "pass");

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal(10, result[0].AssetId);
        Assert.Equal("Sensor A", result[0].AssetName);
    }

    [Fact]
    public async Task ReturnsList_WhenApiReturnsArrayDirectly()
    {
        var list = new[]
        {
        new { assetId = 20, assetName = "Asset X", type = "Linked" },
        new { assetId = 21, assetName = "Asset Y", type = "Parent" }
    };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(list), Encoding.UTF8, "application/json")
        };

        var service = CreateService(response);
        var result = await service.GetAssetRelations(100, 2, 15, "user", "pass");

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("Asset X", result[0].AssetName);
    }

    [Fact]
    public async Task BuildsUrlAndSendsAuthorizationHeader()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]", Encoding.UTF8, "application/json")
        };

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) =>
            {
                // URL contiene assetId reemplazado
                Assert.Contains("assets/relation/123", req.RequestUri!.AbsoluteUri);

                // Querystring correcto
                var q = req.RequestUri.Query;
                Assert.Contains("page=2", q);
                Assert.Contains("pageSize=10", q);

                // Authorization header
                Assert.NotNull(req.Headers.Authorization);
                Assert.Equal("Bearer", req.Headers.Authorization!.Scheme);
                Assert.Equal("test-token", req.Headers.Authorization.Parameter);
            })
            .ReturnsAsync(response);

        var httpClient = new HttpClient(handlerMock.Object);
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var mockAuthService = new Mock<ISondaAuthService>();
        mockAuthService.Setup(x => x.GetUserTokenAMAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("test-token");

        var apiConfig = new ApiConfig
        {
            BaseUrl = new BaseUrlConfig { UrlAM = "http://localhost/api/" },
            EndpointsAM = new Dictionary<string, Dictionary<string, string>>
            {
                ["Relation"] = new Dictionary<string, string> { ["GetAssetRelations"] = "assets/relation/{assetId}" }
            }
        };
        var options = Options.Create(apiConfig);
        var service = new SondaAMService(httpClientFactoryMock.Object, mockAuthService.Object, options);

        await service.GetAssetRelations(123, 2, 10, "user", "pass");
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "No tienes permisos: token inválido o expirado (401 Unauthorized).")]
    [InlineData(HttpStatusCode.Forbidden, "No tienes permisos para acceder a este recurso (403 Forbidden).")]
    public async Task GetAssetRelations_Throws_OnUnauthorizedOrForbidden(HttpStatusCode statusCode, string expectedMessage)
    {
        // Arrange: API devuelve 401 o 403
        var response = new HttpResponseMessage(statusCode);
        var service = CreateService(response);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() =>
            service.GetAssetRelations(10, null, null, "user", "pass"));

        Assert.Equal(expectedMessage, ex.Message);
    }

    [Fact]
    public async Task GetAssetRelations_ThrowsAssetNotFound_On404()
    {
        // Arrange: API devuelve 404
        var response = new HttpResponseMessage(HttpStatusCode.NotFound);
        var service = CreateService(response);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() =>
            service.GetAssetRelations(999, null, null, "user", "pass"));

        Assert.Equal("AssetNotFound", ex.Message);
    }

    // Tests para GetBundles
    [Fact]
    public async Task GetBundles_ReturnsList_WhenApiReturnsObjectWithResults()
    {
        var bundles = new List<BundleDto>
        {
            new BundleDto { Id = 1, Name = "B1" },
            new BundleDto { Id = 2, Name = "B2" }
        };
        var payload = new { results = bundles };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

        var service = CreateService(response);
        var result = await service.GetBundles(1, null, null, 10, "user", "pass");
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("B1", result[0].Name);
    }

    [Fact]
    public async Task GetBundles_ReturnsList_WhenApiReturnsArrayDirectly()
    {
        var bundles = new List<BundleDto>
        {
            new BundleDto { Id = 5, Name = "X" }
        };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(bundles), Encoding.UTF8, "application/json")
        };

        var service = CreateService(response);
        var result = await service.GetBundles(null, null, null, null, "user", "pass");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("X", result[0].Name);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "No tienes permisos: token inválido o expirado (401 Unauthorized).")]
    [InlineData(HttpStatusCode.Forbidden, "No tienes permisos para acceder a este recurso (403 Forbidden).")]
    public async Task GetBundles_Throws_OnUnauthorizedOrForbidden(HttpStatusCode statusCode, string expectedMessage)
    {
        // Arrange
        var response = new HttpResponseMessage(statusCode);
        var service = CreateService(response);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => service.GetBundles(null, null, null, null, "user", "pass"));

        Assert.Equal(expectedMessage, ex.Message);
    }

    [Fact]
    public async Task GetBundles_BuildsQueryStringAndSendsAuthorizationHeader()
    {
        // Arrange: response with array so deserialization succeeds
        var bundles = new List<BundleDto> { new BundleDto { Id = 9, Name = "Z" } };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(bundles), Encoding.UTF8, "application/json")
        };

        var handlerMock = new Mock<HttpMessageHandler>();
        int page = 2;
        string queryString = "hello world&x=1";
        string sort = "name_desc";
        int pageSize = 30;

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) =>
            {
                // parse query into dictionary for robust assertions
                var q = req.RequestUri!.Query.TrimStart('?');
                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (!string.IsNullOrEmpty(q))
                {
                    foreach (var kv in q.Split('&', StringSplitOptions.RemoveEmptyEntries))
                    {
                        var parts = kv.Split('=', 2);
                        var key = Uri.UnescapeDataString(parts[0]);
                        var val = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty;
                        dict[key] = val;
                    }
                }

                Assert.True(dict.ContainsKey("page"));
                Assert.Equal(page.ToString(), dict["page"]);

                Assert.True(dict.ContainsKey("queryString"));
                Assert.Equal(queryString, dict["queryString"]);

                Assert.True(dict.ContainsKey("sort"));
                Assert.Equal(sort, dict["sort"]);

                Assert.True(dict.ContainsKey("pageSize"));
                Assert.Equal(pageSize.ToString(), dict["pageSize"]);

                // Authorization header
                Assert.NotNull(req.Headers.Authorization);
                Assert.Equal("Bearer", req.Headers.Authorization!.Scheme);
                Assert.Equal("test-token", req.Headers.Authorization.Parameter);
            })
            .ReturnsAsync(response);

        // build service with our custom handler
        var httpClient = new HttpClient(handlerMock.Object);
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var mockAuthService = new Mock<ISondaAuthService>();
        mockAuthService.Setup(x => x.GetUserTokenAMAsync(It.IsAny<string>(), It.IsAny<string>()))
                       .ReturnsAsync("test-token");

        var apiConfig = new ApiConfig
        {
            BaseUrl = new BaseUrlConfig { UrlAM = "http://localhost/api/" },
            EndpointsAM = new Dictionary<string, Dictionary<string, string>>
            {
                ["Bundle"] = new Dictionary<string, string>
                {
                    ["GetBundles"] = "bundles"
                }
            }
        };
        var options = Options.Create(apiConfig);

        var service = new SondaAMService(httpClientFactoryMock.Object, mockAuthService.Object, options);

        // Act
        var result = await service.GetBundles(page, queryString, sort, pageSize, "user", "pass");

        // Assert
        Assert.Single(result);
        Assert.Equal("Z", result[0].Name);
    }

    [Theory]
    [InlineData("", "está vacía")]             
    [InlineData("not-a-json", "no es JSON válido")] 
    public async Task GetBundles_Throws_OnEmptyOrInvalidJson(string responseBody, string expectedMessageFragment)
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
        };

        var service = CreateService(response);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() =>
            service.GetBundles(1, null, null, 10, "user", "pass"));

        Assert.Contains(expectedMessageFragment, ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // Tests para GetEventTaskInstanceStock 

    [Fact]
    public async Task GetEventTaskInstanceStock_ReturnsList_WhenApiReturnsArray()
    {
        var stocks = new List<EventTaskInstanceStockDto>
        {
            new EventTaskInstanceStockDto { Id = 1, Name = "S1", Quantity = 2 },
            new EventTaskInstanceStockDto { Id = 2, Name = "S2", Quantity = 3 }
        };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(stocks), Encoding.UTF8, "application/json")
        };

        var service = CreateService(response);
        var result = await service.GetEventTaskInstanceStock(42, "user", "pass");
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].Id);
        Assert.Equal("S1", result[0].Name);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "No tienes permisos: token inválido o expirado (401 Unauthorized).")]
    [InlineData(HttpStatusCode.Forbidden, "No tienes permisos para acceder a este recurso (403 Forbidden).")]
    public async Task GetEventTaskInstanceStock_Throws_OnUnauthorizedOrForbidden(HttpStatusCode code, string expectedMessage)
    {
        // Arrange
        var response = new HttpResponseMessage(code);
        var service = CreateService(response);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => service.GetEventTaskInstanceStock(7, "user", "pass"));
        Assert.Equal(expectedMessage, ex.Message);
    }

    [Theory]
    [InlineData("", "está vacía")]       
    [InlineData("not-a-json", null)]      
    public async Task GetEventTaskInstanceStock_Throws_OnEmptyOrInvalidJson(string responseBody, string? expectedMessageFragment)
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
        };
        var service = CreateService(response);

        // Act & Assert
        if (!string.IsNullOrEmpty(expectedMessageFragment))
        {
            var ex = await Assert.ThrowsAsync<Exception>(() => service.GetEventTaskInstanceStock(1, "user", "pass"));
            Assert.Contains(expectedMessageFragment, ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            await Assert.ThrowsAsync<System.Text.Json.JsonException>(() => service.GetEventTaskInstanceStock(1, "user", "pass"));
        }
    }

    [Fact]
    public async Task GetEventTaskInstanceStock_ThrowsArgumentException_WhenTaskInstanceIdIsInvalid()
    {
        // Arrange: response doesn't matter because exception should be thrown before HTTP call
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]", Encoding.UTF8, "application/json")
        };
        var service = CreateService(response);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetEventTaskInstanceStock(0, "user", "pass"));
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetEventTaskInstanceStock(-5, "user", "pass"));
    }

    [Fact]
    public async Task GetEventTaskInstanceStock_ThrowsHttpRequestException_OnNotFound()
    {
        // Arrange: API returns 404
        var response = new HttpResponseMessage(HttpStatusCode.NotFound);
        var service = CreateService(response);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => service.GetEventTaskInstanceStock(123, "user", "pass"));
    }

    // Tests para GetEventTaskInstanceById

    [Fact]
    public async Task GetEventTaskInstanceById_ReturnsDto_WhenResponseIsSuccessful()
    {
        var dto = new EventTaskInstanceDto { Id = 123, State = "Finished" }; 
        var json = System.Text.Json.JsonSerializer.Serialize(dto);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var service = CreateService(response);
        var result = await service.GetEventTaskInstanceById(123, "user", "pass");
        Assert.NotNull(result);
        Assert.Equal(123, result.Id);
        Assert.Equal("Finished", result.State);
    }

    [Theory]
    [InlineData("")]    // empty body -> method returns null
    [InlineData("[]")]  // array body -> method returns null because it doesn't start with '{'
    public async Task GetEventTaskInstanceById_ReturnsNull_OnEmptyOrArrayBody(string responseBody)
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
        };

        var service = CreateService(response);

        // Act
        var result = await service.GetEventTaskInstanceById(5, "user", "pass");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetEventTaskInstanceById_ThrowsJsonException_OnMalformedJson()
    {
        // Arrange: starts with '{' so method attempts deserialization -> JsonException expected
        var badJson = "{ this_is: not valid json }";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(badJson, Encoding.UTF8, "application/json")
        };

        var service = CreateService(response);

        // Act & Assert
        await Assert.ThrowsAsync<System.Text.Json.JsonException>(() => service.GetEventTaskInstanceById(7, "user", "pass"));
    }

    // Tests sobre GetEventTaskInstanceActions

    [Fact]
    public async Task GetEventTaskInstanceActions_ReturnsList_OnSuccess()
    {
        // Arrange
        var expected = new List<EventTaskActionDto>
        {
            new EventTaskActionDto { Id = 1, Name = "Action1" },
            new EventTaskActionDto { Id = 2, Name = "Action2" }
        };

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(expected), Encoding.UTF8, "application/json")
        };

        var service = CreateService(response);

        // Act
        var result = await service.GetEventTaskInstanceActions(5, "user", "pass");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("Action1", result[0].Name);
    }

    [Fact]
    public async Task GetEventTaskInstanceById_ReturnsNull_OnNotFound()
    {
        // Arrange: API returns 404
        var response = new HttpResponseMessage(HttpStatusCode.NotFound);
        var service = CreateService(response);

        // Act
        var result = await service.GetEventTaskInstanceById(999, "user", "pass");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetEventTaskInstanceActions_ThrowsArgumentException_WhenIdIsInvalid()
    {
        var service = CreateService(new HttpResponseMessage(HttpStatusCode.OK));

        await Assert.ThrowsAsync<ArgumentException>(() => service.GetEventTaskInstanceActions(0, "user", "pass"));
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetEventTaskInstanceActions(-10, "user", "pass"));
    }

    [Theory]
    [InlineData("", typeof(Exception), "La respuesta de la API está vacía.")]
    [InlineData("[{invalid-json}]", typeof(JsonException), "")]
    public async Task GetEventTaskInstanceActions_Throws_OnEmptyOrInvalidJson(string responseBody, Type expectedException, string expectedMessage)
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
        };
        var service = CreateService(response);

        // Act & Assert
        var ex = await Assert.ThrowsAsync(expectedException, () => service.GetEventTaskInstanceActions(1, "user", "pass"));
        if (expectedMessage != "")
            Assert.Equal(expectedMessage, ex.Message);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "No tienes permisos: token inválido o expirado (401 Unauthorized).")]
    [InlineData(HttpStatusCode.Forbidden, "No tienes permisos para acceder a este recurso (403 Forbidden).")]
    public async Task GetEventTaskInstanceActions_Throws_OnUnauthorizedOrForbidden(HttpStatusCode code, string expectedMessage)
    {
        // Arrange
        var response = new HttpResponseMessage(code);
        var service = CreateService(response);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => service.GetEventTaskInstanceActions(3, "user", "pass"));
        Assert.Equal(expectedMessage, ex.Message);
    }

    [Fact]
    public async Task GetEventTaskInstanceActions_BuildsCorrectUrl_AndSendsAuthHeader()
    {
        // Arrange
        var actions = new List<EventTaskActionDto> { new EventTaskActionDto { Id = 1, Name = "A1" } };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(actions), Encoding.UTF8, "application/json")
        };

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) =>
            {
                Assert.Contains("eventtaskinstance/42/actions", req.RequestUri!.AbsoluteUri);
                Assert.NotNull(req.Headers.Authorization);
                Assert.Equal("Bearer", req.Headers.Authorization!.Scheme);
                Assert.Equal("test-token", req.Headers.Authorization.Parameter);
            })
            .ReturnsAsync(response);

        var httpClient = new HttpClient(handlerMock.Object);
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var mockAuthService = new Mock<ISondaAuthService>();
        mockAuthService.Setup(x => x.GetUserTokenAMAsync(It.IsAny<string>(), It.IsAny<string>()))
                       .ReturnsAsync("test-token");

        var apiConfig = new ApiConfig
        {
            BaseUrl = new BaseUrlConfig { UrlAM = "http://localhost/api/" },
            EndpointsAM = new Dictionary<string, Dictionary<string, string>>
            {
                ["EventTaskInstance"] = new Dictionary<string, string>
                {
                    ["GetActions"] = "eventtaskinstance/{taskInstanceId}/actions"
                }
            }
        };
        var options = Options.Create(apiConfig);

        var service = new SondaAMService(httpClientFactoryMock.Object, mockAuthService.Object, options);

        // Act
        var result = await service.GetEventTaskInstanceActions(42, "user", "pass");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("A1", result[0].Name);
    }

    // Tests para GetEventTaskInstances

    [Fact]
    public async Task GetEventTaskInstances_ReturnsList_OnSuccess()
    {
        // Arrange
        var expected = new EventTaskInstanceApiResponse
        {
            Results = new List<EventTaskInstanceDto>
            {
                new EventTaskInstanceDto { Id = 1, State = "State1" },
                new EventTaskInstanceDto { Id = 2, State = "State2" }
            }
        };

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(expected), Encoding.UTF8, "application/json")
        };

        var service = CreateService(response);

        // Act
        var result = await service.GetEventTaskInstances("2025-01-01", 2, "search", 10, "pending", "name", 3, 1, 20, true, false, "user", "pass");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("State1", result[0].State);
    }

    [Theory]
    [InlineData("", typeof(Exception), "La respuesta de la API no es JSON válido. Respuesta: ")]
    [InlineData("invalid-json", typeof(Exception), "La respuesta de la API no es JSON válido. Respuesta: invalid-json")]
    public async Task GetEventTaskInstances_Throws_OnEmptyOrInvalidJson(string responseBody, Type expectedException, string expectedMessage)
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
        };

        var service = CreateService(response);

        // Act & Assert
        var ex = await Assert.ThrowsAsync(expectedException, () => service.GetEventTaskInstances("2025-01-01", 1, "", null, "", "", null, null, null, false, false, "user", "pass"));
        Assert.StartsWith(expectedMessage, ex.Message);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "No tienes permisos: token inválido o expirado (401 Unauthorized).")]
    [InlineData(HttpStatusCode.Forbidden, "No tienes permisos para acceder a este recurso (403 Forbidden).")]
    public async Task GetEventTaskInstances_Throws_OnUnauthorizedOrForbidden(HttpStatusCode code, string expectedMessage)
    {
        // Arrange
        var response = new HttpResponseMessage(code);
        var service = CreateService(response);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => service.GetEventTaskInstances("", null, "", null, "", "", null, null, null, false, false, "user", "pass"));
        Assert.Equal(expectedMessage, ex.Message);
    }

    [Fact]
    public async Task GetEventTaskInstances_BuildsCorrectQueryString_AndSendsAuthHeader()
    {
        // Arrange
        var apiResponse = new EventTaskInstanceApiResponse
        {
            Results = new List<EventTaskInstanceDto>
            {
                new EventTaskInstanceDto { Id = 1, State = "T1" }
            }
        };

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(apiResponse), Encoding.UTF8, "application/json")
        };

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) =>
            {
                var uri = req.RequestUri!.ToString();
                Assert.Contains("dates=2025-01-01", uri);
                Assert.Contains("page=1", uri);
                Assert.Contains("queryString=query", uri);
                Assert.Contains("bundleId=5", uri);
                Assert.Contains("state=open", uri);
                Assert.Contains("sort=date", uri);
                Assert.Contains("taskTypeId=2", uri);
                Assert.Contains("groupId=10", uri);
                Assert.Contains("pageSize=25", uri);
                Assert.Contains("tasksAssignedToMe=true", uri);
                Assert.Contains("tasksPendingApproval=false", uri);

                Assert.NotNull(req.Headers.Authorization);
                Assert.Equal("Bearer", req.Headers.Authorization!.Scheme);
                Assert.Equal("test-token", req.Headers.Authorization.Parameter);
            })
            .ReturnsAsync(response);

        var httpClient = new HttpClient(handlerMock.Object);
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var mockAuthService = new Mock<ISondaAuthService>();
        mockAuthService.Setup(x => x.GetUserTokenAMAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("test-token");

        var apiConfig = new ApiConfig
        {
            BaseUrl = new BaseUrlConfig { UrlAM = "http://localhost/api/" },
            EndpointsAM = new Dictionary<string, Dictionary<string, string>>
            {
                ["EventTaskInstance"] = new Dictionary<string, string>
                {
                    ["GetAll"] = "eventtaskinstance"
                }
            }
        };
        var options = Options.Create(apiConfig);

        var service = new SondaAMService(httpClientFactoryMock.Object, mockAuthService.Object, options);

        // Act
        var result = await service.GetEventTaskInstances("2025-01-01", 1, "query", 5, "open", "date", 2, 10, 25, true, false, "user", "pass");

        // Assert
        Assert.Single(result);
        Assert.Equal("T1", result[0].State);
    }

    [Fact]
    public async Task GetEventTaskInstances_ThrowsHttpRequestException_OnNotFound()
    {
        // Arrange: API devuelve 404
        var response = new HttpResponseMessage(HttpStatusCode.NotFound);
        var service = CreateService(response);

        // Act & Assert: hoy EnsureSuccessStatusCode() provoca HttpRequestException
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            service.GetEventTaskInstances("", null, "", null, "", "", null, null, null, false, false, "user", "pass"));
    }

    // Tests para GetAssetBasicData

    [Fact]
    public async Task GetAssetsBasicData_ShouldReturnBasicDataList_WhenResponseIsSuccessful()
    {
        var basicDataList = new List<AssetDto>
        {
            new AssetDto { Id = "1", Name = "Asset 1" },
            new AssetDto { Id = "2", Name = "Asset 2" }
        };
        var payload = new { results = basicDataList };
        var json = System.Text.Json.JsonSerializer.Serialize(payload);

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        var service = CreateService(response);
        var result = await service.GetAssetsBasicData(1, null, 10, 5, "user", "pass");
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("Asset 1", result[0].Name);
        Assert.Equal("Asset 2", result[1].Name);
    }

    [Theory]
    [InlineData(null, 10, 5, "page")]
    [InlineData(1, null, 5, "pageSize")]
    [InlineData(1, 10, null, "bundleId")]
    public async Task GetAssetsBasicData_ThrowsArgumentException_WhenRequiredParamMissing(int? page, int? pageSize, int? bundleId, string paramName)
    {
        var service = CreateService(new HttpResponseMessage(HttpStatusCode.OK));
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.GetAssetsBasicData(page, null, pageSize, bundleId, "user", "pass"));
        Assert.Contains(paramName, ex.ParamName ?? ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "No tienes permisos: token inválido o expirado (401 Unauthorized).")]
    [InlineData(HttpStatusCode.Forbidden, "No tienes permisos para acceder a este recurso (403 Forbidden).")]
    public async Task GetAssetsBasicData_ThrowsException_WhenAuthFails(HttpStatusCode statusCode, string expectedMessage)
    {
        var service = CreateService(new HttpResponseMessage(statusCode));
        var ex = await Assert.ThrowsAsync<Exception>(() => service.GetAssetsBasicData(1, null, 10, 5, "user", "pass"));
        Assert.Equal(expectedMessage, ex.Message);
    }

    [Fact]
    public async Task GetAssetsBasicData_ShouldReturnNull_WhenResponseIsNotFound()
    {
        var service = CreateService(new HttpResponseMessage(HttpStatusCode.NotFound));
        var result = await service.GetAssetsBasicData(1, null, 10, 5, "user", "pass");
        Assert.Null(result);
    }

    [Theory]
    [InlineData("Invalid JSON")]
    [InlineData("")]
    public async Task GetAssetsBasicData_ShouldThrowException_WhenBodyIsInvalidorEmptyJson(string content)
    {
        var service = CreateService(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(content)
        });
        var ex = await Assert.ThrowsAsync<Exception>(() => service.GetAssetsBasicData(1, null, 10, 5, "user", "pass"));
        Assert.Equal("La respuesta de la API no es JSON válido. Respuesta: " + content, ex.Message);
    }

    [Fact]
    public async Task GetAssetsBasicData_CallCorrectUrl()
    {
        var basicDataList = new List<AssetDto>
        {
            new AssetDto { Id = "1", Name = "Asset 1" },
            new AssetDto { Id = "2", Name = "Asset 2" }
        };
        var payload = new { results = basicDataList };
        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        string? capturedUrl = null;
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) =>
            {
                capturedUrl = req.RequestUri?.ToString();
            })
            .ReturnsAsync(response);
        var httpClient = new HttpClient(handlerMock.Object);
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var mockAuthService = new Mock<ISondaAuthService>();
        mockAuthService.Setup(x => x.GetUserTokenAMAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("test-token");
        var apiConfig = new ApiConfig
        {
            BaseUrl = new BaseUrlConfig { UrlAM = "http://localhost/api/" },
            EndpointsAM = new Dictionary<string, Dictionary<string, string>>
            {
                ["Asset"] = new Dictionary<string, string>
                {
                    ["GetAssets"] = "assets",
                    ["GetById"] = "assets/get",
                    ["GetAssetsBasicData"] = "assets/basic"
                }
            }
        };
        var options = Options.Create<ApiConfig>(apiConfig);
        var service = new SondaAMService(httpClientFactoryMock.Object, mockAuthService.Object, options);
        var result = await service.GetAssetsBasicData(1, null, 10, 5, "user", "pass");
        Assert.Equal("http://localhost/api/assets/basic?page=1&pageSize=10&bundleId=5", capturedUrl);
    }

    // Tests sobre GetLinkedAssets

    [Fact]
    public async Task GetLinkedAssets_ShouldReturnBasicDataList_WhenResponseIsSuccessful()
    {
        var assets = new List<AssetDto>
        {
            new AssetDto { Id = "1", Name = "Asset"}
        };
        var payload = new { results = assets };
        var json = System.Text.Json.JsonSerializer.Serialize(payload);

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        var service = CreateService(response);
        var result = await service.GetLinkedAssets(1, null, "sort", 5, "user", "pass");
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Asset", result[0].Name);
    }

    [Fact]
    public async Task GetLinkedAssets_ShouldReturnNull_WhenResponseIsNotFound()
    {
        var service = CreateService(new HttpResponseMessage(HttpStatusCode.NotFound));
        var result = await service.GetLinkedAssets(1, null, "sort", 5, "user", "pass");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetLinkedAssets_CallCorrectUrl()
    {
        var basicDataList = new List<AssetDto>
        {
            new AssetDto { Id = "1", Name = "Asset 1" },
            new AssetDto { Id = "2", Name = "Asset 2" }
        };
        var payload = new { results = basicDataList };
        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        string? capturedUrl = null;
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) =>
            {
                capturedUrl = req.RequestUri?.ToString();
            })
            .ReturnsAsync(response);
        var httpClient = new HttpClient(handlerMock.Object);
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var mockAuthService = new Mock<ISondaAuthService>();
        mockAuthService.Setup(x => x.GetUserTokenAMAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("test-token");
        var apiConfig = new ApiConfig
        {
            BaseUrl = new BaseUrlConfig { UrlAM = "http://localhost/api/" },
            EndpointsAM = new Dictionary<string, Dictionary<string, string>>
            {
                ["Asset"] = new Dictionary<string, string>
                {
                    ["GetAssets"] = "assets",
                    ["GetById"] = "assets/get",
                    ["GetLinkedAssets"] = "assets/linked"
                }
            }
        };
        var options = Options.Create<ApiConfig>(apiConfig);
        var service = new SondaAMService(httpClientFactoryMock.Object, mockAuthService.Object, options);
        var result = await service.GetLinkedAssets(1, null, "sort", 5, "user", "pass");
        Assert.Equal("http://localhost/api/assets/linked?page=1&sort=sort&pageSize=5", capturedUrl);
    }

    [Theory]
    [InlineData("Invalid JSON")]
    [InlineData("")]
    public async Task GetLinkedAssets_ShouldThrowException_WhenBodyIsInvalidorEmptyJson(string content)
    {
        var service = CreateService(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(content)
        });
        var ex = await Assert.ThrowsAsync<Exception>(() => service.GetLinkedAssets(1, null, "sort", 5, "user", "pass"));
        Assert.Equal("La respuesta de la API no es JSON válido. Respuesta: " + content, ex.Message);
    }



}

