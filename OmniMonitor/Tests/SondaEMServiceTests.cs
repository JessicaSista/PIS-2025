using Xunit;
using Moq;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;
using OmniMonitor.Server.Configuration;
using OmniMonitor.Server.Services;
using OmniMonitor.Shared.Dtos.EM;
using Moq.Protected;

public class SondaEMServiceTests
{
    private (SondaEMService service, Mock<HttpMessageHandler> handlerMock) SetupService(
        HttpStatusCode code,
        object? content = null,
        string token = "test-token")
    {
        var response = new HttpResponseMessage(code)
        {
            Content = content != null ? new StringContent(JsonSerializer.Serialize(content)) : null
        };
        var mockAuthService = new Mock<ISondaAuthService>();
        mockAuthService.Setup(x => x.GetUserTokenEMAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(token);

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
            BaseUrl = new BaseUrlConfig { UrlEM = "http://localhost/api/" },
            EndpointsEM = new Dictionary<string, Dictionary<string, string>>
            {
                ["Event"] = new Dictionary<string, string>
                {
                    ["GetById"] = "events/get/{eventId}",
                    ["GetEvents"] = "events"
                },
                ["Alert"] = new Dictionary<string, string>
                {
                    ["GetById"] = "alerts/get/{alertId}",
                    ["GetAll"] = "alerts",
                    ["GetStored"] = "alerts/stored"
                },
                ["EventType"] = new Dictionary<string, string>
                {
                    ["GetEventTypes"] = "eventtypes"
                },
                ["Extension"] = new Dictionary<string, string>
                {
                    ["GetById"] = "extensions/get/{extensionId}",
                    ["GetAll"] = "extensions",
                    ["GetAttachedItems"] = "extensions/{extensionId}/attachments"
                },
                ["ResourceType"] = new Dictionary<string, string>
                {
                    ["GetById"] = "resources/get/{id}"
                }
            }
        };
        var options = Options.Create(apiConfig);

        var service = new SondaEMService(httpClientFactoryMock.Object, mockAuthService.Object, options);
        return (service, handlerMock);
    }

    [Fact]
    public async Task GetEventById_ReturnsEvent_WhenResponseIsSuccessful()
    {
        var eventDto = new EventDto { Id = 1, Name = "Evento Test" };
        var (service, _) = SetupService(HttpStatusCode.OK, eventDto);

        var result = await service.GetEventById(1, "user", "pass");
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Evento Test", result.Name);
    }

    [Fact]
    public async Task GetEventById_ReturnsNull_WhenNotFound()
    {
        var (service, _) = SetupService(HttpStatusCode.NotFound);

        var result = await service.GetEventById(1, "user", "pass");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetEventById_ThrowsException_WhenServerError()
    {
        var (service, _) = SetupService(HttpStatusCode.InternalServerError);

        await Assert.ThrowsAsync<HttpRequestException>(() => service.GetEventById(1, "user", "pass"));
    }

    [Fact]
    public async Task GetEvents_BuildsUrlWithPaginationAndSort()
    {
        var events = new List<EventDto> { new EventDto { Id = 1, Name = "Evento 1" } };
        var apiResponse = new { results = events };
        var (service, handlerMock) = SetupService(HttpStatusCode.OK, apiResponse);

        await service.GetEvents(2, 5, "date", "test", "user", "pass");

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.RequestUri!.ToString().Contains("page=2") &&
                req.RequestUri!.ToString().Contains("pageSize=5") &&
                req.RequestUri!.ToString().Contains("sort=date") &&
                req.RequestUri!.ToString().Contains("query=test")
            ),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task GetEvents_SendsAuthorizationHeader()
    {
        var events = new List<EventDto> { new EventDto { Id = 1, Name = "Evento 1" } };
        var apiResponse = new { results = events };
        var (service, handlerMock) = SetupService(HttpStatusCode.OK, apiResponse, token: "my-fake-token");

        await service.GetEvents(1, 10, null, null, "user", "pass");

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Headers.Authorization != null &&
                req.Headers.Authorization.Scheme == "Bearer" &&
                req.Headers.Authorization.Parameter == "my-fake-token"
            ),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task GetAlertById_ReturnsAlert_WhenResponseIsSuccessful()
    {
        var alertDto = new AlertDto { AlertId = 1, AlertName = "Alerta Test" };
        var (service, _) = SetupService(HttpStatusCode.OK, alertDto);

        var result = await service.GetAlertById(1, "user", "pass");
        Assert.NotNull(result);
        Assert.Equal(1, result.AlertId);
        Assert.Equal("Alerta Test", result.AlertName);
    }

    [Fact]
    public async Task GetAlertById_ReturnsNull_WhenNotFound()
    {
        var (service, _) = SetupService(HttpStatusCode.NotFound);

        var result = await service.GetAlertById(1, "user", "pass");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAlertById_ThrowsException_WhenServerError()
    {
        var (service, _) = SetupService(HttpStatusCode.InternalServerError);

        await Assert.ThrowsAsync<HttpRequestException>(() => service.GetAlertById(1, "user", "pass"));
    }

    [Fact]
    public async Task GetAlerts_BuildsUrlWithPaginationAndFilters()
    {
        var alerts = new List<AlertDto> { new AlertDto { AlertId = 1, AlertName = "Alerta 1" } };
        var apiResponse = new { results = alerts };
        var (service, handlerMock) = SetupService(HttpStatusCode.OK, apiResponse);

        await service.GetAlerts(2, 5, "test", "open,closed", 1.1, 2.2, 3.3, true, "date", "user", "pass");

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.RequestUri!.ToString().Contains("page=2") &&
                req.RequestUri!.ToString().Contains("pageSize=5") &&
                req.RequestUri!.ToString().Contains("query=test") &&
                req.RequestUri!.ToString().Contains("stateList=open%2Cclosed") &&
                req.RequestUri!.ToString().Contains("x=1.1") &&
                req.RequestUri!.ToString().Contains("y=2.2") &&
                req.RequestUri!.ToString().Contains("r=3.3") &&
                req.RequestUri!.ToString().Contains("forceGps=true") &&
                req.RequestUri!.ToString().Contains("sort=date")
            ),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task GetAlerts_SendsAuthorizationHeader()
    {
        var alerts = new List<AlertDto> { new AlertDto { AlertId = 1, AlertName = "Alerta 1" } };
        var apiResponse = new { results = alerts };
        var (service, handlerMock) = SetupService(HttpStatusCode.OK, apiResponse, token: "my-fake-token");

        await service.GetAlerts(1, 10, null, null, null, null, null, null, null, "user", "pass");

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Headers.Authorization != null &&
                req.Headers.Authorization.Scheme == "Bearer" &&
                req.Headers.Authorization.Parameter == "my-fake-token"
            ),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task GetStoredAlerts_BuildsUrlWithPaginationAndFilters()
    {
        var alerts = new List<AlertDto> { new AlertDto { AlertId = 1, AlertName = "Alerta 1" } };
        var apiResponse = new { results = alerts };
        var (service, handlerMock) = SetupService(HttpStatusCode.OK, apiResponse);

        await service.GetStoredAlerts(2, 5, "test", "open,closed", 1.1, 2.2, 3.3, "date", "user", "pass");

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.RequestUri!.ToString().Contains("page=2") &&
                req.RequestUri!.ToString().Contains("pageSize=5") &&
                req.RequestUri!.ToString().Contains("query=test") &&
                req.RequestUri!.ToString().Contains("stateList=open%2Cclosed") &&
                req.RequestUri!.ToString().Contains("x=1.1") &&
                req.RequestUri!.ToString().Contains("y=2.2") &&
                req.RequestUri!.ToString().Contains("r=3.3") &&
                req.RequestUri!.ToString().Contains("sort=date")
            ),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task GetEventTypes_ReturnsList_WhenResponseIsSuccessful()
    {
        var eventTypes = new List<EventTypeDto> { new EventTypeDto { Id = 1, Name = "Tipo 1" } };
        var (service, _) = SetupService(HttpStatusCode.OK, eventTypes);

        var result = await service.GetEventTypes("user", "pass");
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Tipo 1", result[0].Name);
    }

    [Fact]
    public async Task GetExtensionById_ReturnsExtension_WhenResponseIsSuccessful()
    {
        var extension = new ExtensionDtoDup { Id = 1 };
        var (service, _) = SetupService(HttpStatusCode.OK, extension);

        var result = await service.GetExtensionById(1, "user", "pass");
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task GetExtensions_BuildsUrlWithPaginationAndFilters()
    {
        var extensions = new List<ExtensionDto> { new ExtensionDto { ExtensionId = 1, ExtensionState = "Activa" } };
        var apiResponse = new { results = extensions };
        var (service, handlerMock) = SetupService(HttpStatusCode.OK, apiResponse);

        await service.GetExtensions(2, 5, "date", "test", "open", "2024-01-01", "high", "cat1", "zone1", "user", "pass");

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.RequestUri!.ToString().Contains("page=2") &&
                req.RequestUri!.ToString().Contains("pageSize=5") &&
                req.RequestUri!.ToString().Contains("sort=date") &&
                req.RequestUri!.ToString().Contains("query=test") &&
                req.RequestUri!.ToString().Contains("states=open") &&
                req.RequestUri!.ToString().Contains("dates=2024-01-01") &&
                req.RequestUri!.ToString().Contains("priorities=high") &&
                req.RequestUri!.ToString().Contains("categories=cat1") &&
                req.RequestUri!.ToString().Contains("zones=zone1")
            ),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task GetAttachedItems_ReturnsList_WhenResponseIsSuccessful()
    {
        var attachments = new List<AttachmentDto> { new AttachmentDto { AttachmentId = 1, Name = "Archivo 1" } };
        var (service, _) = SetupService(HttpStatusCode.OK, attachments);

        var result = await service.GetAttachedItems(1, "user", "pass");
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Archivo 1", result[0].Name);
    }

    [Fact]
    public async Task GetResourceById_ReturnsResource_WhenResponseIsSuccessful()
    {
        var resource = new ResourceDto { Id = 1, Name = "Recurso 1" };
        var (service, _) = SetupService(HttpStatusCode.OK, resource);

        var result = await service.GetResourceById(1, "user", "pass");
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Recurso 1", result.Name);
    }

    [Fact]
    public async Task GetEventById_ThrowsArgumentException_WhenIdIsInvalid()
    {
        var (service, _) = SetupService(HttpStatusCode.OK);
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetEventById(0, "user", "pass"));
    }

    [Fact]
    public async Task GetAlertById_ThrowsArgumentException_WhenIdIsInvalid()
    {
        var (service, _) = SetupService(HttpStatusCode.OK);
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetAlertById(0, "user", "pass"));
    }

    [Fact]
    public async Task GetExtensionById_ThrowsArgumentException_WhenIdIsInvalid()
    {
        var (service, _) = SetupService(HttpStatusCode.OK);
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetExtensionById(0, "user", "pass"));
    }

    [Fact]
    public async Task GetResourceById_ThrowsArgumentException_WhenIdIsInvalid()
    {
        var (service, _) = SetupService(HttpStatusCode.OK);
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetResourceById(0, "user", "pass"));
    }

    [Fact]
    public async Task GetEventById_ThrowsException_WhenUnauthorized()
    {
        var (service, _) = SetupService(HttpStatusCode.Unauthorized);

        var ex = await Assert.ThrowsAsync<Exception>(() => service.GetEventById(1, "user", "pass"));
        Assert.Contains("token inválido o expirado", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetEventById_ThrowsException_WhenForbidden()
    {
        var (service, _) = SetupService(HttpStatusCode.Forbidden);

        var ex = await Assert.ThrowsAsync<Exception>(() => service.GetEventById(1, "user", "pass"));
        Assert.Contains("Forbidden", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetEventById_ThrowsJsonException_WhenResponseIsInvalidJson()
    {
        var (service, _) = SetupService(HttpStatusCode.OK, "<html>not json</html>");

        await Assert.ThrowsAsync<JsonException>(() => service.GetEventById(1, "user", "pass"));
    }

    [Fact]
    public async Task GetEventById_ReturnsNull_WhenResponseIsEmpty()
    {
        var (service, _) = SetupService(HttpStatusCode.OK, "");

        var result = await service.GetEventById(1, "user", "pass");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAttachedItems_ReturnsEmptyList_WhenNotFound()
    {
        var (service, _) = SetupService(HttpStatusCode.NotFound);

        var result = await service.GetAttachedItems(1, "user", "pass");
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetEventTypes_ReturnsEmptyList_WhenResponseIsEmpty()
    {
        var (service, _) = SetupService(HttpStatusCode.OK, "");

        var result = await service.GetEventTypes("user", "pass");
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetStoredAlerts_ThrowsException_WhenUnauthorized()
    {
        var (service, _) = SetupService(HttpStatusCode.Unauthorized);

        var ex = await Assert.ThrowsAsync<Exception>(() => service.GetStoredAlerts(1, 10, null, null, null, null, null, null, "user", "pass"));
        Assert.Contains("Unauthorized", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetStoredAlerts_ThrowsException_WhenForbidden()
    {
        var (service, _) = SetupService(HttpStatusCode.Forbidden);

        var ex = await Assert.ThrowsAsync<Exception>(() => service.GetStoredAlerts(1, 10, null, null, null, null, null, null, "user", "pass"));
        Assert.Contains("Forbidden", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetExtensions_ReturnsEmptyList_WhenResponseIsEmpty()
    {
        var (service, _) = SetupService(HttpStatusCode.OK, "");

        var result = await service.GetExtensions(1, 10, null, null, null, null, null, null, null, "user", "pass");
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetExtensions_ThrowsJsonException_WhenResponseIsInvalidJson()
    {
        var (service, _) = SetupService(HttpStatusCode.OK, "<html>not json</html>");

        await Assert.ThrowsAsync<JsonException>(() => service.GetExtensions(1, 10, null, null, null, null, null, null, null, "user", "pass"));
    }

    [Fact]
    public async Task GetEventById_ThrowsHttpRequestException_WhenUnexpectedStatusCode()
    {
        var (service, _) = SetupService((HttpStatusCode)418);

        await Assert.ThrowsAsync<HttpRequestException>(() => service.GetEventById(1, "user", "pass"));
    }

    [Fact]
    public async Task GetEvents_ThrowsArgumentException_WhenPageIsNegative()
    {
        var (service, _) = SetupService(HttpStatusCode.OK);

        await Assert.ThrowsAsync<ArgumentException>(() => service.GetEvents(-1, 10, null, null, "user", "pass"));
    }

    [Fact]
    public async Task GetExtensionById_ReturnsNull_WhenResponseIsEmpty()
    {
        var (service, _) = SetupService(HttpStatusCode.OK, "");

        var result = await service.GetExtensionById(1, "user", "pass");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetStoredAlerts_ReturnsEmptyList_WhenResponseIsEmpty()
    {
        var (service, _) = SetupService(HttpStatusCode.OK, "");
        var result = await service.GetStoredAlerts(1, 10, null, null, null, null, null, null, "user", "pass");
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetStoredAlerts_ThrowsJsonException_WhenResponseIsInvalidJson()
    {
        var (service, _) = SetupService(HttpStatusCode.OK, "<html>not json</html>");
        await Assert.ThrowsAsync<JsonException>(() => service.GetStoredAlerts(1, 10, null, null, null, null, null, null, "user", "pass"));
    }

    [Fact]
    public async Task GetStoredAlerts_ThrowsHttpRequestException_WhenUnexpectedStatusCode()
    {
        var (service, _) = SetupService((HttpStatusCode)418);
        await Assert.ThrowsAsync<HttpRequestException>(() => service.GetStoredAlerts(1, 10, null, null, null, null, null, null, "user", "pass"));
    }

    [Fact]
    public async Task GetEventTypes_ThrowsJsonException_WhenResponseIsInvalidJson()
    {
        var (service, _) = SetupService(HttpStatusCode.OK, "<html>not json</html>");
        await Assert.ThrowsAsync<JsonException>(() => service.GetEventTypes("user", "pass"));
    }

    [Fact]
    public async Task GetEventTypes_ThrowsHttpRequestException_WhenUnexpectedStatusCode()
    {
        var (service, _) = SetupService((HttpStatusCode)418);
        await Assert.ThrowsAsync<HttpRequestException>(() => service.GetEventTypes("user", "pass"));
    }

    [Fact]
    public async Task GetAttachedItems_ThrowsJsonException_WhenResponseIsInvalidJson()
    {
        var (service, _) = SetupService(HttpStatusCode.OK, "<html>not json</html>");
        await Assert.ThrowsAsync<JsonException>(() => service.GetAttachedItems(1, "user", "pass"));
    }

    [Fact]
    public async Task GetAttachedItems_ThrowsHttpRequestException_WhenUnexpectedStatusCode()
    {
        var (service, _) = SetupService((HttpStatusCode)418);
        await Assert.ThrowsAsync<HttpRequestException>(() => service.GetAttachedItems(1, "user", "pass"));
    }

    [Fact]
    public async Task GetResourceById_ReturnsNull_WhenResponseIsEmpty()
    {
        var (service, _) = SetupService(HttpStatusCode.OK, "");
        var result = await service.GetResourceById(1, "user", "pass");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetResourceById_ThrowsJsonException_WhenResponseIsInvalidJson()
    {
        var (service, _) = SetupService(HttpStatusCode.OK, "<html>not json</html>");
        await Assert.ThrowsAsync<JsonException>(() => service.GetResourceById(1, "user", "pass"));
    }

    [Fact]
    public async Task GetResourceById_ThrowsHttpRequestException_WhenUnexpectedStatusCode()
    {
        var (service, _) = SetupService((HttpStatusCode)418);
        await Assert.ThrowsAsync<HttpRequestException>(() => service.GetResourceById(1, "user", "pass"));
    }
}