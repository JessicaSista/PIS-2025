using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using OmniMonitor.Server.Context;
using OmniMonitor.Server.Controllers;
using OmniMonitor.Server.Services;
using OmniMonitor.Server.Configuration;
using OmniMonitor.Shared.Dtos;
using OmniMonitor.Shared.Dtos.EM;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Xunit;

namespace OmniMonitor.Tests
{
    /// <summary>
    /// Tests de integración que verifican el flujo completo:
    /// Frontend -> DatasetEMController -> DatasetEMService -> SondaEMService -> API Externa EM
    /// 
    /// Estos tests simulan el comportamiento real del sistema verificando:
    /// 1. El flujo de datos desde el controller hasta la API externa
    /// 2. Las validaciones en cada capa
    /// 3. El manejo de errores a lo largo del pipeline
    /// 4. Los permisos y autenticación
    /// </summary>
    public class DatasetEMIntegrationTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly Mock<ISondaAuthService> _mockAuthService;
        private readonly HttpClient _httpClient;
        private readonly DatasetEMController _controller;
        private readonly DatasetEMService _datasetService;
        private readonly SondaEMService _sondaService;

        public DatasetEMIntegrationTests()
        {
            // Configurar base de datos en memoria
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);

            // Configurar HttpClient mock
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            mockHttpClientFactory.Setup(f => f.CreateClient()).Returns(_httpClient);

            // Configurar autenticación mock
            _mockAuthService = new Mock<ISondaAuthService>();
            _mockAuthService.Setup(a => a.GetUserTokenEMAsync("testuser", "testpass"))
                          .ReturnsAsync("test-token-123");

            // Configurar API
            var apiConfig = new ApiConfig
            {
                BaseUrl = new BaseUrls { UrlEM = "https://api.em.test/" },
                EndpointsEM = new Dictionary<string, Dictionary<string, string>>
                {
                    ["Alert"] = new Dictionary<string, string>
                    {
                        ["GetAll"] = "alerts",
                        ["GetById"] = "alerts/{alertId}"
                    },
                    ["Event"] = new Dictionary<string, string>
                    {
                        ["GetEvents"] = "events",
                        ["GetById"] = "events/{eventId}"
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

            // Crear instancias de servicios
            _sondaService = new SondaEMService(mockHttpClientFactory.Object, _mockAuthService.Object, Options.Create(apiConfig));
            _datasetService = new DatasetEMService(_context, _sondaService);
            _controller = new DatasetEMController(_datasetService);

            // Configurar el contexto del controller para simular autenticación
            SetupControllerContext();

            // Seed inicial
            SeedTestData();
        }

        private void SetupControllerContext()
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim("Permission", "Crear Datasets EM"),
                new Claim("Permission", "Ver Datasets EM"),
                new Claim("Permission", "Eliminar Datasets EM")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        private void SeedTestData()
        {
            _context.Users.Add(new User 
            { 
                Id = 1, 
                Username = "testuser", 
                Password = "testpass" 
            });

            _context.SaveChanges();
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

        public void Dispose()
        {
            _context.Dispose();
            _httpClient.Dispose();
        }

        #region Flujo Completo - Crear Dataset Individual

        [Fact]
        public async Task FullFlow_CreateIndividualDatasetWithAlerts_Success()
        {
            // Arrange - Crear un dataset individual con alerts específicos
            var request = new CreateDatasetEMRequest
            {
                Name = "Integration Test Dataset",
                Description = "Dataset para test de integración",
                Username = "testuser",
                IsDataset = "N", // Individual
                AlertIds = new List<int> { 1, 2, 3 }
            };

            // Act - Llamar al controller (simula llamada desde el frontend)
            var result = await _controller.CreateDataset(request);

            // Assert - Verificar respuesta del controller
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(201, createdResult.StatusCode);

            var createdDataset = Assert.IsType<DatasetEM>(createdResult.Value);
            Assert.Equal("Integration Test Dataset", createdDataset.Name);
            Assert.Equal("N", createdDataset.Is_Dataset);
            Assert.Equal("1", createdDataset.ContentType); // ContentType = "1" para alerts
            Assert.Equal(3, createdDataset.DatasetAlerts.Count);

            // Verificar que se guardó en la base de datos
            var savedDataset = await _context.DatasetsEM
                .Include(d => d.DatasetAlerts)
                .FirstAsync(d => d.Id == createdDataset.Id);
            
            Assert.Equal("Integration Test Dataset", savedDataset.Name);
            Assert.Equal(3, savedDataset.DatasetAlerts.Count);
            Assert.Contains(savedDataset.DatasetAlerts, a => a.Id_alert == 1);
            Assert.Contains(savedDataset.DatasetAlerts, a => a.Id_alert == 2);
            Assert.Contains(savedDataset.DatasetAlerts, a => a.Id_alert == 3);
        }

        #endregion

        #region Flujo Completo - Crear Dataset Formal con Búsqueda Dinámica

        [Fact]
        public async Task FullFlow_CreateFormalDatasetAndRetrieveWithDynamicSearch_Success()
        {
            // Arrange - Crear dataset formal con filtros para búsqueda dinámica
            var createRequest = new CreateDatasetEMRequest
            {
                Name = "Formal Dataset Dynamic Search",
                Username = "testuser",
                IsDataset = "S", // Formal
                AlertId = 1,
                AlertState = "Active"
            };

            // Mock de respuesta de la API externa para alerts
            var mockAlerts = new List<AlertDto>
            {
                new AlertDto { AlertId = 1, AlertName = "Alert 1", AlertState = "Active" },
                new AlertDto { AlertId = 5, AlertName = "Alert 5", AlertState = "Active" }
            };
            var apiResponse = new AlertApiResponse { Results = mockAlerts };
            var jsonResponse = JsonSerializer.Serialize(apiResponse);
            SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

            // Act 1 - Crear el dataset
            var createResult = await _controller.CreateDataset(createRequest);

            // Assert 1 - Verificar creación
            var createdResult = Assert.IsType<CreatedAtActionResult>(createResult.Result);
            var createdDataset = Assert.IsType<DatasetEM>(createdResult.Value);
            Assert.Equal("S", createdDataset.Is_Dataset);
            Assert.Equal("0", createdDataset.ContentType); // ContentType = "0" para formal

            // Act 2 - Obtener el dataset (esto debe activar la búsqueda dinámica)
            var getResult = await _controller.GetDatasetById(createdDataset.Id, "testuser");

            // Assert 2 - Verificar que se realizó la búsqueda dinámica
            var okResult = Assert.IsType<OkObjectResult>(getResult.Result);
            var retrievedDataset = Assert.IsType<DatasetEM>(okResult.Value);

            // Debe tener solo el alert con ID = 1 (filtrado por Id_Alert)
            Assert.Single(retrievedDataset.DatasetAlerts);
            Assert.Equal(1, retrievedDataset.DatasetAlerts.First().Id_alert);

            // Verificar que se llamó a la API externa
            _mockAuthService.Verify(a => a.GetUserTokenEMAsync("testuser", "testpass"), Times.Once);
        }

        #endregion

        #region Flujo Completo - Validaciones de Error

        [Fact]
        public async Task FullFlow_CreateDatasetWithDuplicateName_Returns400()
        {
            // Arrange - Crear primer dataset
            var firstRequest = new CreateDatasetEMRequest
            {
                Name = "Duplicate Name Dataset",
                Username = "testuser",
                IsDataset = "S"
            };

            await _controller.CreateDataset(firstRequest);

            // Intentar crear segundo dataset con mismo nombre
            var duplicateRequest = new CreateDatasetEMRequest
            {
                Name = "Duplicate Name Dataset", // Mismo nombre
                Username = "testuser",
                IsDataset = "N"
            };

            // Act
            var result = await _controller.CreateDataset(duplicateRequest);

            // Assert - Debe retornar 400 Bad Request
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(400, badRequestResult.StatusCode);
            Assert.Contains("Ya existe un dataset", badRequestResult.Value.ToString());
            Assert.Contains("Duplicate Name Dataset", badRequestResult.Value.ToString());
        }

        [Fact]
        public async Task FullFlow_GetNonExistentDataset_Returns404()
        {
            // Act - Intentar obtener dataset que no existe
            var result = await _controller.GetDatasetById(999, "testuser");

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal(404, notFoundResult.StatusCode);
            Assert.Contains("No se encontró el dataset con ID 999", notFoundResult.Value.ToString());
        }

        [Fact]
        public async Task FullFlow_CreateDatasetWithInvalidRequest_Returns400()
        {
            // Arrange - Request con datos inválidos
            var invalidRequest = new CreateDatasetEMRequest
            {
                Name = "", // Nombre vacío (inválido)
                Username = "testuser",
                IsDataset = "S"
            };

            // Act
            var result = await _controller.CreateDataset(invalidRequest);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(400, badRequestResult.StatusCode);
            Assert.Equal("El nombre del dataset es requerido.", badRequestResult.Value);
        }

        #endregion

        #region Flujo Completo - Update Dataset

        [Fact]
        public async Task FullFlow_UpdateDataset_Success()
        {
            // Arrange - Crear dataset inicial
            var createRequest = new CreateDatasetEMRequest
            {
                Name = "Original Dataset",
                Description = "Original Description",
                Username = "testuser",
                IsDataset = "N",
                EventIds = new List<int> { 1, 2 }
            };

            var createResult = await _controller.CreateDataset(createRequest);
            var createdDataset = Assert.IsType<DatasetEM>(((CreatedAtActionResult)createResult.Result).Value);

            // Preparar request de actualización
            var updateRequest = new CreateDatasetEMRequest
            {
                Name = "Updated Dataset",
                Description = "Updated Description",
                Username = "testuser",
                IsDataset = "N",
                AlertIds = new List<int> { 3, 4, 5 } // Cambiar de events a alerts
            };

            // Act - Actualizar el dataset
            var updateResult = await _controller.UpdateDataset(createdDataset.Id, updateRequest);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(updateResult.Result);
            var updatedDataset = Assert.IsType<DatasetEM>(okResult.Value);

            Assert.Equal("Updated Dataset", updatedDataset.Name);
            Assert.Equal("Updated Description", updatedDataset.Description);
            Assert.Equal("1", updatedDataset.ContentType); // Cambió a alerts (ContentType = "1")

            // Verificar en base de datos
            var datasetInDb = await _context.DatasetsEM
                .Include(d => d.DatasetAlerts)
                .Include(d => d.DatasetEvents)
                .FirstAsync(d => d.Id == createdDataset.Id);

            Assert.Equal("Updated Dataset", datasetInDb.Name);
            Assert.Equal(3, datasetInDb.DatasetAlerts.Count); // Ahora tiene alerts
            Assert.Empty(datasetInDb.DatasetEvents); // Ya no tiene events
        }

        #endregion

        #region Flujo Completo - Delete Dataset

        [Fact]
        public async Task FullFlow_DeleteDataset_Success()
        {
            // Arrange - Crear dataset
            var createRequest = new CreateDatasetEMRequest
            {
                Name = "Dataset To Delete",
                Username = "testuser",
                IsDataset = "S"
            };

            var createResult = await _controller.CreateDataset(createRequest);
            var createdDataset = Assert.IsType<DatasetEM>(((CreatedAtActionResult)createResult.Result).Value);

            // Verificar que existe en la BD
            var existingDataset = await _context.DatasetsEM.FindAsync(createdDataset.Id);
            Assert.NotNull(existingDataset);

            // Act - Eliminar el dataset
            var deleteResult = await _controller.DeleteDataset(createdDataset.Id, "testuser");

            // Assert
            Assert.IsType<NoContentResult>(deleteResult);

            // Verificar que se eliminó de la BD
            var deletedDataset = await _context.DatasetsEM.FindAsync(createdDataset.Id);
            Assert.Null(deletedDataset);
        }

        [Fact]
        public async Task FullFlow_DeleteNonExistentDataset_Returns404()
        {
            // Act - Intentar eliminar dataset que no existe
            var result = await _controller.DeleteDataset(999, "testuser");

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(404, notFoundResult.StatusCode);
            Assert.Contains("No se encontró el dataset con ID 999", notFoundResult.Value.ToString());
        }

        #endregion

        #region Flujo Completo - API Externa Errors

        [Fact]
        public async Task FullFlow_FormalDatasetWithAPIError_HandlesGracefully()
        {
            // Arrange - Crear dataset formal
            var createRequest = new CreateDatasetEMRequest
            {
                Name = "Dataset with API Error",
                Username = "testuser",
                IsDataset = "S",
                AlertState = "Active"
            };

            var createResult = await _controller.CreateDataset(createRequest);
            var createdDataset = Assert.IsType<DatasetEM>(((CreatedAtActionResult)createResult.Result).Value);

            // Configurar respuesta de error de la API externa
            SetupHttpResponse(HttpStatusCode.Unauthorized);

            // Act & Assert - Al obtener el dataset, debería manejar el error de la API
            var exception = await Assert.ThrowsAsync<Exception>(
                () => _controller.GetDatasetById(createdDataset.Id, "testuser"));

            Assert.Contains("No tienes permisos: token inválido o expirado", exception.Message);
        }

        [Fact]
        public async Task FullFlow_FormalDatasetWithAPINotFound_ReturnsDatasetWithoutDynamicData()
        {
            // Arrange - Crear dataset formal
            var createRequest = new CreateDatasetEMRequest
            {
                Name = "Dataset with API Not Found",
                Username = "testuser",
                IsDataset = "S",
                Id_Resource = 999 // Resource que no existe
            };

            var createResult = await _controller.CreateDataset(createRequest);
            var createdDataset = Assert.IsType<DatasetEM>(((CreatedAtActionResult)createResult.Result).Value);

            // Configurar respuesta 404 de la API externa
            SetupHttpResponse(HttpStatusCode.NotFound);

            // Act - Obtener el dataset
            var getResult = await _controller.GetDatasetById(createdDataset.Id, "testuser");

            // Assert - Debe retornar el dataset sin los datos dinámicos
            var okResult = Assert.IsType<OkObjectResult>(getResult.Result);
            var retrievedDataset = Assert.IsType<DatasetEM>(okResult.Value);

            Assert.Empty(retrievedDataset.DatasetResources); // No debe tener resources porque la API retornó 404
            Assert.Equal(999, retrievedDataset.Id_Resource); // Pero debe mantener el filtro original
        }

        #endregion

        #region Flujo Completo - Obtener Todos los Datasets

        [Fact]
        public async Task FullFlow_GetAllDatasetsForUser_ReturnsOnlyUserDatasets()
        {
            // Arrange - Crear varios datasets para diferentes usuarios
            var user1Requests = new[]
            {
                new CreateDatasetEMRequest { Name = "User1 Dataset 1", Username = "testuser", IsDataset = "S" },
                new CreateDatasetEMRequest { Name = "User1 Dataset 2", Username = "testuser", IsDataset = "N" }
            };

            // Crear datasets para testuser
            foreach (var request in user1Requests)
            {
                await _controller.CreateDataset(request);
            }

            // Crear dataset para otro usuario directamente en BD (simula que otro usuario también tiene datasets)
            _context.DatasetsEM.Add(new DatasetEM
            {
                Name = "Other User Dataset",
                Username = "otheruser",
                Is_Dataset = "S"
            });
            await _context.SaveChangesAsync();

            // Act - Obtener todos los datasets del usuario testuser
            var result = await _controller.GetAllDatasets("testuser");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var datasets = Assert.IsType<List<DatasetEM>>(okResult.Value);

            Assert.Equal(2, datasets.Count); // Solo los 2 de testuser
            Assert.All(datasets, d => Assert.Equal("testuser", d.Username));
            Assert.DoesNotContain(datasets, d => d.Name == "Other User Dataset");
        }

        #endregion
    }
}