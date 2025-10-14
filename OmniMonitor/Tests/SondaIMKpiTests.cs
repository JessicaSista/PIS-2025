using Xunit;
using Moq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using OmniMonitor.Server.Configuration;
using OmniMonitor.Server.Services;
using OmniMonitor.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Collections.Generic;
using Moq.Protected;
using System;
using AutoFixture;
using Microsoft.Extensions.Logging;
    
public class SondaIMKpiTests
{
    // Solo si SondaIMService recibe ILogger<SondaIMService> por DI
    private readonly Mock<ILogger<SondaIMService>> _loggerMock = new Mock<ILogger<SondaIMService>>();
    
    [Fact]
    public async Task GetSSDeviceCount_ReturnsCount()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("42")
        };
        var service = SondaTestFactory.CreateIMService(response, out _);

        var result = await service.GetSSDeviceCount("user", "pass");

        Assert.Equal(42, result);
    }

    [Fact]
    public async Task GetSSDeviceCount_ReturnsZero_WhenNotFound()
    {
        var response = new HttpResponseMessage(HttpStatusCode.NotFound);
        var service = SondaTestFactory.CreateIMService(response, out _);

        var result = await service.GetSSDeviceCount("user", "pass");

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task GetSSDataStatus_ReturnsObject()
    {
        var fixture = new Fixture();
        var dto = fixture.Create<DeviceDataStatusResponse>();
        var json = JsonSerializer.Serialize(dto);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json)
        };
        var service = SondaTestFactory.CreateIMService(response, out _);

        var result = await service.GetSSDataStatus("user", "pass");

        Assert.NotNull(result);
        Assert.Equal(dto.CountDeviceData, result.CountDeviceData);
        Assert.Equal(dto.CountHistoricDeviceData, result.CountHistoricDeviceData);
    }

    [Fact]
    public async Task GetSSDataStatus_ReturnsNull_WhenNotFound()
    {
        var response = new HttpResponseMessage(HttpStatusCode.NotFound);
        var service = SondaTestFactory.CreateIMService(response, out _);

        var result = await service.GetSSDataStatus("user", "pass");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetSSDataStatus_ThrowsJsonException_OnMalformedJson()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{ invalid json }")
        };
        var service = SondaTestFactory.CreateIMService(response, out _);

        await Assert.ThrowsAsync<JsonException>(() => service.GetSSDataStatus("user", "pass"));
    }

    [Theory]
    [InlineData(null, "pass")]
    [InlineData("user", null)]
    [InlineData("", "pass")]
    [InlineData("user", "")]
    public async Task GetSSDeviceCount_ThrowsArgumentException_OnInvalidArgs_Service(string username, string password)
    {
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest);
        var service = SondaTestFactory.CreateIMService(response, out _);

        await Assert.ThrowsAsync<ArgumentException>(() => service.GetSSDeviceCount(username, password));
    }

    [Fact]
    public async Task GetSSDeviceCount_BuildsCorrectUrl()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("42")
        };
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response)
            .Verifiable();

        var httpClient = new HttpClient(handlerMock.Object);
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var mockAuthService = new Mock<ISondaAuthService>();
        mockAuthService.Setup(x => x.GetUserTokenIMAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("token");

        var apiConfig = new ApiConfig
        {
            BaseUrl = new BaseUrlConfig { UrlIM = "http://localhost/" },
            EndpointsIM = new Dictionary<string, Dictionary<string, string>>
            {
                { "SystemStatus", new Dictionary<string, string> {
                    { "DeviceCount", "api/devicecount" }
                } }
            }
        };
        var options = Options.Create(apiConfig);

        var service = new SondaIMService(httpClientFactoryMock.Object, mockAuthService.Object, options);

        await service.GetSSDeviceCount("user", "pass");

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.RequestUri.ToString() == "http://localhost/api/devicecount"
            ),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task GetSSDeviceCount_ReturnsZero_OnInternalServerError()
    {
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        var service = SondaTestFactory.CreateIMService(response, out _);

        var result = await service.GetSSDeviceCount("user", "pass");
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task GetSSDataStatus_ReturnsNull_OnInternalServerError()
    {
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        var service = SondaTestFactory.CreateIMService(response, out _);

        var result = await service.GetSSDataStatus("user", "pass");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetSSDeviceCount_AddsAuthorizationHeader()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("42")
        };
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Headers.Authorization != null &&
                    req.Headers.Authorization.Scheme == "Bearer" &&
                    req.Headers.Authorization.Parameter == "token"
                ),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response)
            .Verifiable();

        var httpClient = new HttpClient(handlerMock.Object);
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var mockAuthService = new Mock<ISondaAuthService>();
        mockAuthService.Setup(x => x.GetUserTokenIMAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("token");

        var apiConfig = new ApiConfig
        {
            BaseUrl = new BaseUrlConfig { UrlIM = "http://localhost/" },
            EndpointsIM = new Dictionary<string, Dictionary<string, string>>
            {
                { "SystemStatus", new Dictionary<string, string> {
                    { "DeviceCount", "api/devicecount" }
                } }
            }
        };
        var options = Options.Create(apiConfig);

        var service = new SondaIMService(httpClientFactoryMock.Object, mockAuthService.Object, options);

        await service.GetSSDeviceCount("user", "pass");

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task GetSSDeviceCount_CompletesWithinTimeout()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("42")
        };
        var service = SondaTestFactory.CreateIMService(response, out _);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var task = service.GetSSDeviceCount("user", "pass");
        var completed = await Task.WhenAny(task, Task.Delay(3000, cts.Token));
        Assert.True(completed == task, "El método tardó demasiado en responder.");
    }

    [Fact]
    public async Task GetSSDeviceCount_ReturnsZero_OnHttpClientException()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var httpClient = new HttpClient(handlerMock.Object);
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var mockAuthService = new Mock<ISondaAuthService>();
        mockAuthService.Setup(x => x.GetUserTokenIMAsync(It.IsAny<string>(), It.IsAny<string>()))
                       .ReturnsAsync("token");

        var apiConfig = new ApiConfig
        {
            BaseUrl = new BaseUrlConfig { UrlIM = "http://localhost/" },
            EndpointsIM = new Dictionary<string, Dictionary<string, string>>
            {
                { "SystemStatus", new Dictionary<string, string> { { "DeviceCount", "api/devicecount" } } }
            }
        };
        var options = Options.Create(apiConfig);
        var service = new SondaIMService(httpClientFactoryMock.Object, mockAuthService.Object, options);

        var result = await service.GetSSDeviceCount("user", "pass");
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task GetSSDeviceCount_CallsAuthServiceOnce()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("42")
        };
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var httpClient = new HttpClient(handlerMock.Object);
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var mockAuth = new Mock<ISondaAuthService>();
        mockAuth.Setup(x => x.GetUserTokenIMAsync("user", "pass")).ReturnsAsync("token");

        var options = Options.Create(new ApiConfig
        {
            BaseUrl = new BaseUrlConfig { UrlIM = "http://localhost/" },
            EndpointsIM = new Dictionary<string, Dictionary<string, string>>
            {
                { "SystemStatus", new Dictionary<string, string> { { "DeviceCount", "api/devicecount" } } }
            }
        });

        var service = new SondaIMService(httpClientFactory.Object, mockAuth.Object, options);
        await service.GetSSDeviceCount("user", "pass");

        mockAuth.Verify(x => x.GetUserTokenIMAsync("user", "pass"), Times.Once);
    }

    [Fact]
    public async Task GetSSDeviceCount_SendsCorrectContentTypeHeader()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("42")
        };
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Content != null &&
                    req.Content.Headers.ContentType != null &&
                    req.Content.Headers.ContentType.MediaType == "application/json"
                ),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response)
            .Verifiable();

        var httpClient = new HttpClient(handlerMock.Object);
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var mockAuthService = new Mock<ISondaAuthService>();
        mockAuthService.Setup(x => x.GetUserTokenIMAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("token");

        var apiConfig = new ApiConfig
        {
            BaseUrl = new BaseUrlConfig { UrlIM = "http://localhost/" },
            EndpointsIM = new Dictionary<string, Dictionary<string, string>>
            {
                { "SystemStatus", new Dictionary<string, string> { { "DeviceCount", "api/devicecount" } } }
            }
        };
        var options = Options.Create(apiConfig);

        var service = new SondaIMService(httpClientFactoryMock.Object, mockAuthService.Object, options);

        await service.GetSSDeviceCount("user", "pass");

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task GetSSDeviceCount_UsesHttpGetMethod()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("42")
        };
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get
                ),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response)
            .Verifiable();

        var httpClient = new HttpClient(handlerMock.Object);
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var mockAuthService = new Mock<ISondaAuthService>();
        mockAuthService.Setup(x => x.GetUserTokenIMAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("token");

        var apiConfig = new ApiConfig
        {
            BaseUrl = new BaseUrlConfig { UrlIM = "http://localhost/" },
            EndpointsIM = new Dictionary<string, Dictionary<string, string>>
            {
                { "SystemStatus", new Dictionary<string, string> { { "DeviceCount", "api/devicecount" } } }
            }
        };
        var options = Options.Create(apiConfig);

        var service = new SondaIMService(httpClientFactoryMock.Object, mockAuthService.Object, options);

        await service.GetSSDeviceCount("user", "pass");

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task GetSSDeviceCount_ReturnsZero_WhenTokenIsNull()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("42")
        };
        var authMock = new Mock<ISondaAuthService>();
        authMock.Setup(x => x.GetUserTokenIMAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync((string)null);

        var service = SondaTestFactory.CreateIMService(response, out _, authMock);

        var result = await service.GetSSDeviceCount("user", "pass");
        Assert.Equal(0, result);
    }
}

// ----------- TESTS DE CONTROLADOR -----------
public class SondaMainControllerKpiUnitTests
{
    [Fact]
    public async Task GetDeviceCount_ReturnsOkWithValue()
    {
        var mockIMService = new Mock<ISondaIMService>();
        mockIMService.Setup(s => s.GetSSDeviceCount(It.IsAny<string>(), It.IsAny<string>()))
                     .ReturnsAsync(42);
        var mockUMService = new Mock<ISondaUMService>();
        var controller = new SondaMainController(mockIMService.Object, mockUMService.Object);

        var result = await controller.GetDeviceCount();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(42, okResult.Value);
    }

    [Fact]
    public async Task GetDeviceCount_Returns500OnException()
    {
        var mockIMService = new Mock<ISondaIMService>();
        mockIMService.Setup(s => s.GetSSDeviceCount(It.IsAny<string>(), It.IsAny<string>()))
                     .ThrowsAsync(new Exception("Token inválido"));
        var mockUMService = new Mock<ISondaUMService>();
        var controller = new SondaMainController(mockIMService.Object, mockUMService.Object);

        var result = await controller.GetDeviceCount();
        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, objectResult.StatusCode);
        Assert.Contains("Token inválido", objectResult.Value.ToString());
    }

    [Fact]
    public async Task GetDataStatus_ReturnsOkWithDto()
    {
        var expected = new DeviceDataStatusResponse { CountDeviceData = 10, CountHistoricDeviceData = 5 };
        var mockIMService = new Mock<ISondaIMService>();
        mockIMService.Setup(s => s.GetSSDataStatus(It.IsAny<string>(), It.IsAny<string>()))
                     .ReturnsAsync(expected);
        var mockUMService = new Mock<ISondaUMService>();
        var controller = new SondaMainController(mockIMService.Object, mockUMService.Object);

        var result = await controller.GetDataStatus();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expected, okResult.Value);
    }

    [Fact]
    public async Task GetDataStatus_Returns500OnException()
    {
        var mockIMService = new Mock<ISondaIMService>();
        mockIMService.Setup(s => s.GetSSDataStatus(It.IsAny<string>(), It.IsAny<string>()))
                     .ThrowsAsync(new Exception("error"));
        var mockUMService = new Mock<ISondaUMService>();
        var controller = new SondaMainController(mockIMService.Object, mockUMService.Object);

        var result = await controller.GetDataStatus();

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, objectResult.StatusCode);
        Assert.Contains("error", objectResult.Value.ToString());
    }

    [Fact]
    public async Task GetDataStatus_ReturnsOkNull_WhenServiceReturnsNull()
    {
        var mockIMService = new Mock<ISondaIMService>();
        mockIMService.Setup(s => s.GetSSDataStatus(It.IsAny<string>(), It.IsAny<string>()))
                     .ReturnsAsync((DeviceDataStatusResponse)null);
        var mockUMService = new Mock<ISondaUMService>();
        var controller = new SondaMainController(mockIMService.Object, mockUMService.Object);

        var result = await controller.GetDataStatus();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Null(okResult.Value);
    }

    [Fact]
    public async Task GetDeviceCount_ReturnsActionResultOfInt()
    {
        var mockIM = new Mock<ISondaIMService>();
        mockIM.Setup(s => s.GetSSDeviceCount(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(10);
        var mockUM = new Mock<ISondaUMService>();
        var controller = new SondaMainController(mockIM.Object, mockUM.Object);

        var result = await controller.GetDeviceCount();

        Assert.IsType<ActionResult<int>>(result);
    }

    [Fact]
    public async Task GetDeviceCount_CallsServiceOnce()
    {
        var mockIM = new Mock<ISondaIMService>();
        mockIM.Setup(s => s.GetSSDeviceCount(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(42);

        var controller = new SondaMainController(mockIM.Object, Mock.Of<ISondaUMService>());
        await controller.GetDeviceCount();

        mockIM.Verify(s => s.GetSSDeviceCount(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }
}