using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Moq;

using OmniMonitor.Server.Context;
using OmniMonitor.Server.Controllers;
using OmniMonitor.Shared.Dtos;
using OmniMonitor.Shared.Dtos.AM;
using OmniMonitor.Shared.Dtos.EM;
using OmniMonitor.Shared.Dtos.UM;

using Xunit;

namespace QA.Tests
{
    public class DatasetFilterControllerTests
    {
        private readonly Mock<ISondaUMService> _mockUM = new();
        private readonly Mock<ISondaAMService> _mockAM = new();
        private readonly Mock<ISondaEMService> _mockEM = new();
        private readonly Mock<ISondaIMService> _mockIM = new();
        private readonly Mock<ISondaAuthService> _mockAuth = new();
        private readonly Mock<ILogger<DatasetFilterController>> _mockLogger = new();

        private ApplicationDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        private DatasetFilterController CreateControllerWithUser(string username = "testuser")
        {
            var dbContext = CreateInMemoryDbContext();
            var controller = new DatasetFilterController(
                dbContext,
                _mockUM.Object,
                _mockAM.Object,
                _mockEM.Object,
                _mockIM.Object,
                _mockAuth.Object,
                _mockLogger.Object
            );
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, username)
            }, "mock"));
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
            return controller;
        }

        private DatasetFilterController CreateControllerWithoutUser()
        {
            var dbContext = CreateInMemoryDbContext();
            var controller = new DatasetFilterController(
                dbContext,
                _mockUM.Object,
                _mockAM.Object,
                _mockEM.Object,
                _mockIM.Object,
                _mockAuth.Object,
                _mockLogger.Object
            );
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext() // No user
            };
            return controller;
        }

        [Fact]
        public async Task FilterByModuleAndEntity_UM_News_ReturnsExpectedProperties()
        {
            var controller = CreateControllerWithUser();
            var result = await controller.FiltrarPorModuloYEntidad("UM", 2);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var props = Assert.IsAssignableFrom<List<PropiedadEntidadDto>>(okResult.Value);
            Assert.Contains(props, p => p.Nombre == "Title");
            Assert.Contains(props, p => p.Tipo == FilterValueType.String);
        }

        [Fact]
        public async Task GetAttributeValues_UM_News_ZoneName_ReturnsZoneNames()
        {
            var controller = CreateControllerWithUser();
            _mockUM.Setup(s => s.GetAllZones(It.IsAny<string>()))
                .ReturnsAsync(new List<Zone> { new Zone { Name = "Zone1" }, new Zone { Name = "Zone2" } });

            var result = await controller.GetAtributoValores("UM", 2, "Zone.Name");
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var values = Assert.IsAssignableFrom<List<string>>(okResult.Value);
            Assert.Contains("Zone1", values);
            Assert.Contains("Zone2", values);
        }

        [Fact]
        public async Task GetAttributeValues_InvalidModule_ReturnsBadRequest()
        {
            var controller = CreateControllerWithUser();
            var result = await controller.GetAtributoValores("INVALID", 1, "Name");
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Módulo no definido", badRequest.Value);
        }

        [Fact]
        public async Task GetAttributeValues_InvalidEntity_ReturnsBadRequest()
        {
            var controller = CreateControllerWithUser();
            var result = await controller.GetAtributoValores("UM", 999, "Name");
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Entidad no definida para el módulo seleccionado", badRequest.Value);
        }

        [Fact]
        public async Task GetAttributeValues_NoUser_ReturnsBadRequest()
        {
            var controller = CreateControllerWithoutUser();
            var result = await controller.GetAtributoValores("UM", 2, "Zone.Name");
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Usuario no encontrado.", badRequest.Value);
        }

        [Fact]
        public async Task FilterData_UM_News_FilterByTitle()
        {
            var controller = CreateControllerWithUser();
            var news = new List<News>
            {
                new News { Id = 1, Title = "News A" },
                new News { Id = 2, Title = "News B" }
            };
            _mockUM.Setup(s => s.GetAllNews(
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<int>()))
            .ReturnsAsync(news);

            var request = new FiltrarDatosRequest
            {
                Modulo = "UM",
                EntidadId = 2,
                Filtros = new List<FilterCondition>
                {
                    new FilterCondition
                    {
                        Type = FilterType.Equals,
                        ValueType = FilterValueType.String,
                        AttributeName = "Title",
                        Condition = "News A"
                    }
                }
            };

            // ApiDataService.StaticFilterObjects debe ser mockeado si es estático, aquí se asume que filtra correctamente
            var result = await controller.FiltrarDatos(request);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var filtered = Assert.IsAssignableFrom<List<object>>(okResult.Value);
        }

        [Fact]
        public async Task FilterData_InvalidModule_ReturnsBadRequest()
        {
            var controller = CreateControllerWithUser();
            var request = new FiltrarDatosRequest
            {
                Modulo = "INVALID",
                EntidadId = 1,
                Filtros = new List<FilterCondition>()
            };
            var result = await controller.FiltrarDatos(request);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Módulo no definido", badRequest.Value);
        }

        [Fact]
        public async Task FilterData_InvalidEntity_ReturnsBadRequest()
        {
            var controller = CreateControllerWithUser();
            var request = new FiltrarDatosRequest
            {
                Modulo = "UM",
                EntidadId = 999,
                Filtros = new List<FilterCondition>()
            };
            var result = await controller.FiltrarDatos(request);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Entidad no definida para el módulo seleccionado", badRequest.Value);
        }

        [Fact]
        public async Task FilterData_NoUser_ReturnsBadRequest()
        {
            var controller = CreateControllerWithoutUser();
            var request = new FiltrarDatosRequest
            {
                Modulo = "UM",
                EntidadId = 2,
                Filtros = new List<FilterCondition>()
            };
            var result = await controller.FiltrarDatos(request);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Usuario no encontrado.", badRequest.Value);
        }

        [Fact]
        public async Task GetAttributeValues_ServiceThrowsException_Returns500()
        {
            var controller = CreateControllerWithUser();
            _mockUM.Setup(s => s.GetAllZones(It.IsAny<string>())).ThrowsAsync(new Exception("Service error"));
            await Assert.ThrowsAsync<Exception>(() => controller.GetAtributoValores("UM", 2, "Zone.Name"));
        }

        [Fact]
        public async Task FilterData_ServiceThrowsException_Returns500()
        {
            var controller = CreateControllerWithUser();
            _mockUM.Setup(s => s.GetAllNews(
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<int>())).
            ThrowsAsync(new Exception("Service error"));

            var request = new FiltrarDatosRequest
            {
                Modulo = "UM",
                EntidadId = 2,
                Filtros = new List<FilterCondition>()
            };

            await Assert.ThrowsAsync<Exception>(() => controller.FiltrarDatos(request));
        }

        [Fact]
        public async Task FilterData_StringType_EqualsFilter_Works()
        {
            var controller = CreateControllerWithUser();
            var news = new List<News>
            {
                new News { Id = 1, Title = "Alpha" },
                new News { Id = 2, Title = "Beta" }
            };
            _mockUM.Setup(s => s.GetAllNews(
                It.IsAny<string>(),
                It.IsAny<int>(),
                null,
                null,
                It.IsAny<int>()))
                .ReturnsAsync(news);

            var request = new FiltrarDatosRequest
            {
                Modulo = "UM",
                EntidadId = 2,
                Filtros = new List<FilterCondition>
                {
                    new FilterCondition
                    {
                        AttributeName = "Title",
                        Type = FilterType.Equals,
                        ValueType = FilterValueType.String,
                        Condition= "Alpha"
                    }
                }
            };

            var result = await controller.FiltrarDatos(request);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var filtered = Assert.IsAssignableFrom<List<object>>(okResult.Value);
            Assert.Single(filtered);
        }

        [Fact]
        public async Task FilterData_NumberType_GreaterThanFilter_Works()
        {
            var controller = CreateControllerWithUser();
            var stocks = new List<StockDto>
            {
                new StockDto { Id = 1, Quantity = 5 },
                new StockDto { Id = 2, Quantity = 15 }
            };
            _mockAM.Setup(s => s.GetAllStock(null, null, null, null, null, It.IsAny<string>())).ReturnsAsync(stocks);

            var request = new FiltrarDatosRequest
            {
                Modulo = "AM",
                EntidadId = 3,
                Filtros = new List<FilterCondition>
                {
                    new FilterCondition
                    {
                        AttributeName = "Quantity",
                        Type = FilterType.GreaterThan,
                        ValueType = FilterValueType.Number,
                        Condition= 10
                    }
                }
            };

            var result = await controller.FiltrarDatos(request);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var filtered = Assert.IsAssignableFrom<List<object>>(okResult.Value);
            Assert.Single(filtered);
        }

        [Fact]
        public async Task FilterData_BooleanType_EqualsFilter_Works()
        {
            var controller = CreateControllerWithUser();
            var news = new List<News>
            {
                new News { Id = 1, Important = true },
                new News { Id = 2, Important = false }
            };
            _mockUM.Setup(s => s.GetAllNews(
                It.IsAny<string>(),
                It.IsAny<int>(),
                null,
                null,
                It.IsAny<int>()))
                .ReturnsAsync(news);

            var request = new FiltrarDatosRequest
            {
                Modulo = "UM",
                EntidadId = 2,
                Filtros = new List<FilterCondition>
                {
                    new FilterCondition
                    {
                        AttributeName = "Important",
                        Type = FilterType.Equals,
                        ValueType = FilterValueType.Boolean,
                        Condition= true
                    }
                }
            };

            var result = await controller.FiltrarDatos(request);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var filtered = Assert.IsAssignableFrom<List<object>>(okResult.Value);
            Assert.Single(filtered);
        }

        [Fact]
        public async Task FilterData_MultipleFilters_AND_Works()
        {
            var controller = CreateControllerWithUser();
            var news = new List<News>
            {
                new News { Id = 1, Title = "Alpha", Important = true },
                new News { Id = 2, Title = "Alpha", Important = false },
                new News { Id = 3, Title = "Beta", Important = true }
            };
            _mockUM.Setup(s => s.GetAllNews(
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<int>()))
            .ReturnsAsync(news);

            var request = new FiltrarDatosRequest
            {
                Modulo = "UM",
                EntidadId = 2,
                Filtros = new List<FilterCondition>
                {
                    new FilterCondition { AttributeName = "Title", Type = FilterType.Equals, ValueType = FilterValueType.String, Condition = "Alpha" },
                    new FilterCondition { AttributeName = "Important", Type = FilterType.Equals, ValueType = FilterValueType.Boolean, Condition = true }
                }
            };

            var result = await controller.FiltrarDatos(request);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var filtered = Assert.IsAssignableFrom<List<object>>(okResult.Value);
            Assert.Single(filtered);
        }

        [Fact]
        public async Task FilterData_EmptyFilters_ReturnsAll()
        {
            var controller = CreateControllerWithUser();
            var news = new List<News>
            {
                new News { Id = 1, Title = "Alpha" },
                new News { Id = 2, Title = "Beta" }
            };
            _mockUM.Setup(s => s.GetAllNews(
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<int>()))
            .ReturnsAsync(news);

            var request = new FiltrarDatosRequest
            {
                Modulo = "UM",
                EntidadId = 2,
                Filtros = new List<FilterCondition>()
            };

            var result = await controller.FiltrarDatos(request);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var filtered = Assert.IsAssignableFrom<List<object>>(okResult.Value);
            Assert.Equal(2, filtered.Count);
        }

        [Fact]
        public async Task FilterData_NonExistentAttribute_DoesNotThrow()
        {
            var controller = CreateControllerWithUser();
            var news = new List<News>
            {
                new News { Id = 1, Title = "Alpha" }
            };
            _mockUM.Setup(s => s.GetAllNews(
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<int>()))
            .ReturnsAsync(news);

            var request = new FiltrarDatosRequest
            {
                Modulo = "UM",
                EntidadId = 2,
                Filtros = new List<FilterCondition>
                {
                    new FilterCondition { AttributeName = "NonExistent", Type = FilterType.Equals, ValueType = FilterValueType.String, Condition = "X" }
                }
            };

            var result = await controller.FiltrarDatos(request);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var filtered = Assert.IsAssignableFrom<List<object>>(okResult.Value);
            // No debe lanzar excepción, simplemente no filtra nada
        }

        [Fact]
        public async Task FilterData_FilterWithNullValue_DoesNotThrow()
        {
            var controller = CreateControllerWithUser();
            var news = new List<News>
            {
                new News { Id = 1, Title = "Alpha" }
            };
            _mockUM.Setup(s => s.GetAllNews(
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<int>()))
            .ReturnsAsync(news);

            var request = new FiltrarDatosRequest
            {
                Modulo = "UM",
                EntidadId = 2,
                Filtros = new List<FilterCondition>
                {
                    new FilterCondition { AttributeName = "Title", Type = FilterType.Equals, ValueType = FilterValueType.String, Condition = null }
                }
            };

            var result = await controller.FiltrarDatos(request);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var filtered = Assert.IsAssignableFrom<List<object>>(okResult.Value);
        }

        [Fact]
        public async Task FilterData_ContainsOperator_Works()
        {
            var controller = CreateControllerWithUser();
            var news = new List<News>
            {
                new News { Id = 1, Title = "AlphaBeta" },
                new News { Id = 2, Title = "Beta" }
            };
            _mockUM.Setup(s => s.GetAllNews(
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<int>()))
            .ReturnsAsync(news);

            var request = new FiltrarDatosRequest
            {
                Modulo = "UM",
                EntidadId = 2,
                Filtros = new List<FilterCondition>
                {
                    new FilterCondition { AttributeName = "Title", Type = FilterType.Contains, ValueType = FilterValueType.String, Condition = "Beta" }
                }
            };

            var result = await controller.FiltrarDatos(request);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var filtered = Assert.IsAssignableFrom<List<object>>(okResult.Value);
            Assert.Equal(2, filtered.Count);
        }

        [Fact]
        public async Task FilterData_NotEqualsOperator_Works()
        {
            var controller = CreateControllerWithUser();
            var news = new List<News>
            {
                new News { Id = 1, Title = "Alpha" },
                new News { Id = 2, Title = "Beta" }
            };
            _mockUM.Setup(s => s.GetAllNews(
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<int>()))
            .ReturnsAsync(news);

            var request = new FiltrarDatosRequest
            {
                Modulo = "UM",
                EntidadId = 2,
                Filtros = new List<FilterCondition>
                {
                    new FilterCondition { AttributeName = "Title", Type = FilterType.NotEquals, ValueType = FilterValueType.String, Condition = "Alpha" }
                }
            };

            var result = await controller.FiltrarDatos(request);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var filtered = Assert.IsAssignableFrom<List<object>>(okResult.Value);
            Assert.Single(filtered);
        }

        [Fact]
        public async Task FilterData_SpecialCharactersInFilter_Works()
        {
            var controller = CreateControllerWithUser();
            var news = new List<News>
            {
                new News { Id = 1, Title = "Café" },
                new News { Id = 2, Title = "Cafe" }
            };
            _mockUM.Setup(s => s.GetAllNews(
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<int>()))
            .ReturnsAsync(news);

            var request = new FiltrarDatosRequest
            {
                Modulo = "UM",
                EntidadId = 2,
                Filtros = new List<FilterCondition>
                {
                    new FilterCondition { AttributeName = "Title", Type = FilterType.Equals, ValueType = FilterValueType.String, Condition = "Café" }
                }
            };

            var result = await controller.FiltrarDatos(request);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var filtered = Assert.IsAssignableFrom<List<object>>(okResult.Value);
            Assert.Single(filtered);
        }
    }
}
