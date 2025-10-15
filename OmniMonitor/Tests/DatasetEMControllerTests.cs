using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OmniMonitor.Server.Controllers;
using OmniMonitor.Server.Services;
using OmniMonitor.Shared.Dtos;
using System.Security.Claims;
using Xunit;

namespace OmniMonitor.Tests
{
    /// <summary>
    /// Tests para DatasetEMController que verifican los 4 endpoints principales:
    /// 1. POST /api/DatasetEM - Crear dataset
    /// 2. GET /api/DatasetEM/user/{username} - Obtener todos los datasets
    /// 3. GET /api/DatasetEM/{datasetId}/{username} - Obtener dataset por ID
    /// 4. PUT /api/DatasetEM/{datasetId} - Actualizar dataset
    /// 5. DELETE /api/DatasetEM/{datasetId}/{username} - Eliminar dataset
    /// </summary>
    public class DatasetEMControllerTests
    {
        private readonly Mock<IDatasetEMService> _mockService;
        private readonly DatasetEMController _controller;

        public DatasetEMControllerTests()
        {
            _mockService = new Mock<IDatasetEMService>();
            _controller = new DatasetEMController(_mockService.Object);
            
            // Configurar el contexto HTTP para simular autenticación
            SetupControllerContext();
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
                HttpContext = new DefaultHttpContext
                {
                    User = principal
                }
            };
        }

        #region CreateDataset Tests

        [Fact]
        public async Task CreateDataset_ValidRequest_Returns201Created()
        {
            // Arrange
            var request = new CreateDatasetEMRequest
            {
                Name = "Test Dataset EM",
                Description = "Test Description",
                Username = "testuser",
                IsDataset = "S",
                AlertId = 1,
                AlertState = "Active"
            };

            var expectedDataset = new DatasetEM
            {
                Id = 1,
                Name = request.Name,
                Description = request.Description,
                Username = request.Username,
                Is_Dataset = request.IsDataset,
                ContentType = "0"
            };

            _mockService.Setup(s => s.CreateDatasetEMAsync(request))
                       .ReturnsAsync(expectedDataset);

            // Act
            var result = await _controller.CreateDataset(request);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(201, createdResult.StatusCode);
            Assert.Equal(expectedDataset, createdResult.Value);
            _mockService.Verify(s => s.CreateDatasetEMAsync(request), Times.Once);
        }

        [Fact]
        public async Task CreateDataset_NullRequest_Returns400BadRequest()
        {
            // Act
            var result = await _controller.CreateDataset(null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(400, badRequestResult.StatusCode);
            Assert.Equal("El cuerpo de la petición no puede estar vacío.", badRequestResult.Value);
        }

        [Fact]
        public async Task CreateDataset_EmptyName_Returns400BadRequest()
        {
            // Arrange
            var request = new CreateDatasetEMRequest
            {
                Name = "",
                Username = "testuser",
                IsDataset = "S"
            };

            // Act
            var result = await _controller.CreateDataset(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(400, badRequestResult.StatusCode);
            Assert.Equal("El nombre del dataset es requerido.", badRequestResult.Value);
        }

        [Fact]
        public async Task CreateDataset_EmptyUsername_Returns400BadRequest()
        {
            // Arrange
            var request = new CreateDatasetEMRequest
            {
                Name = "Test Dataset",
                Username = "",
                IsDataset = "S"
            };

            // Act
            var result = await _controller.CreateDataset(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(400, badRequestResult.StatusCode);
            Assert.Equal("El nombre de usuario es requerido.", badRequestResult.Value);
        }

        [Fact]
        public async Task CreateDataset_EmptyIsDataset_Returns400BadRequest()
        {
            // Arrange
            var request = new CreateDatasetEMRequest
            {
                Name = "Test Dataset",
                Username = "testuser",
                IsDataset = ""
            };

            // Act
            var result = await _controller.CreateDataset(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(400, badRequestResult.StatusCode);
            Assert.Equal("El tipo de dataset es requerido.", badRequestResult.Value);
        }

        [Fact]
        public async Task CreateDataset_DuplicateName_Returns400BadRequest()
        {
            // Arrange
            var request = new CreateDatasetEMRequest
            {
                Name = "Duplicate Dataset",
                Username = "testuser",
                IsDataset = "S"
            };

            _mockService.Setup(s => s.CreateDatasetEMAsync(request))
                       .ThrowsAsync(new InvalidOperationException("Ya existe un dataset con el nombre 'Duplicate Dataset' para el usuario 'testuser'."));

            // Act
            var result = await _controller.CreateDataset(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(400, badRequestResult.StatusCode);
            Assert.Contains("Ya existe un dataset", badRequestResult.Value.ToString());
        }

        [Fact]
        public async Task CreateDataset_ServiceException_Returns500InternalServerError()
        {
            // Arrange
            var request = new CreateDatasetEMRequest
            {
                Name = "Test Dataset",
                Username = "testuser",
                IsDataset = "S"
            };

            _mockService.Setup(s => s.CreateDatasetEMAsync(request))
                       .ThrowsAsync(new Exception("Database connection error"));

            // Act
            var result = await _controller.CreateDataset(request);

            // Assert
            var serverErrorResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, serverErrorResult.StatusCode);
            Assert.Contains("Error interno al crear el dataset", serverErrorResult.Value.ToString());
        }

        #endregion

        #region GetAllDatasets Tests

        [Fact]
        public async Task GetAllDatasets_ValidUsername_Returns200Ok()
        {
            // Arrange
            var username = "testuser";
            var expectedDatasets = new List<DatasetEM>
            {
                new DatasetEM { Id = 1, Name = "Dataset 1", Username = username },
                new DatasetEM { Id = 2, Name = "Dataset 2", Username = username }
            };

            _mockService.Setup(s => s.GetAllDatasetsEMAsync(username))
                       .ReturnsAsync(expectedDatasets);

            // Act
            var result = await _controller.GetAllDatasets(username);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(200, okResult.StatusCode);
            Assert.Equal(expectedDatasets, okResult.Value);
            _mockService.Verify(s => s.GetAllDatasetsEMAsync(username), Times.Once);
        }

        [Fact]
        public async Task GetAllDatasets_ServiceException_Returns500InternalServerError()
        {
            // Arrange
            var username = "testuser";
            _mockService.Setup(s => s.GetAllDatasetsEMAsync(username))
                       .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetAllDatasets(username);

            // Assert
            var serverErrorResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, serverErrorResult.StatusCode);
            Assert.Contains("Error interno al obtener los datasets", serverErrorResult.Value.ToString());
        }

        #endregion

        #region GetDatasetById Tests

        [Fact]
        public async Task GetDatasetById_ValidParameters_Returns200Ok()
        {
            // Arrange
            var datasetId = 1;
            var username = "testuser";
            var expectedDataset = new DatasetEM
            {
                Id = datasetId,
                Name = "Test Dataset",
                Username = username
            };

            _mockService.Setup(s => s.GetDatasetEMByIdAsync(datasetId, username))
                       .ReturnsAsync(expectedDataset);

            // Act
            var result = await _controller.GetDatasetById(datasetId, username);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(200, okResult.StatusCode);
            Assert.Equal(expectedDataset, okResult.Value);
            _mockService.Verify(s => s.GetDatasetEMByIdAsync(datasetId, username), Times.Once);
        }

        [Fact]
        public async Task GetDatasetById_DatasetNotFound_Returns404NotFound()
        {
            // Arrange
            var datasetId = 999;
            var username = "testuser";

            _mockService.Setup(s => s.GetDatasetEMByIdAsync(datasetId, username))
                       .ReturnsAsync((DatasetEM?)null);

            // Act
            var result = await _controller.GetDatasetById(datasetId, username);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal(404, notFoundResult.StatusCode);
            Assert.Contains($"No se encontró el dataset con ID {datasetId}", notFoundResult.Value.ToString());
        }

        [Fact]
        public async Task GetDatasetById_ServiceException_Returns500InternalServerError()
        {
            // Arrange
            var datasetId = 1;
            var username = "testuser";

            _mockService.Setup(s => s.GetDatasetEMByIdAsync(datasetId, username))
                       .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetDatasetById(datasetId, username);

            // Assert
            var serverErrorResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, serverErrorResult.StatusCode);
            Assert.Contains("Error interno al obtener el dataset", serverErrorResult.Value.ToString());
        }

        #endregion

        #region UpdateDataset Tests

        [Fact]
        public async Task UpdateDataset_ValidRequest_Returns200Ok()
        {
            // Arrange
            var datasetId = 1;
            var request = new CreateDatasetEMRequest
            {
                Name = "Updated Dataset",
                Description = "Updated Description",
                Username = "testuser",
                IsDataset = "N",
                AlertIds = new List<int> { 1, 2 }
            };

            var existingDataset = new DatasetEM
            {
                Id = datasetId,
                Name = "Old Dataset",
                Username = "testuser"
            };

            var updatedDataset = new DatasetEM
            {
                Id = datasetId,
                Name = request.Name,
                Description = request.Description,
                Username = request.Username,
                Is_Dataset = request.IsDataset
            };

            _mockService.Setup(s => s.GetDatasetEMByIdForEditAsync(datasetId, request.Username))
                       .ReturnsAsync(existingDataset);

            _mockService.Setup(s => s.UpdateDatasetEMAsync(It.IsAny<DatasetEM>()))
                       .ReturnsAsync(updatedDataset);

            // Act
            var result = await _controller.UpdateDataset(datasetId, request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(200, okResult.StatusCode);
            Assert.Equal(updatedDataset, okResult.Value);
            _mockService.Verify(s => s.UpdateDatasetEMAsync(It.IsAny<DatasetEM>()), Times.Once);
        }

        [Fact]
        public async Task UpdateDataset_NullRequest_Returns400BadRequest()
        {
            // Arrange
            var datasetId = 1;

            // Act
            var result = await _controller.UpdateDataset(datasetId, null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(400, badRequestResult.StatusCode);
            Assert.Equal("El cuerpo de la petición no puede estar vacío.", badRequestResult.Value);
        }

        [Fact]
        public async Task UpdateDataset_DatasetNotFound_Returns404NotFound()
        {
            // Arrange
            var datasetId = 999;
            var request = new CreateDatasetEMRequest
            {
                Name = "Updated Dataset",
                Username = "testuser",
                IsDataset = "S"
            };

            _mockService.Setup(s => s.GetDatasetEMByIdForEditAsync(datasetId, request.Username))
                       .ReturnsAsync((DatasetEM?)null);

            // Act
            var result = await _controller.UpdateDataset(datasetId, request);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal(404, notFoundResult.StatusCode);
            Assert.Contains($"No se encontró el dataset con ID {datasetId}", notFoundResult.Value.ToString());
        }

        [Fact]
        public async Task UpdateDataset_DuplicateName_Returns400BadRequest()
        {
            // Arrange
            var datasetId = 1;
            var request = new CreateDatasetEMRequest
            {
                Name = "Duplicate Name",
                Username = "testuser",
                IsDataset = "S"
            };

            var existingDataset = new DatasetEM { Id = datasetId, Name = "Old Name", Username = "testuser" };

            _mockService.Setup(s => s.GetDatasetEMByIdForEditAsync(datasetId, request.Username))
                       .ReturnsAsync(existingDataset);

            _mockService.Setup(s => s.UpdateDatasetEMAsync(It.IsAny<DatasetEM>()))
                       .ThrowsAsync(new InvalidOperationException("Ya existe un dataset con el nombre 'Duplicate Name'"));

            // Act
            var result = await _controller.UpdateDataset(datasetId, request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(400, badRequestResult.StatusCode);
            Assert.Contains("Ya existe un dataset", badRequestResult.Value.ToString());
        }

        #endregion

        #region DeleteDataset Tests

        [Fact]
        public async Task DeleteDataset_ValidParameters_Returns204NoContent()
        {
            // Arrange
            var datasetId = 1;
            var username = "testuser";

            _mockService.Setup(s => s.DeleteDatasetEMAsync(datasetId, username))
                       .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeleteDataset(datasetId, username);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockService.Verify(s => s.DeleteDatasetEMAsync(datasetId, username), Times.Once);
        }

        [Fact]
        public async Task DeleteDataset_DatasetNotFound_Returns404NotFound()
        {
            // Arrange
            var datasetId = 999;
            var username = "testuser";

            _mockService.Setup(s => s.DeleteDatasetEMAsync(datasetId, username))
                       .ThrowsAsync(new InvalidOperationException($"No se encontró el dataset con ID {datasetId}"));

            // Act
            var result = await _controller.DeleteDataset(datasetId, username);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(404, notFoundResult.StatusCode);
            Assert.Contains($"No se encontró el dataset con ID {datasetId}", notFoundResult.Value.ToString());
        }

        [Fact]
        public async Task DeleteDataset_ServiceException_Returns500InternalServerError()
        {
            // Arrange
            var datasetId = 1;
            var username = "testuser";

            _mockService.Setup(s => s.DeleteDatasetEMAsync(datasetId, username))
                       .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.DeleteDataset(datasetId, username);

            // Assert
            var serverErrorResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, serverErrorResult.StatusCode);
            Assert.Contains("Error interno al eliminar el dataset", serverErrorResult.Value.ToString());
        }

        #endregion
    }
}