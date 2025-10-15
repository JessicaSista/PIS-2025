using Microsoft.EntityFrameworkCore;
using Moq;
using OmniMonitor.Server.Context;
using OmniMonitor.Server.Services;
using OmniMonitor.Shared.Dtos;
using OmniMonitor.Shared.Dtos.EM;
using Xunit;

namespace OmniMonitor.Tests
{
    /// <summary>
    /// Tests para DatasetEMService que verifican:
    /// 1. Lógica de negocio y validaciones
    /// 2. Búsqueda dinámica para datasets formales
    /// 3. Integración con SondaEMService
    /// 4. Operaciones CRUD con base de datos
    /// </summary>
    public class DatasetEMServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ISondaEMService> _mockSondaService;
        private readonly DatasetEMService _service;

        public DatasetEMServiceTests()
        {
            // Configurar base de datos en memoria
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _mockSondaService = new Mock<ISondaEMService>();
            _service = new DatasetEMService(_context, _mockSondaService.Object);

            // Seed inicial para tests
            SeedTestData();
        }

        private void SeedTestData()
        {
            // Agregar usuarios de prueba
            _context.Users.AddRange(
                new User { Id = 1, Username = "testuser", Password = "testpass" },
                new User { Id = 2, Username = "otheruser", Password = "otherpass" }
            );

            // Agregar datasets existentes
            _context.DatasetsEM.AddRange(
                new DatasetEM
                {
                    Id = 1,
                    Name = "Existing Dataset",
                    Username = "testuser",
                    Is_Dataset = "S",
                    ContentType = "0"
                }
            );

            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        #region CreateDatasetEMAsync Tests

        [Fact]
        public async Task CreateDatasetEMAsync_ValidFormalDataset_CreatesSuccessfully()
        {
            // Arrange
            var request = new CreateDatasetEMRequest
            {
                Name = "New Formal Dataset",
                Description = "Test Description",
                Username = "testuser",
                IsDataset = "S",
                AlertId = 1,
                AlertState = "Active"
            };

            // Act
            var result = await _service.CreateDatasetEMAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Id > 0);
            Assert.Equal(request.Name, result.Name);
            Assert.Equal(request.Description, result.Description);
            Assert.Equal(request.Username, result.Username);
            Assert.Equal("S", result.Is_Dataset);
            Assert.Equal("0", result.ContentType); // 0 para dataset formal
            Assert.Equal(1, result.Id_Alert);
            Assert.Equal("Active", result.AlertState);

            // Verificar que se guardó en la base de datos
            var savedDataset = await _context.DatasetsEM.FindAsync(result.Id);
            Assert.NotNull(savedDataset);
            Assert.Equal(request.Name, savedDataset.Name);
        }

        [Fact]
        public async Task CreateDatasetEMAsync_ValidIndividualDatasetWithAlerts_CreatesWithCorrectContentType()
        {
            // Arrange
            var request = new CreateDatasetEMRequest
            {
                Name = "Alert Dataset",
                Username = "testuser",
                IsDataset = "N",
                AlertIds = new List<int> { 1, 2, 3 }
            };

            // Act
            var result = await _service.CreateDatasetEMAsync(request);

            // Assert
            Assert.Equal("N", result.Is_Dataset);
            Assert.Equal("1", result.ContentType); // 1 para alerts
            Assert.Equal(3, result.DatasetAlerts.Count);
            Assert.All(result.DatasetAlerts, alert => Assert.Contains(alert.Id_alert, request.AlertIds));
        }

        [Fact]
        public async Task CreateDatasetEMAsync_ValidIndividualDatasetWithEvents_CreatesWithCorrectContentType()
        {
            // Arrange
            var request = new CreateDatasetEMRequest
            {
                Name = "Event Dataset",
                Username = "testuser",
                IsDataset = "N",
                EventIds = new List<int> { 1, 2 }
            };

            // Act
            var result = await _service.CreateDatasetEMAsync(request);

            // Assert
            Assert.Equal("N", result.Is_Dataset);
            Assert.Equal("2", result.ContentType); // 2 para events
            Assert.Equal(2, result.DatasetEvents.Count);
            Assert.All(result.DatasetEvents, evt => Assert.Contains(evt.Id_event, request.EventIds));
        }

        [Fact]
        public async Task CreateDatasetEMAsync_ValidIndividualDatasetWithExtensions_CreatesWithCorrectContentType()
        {
            // Arrange
            var request = new CreateDatasetEMRequest
            {
                Name = "Extension Dataset",
                Username = "testuser",
                IsDataset = "N",
                ExtensionIds = new List<int> { 1, 2 }
            };

            // Act
            var result = await _service.CreateDatasetEMAsync(request);

            // Assert
            Assert.Equal("N", result.Is_Dataset);
            Assert.Equal("3", result.ContentType); // 3 para extensions
            Assert.Equal(2, result.DatasetExtensions.Count);
            Assert.All(result.DatasetExtensions, ext => Assert.Contains(ext.Id_extension, request.ExtensionIds));
        }

        [Fact]
        public async Task CreateDatasetEMAsync_ValidIndividualDatasetWithResources_CreatesWithCorrectContentType()
        {
            // Arrange
            var request = new CreateDatasetEMRequest
            {
                Name = "Resource Dataset",
                Username = "testuser",
                IsDataset = "N",
                ResourceIds = new List<int> { 1, 2 }
            };

            // Act
            var result = await _service.CreateDatasetEMAsync(request);

            // Assert
            Assert.Equal("N", result.Is_Dataset);
            Assert.Equal("4", result.ContentType); // 4 para resources
            Assert.Equal(2, result.DatasetResources.Count);
            Assert.All(result.DatasetResources, res => Assert.Contains(res.Id_resource, request.ResourceIds));
        }

        [Fact]
        public async Task CreateDatasetEMAsync_DuplicateName_ThrowsInvalidOperationException()
        {
            // Arrange
            var request = new CreateDatasetEMRequest
            {
                Name = "Existing Dataset", // Ya existe en SeedTestData
                Username = "testuser",
                IsDataset = "S"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CreateDatasetEMAsync(request));

            Assert.Contains("Ya existe un dataset con el nombre", exception.Message);
            Assert.Contains("Existing Dataset", exception.Message);
            Assert.Contains("testuser", exception.Message);
        }

        #endregion

        #region GetAllDatasetsEMAsync Tests

        [Fact]
        public async Task GetAllDatasetsEMAsync_ValidUsername_ReturnsUserDatasets()
        {
            // Arrange
            var username = "testuser";

            // Agregar más datasets para el test
            _context.DatasetsEM.AddRange(
                new DatasetEM { Name = "Dataset 2", Username = username, Is_Dataset = "S" },
                new DatasetEM { Name = "Dataset 3", Username = username, Is_Dataset = "N" },
                new DatasetEM { Name = "Other User Dataset", Username = "otheruser", Is_Dataset = "S" }
            );
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetAllDatasetsEMAsync(username);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count); // Solo los del usuario testuser
            Assert.All(result, dataset => Assert.Equal(username, dataset.Username));
            Assert.True(result.All(d => d.Name != "Other User Dataset")); // No debe incluir datasets de otros usuarios
        }

        [Fact]
        public async Task GetAllDatasetsEMAsync_NonExistentUser_ReturnsEmptyList()
        {
            // Act
            var result = await _service.GetAllDatasetsEMAsync("nonexistentuser");

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion

        #region GetDatasetEMByIdAsync Tests (con búsqueda dinámica)

        [Fact]
        public async Task GetDatasetEMByIdAsync_IndividualDataset_ReturnsWithoutDynamicSearch()
        {
            // Arrange
            var dataset = new DatasetEM
            {
                Name = "Individual Dataset",
                Username = "testuser",
                Is_Dataset = "N",
                ContentType = "1"
            };
            dataset.DatasetAlerts.Add(new DatasetAlert { Id_alert = 1 });

            _context.DatasetsEM.Add(dataset);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetDatasetEMByIdAsync(dataset.Id, "testuser");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dataset.Id, result.Id);
            Assert.Single(result.DatasetAlerts);
            Assert.Equal(1, result.DatasetAlerts.First().Id_alert);

            // Verificar que no se llamó a SondaEMService (no es búsqueda dinámica)
            _mockSondaService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetDatasetEMByIdAsync_FormalDatasetWithAlertFilter_PerformsDynamicSearch()
        {
            // Arrange
            var dataset = new DatasetEM
            {
                Name = "Formal Dataset with Alert",
                Username = "testuser",
                Is_Dataset = "S",
                ContentType = "0",
                Id_Alert = 1,
                AlertState = "Active"
            };

            _context.DatasetsEM.Add(dataset);
            await _context.SaveChangesAsync();

            // Mock alerts desde la API
            var mockAlerts = new List<AlertDto>
            {
                new AlertDto { AlertId = 1, AlertName = "Alert 1", AlertState = "Active" },
                new AlertDto { AlertId = 2, AlertName = "Alert 2", AlertState = "Active" }
            };

            _mockSondaService.Setup(s => s.GetAlerts(1, 1000, null, "Active", null, null, null, null, null, "testuser", "testpass"))
                           .ReturnsAsync(mockAlerts);

            // Act
            var result = await _service.GetDatasetEMByIdAsync(dataset.Id, "testuser");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.DatasetAlerts); // Solo el que coincide con Id_Alert = 1
            Assert.Equal(1, result.DatasetAlerts.First().Id_alert);

            // Verificar que se llamó a SondaEMService
            _mockSondaService.Verify(s => s.GetAlerts(1, 1000, null, "Active", null, null, null, null, null, "testuser", "testpass"), Times.Once);
        }

        [Fact]
        public async Task GetDatasetEMByIdAsync_FormalDatasetWithEventFilter_PerformsDynamicSearch()
        {
            // Arrange
            var dataset = new DatasetEM
            {
                Name = "Formal Dataset with Event",
                Username = "testuser",
                Is_Dataset = "S",
                ContentType = "0",
                Id_Event = 1,
                EventState = "Open"
            };

            _context.DatasetsEM.Add(dataset);
            await _context.SaveChangesAsync();

            // Mock events desde la API
            var mockEvents = new List<EventDto>
            {
                new EventDto { Id = 1, Name = "Event 1", State = "Open" },
                new EventDto { Id = 2, Name = "Event 2", State = "Closed" }
            };

            _mockSondaService.Setup(s => s.GetEvents(1, 1000, null, null, "testuser", "testpass"))
                           .ReturnsAsync(mockEvents);

            // Act
            var result = await _service.GetDatasetEMByIdAsync(dataset.Id, "testuser");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.DatasetEvents); // Solo el que coincide con Id_Event = 1 y State = "Open"
            Assert.Equal(1, result.DatasetEvents.First().Id_event);

            // Verificar que se llamó a SondaEMService
            _mockSondaService.Verify(s => s.GetEvents(1, 1000, null, null, "testuser", "testpass"), Times.Once);
        }

        [Fact]
        public async Task GetDatasetEMByIdAsync_FormalDatasetWithExtensionFilter_PerformsDynamicSearch()
        {
            // Arrange
            var dataset = new DatasetEM
            {
                Name = "Formal Dataset with Extension",
                Username = "testuser",
                Is_Dataset = "S",
                ContentType = "0",
                Id_Extension = 1,
                ExtensionState = "Active"
            };

            _context.DatasetsEM.Add(dataset);
            await _context.SaveChangesAsync();

            // Mock extensions desde la API
            var mockExtensions = new List<ExtensionDto>
            {
                new ExtensionDto { ExtensionId = 1, EventName = "Extension 1" },
                new ExtensionDto { ExtensionId = 2, EventName = "Extension 2" }
            };

            _mockSondaService.Setup(s => s.GetExtensions(1, 1000, null, null, "Active", null, null, null, null, "testuser", "testpass"))
                           .ReturnsAsync(mockExtensions);

            // Act
            var result = await _service.GetDatasetEMByIdAsync(dataset.Id, "testuser");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.DatasetExtensions); // Solo el que coincide con Id_Extension = 1
            Assert.Equal(1, result.DatasetExtensions.First().Id_extension);

            // Verificar que se llamó a SondaEMService
            _mockSondaService.Verify(s => s.GetExtensions(1, 1000, null, null, "Active", null, null, null, null, "testuser", "testpass"), Times.Once);
        }

        [Fact]
        public async Task GetDatasetEMByIdAsync_FormalDatasetWithResourceFilter_PerformsDynamicSearch()
        {
            // Arrange
            var dataset = new DatasetEM
            {
                Name = "Formal Dataset with Resource",
                Username = "testuser",
                Is_Dataset = "S",
                ContentType = "0",
                Id_Resource = 1
            };

            _context.DatasetsEM.Add(dataset);
            await _context.SaveChangesAsync();

            // Mock resource desde la API
            var mockResource = new ResourceDto { Id = 1, Name = "Resource 1" };

            _mockSondaService.Setup(s => s.GetResourceById(1, "testuser", "testpass"))
                           .ReturnsAsync(mockResource);

            // Act
            var result = await _service.GetDatasetEMByIdAsync(dataset.Id, "testuser");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.DatasetResources);
            Assert.Equal(1, result.DatasetResources.First().Id_resource);

            // Verificar que se llamó a SondaEMService
            _mockSondaService.Verify(s => s.GetResourceById(1, "testuser", "testpass"), Times.Once);
        }

        [Fact]
        public async Task GetDatasetEMByIdAsync_DatasetNotFound_ReturnsNull()
        {
            // Act
            var result = await _service.GetDatasetEMByIdAsync(999, "testuser");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetDatasetEMByIdAsync_UserNotInDatabase_ReturnsNull()
        {
            // Arrange
            var dataset = new DatasetEM
            {
                Name = "Test Dataset",
                Username = "nonexistentuser",
                Is_Dataset = "S"
            };

            _context.DatasetsEM.Add(dataset);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetDatasetEMByIdAsync(dataset.Id, "nonexistentuser");

            // Assert
            Assert.Null(result); // Debe retornar null si el usuario no existe en la BD local
        }

        #endregion

        #region GetDatasetEMByIdForEditAsync Tests

        [Fact]
        public async Task GetDatasetEMByIdForEditAsync_ValidDataset_ReturnsWithoutDynamicSearch()
        {
            // Arrange
            var dataset = new DatasetEM
            {
                Name = "Edit Dataset",
                Username = "testuser",
                Is_Dataset = "S",
                Id_Alert = 1
            };

            _context.DatasetsEM.Add(dataset);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetDatasetEMByIdForEditAsync(dataset.Id, "testuser");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dataset.Id, result.Id);
            Assert.Equal("Edit Dataset", result.Name);

            // Verificar que NO se realizó búsqueda dinámica
            _mockSondaService.VerifyNoOtherCalls();
        }

        #endregion

        #region UpdateDatasetEMAsync Tests

        [Fact]
        public async Task UpdateDatasetEMAsync_ValidUpdate_UpdatesSuccessfully()
        {
            // Arrange
            var originalDataset = new DatasetEM
            {
                Name = "Original Name",
                Description = "Original Description",
                Username = "testuser",
                Is_Dataset = "S"
            };

            _context.DatasetsEM.Add(originalDataset);
            await _context.SaveChangesAsync();

            // Modificar el dataset
            originalDataset.Name = "Updated Name";
            originalDataset.Description = "Updated Description";

            // Act
            var result = await _service.UpdateDatasetEMAsync(originalDataset);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated Name", result.Name);
            Assert.Equal("Updated Description", result.Description);

            // Verificar en la base de datos
            var updatedInDb = await _context.DatasetsEM.FindAsync(originalDataset.Id);
            Assert.Equal("Updated Name", updatedInDb.Name);
            Assert.Equal("Updated Description", updatedInDb.Description);
        }

        [Fact]
        public async Task UpdateDatasetEMAsync_DuplicateName_ThrowsInvalidOperationException()
        {
            // Arrange
            // Crear dos datasets diferentes
            var dataset1 = new DatasetEM { Name = "Dataset 1", Username = "testuser", Is_Dataset = "S" };
            var dataset2 = new DatasetEM { Name = "Dataset 2", Username = "testuser", Is_Dataset = "S" };

            _context.DatasetsEM.AddRange(dataset1, dataset2);
            await _context.SaveChangesAsync();

            // Intentar cambiar dataset2 para que tenga el mismo nombre que dataset1
            dataset2.Name = "Dataset 1";

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.UpdateDatasetEMAsync(dataset2));

            Assert.Contains("Ya existe un dataset con el nombre", exception.Message);
            Assert.Contains("Dataset 1", exception.Message);
        }

        #endregion

        #region DeleteDatasetEMAsync Tests

        [Fact]
        public async Task DeleteDatasetEMAsync_ValidDataset_DeletesSuccessfully()
        {
            // Arrange
            var dataset = new DatasetEM
            {
                Name = "To Delete",
                Username = "testuser",
                Is_Dataset = "S"
            };

            _context.DatasetsEM.Add(dataset);
            await _context.SaveChangesAsync();
            var datasetId = dataset.Id;

            // Act
            await _service.DeleteDatasetEMAsync(datasetId, "testuser");

            // Assert
            var deletedDataset = await _context.DatasetsEM.FindAsync(datasetId);
            Assert.Null(deletedDataset);
        }

        [Fact]
        public async Task DeleteDatasetEMAsync_DatasetNotFound_ThrowsInvalidOperationException()
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.DeleteDatasetEMAsync(999, "testuser"));

            Assert.Contains("No se encontró el dataset con ID 999", exception.Message);
            Assert.Contains("testuser", exception.Message);
        }

        [Fact]
        public async Task DeleteDatasetEMAsync_WrongUser_ThrowsInvalidOperationException()
        {
            // Arrange
            var dataset = new DatasetEM
            {
                Name = "Other User Dataset",
                Username = "otheruser",
                Is_Dataset = "S"
            };

            _context.DatasetsEM.Add(dataset);
            await _context.SaveChangesAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.DeleteDatasetEMAsync(dataset.Id, "testuser"));

            Assert.Contains($"No se encontró el dataset con ID {dataset.Id}", exception.Message);
            Assert.Contains("testuser", exception.Message);
        }

        #endregion
    }
}