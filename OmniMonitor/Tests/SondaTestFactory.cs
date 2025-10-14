using Moq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using OmniMonitor.Server.Configuration;
using OmniMonitor.Server.Services;
using System.Collections.Generic;
using Moq.Protected;

public static class SondaTestFactory
{
    public static SondaIMService CreateIMService(HttpResponseMessage response, out Mock<HttpMessageHandler> handlerMock, Mock<ISondaAuthService>? authMock = null)
    {
        handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var httpClient = new HttpClient(handlerMock.Object);
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        authMock ??= new Mock<ISondaAuthService>();
        authMock.Setup(a => a.GetUserTokenIMAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("token");

        var config = new ApiConfig
        {
            BaseUrl = new BaseUrlConfig { UrlIM = "http://localhost/" },
            EndpointsIM = new() { { "SystemStatus", new() { { "DeviceCount", "api/devicecount" }, { "DataStatus", "api/datastatus" } } } }
        };

        return new SondaIMService(factoryMock.Object, authMock.Object, Options.Create(config));
    }
}