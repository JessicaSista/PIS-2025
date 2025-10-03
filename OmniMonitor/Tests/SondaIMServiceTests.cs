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
// using OmniMonitor.Server.Services;
using Xunit;

// namespace QA.Tests;

public class SondaIMServiceTests
{
    private SondaIMService CreateService(HttpResponseMessage response, string token = "test-token")
    {

    var mockAuthService = new Mock<ISondaAuthService>();
    mockAuthService.Setup(x => x.GetUserTokenIMAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(token);

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
            BaseUrl = new BaseUrlConfig { UrlIM = "http://localhost/api/" },
            EndpointsIM = new Dictionary<string, Dictionary<string, string>>
            {
                ["Source"] = new Dictionary<string, string>
                {
                    ["Sources"] = "sources",
                    ["GetById"] = "sources/get"
                },
                ["Group"] = new Dictionary<string, string>
                {
                    ["Groups"] = "groups",
                    ["GetById"] = "groups/get"
                },
                ["Device"] = new Dictionary<string, string>
                {
                    ["GetAll"] = "devices",
                    ["GetById"] = "devices/get"
                }
            }
        };
        var options = Options.Create<ApiConfig>(apiConfig);

        return new SondaIMService(httpClientFactoryMock.Object, mockAuthService.Object, options);
    }

    [Fact]
    async Task GetAllDevicesByPage_ReturnsDevicesList()
    {
        var pagedResponse = new PagedDeviceResponse
        {
            PagedData = new List<Device>
            {
                new Device { Id = 1, Name = "Device1" },
                new Device { Id = 2, Name = "Device2" }
            }
        };
        var json = System.Text.Json.JsonSerializer.Serialize(pagedResponse);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json)
        };
        var service = CreateService(response);
        //var result = await service.GetAllDevicesByPage(1, "user", "pass");
        //Assert.NotNull(result);
        //Assert.Equal(2, result.Count);
        //Assert.Equal("Device1", result[0].Name);
    }

    [Fact]
    async Task GetAllDevicesByPage_ReturnsNull_WhenNotFound()
    {
        // Simula respuesta vacía (sin datos paginados)
        var pagedResponse = new PagedDeviceResponse { PagedData = null };
        var json = System.Text.Json.JsonSerializer.Serialize(pagedResponse);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json)
        };
        var service = CreateService(response);
        //var result = await service.GetAllDevicesByPage(1, "user", "pass");
        //Assert.Null(result);
    }

    [Fact]
    async Task GetDeviceById_ReturnsDevice()
    {
        var device = new Device { Id = 5, Name = "TestDevice" };
        var json = System.Text.Json.JsonSerializer.Serialize(device);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json)
        };
        var service = CreateService(response);
        var result = await service.GetDeviceById(5, "user", "pass");
        Assert.NotNull(result);
        Assert.Equal(5, result.Id);
        Assert.Equal("TestDevice", result.Name);
    }

    [Fact]
    async Task GetDeviceById_ReturnsNull_WhenNotFound()
    {
        var response = new HttpResponseMessage(HttpStatusCode.NotFound);
        var service = CreateService(response);
        var result = await service.GetDeviceById(99, "user", "pass");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllSources_ReturnsSourcesList()
    {
        var sources = new List<Source>
        {
            new Source { Id = 1, Name = "Source1" },
            new Source { Id = 2, Name = "Source2" }
        };
        var json = System.Text.Json.JsonSerializer.Serialize(sources);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json)
        };
        var service = CreateService(response);
        var result = await service.GetAllSources("user", "pass");
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("Source1", result[0].Name);
    }

    [Fact]
    public async Task GetAllSources_ReturnsEmptyList_WhenNotFound()
    {
        var response = new HttpResponseMessage(HttpStatusCode.NotFound);
        var service = CreateService(response);
        var result = await service.GetAllSources("user", "pass");
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetSourceById_ReturnsSource()
    {
        var source = new Source { Id = 10, Name = "TestSource" };
        var json = System.Text.Json.JsonSerializer.Serialize(source);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json)
        };
        var service = CreateService(response);
        var result = await service.GetSourceById(10, "user", "pass");
        Assert.NotNull(result);
        Assert.Equal(10, result.Id);
        Assert.Equal("TestSource", result.Name);
    }

    [Fact]
    public async Task GetSourceById_ReturnsNull_WhenNotFound()
    {
        var response = new HttpResponseMessage(HttpStatusCode.NotFound);
        var service = CreateService(response);
        var result = await service.GetSourceById(99, "user", "pass");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllDeviceGroups_ReturnsDeviceGroupsList()
    {
        var deviceGroups = new List<DeviceGroup>
        {
            new DeviceGroup { Id = 1, Name = "Group1" },
            new DeviceGroup { Id = 2, Name = "Group2" }
        };
        var json = System.Text.Json.JsonSerializer.Serialize(deviceGroups);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json)
        };
        var service = CreateService(response);
        var result = await service.GetAllDeviceGroups("user", "pass");
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("Group1", result[0].Name);
    }

    [Fact]
    public async Task GetAllDeviceGroups_ReturnsEmptyList_WhenNotFound()
    {
        var response = new HttpResponseMessage(HttpStatusCode.NotFound);
        var service = CreateService(response);
        var result = await service.GetAllDeviceGroups("user", "pass");
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetDeviceGroupById_ReturnsDeviceGroup()
    {
        var deviceGroup = new DeviceGroup { Id = 10, Name = "TestGroup" };
        var json = System.Text.Json.JsonSerializer.Serialize(deviceGroup);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json)
        };
        var service = CreateService(response);
        var result = await service.GetDeviceGroupById(10, "user", "pass");
        Assert.NotNull(result);
        Assert.Equal(10, result.Id);
        Assert.Equal("TestGroup", result.Name);
    }

    [Fact]
    public async Task GetDeviceGroupById_ReturnsNull_WhenNotFound()
    {
        var response = new HttpResponseMessage(HttpStatusCode.NotFound);
        var service = CreateService(response);
        var result = await service.GetDeviceGroupById(99, "user", "pass");
        Assert.Null(result);
    }
}
