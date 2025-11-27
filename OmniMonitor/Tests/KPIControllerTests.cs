using System.Globalization;
using System.Security.Claims;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Moq;

using OmniMonitor.Server.Context;
using OmniMonitor.Server.Controllers;
using OmniMonitor.Server.Services;
using OmniMonitor.Shared.Dtos;

using Xunit;

namespace QA.Tests
{
    public class KPIControllerTests
    {
        private ApplicationDbContext BuildInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new ApplicationDbContext(options);
        }

        private ClaimsPrincipal GetUser(string username = "testuser")
        {
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, username) }, "TestAuth");
            return new ClaimsPrincipal(identity);
        }

        private KPIController GetController(
            Mock<ISondaAuthService>? authMock = null,
            Mock<IKpiService>? kpiServiceMock = null,
            Mock<ISondaIMService>? sondaIMMock = null,
            Mock<ILogger<KPIController>>? loggerMock = null,
            ClaimsPrincipal? user = null)
        {
            authMock ??= new Mock<ISondaAuthService>();
            kpiServiceMock ??= new Mock<IKpiService>();
            sondaIMMock ??= new Mock<ISondaIMService>();
            loggerMock ??= new Mock<ILogger<KPIController>>();
            var controller = new KPIController(authMock.Object, kpiServiceMock.Object, sondaIMMock.Object, loggerMock.Object);
            if (user != null)
            {
                controller.ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = user }
                };
            }
            return controller;
        }

        /* Tests de CreateKpi */

        [Fact]
        public async Task CreateKpi_ReturnsOk_WhenValid()
        {
            var kpiServiceMock = new Mock<IKpiService>();
            var request = new KpiRequest { Name = "KPI1", SourceModule = "IM", DatasetId = 1 };
            var kpi = new Kpi { Id = 1, Name = "KPI1", SourceModule = "IM", DatasetId = 1 };
            kpiServiceMock.Setup(s => s.CreateKpiAsync(request, "testuser")).ReturnsAsync(kpi);

            var controller = GetController(kpiServiceMock: kpiServiceMock, user: GetUser());
            var result = await controller.CreateKpi(request);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(kpi, ok.Value);
        }

        [Fact]
        public async Task CreateKpi_ReturnsBadRequest_WhenRequestIsNull()
        {
            var controller = GetController(user: GetUser());
            var result = await controller.CreateKpi(null);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("nulo", badRequest.Value.ToString());
        }

        [Fact]
        public async Task CreateKpi_ReturnsBadRequest_WhenUserIsNull()
        {
            var controller = GetController();
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
            };
            var request = new KpiRequest { Name = "KPI1", SourceModule = "IM", DatasetId = 1 };
            var result = await controller.CreateKpi(request);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("Token inválido", badRequest.Value.ToString());
        }

        [Fact]
        public async Task CreateKpi_ReturnsBadRequest_OnArgumentException()
        {
            var kpiServiceMock = new Mock<IKpiService>();
            var request = new KpiRequest { Name = "KPI1", SourceModule = "IM", DatasetId = 1 };
            kpiServiceMock.Setup(s => s.CreateKpiAsync(request, "testuser"))
                .ThrowsAsync(new ArgumentException("Nombre duplicado"));

            var controller = GetController(kpiServiceMock: kpiServiceMock, user: GetUser());
            var result = await controller.CreateKpi(request);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("Nombre duplicado", badRequest.Value.ToString());
        }

        [Fact]
        public async Task CreateKpi_ReturnsServerError_OnException()
        {
            var kpiServiceMock = new Mock<IKpiService>();
            var request = new KpiRequest { Name = "KPI1", SourceModule = "IM", DatasetId = 1 };
            kpiServiceMock.Setup(s => s.CreateKpiAsync(request, "testuser"))
                .ThrowsAsync(new Exception("DB error"));

            var controller = GetController(kpiServiceMock: kpiServiceMock, user: GetUser());
            var result = await controller.CreateKpi(request);
            var serverError = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, serverError.StatusCode);
        }

        [Fact]
        public async Task CreateKpi_ReturnsServerError_OnDbUpdateException()
        {
            var kpiServiceMock = new Mock<IKpiService>();
            var request = new KpiRequest { Name = "KPI1", SourceModule = "IM", DatasetId = 1 };
            kpiServiceMock.Setup(s => s.CreateKpiAsync(request, "testuser"))
                .ThrowsAsync(new Microsoft.EntityFrameworkCore.DbUpdateException("DB error", new Exception("Inner")));

            var controller = GetController(kpiServiceMock: kpiServiceMock, user: GetUser());
            var result = await controller.CreateKpi(request);
            var serverError = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, serverError.StatusCode);
        }

        /* Tests de GetKpiById */

        [Fact]
        public async Task GetKpiById_ReturnsOk_WhenFound()
        {
            var kpiServiceMock = new Mock<IKpiService>();
            var kpiResponse = new KpiResponse { Id = 1, Name = "KPI1" };
            kpiServiceMock.Setup(s => s.CalculateKpiValueAsync(1, "testuser")).ReturnsAsync(kpiResponse);

            var controller = GetController(kpiServiceMock: kpiServiceMock, user: GetUser());
            var result = await controller.GetKpiById(1);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(kpiResponse, ok.Value);
        }

        [Fact]
        public async Task GetKpiById_ReturnsBadRequest_WhenUserIsNull()
        {
            var controller = GetController();
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal()  }
            };
            var result = await controller.GetKpiById(1);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("Token inválido", badRequest.Value.ToString());
        }

        [Fact]
        public async Task GetKpiById_ReturnsNotFound_WhenNotFound()
        {
            var kpiServiceMock = new Mock<IKpiService>();
            kpiServiceMock.Setup(s => s.CalculateKpiValueAsync(1, "testuser"))
                .ThrowsAsync(new Exception("No se encontró"));
            kpiServiceMock.Setup(s => s.GetKpiDefinitionAsync(1))
                .ThrowsAsync(new Exception("No se encontró"));

            var controller = GetController(kpiServiceMock: kpiServiceMock, user: GetUser());
            var result = await controller.GetKpiById(1);
            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Contains("No se encontró", notFound.Value.ToString());
        }

        [Fact]
        public async Task GetKpiById_ReturnsOk_NoDataResponse_WhenCalculationFailsButDefinitionExists()
        {
            var kpiServiceMock = new Mock<IKpiService>();
            kpiServiceMock.Setup(s => s.CalculateKpiValueAsync(1, "testuser"))
                .ThrowsAsync(new Exception("Error de cálculo"));
            kpiServiceMock.Setup(s => s.GetKpiDefinitionAsync(1))
                .ReturnsAsync(new Kpi { Id = 1, Name = "KPI1", DefaultColor = "#000000", Unit = "U", Description = "Desc" });

            var controller = GetController(kpiServiceMock: kpiServiceMock, user: GetUser());
            var result = await controller.GetKpiById(1);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var kpiResp = Assert.IsType<KpiResponse>(ok.Value);
            Assert.Equal(1, kpiResp.Id);
            Assert.Null(kpiResp.Value);
        }

        [Fact]
        public async Task GetKpiById_ReturnsServerError_OnException()
        {
            var kpiServiceMock = new Mock<IKpiService>();
            kpiServiceMock.Setup(s => s.CalculateKpiValueAsync(1, "testuser"))
                .ThrowsAsync(new Exception("Error interno"));
            kpiServiceMock.Setup(s => s.GetKpiDefinitionAsync(1))
                .ThrowsAsync(new Exception("Error interno"));

            var controller = GetController(kpiServiceMock: kpiServiceMock, user: GetUser());
            var result = await controller.GetKpiById(1);
            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetKpiById_ReturnsOk_WhenKpiValueIsNull()
        {
            var kpiServiceMock = new Mock<IKpiService>();
            kpiServiceMock.Setup(s => s.CalculateKpiValueAsync(1, "testuser")).ReturnsAsync((KpiResponse)null);

            var controller = GetController(kpiServiceMock: kpiServiceMock, user: GetUser());
            var result = await controller.GetKpiById(1);
            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        /* Tests de CalculateKpiData */

        [Fact]
        public async Task CalculateKpiData_ReturnsOk_WhenValid()
        {
            var kpiServiceMock = new Mock<IKpiService>();
            var request = new KpiRequest { Name = "KPI1", SourceModule = "IM", DatasetId = 1 };
            var kpiResponse = new KpiResponse { Id = 1, Name = "KPI1" };
            kpiServiceMock.Setup(s => s.CalculateKpiDataAsync(request, "testuser")).ReturnsAsync(kpiResponse);

            var controller = GetController(kpiServiceMock: kpiServiceMock, user: GetUser());
            var result = await controller.CalculateKpiData(request);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(kpiResponse, ok.Value);
        }

        [Fact]
        public async Task CalculateKpiData_IM_LastValue_ReturnsParsedAndMultipliedValue_SensorDto()
        {
            // Arrange - InMemory DB (necesario por el constructor de KpiService)
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var db = new ApplicationDbContext(options);

            // Mocks de dependencias
            var datasetServiceMock = new Mock<IDatasetService>();
            var sondaEMMock = new Mock<ISondaEMService>();
            var sondaIMMock = new Mock<ISondaIMService>();
            var sondaAuthMock = new Mock<ISondaAuthService>();
            var kpiAMServiceMock = new Mock<IKpiAMService>();
            var datasetAmServiceMock = new Mock<IDatasetAmService>();
            var datasetUMMock = new Mock<IDatasetUMService>();
            var sondaUMMock = new Mock<ISondaUMService>();
            var sondaAMMock = new Mock<ISondaAMService>();
            var datasetEmMock = new Mock<IDatasetEMService>();

            // Request: IM lastValue with multiplier
            var request = new KpiRequest
            {
                Name = "IM LastValue KPI",
                SourceModule = "IM",
                DatasetId = 555,
                Metric = "lastValue",
                Multiplier = 2.0
            };

            // DatasetIM returned by IDatasetService
            var datasetIM = new DatasetIM
            {
                Id = request.DatasetId.Value,
                Id_Source = 10,
                SensorName = "TEMP_SENSOR"
            };

            datasetServiceMock
                .Setup(s => s.GetDatasetIMByIdAsync(request.DatasetId.Value, It.IsAny<string>()))
                .ReturnsAsync(datasetIM);

            // Source with one device summary (Sonda API style)
            var source = new Source
            {
                Id = (int)datasetIM.Id_Source,
                Devices = new List<Device> { new Device { Id = 100 } }
            };

            sondaIMMock
                .Setup(s => s.GetSourceById((int)datasetIM.Id_Source, It.IsAny<string>()))
                .ReturnsAsync(source);

            // Device with Sensor matching SensorName (use your real Sensor DTO)
            var sensor = new OmniMonitor.Shared.Dtos.Sensor
            {
                Name = datasetIM.SensorName,
                Type = "double",
                LastValue = "12.5"
            };

            var device = new Device
            {
                Id = 100,
                Sensors = new List<OmniMonitor.Shared.Dtos.Sensor> { sensor }
            };

            sondaIMMock
                .Setup(s => s.GetDeviceById(device.Id, It.IsAny<string>()))
                .ReturnsAsync(device);

            // Construir KpiService real con mocks
            var kpiService = new KpiService(
                db,
                datasetServiceMock.Object,
                sondaEMMock.Object,
                sondaIMMock.Object,
                sondaAuthMock.Object,
                kpiAMServiceMock.Object,
                datasetAmServiceMock.Object,
                datasetUMMock.Object,
                sondaUMMock.Object,
                sondaAMMock.Object,
                datasetEmMock.Object
            );

            // Construir KPIController con KpiService real
            var authMock = new Mock<ISondaAuthService>();
            var loggerMock = new Mock<ILogger<KPIController>>();
            var controller = new KPIController(authMock.Object, kpiService, sondaIMMock.Object, loggerMock.Object);

            // Setear usuario en el ControllerContext
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "testuser")], "TestAuth"))
                }
            };

            // Act
            var actionResult = await controller.CalculateKpiData(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var kpiResp = Assert.IsType<KpiResponse>(okResult.Value);

            // LastValue "12.5" * multiplier 2.0 => 25.0
            Assert.NotNull(kpiResp.Value);
            Assert.IsType<double>(kpiResp.Value);
            var actual = Convert.ToDouble(kpiResp.Value, CultureInfo.InvariantCulture);
            Assert.Equal(25.0, actual, 5);

            Assert.Equal("IM LastValue KPI", kpiResp.Name);
            Assert.Equal("double", kpiResp.Type);
        }

        [Fact]
        public async Task CalculateKpiData_ReturnsBadRequest_WhenRequestIsNull()
        {
            var controller = GetController(user: GetUser());
            var result = await controller.CalculateKpiData(null);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("requeridos", badRequest.Value.ToString());
        }

        [Fact]
        public async Task CalculateKpiData_ReturnsBadRequest_WhenUserIsNull()
        {
            var controller = GetController();
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
            };
            var request = new KpiRequest { Name = "KPI1", SourceModule = "IM", DatasetId = 1 };
            var result = await controller.CalculateKpiData(request);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("Token inválido", badRequest.Value.ToString());
        }

        [Fact]
        public async Task CalculateKpiData_ReturnsBadRequest_OnArgumentException()
        {
            var kpiServiceMock = new Mock<IKpiService>();
            var request = new KpiRequest { Name = "KPI1", SourceModule = "IM", DatasetId = 1 };
            kpiServiceMock.Setup(s => s.CalculateKpiDataAsync(request, "testuser"))
                .ThrowsAsync(new ArgumentException("Argumento inválido"));

            var controller = GetController(kpiServiceMock: kpiServiceMock, user: GetUser());
            var result = await controller.CalculateKpiData(request);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("Argumento inválido", badRequest.Value.ToString());
        }

        [Fact]
        public async Task CalculateKpiData_ReturnsServerError_OnException()
        {
            var kpiServiceMock = new Mock<IKpiService>();
            var request = new KpiRequest { Name = "KPI1", SourceModule = "IM", DatasetId = 1 };
            kpiServiceMock.Setup(s => s.CalculateKpiDataAsync(request, "testuser"))
                .ThrowsAsync(new Exception("Error interno"));

            var controller = GetController(kpiServiceMock: kpiServiceMock, user: GetUser());
            var result = await controller.CalculateKpiData(request);
            var serverError = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, serverError.StatusCode);
        }

        [Fact]
        public async Task CalculateKpiData_ReturnsOk_WhenKpiResponseIsNull()
        {
            var kpiServiceMock = new Mock<IKpiService>();
            var request = new KpiRequest { Name = "KPI1", SourceModule = "IM", DatasetId = 1 };
            kpiServiceMock.Setup(s => s.CalculateKpiDataAsync(request, "testuser")).ReturnsAsync((KpiResponse)null);

            var controller = GetController(kpiServiceMock: kpiServiceMock, user: GetUser());
            var result = await controller.CalculateKpiData(request);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Null(ok.Value);
        }

        /* Tests de GetKpiByIdSinToken */

        [Fact]
        public async Task GetKpiByIdSinToken_ReturnsOk_WhenFound()
        {
            var kpiServiceMock = new Mock<IKpiService>();
            var kpiResponse = new KpiResponse { Id = 1, Name = "KPI1" };
            kpiServiceMock.Setup(s => s.CalculateKpiValueAsyncSinToken(1)).ReturnsAsync(kpiResponse);

            var controller = GetController(kpiServiceMock: kpiServiceMock);
            var result = await controller.GetKpiByIdSinToken(1);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(kpiResponse, ok.Value);
        }

        [Fact]
        public async Task GetKpiByIdSinToken_ReturnsNotFound_WhenNotFound()
        {
            var kpiServiceMock = new Mock<IKpiService>();
            kpiServiceMock.Setup(s => s.CalculateKpiValueAsyncSinToken(1)).ReturnsAsync((KpiResponse)null);

            var controller = GetController(kpiServiceMock: kpiServiceMock);
            var result = await controller.GetKpiByIdSinToken(1);

            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Contains("No se encontró", notFound.Value.ToString());
        }

        [Fact]
        public async Task GetKpiByIdSinToken_ReturnsServerError_OnException()
        {
            var kpiServiceMock = new Mock<IKpiService>();
            kpiServiceMock.Setup(s => s.CalculateKpiValueAsyncSinToken(1))
                .ThrowsAsync(new Exception("Error interno"));

            var controller = GetController(kpiServiceMock: kpiServiceMock);
            var result = await controller.GetKpiByIdSinToken(1);

            var serverError = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, serverError.StatusCode);
        }

        [Fact]
        public async Task GetKpiByIdSinToken_ReturnsOk_WithNullValue()
        {
            var kpiServiceMock = new Mock<IKpiService>();
            var kpiResponse = new KpiResponse { Id = 1, Name = "KPI1", Value = null };
            kpiServiceMock.Setup(s => s.CalculateKpiValueAsyncSinToken(1)).ReturnsAsync(kpiResponse);

            var controller = GetController(kpiServiceMock: kpiServiceMock);
            var result = await controller.GetKpiByIdSinToken(1);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(kpiResponse, ok.Value);
        }

        [Fact]
        public async Task GetKpiByIdSinToken_ReturnsOk_WithLiveEnabled()
        {
            var kpiServiceMock = new Mock<IKpiService>();
            var kpiResponse = new KpiResponse { Id = 2, Name = "KPI2", LiveEnabled = true };
            kpiServiceMock.Setup(s => s.CalculateKpiValueAsyncSinToken(2)).ReturnsAsync(kpiResponse);

            var controller = GetController(kpiServiceMock: kpiServiceMock);
            var result = await controller.GetKpiByIdSinToken(2);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(kpiResponse, ok.Value);
        }

        [Fact]
        public async Task GetKpiByIdSinToken_ReturnsOk_WithLink()
        {
            var kpiServiceMock = new Mock<IKpiService>();
            var kpiResponse = new KpiResponse { Id = 3, Name = "KPI3", Link = "http://test.com" };
            kpiServiceMock.Setup(s => s.CalculateKpiValueAsyncSinToken(3)).ReturnsAsync(kpiResponse);

            var controller = GetController(kpiServiceMock: kpiServiceMock);
            var result = await controller.GetKpiByIdSinToken(3);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(kpiResponse, ok.Value);
        }

        /* Tests de DeleteKpi */

        [Fact]
        public async Task DeleteKpi_ReturnsOk_WhenDeleted()
        {
            var kpiServiceMock = new Mock<IKpiService>();
            kpiServiceMock.Setup(s => s.DeleteKpiAsync(1, "testuser")).Returns(Task.CompletedTask);

            var controller = GetController(kpiServiceMock: kpiServiceMock, user: GetUser());
            var result = await controller.DeleteKpi(1);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("eliminado", ok.Value.ToString());
        }

        [Fact]
        public async Task DeleteKpi_ReturnsBadRequest_WhenUserIsNull()
        {
            var controller = GetController();
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
            };
            var result = await controller.DeleteKpi(1);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Token inválido", badRequest.Value.ToString());
        }

        [Fact]
        public async Task DeleteKpi_ReturnsNotFound_WhenNotFound()
        {
            var kpiServiceMock = new Mock<IKpiService>();
            kpiServiceMock.Setup(s => s.DeleteKpiAsync(1, "testuser"))
                .ThrowsAsync(new KeyNotFoundException("No se encontró"));

            var controller = GetController(kpiServiceMock: kpiServiceMock, user: GetUser());
            var result = await controller.DeleteKpi(1);
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("No se encontró", notFound.Value.ToString());
        }

        [Fact]
        public async Task DeleteKpi_ReturnsForbidden_WhenUnauthorized()
        {
            var kpiServiceMock = new Mock<IKpiService>();
            kpiServiceMock.Setup(s => s.DeleteKpiAsync(1, "testuser"))
                .ThrowsAsync(new UnauthorizedAccessException("No tiene permisos"));

            var controller = GetController(kpiServiceMock: kpiServiceMock, user: GetUser());
            var result = await controller.DeleteKpi(1);
            var forbidden = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, forbidden.StatusCode);
        }

        [Fact]
        public async Task DeleteKpi_ReturnsServerError_OnException()
        {
            var kpiServiceMock = new Mock<IKpiService>();
            kpiServiceMock.Setup(s => s.DeleteKpiAsync(1, "testuser"))
                .ThrowsAsync(new Exception("Error interno"));

            var controller = GetController(kpiServiceMock: kpiServiceMock, user: GetUser());
            var result = await controller.DeleteKpi(1);
            var serverError = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, serverError.StatusCode);
        }

        [Fact]
        public async Task DeleteKpi_ReturnsOk_WithCustomMessage()
        {
            var kpiServiceMock = new Mock<IKpiService>();
            kpiServiceMock.Setup(s => s.DeleteKpiAsync(2, "testuser")).Returns(Task.CompletedTask);

            var controller = GetController(kpiServiceMock: kpiServiceMock, user: GetUser());
            var result = await controller.DeleteKpi(2);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("eliminado", ok.Value.ToString());
        }

        /* Tests de UpdateKpiPartial */

        [Fact]
        public async Task UpdateKpiPartial_ReturnsOk_WhenUpdated()
        {
            var kpiServiceMock = new Mock<IKpiService>();
            var request = new KpiRequest { Name = "KPI1" };
            var kpi = new Kpi { Id = 1, Name = "KPI1" };
            kpiServiceMock.Setup(s => s.UpdateKpiAsync(1, request, "testuser")).ReturnsAsync(kpi);

            var controller = GetController(kpiServiceMock: kpiServiceMock, user: GetUser());
            var result = await controller.UpdateKpiPartial(1, request);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(kpi, ok.Value);
        }

        [Fact]
        public async Task UpdateKpiPartial_ReturnsBadRequest_WhenRequestIsNull()
        {
            var controller = GetController(user: GetUser());
            var result = await controller.UpdateKpiPartial(1, null);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("nulo", badRequest.Value.ToString());
        }

        [Fact]
        public async Task UpdateKpiPartial_ReturnsNotFound_WhenNotFound()
        {
            var kpiServiceMock = new Mock<IKpiService>();
            var request = new KpiRequest { Name = "KPI1" };
            kpiServiceMock.Setup(s => s.UpdateKpiAsync(1, request, "testuser"))
                .ThrowsAsync(new KeyNotFoundException("No se encontró"));

            var controller = GetController(kpiServiceMock: kpiServiceMock, user: GetUser());
            var result = await controller.UpdateKpiPartial(1, request);
            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Contains("No se encontró", notFound.Value.ToString());
        }

        [Fact]
        public async Task UpdateKpiPartial_ReturnsBadRequest_WhenUnauthorized()
        {
            var kpiServiceMock = new Mock<IKpiService>();
            var request = new KpiRequest { Name = "KPI1" };
            kpiServiceMock.Setup(s => s.UpdateKpiAsync(1, request, "testuser"))
                .ThrowsAsync(new UnauthorizedAccessException("No tiene permisos"));

            var controller = GetController(kpiServiceMock: kpiServiceMock, user: GetUser());
            var result = await controller.UpdateKpiPartial(1, request);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("No tiene permisos", badRequest.Value.ToString());
        }

        [Fact]
        public async Task UpdateKpiPartial_ReturnsBadRequest_OnArgumentException()
        {
            var kpiServiceMock = new Mock<IKpiService>();
            var request = new KpiRequest { Name = "KPI1" };
            kpiServiceMock.Setup(s => s.UpdateKpiAsync(1, request, "testuser"))
                .ThrowsAsync(new ArgumentException("Argumento inválido"));

            var controller = GetController(kpiServiceMock: kpiServiceMock, user: GetUser());
            var result = await controller.UpdateKpiPartial(1, request);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("Argumento inválido", badRequest.Value.ToString());
        }

        [Fact]
        public async Task UpdateKpiPartial_ReturnsServerError_OnException()
        {
            var kpiServiceMock = new Mock<IKpiService>();
            var request = new KpiRequest { Name = "KPI1" };
            kpiServiceMock.Setup(s => s.UpdateKpiAsync(1, request, "testuser"))
                .ThrowsAsync(new Exception("Error interno"));

            var controller = GetController(kpiServiceMock: kpiServiceMock, user: GetUser());
            var result = await controller.UpdateKpiPartial(1, request);
            var serverError = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, serverError.StatusCode);
        }

        /* Tests de GetAllKpiDtoPaginated */

        [Fact]
        public async Task GetAllKpiDtoPaginated_ReturnsOk_WhenValid()
        {
            var kpiServiceMock = new Mock<IKpiService>();
            var response = new KpiSimplePaginatedResponse { Items = new List<KpiSimpleDto> { new KpiSimpleDto { Id = 1, Name = "KPI1" } }, TotalCount = 1, Page = 1, PageSize = 10, TotalPages = 1 };
            kpiServiceMock.Setup(s => s.GetAllKpisPaginatedAsync("testuser", 1, 10, null)).ReturnsAsync(response);

            var controller = GetController(kpiServiceMock: kpiServiceMock, user: GetUser());
            var result = await controller.GetAllKpiDtoPaginated(1, 10, null);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(response, ok.Value);
        }

        [Fact]
        public async Task GetAllKpiDtoPaginated_ReturnsBadRequest_WhenUserIsNull()
        {
            var controller = GetController();
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
            };
            var result = await controller.GetAllKpiDtoPaginated(1, 10, null);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("Usuario no encontrado", badRequest.Value.ToString());
        }

        [Fact]
        public async Task GetAllKpiDtoPaginated_ReturnsBadRequest_WhenPageInvalid()
        {
            var controller = GetController(user: GetUser());
            var result = await controller.GetAllKpiDtoPaginated(0, 10, null);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("mayores a 0", badRequest.Value.ToString());
        }

        [Fact]
        public async Task GetAllKpiDtoPaginated_ReturnsServerError_OnException()
        {
            var kpiServiceMock = new Mock<IKpiService>();
            kpiServiceMock.Setup(s => s.GetAllKpisPaginatedAsync("testuser", 1, 10, null))
                .ThrowsAsync(new Exception("Error interno"));

            var controller = GetController(kpiServiceMock: kpiServiceMock, user: GetUser());
            var result = await controller.GetAllKpiDtoPaginated(1, 10, null);
            var serverError = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, serverError.StatusCode);
        }

        [Fact]
        public async Task GetAllKpiDtoPaginated_ReturnsOk_EmptyItems()
        {
            var kpiServiceMock = new Mock<IKpiService>();
            var response = new KpiSimplePaginatedResponse { Items = new List<KpiSimpleDto>(), TotalCount = 0, Page = 1, PageSize = 10, TotalPages = 0 };
            kpiServiceMock.Setup(s => s.GetAllKpisPaginatedAsync("testuser", 1, 10, null)).ReturnsAsync(response);

            var controller = GetController(kpiServiceMock: kpiServiceMock, user: GetUser());
            var result = await controller.GetAllKpiDtoPaginated(1, 10, null);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(response, ok.Value);
        }

        [Fact]
        public async Task GetAllKpiDtoPaginated_ReturnsOk_WithQuery()
        {
            var kpiServiceMock = new Mock<IKpiService>();
            var response = new KpiSimplePaginatedResponse { Items = new List<KpiSimpleDto> { new KpiSimpleDto { Id = 2, Name = "KPI2" } }, TotalCount = 1, Page = 1, PageSize = 10, TotalPages = 1 };
            kpiServiceMock.Setup(s => s.GetAllKpisPaginatedAsync("testuser", 1, 10, "KPI2")).ReturnsAsync(response);

            var controller = GetController(kpiServiceMock: kpiServiceMock, user: GetUser());
            var result = await controller.GetAllKpiDtoPaginated(1, 10, "KPI2");

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(response, ok.Value);
        }

        /* Tests de GetFieldValues */

        [Fact]
        public async Task GetFieldValues_ReturnsOk_WhenValid()
        {
            var kpiServiceMock = new Mock<IKpiService>();
            var values = new List<string> { "A", "B" };
            kpiServiceMock.Setup(s => s.GetFieldValuesAsync(1, "AM", "Nombre", 1, "testuser")).ReturnsAsync(values);

            var controller = GetController(kpiServiceMock: kpiServiceMock, user: GetUser());
            var result = await controller.GetFieldValues(1, "AM", "Nombre", 1);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(values, ok.Value);
        }

        [Fact]
        public async Task GetFieldValues_ReturnsBadRequest_WhenDatasetIdInvalid()
        {
            var controller = GetController(user: GetUser());
            var result = await controller.GetFieldValues(0, "AM", "Nombre", 1);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("ID de dataset válido", badRequest.Value.ToString());
        }

        [Fact]
        public async Task GetFieldValues_ReturnsBadRequest_WhenModuloMissing()
        {
            var controller = GetController(user: GetUser());
            var result = await controller.GetFieldValues(1, "", "Nombre", 1);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("módulo", badRequest.Value.ToString());
        }

        [Fact]
        public async Task GetFieldValues_ReturnsBadRequest_WhenCampoMissing()
        {
            var controller = GetController(user: GetUser());
            var result = await controller.GetFieldValues(1, "AM", "", 1);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("campo", badRequest.Value.ToString());
        }

        [Fact]
        public async Task GetFieldValues_ReturnsBadRequest_WhenUserIsNull()
        {
            var controller = GetController();
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
            };
            var result = await controller.GetFieldValues(1, "AM", "Nombre", 1);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("Token inválido", badRequest.Value.ToString());
        }

        [Fact]
        public async Task GetFieldValues_ReturnsOk_EmptyList_WhenNoValues()
        {
            var kpiServiceMock = new Mock<IKpiService>();
            kpiServiceMock.Setup(s => s.GetFieldValuesAsync(1, "AM", "Nombre", 1, "testuser")).ReturnsAsync(new List<string>());

            var controller = GetController(kpiServiceMock: kpiServiceMock, user: GetUser());
            var result = await controller.GetFieldValues(1, "AM", "Nombre", 1);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Empty((List<string>)ok.Value);
        }

        [Fact]
        public async Task GetFieldValues_ReturnsBadRequest_OnArgumentException()
        {
            var kpiServiceMock = new Mock<IKpiService>();
            kpiServiceMock.Setup(s => s.GetFieldValuesAsync(1, "AM", "Nombre", 1, "testuser"))
                .ThrowsAsync(new ArgumentException("Argumento inválido"));

            var controller = GetController(kpiServiceMock: kpiServiceMock, user: GetUser());
            var result = await controller.GetFieldValues(1, "AM", "Nombre", 1);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("Argumento inválido", badRequest.Value.ToString());
        }

        [Fact]
        public async Task GetFieldValues_ReturnsServerError_OnException()
        {
            var kpiServiceMock = new Mock<IKpiService>();
            kpiServiceMock.Setup(s => s.GetFieldValuesAsync(1, "AM", "Nombre", 1, "testuser"))
                .ThrowsAsync(new Exception("Error interno"));

            var controller = GetController(kpiServiceMock: kpiServiceMock, user: GetUser());
            var result = await controller.GetFieldValues(1, "AM", "Nombre", 1);

            var serverError = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, serverError.StatusCode);
        }
    }
}
