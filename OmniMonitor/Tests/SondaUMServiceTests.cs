using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using OmniMonitor.Server.Configuration;
using OmniMonitor.Shared.Dtos;
using Xunit;

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
            BaseUrl = new BaseUrlConfig { UrlUM = "http://localhost/api/um/" },
            EndpointsUM = new Dictionary<string, Dictionary<string, string>>
            {
                ["Zone"] = new Dictionary<string, string>
                {
                    ["Zones"] = "zones",
                    ["GetById"] = "zones/get"
                }
            }
        };
        var options = Options.Create<ApiConfig>(apiConfig);

        return new SondaUMService(httpClientFactoryMock.Object, mockAuthService.Object, options);
    }

    [Fact]
    public async Task TestUMAPI_ReturnsToken()
    {
        // Arrange
        string expectedToken = "test-um-token-123";
        var response = new HttpResponseMessage(HttpStatusCode.OK); // No necesitamos respuesta HTTP para este test
        var service = CreateService(response, expectedToken);

        // Act
        var result = await service.TestUMAPI("user", "pass");

        // Assert
        Assert.Equal(expectedToken, result);
    }

    [Fact]
    public async Task GetAllZones_ReturnsZonesList()
    {
        // Arrange
        var zones = new List<Zone>
        {
            new Zone { Id = 1, Name = "Zone1" },
            new Zone { Id = 2, Name = "Zone2" }
        };
        var json = System.Text.Json.JsonSerializer.Serialize(zones);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json)
        };
        var service = CreateService(response);

        // Act
        var result = await service.GetAllZones("user", "pass");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("Zone1", result[0].Name);
        Assert.Equal("Zone2", result[1].Name);
    }

    [Fact]
    public async Task GetAllZones_ReturnsEmptyList_WhenNotFound()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.NotFound);
        var service = CreateService(response);

        // Act
        var result = await service.GetAllZones("user", "pass");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllZones_ReturnsEmptyList_WhenNullResponse()
    {
        // Arrange
        var json = "null";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json)
        };
        var service = CreateService(response);

        // Act
        var result = await service.GetAllZones("user", "pass");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetZoneById_ReturnsZone()
    {
        // Arrange
        var zone = new Zone { Id = 5, Name = "TestZone" };
        var json = System.Text.Json.JsonSerializer.Serialize(zone);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json)
        };
        var service = CreateService(response);

        // Act
        var result = await service.GetZoneById(5, "user", "pass");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.Id);
        Assert.Equal("TestZone", result.Name);
    }

    [Fact]
    public async Task GetZoneById_ReturnsNull_WhenNotFound()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.NotFound);
        var service = CreateService(response);

        // Act
        var result = await service.GetZoneById(99, "user", "pass");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetZoneById_ReturnsNull_WhenNullResponse()
    {
        // Arrange
        var json = "null";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json)
        };
        var service = CreateService(response);

        // Act
        var result = await service.GetZoneById(10, "user", "pass");

        // Assert
        Assert.Null(result);
    }
}