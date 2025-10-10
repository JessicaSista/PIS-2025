using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using OmniMonitor.Server.Configuration;
using OmniMonitor.Server.Services;
using OmniMonitor.Shared.Dtos.EM;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Xunit;

namespace OmniMonitor.Tests
{
    /// <summary>
    /// Tests para SondaEMService que verifican:
    /// 1. Comunicación correcta con la API externa EM
    /// 2. Manejo de errores HTTP (400, 401, 403, 404, 500)
    /// 3. Autenticación y autorización
    /// 4. Serialización/deserialización de datos
    /// 5. Parámetros de consulta correctos
    /// </summary>
    public class SondaEMServiceTests
    {
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly Mock<ISondaAuthService> _mockAuthService;
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly HttpClient _httpClient;
        private readonly ApiConfig _apiConfig;
        private readonly SondaEMService _service;

        public SondaEMServiceTests()
        {
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockAuthService = new Mock<ISondaAuthService>();
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();

            _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
            _mockHttpClientFactory.Setup(f => f.CreateClient()).Returns(_httpClient);

            // Configuración de API
            _apiConfig = new ApiConfig
            {
                BaseUrl = new BaseUrls { UrlEM = "https://api.em.test/" },
                EndpointsEM = new Dictionary<string, Dictionary<string, string>>
                {
                    ["Alert"] = new Dictionary<string, string>
                    {
                        ["GetAll"] = "alerts",
                        ["GetById"] = "alerts/{alertId}",
                        ["GetStored"] = "alerts/stored"
                    },
                    ["Event"] = new Dictionary<string, string>
                    {
                        ["GetEvents"] = "events",
                        ["GetById"] = "events/{eventId}"
                    },
                    ["EventType"] = new Dictionary<string, string>
                    {
                        ["GetEventTypes"] = "eventtypes"
                    },
                    ["Extension"] = new Dictionary<string, string>
                    {
                        ["GetAll"] = "extensions",
                        ["GetById"] = "extensions/{extensionId}"
                    },
                    ["Resource"] = new Dictionary<string, string>
                    {
                        ["GetById"] = "resources/{id}"
                    }
                }
            };

            var options = Options.Create(_apiConfig);
            _service = new SondaEMService(_mockHttpClientFactory.Object, _mockAuthService.Object, options);

            // Mock del token de autenticación
            _mockAuthService.Setup(a => a.GetUserTokenEMAsync("testuser", "testpass"))
                          .ReturnsAsync("test-token-123");
        }

        private void SetupHttpResponse(HttpStatusCode statusCode, string content = "")
        {
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(content, Encoding.UTF8, "application/json")
                });
        }

        #region GetAlerts Tests

        [Fact]
        public async Task GetAlerts_ValidParameters_ReturnsAlertList()
        {
            // Arrange
            var expectedAlerts = new List<AlertDto>
            {
                new AlertDto { AlertId = 1, AlertName = "Alert 1", AlertState = "Active" },
                new AlertDto { AlertId = 2, AlertName = "Alert 2", AlertState = "Inactive" }
            };

            var apiResponse = new AlertApiResponse { Results = expectedAlerts };
            var jsonResponse = JsonSerializer.Serialize(apiResponse);
            SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

            // Act
            var result = await _service.GetAlerts(1, 10, "test", "Active", 1.0, 2.0, 5.0, true, "name", "testuser", "testpass");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("Alert 1", result[0].AlertName);
            Assert.Equal("Alert 2", result[1].AlertName);

            // Verificar que se llamó al servicio de autenticación
            _mockAuthService.Verify(a => a.GetUserTokenEMAsync("testuser", "testpass"), Times.Once);
        }

        [Fact]
        public async Task GetAlerts_DirectListResponse_ReturnsAlertList()
        {
            // Arrange
            var expectedAlerts = new List<AlertDto>
            {
                new AlertDto { AlertId = 1, AlertName = "Alert 1" }
            };

            var jsonResponse = JsonSerializer.Serialize(expectedAlerts);
            SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

            // Act
            var result = await _service.GetAlerts(1, 10, null, null, null, null, null, null, null, "testuser", "testpass");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Alert 1", result[0].AlertName);
        }

        [Fact]
        public async Task GetAlerts_InvalidPageParameter_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.GetAlerts(null, 10, null, null, null, null, null, null, null, "testuser", "testpass"));

            Assert.Equal("page", exception.ParamName);
            Assert.Contains("El parámetro 'page' es requerido", exception.Message);
        }

        [Fact]
        public async Task GetAlerts_InvalidPageSizeParameter_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.GetAlerts(1, null, null, null, null, null, null, null, null, "testuser", "testpass"));

            Assert.Equal("pageSize", exception.ParamName);
            Assert.Contains("El parámetro 'pageSize' es requerido", exception.Message);
        }

        [Fact]
        public async Task GetAlerts_ZeroPage_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.GetAlerts(0, 10, null, null, null, null, null, null, null, "testuser", "testpass"));

            Assert.Equal("page", exception.ParamName);
            Assert.Contains("El parámetro 'page' debe ser mayor que cero", exception.Message);
        }

        [Fact]
        public async Task GetAlerts_EmptyResponse_ReturnsEmptyList()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.OK, "");

            // Act
            var result = await _service.GetAlerts(1, 10, null, null, null, null, null, null, null, "testuser", "testpass");

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion

        #region GetAlertById Tests

        [Fact]
        public async Task GetAlertById_ValidId_ReturnsAlert()
        {
            // Arrange
            var expectedAlert = new AlertDto
            {
                AlertId = 1,
                AlertName = "Test Alert",
                AlertState = "Active",
                SourceAddress = "Test Address"
            };

            var jsonResponse = JsonSerializer.Serialize(expectedAlert);
            SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

            // Act
            var result = await _service.GetAlertById(1, "testuser", "testpass");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.AlertId);
            Assert.Equal("Test Alert", result.AlertName);
            Assert.Equal("Active", result.AlertState);

            _mockAuthService.Verify(a => a.GetUserTokenEMAsync("testuser", "testpass"), Times.Once);
        }

        [Fact]
        public async Task GetAlertById_NotFound_ReturnsNull()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.NotFound);

            // Act
            var result = await _service.GetAlertById(999, "testuser", "testpass");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAlertById_Unauthorized_ThrowsException()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.Unauthorized);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(
                () => _service.GetAlertById(1, "testuser", "testpass"));

            Assert.Contains("No tienes permisos: token inválido o expirado (401 Unauthorized)", exception.Message);
        }

        [Fact]
        public async Task GetAlertById_Forbidden_ThrowsException()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.Forbidden);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(
                () => _service.GetAlertById(1, "testuser", "testpass"));

            Assert.Contains("No tienes permisos para acceder a este recurso (403 Forbidden)", exception.Message);
        }

        [Fact]
        public async Task GetAlertById_InvalidId_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.GetAlertById(0, "testuser", "testpass"));

            Assert.Equal("alertId", exception.ParamName);
            Assert.Contains("El alertId debe ser mayor que cero", exception.Message);
        }

        [Fact]
        public async Task GetAlertById_EmptyResponse_ReturnsNull()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.OK, "");

            // Act
            var result = await _service.GetAlertById(1, "testuser", "testpass");

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetEvents Tests

        [Fact]
        public async Task GetEvents_ValidParameters_ReturnsEventList()
        {
            // Arrange
            var expectedEvents = new List<EventDto>
            {
                new EventDto { Id = 1, Name = "Event 1", State = "Open" },
                new EventDto { Id = 2, Name = "Event 2", State = "Closed" }
            };

            var apiResponse = new EventApiResponse { Results = expectedEvents };
            var jsonResponse = JsonSerializer.Serialize(apiResponse);
            SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

            // Act
            var result = await _service.GetEvents(1, 10, "name", "test", "testuser", "testpass");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("Event 1", result[0].Name);
            Assert.Equal("Event 2", result[1].Name);
        }

        [Fact]
        public async Task GetEvents_NegativePage_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.GetEvents(-1, 10, null, null, "testuser", "testpass"));

            Assert.Contains("El parámetro 'page' debe ser mayor o igual que cero", exception.Message);
        }

        [Fact]
        public async Task GetEvents_EmptyResponse_ReturnsEmptyList()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.OK, "");

            // Act
            var result = await _service.GetEvents(1, 10, null, null, "testuser", "testpass");

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion

        #region GetEventById Tests

        [Fact]
        public async Task GetEventById_ValidId_ReturnsEvent()
        {
            // Arrange
            var expectedEvent = new EventDto
            {
                Id = 1,
                Name = "Test Event",
                State = "Open",
                Origin = "Test Origin"
            };

            var jsonResponse = JsonSerializer.Serialize(expectedEvent);
            SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

            // Act
            var result = await _service.GetEventById(1, "testuser", "testpass");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Test Event", result.Name);
            Assert.Equal("Open", result.State);
        }

        [Fact]
        public async Task GetEventById_InvalidId_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.GetEventById(0, "testuser", "testpass"));

            Assert.Equal("eventId", exception.ParamName);
            Assert.Contains("El eventId debe ser positivo", exception.Message);
        }

        [Fact]
        public async Task GetEventById_NotFound_ReturnsNull()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.NotFound);

            // Act
            var result = await _service.GetEventById(999, "testuser", "testpass");

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetExtensions Tests

        [Fact]
        public async Task GetExtensions_ValidParameters_ReturnsExtensionList()
        {
            // Arrange
            var expectedExtensions = new List<ExtensionDto>
            {
                new ExtensionDto { ExtensionId = 1, EventName = "Extension 1" },
                new ExtensionDto { ExtensionId = 2, EventName = "Extension 2" }
            };

            var apiResponse = new ExtensionApiResponse { Results = expectedExtensions };
            var jsonResponse = JsonSerializer.Serialize(apiResponse);
            SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

            // Act
            var result = await _service.GetExtensions(1, 10, "name", "test", "Active", "2023-01-01", "High", "Category1", "Zone1", "testuser", "testpass");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("Extension 1", result[0].EventName);
            Assert.Equal("Extension 2", result[1].EventName);
        }

        [Fact]
        public async Task GetExtensions_NegativePage_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.GetExtensions(-1, 10, null, null, null, null, null, null, null, "testuser", "testpass"));

            Assert.Contains("El parámetro 'page' debe ser mayor o igual que cero", exception.Message);
        }

        #endregion

        #region GetExtensionById Tests

        [Fact]
        public async Task GetExtensionById_ValidId_ReturnsExtension()
        {
            // Arrange
            var expectedExtension = new ExtensionDtoDup
            {
                ExtensionId = 1,
                EventName = "Test Extension"
            };

            var jsonResponse = JsonSerializer.Serialize(expectedExtension);
            SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

            // Act
            var result = await _service.GetExtensionById(1, "testuser", "testpass");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.ExtensionId);
            Assert.Equal("Test Extension", result.EventName);
        }

        [Fact]
        public async Task GetExtensionById_InvalidId_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.GetExtensionById(0, "testuser", "testpass"));

            Assert.Equal("extensionId", exception.ParamName);
            Assert.Contains("El extensionId debe ser mayor que cero", exception.Message);
        }

        [Fact]
        public async Task GetExtensionById_NotFound_ReturnsNull()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.NotFound);

            // Act
            var result = await _service.GetExtensionById(999, "testuser", "testpass");

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetResourceById Tests

        [Fact]
        public async Task GetResourceById_ValidId_ReturnsResource()
        {
            // Arrange
            var expectedResource = new ResourceDto
            {
                Id = 1,
                Name = "Test Resource"
            };

            var jsonResponse = JsonSerializer.Serialize(expectedResource);
            SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

            // Act
            var result = await _service.GetResourceById(1, "testuser", "testpass");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Test Resource", result.Name);
        }

        [Fact]
        public async Task GetResourceById_InvalidId_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.GetResourceById(0, "testuser", "testpass"));

            Assert.Equal("id", exception.ParamName);
            Assert.Contains("El id debe ser mayor que cero", exception.Message);
        }

        [Fact]
        public async Task GetResourceById_NotFound_ReturnsNull()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.NotFound);

            // Act
            var result = await _service.GetResourceById(999, "testuser", "testpass");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetResourceById_Unauthorized_ThrowsException()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.Unauthorized);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(
                () => _service.GetResourceById(1, "testuser", "testpass"));

            Assert.Contains("No tienes permisos: token inválido o expirado (401 Unauthorized)", exception.Message);
        }

        [Fact]
        public async Task GetResourceById_Forbidden_ThrowsException()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.Forbidden);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(
                () => _service.GetResourceById(1, "testuser", "testpass"));

            Assert.Contains("No tienes permisos para acceder a este recurso (403 Forbidden)", exception.Message);
        }

        #endregion

        #region GetEventTypes Tests

        [Fact]
        public async Task GetEventTypes_ValidRequest_ReturnsEventTypeList()
        {
            // Arrange
            var expectedEventTypes = new List<EventTypeDto>
            {
                new EventTypeDto { Id = 1, Name = "Type 1" },
                new EventTypeDto { Id = 2, Name = "Type 2" }
            };

            var jsonResponse = JsonSerializer.Serialize(expectedEventTypes);
            SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

            // Act
            var result = await _service.GetEventTypes("testuser", "testpass");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("Type 1", result[0].Name);
            Assert.Equal("Type 2", result[1].Name);
        }

        [Fact]
        public async Task GetEventTypes_EmptyResponse_ReturnsEmptyList()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.OK, "");

            // Act
            var result = await _service.GetEventTypes("testuser", "testpass");

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion

        #region GetAttachedItems Tests

        [Fact]
        public async Task GetAttachedItems_ValidExtensionId_ReturnsAttachmentList()
        {
            // Arrange
            var expectedAttachments = new List<AttachmentDto>
            {
                new AttachmentDto { Id = 1, Name = "Attachment 1" },
                new AttachmentDto { Id = 2, Name = "Attachment 2" }
            };

            var jsonResponse = JsonSerializer.Serialize(expectedAttachments);
            SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

            // Act
            var result = await _service.GetAttachedItems(1, "testuser", "testpass");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("Attachment 1", result[0].Name);
            Assert.Equal("Attachment 2", result[1].Name);
        }

        [Fact]
        public async Task GetAttachedItems_EmptyResponse_ReturnsEmptyList()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.OK, "");

            // Act
            var result = await _service.GetAttachedItems(1, "testuser", "testpass");

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion

        #region HTTP Error Handling Tests

        [Fact]
        public async Task GetAlerts_ServerError_ThrowsHttpRequestException()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.InternalServerError);

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(
                () => _service.GetAlerts(1, 10, null, null, null, null, null, null, null, "testuser", "testpass"));
        }

        [Fact]
        public async Task GetEvents_BadRequest_ThrowsHttpRequestException()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.BadRequest);

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(
                () => _service.GetEvents(1, 10, null, null, "testuser", "testpass"));
        }

        #endregion
    }
}