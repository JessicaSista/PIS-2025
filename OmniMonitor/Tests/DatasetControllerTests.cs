using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using OmniMonitor.Server.Context;
using OmniMonitor.Server.Controllers;
using OmniMonitor.Server.Services;
using OmniMonitor.Shared.Dtos;
using Xunit;

namespace QA.Tests
{
    public class DatasetControllerTests
    {
        private static ControllerContext GetControllerContext(string username)
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.Name, username) }, "mock"));
            return new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        private static DatasetController GetController(
            IDatasetService? datasetService = null,
            ISondaAuthService? sondaAuthService = null,
            ISondaIMService? sondaIMService = null,
            IDatasetUMService? datasetUMService = null,
            IKpiService? kpiService = null,
            ApplicationDbContext? context = null,
            string username = "testuser")
        {
            var ctrl = new DatasetController(
                datasetService ?? Mock.Of<IDatasetService>(),
                sondaAuthService ?? Mock.Of<ISondaAuthService>(),
                sondaIMService ?? Mock.Of<ISondaIMService>(),
                datasetUMService ?? Mock.Of<IDatasetUMService>(),
                kpiService ?? Mock.Of<IKpiService>(),
                context ?? new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options)
            );
            ctrl.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(
                        new ClaimsIdentity(
                            new[] { new Claim(ClaimTypes.Name, username) }, "mock"))
                }
            };
            return ctrl;
        }

        /* Tests sobre GetAllDatasets */
        [Fact]
        public async Task GetAllDatasets_ReturnsOk_WithDatasets()
        {
            var datasetService = new Mock<IDatasetService>();
            datasetService.Setup(s => s.GetAllDatasetsIMAsync("testuser"))
                .ReturnsAsync(new List<DatasetIM> { new() { Id = 1, Name = "Test", Username = "testuser", Is_Dataset = "S" } });

            var controller = GetController(datasetService: datasetService.Object);

            var result = await controller.GetAllDatasets();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<List<DatasetIM>>(okResult.Value);
            Assert.Single(returned);
        }

        [Fact]
        public async Task GetAllDatasets_ReturnsOk_EmptyList()
        {
            var datasetService = new Mock<IDatasetService>();
            datasetService.Setup(s => s.GetAllDatasetsIMAsync("testuser")).ReturnsAsync(new List<DatasetIM>());

            var controller = GetController(datasetService: datasetService.Object);

            var result = await controller.GetAllDatasets();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<List<DatasetIM>>(okResult.Value);
            Assert.Empty(returned);
        }

        [Fact]
        public async Task GetAllDatasets_FiltersBySearch()
        {
            var datasetService = new Mock<IDatasetService>();
            var datasets = new List<DatasetIM>
            {
                new() { Id = 1, Name = "Alpha", Username = "testuser", Is_Dataset = "S" },
                new() { Id = 2, Name = "Beta", Username = "testuser", Is_Dataset = "S" }
            };
            datasetService.Setup(s => s.GetAllDatasetsIMAsync("testuser")).ReturnsAsync(datasets);

            var controller = GetController(datasetService: datasetService.Object);

            var result = await controller.GetAllDatasets("alp");

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<List<DatasetIM>>(okResult.Value);
            Assert.Single(returned);
            Assert.Equal("Alpha", returned[0].Name);
        }

        [Fact]
        public async Task GetAllDatasets_Returns500_OnException()
        {
            var datasetService = new Mock<IDatasetService>();
            datasetService.Setup(s => s.GetAllDatasetsIMAsync("testuser")).ThrowsAsync(new Exception("fail"));

            var controller = GetController(datasetService: datasetService.Object);

            var result = await controller.GetAllDatasets();

            var status = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, status.StatusCode);
        }

        [Fact]
        public async Task GetAllDatasets_ReturnsOk_WhenSearchIsNull()
        {
            var datasetService = new Mock<IDatasetService>();
            datasetService.Setup(s => s.GetAllDatasetsIMAsync("testuser")).ReturnsAsync(new List<DatasetIM>());
            var controller = GetController(datasetService: datasetService.Object);
            var result = await controller.GetAllDatasets(null);
            Assert.IsType<OkObjectResult>(result.Result);
        }

        /* Tests sobre GetDatasetById */
        [Fact]
        public async Task GetDatasetById_ReturnsOk_WhenFound()
        {
            var datasetService = new Mock<IDatasetService>();
            var dataset = new DatasetIM { Id = 1, Name = "Test", Username = "testuser", Is_Dataset = "S" };
            datasetService.Setup(s => s.GetDatasetIMByIdForEditAsync(1, "testuser")).ReturnsAsync(dataset);

            var controller = GetController(datasetService: datasetService.Object);

            var result = await controller.GetDatasetById(1);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<DatasetIM>(okResult.Value);
            Assert.Equal(1, returned.Id);
        }

        [Fact]
        public async Task GetDatasetById_ReturnsNotFound_WhenNotFound()
        {
            var datasetService = new Mock<IDatasetService>();
            datasetService.Setup(s => s.GetDatasetIMByIdForEditAsync(99, "testuser")).ReturnsAsync((DatasetIM?)null);

            var controller = GetController(datasetService: datasetService.Object);

            var result = await controller.GetDatasetById(99);

            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetDatasetById_Returns500_OnException()
        {
            var datasetService = new Mock<IDatasetService>();
            datasetService.Setup(s => s.GetDatasetIMByIdForEditAsync(1, "testuser")).ThrowsAsync(new Exception("fail"));

            var controller = GetController(datasetService: datasetService.Object);

            var result = await controller.GetDatasetById(1);

            var status = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, status.StatusCode);
        }

        [Fact]
        public async Task GetDatasetById_NoUsername_Returns500_OnException()
        {
            var datasetService = new Mock<IDatasetService>();
            datasetService.Setup(s => s.GetDatasetIMByIdForEditAsync(1, It.IsAny<string>())).ThrowsAsync(new Exception("fail"));
            var controller = GetController(datasetService: datasetService.Object, username: "");
            var result = await controller.GetDatasetById(1);
            var status = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, status.StatusCode);
        }

        [Fact]
        public async Task GetDatasetById_NegativeId_ReturnsNotFound()
        {
            var controller = GetController();
            var result = await controller.GetDatasetById(-1);
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        /* Tests sobre GetDatasetByIdSinToken */

        [Fact]
        public async Task GetDatasetByIdSinToken_ReturnsOk_WhenFound()
        {
            var datasetService = new Mock<IDatasetService>();
            var dataset = new DatasetIM { Id = 1, Name = "Test", Username = "testuser", Is_Dataset = "S" };
            datasetService.Setup(s => s.GetDatasetIMByIdForEditAsyncSinToken(1)).ReturnsAsync(dataset);

            var controller = GetController(datasetService: datasetService.Object);

            var result = await controller.GetDatasetByIdSinToken(1);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<DatasetIM>(okResult.Value);
            Assert.Equal(1, returned.Id);
        }

        [Fact]
        public async Task GetDatasetByIdSinToken_ReturnsNotFound_WhenNotFound()
        {
            var datasetService = new Mock<IDatasetService>();
            datasetService.Setup(s => s.GetDatasetIMByIdForEditAsyncSinToken(99)).ReturnsAsync((DatasetIM?)null);

            var controller = GetController(datasetService: datasetService.Object);

            var result = await controller.GetDatasetByIdSinToken(99);

            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetDatasetByIdSinToken_Returns500_OnException() {             
            var datasetService = new Mock<IDatasetService>();
            datasetService.Setup(s => s.GetDatasetIMByIdForEditAsyncSinToken(1)).ThrowsAsync(new Exception("fail"));
            var controller = GetController(datasetService: datasetService.Object);
            var result = await controller.GetDatasetByIdSinToken(1);
            var status = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, status.StatusCode);
        }

        [Fact]
        public async Task GetDatasetByIdSinToken_ReturnsOk_WhenIdIsValid()
        {
            var datasetService = new Mock<IDatasetService>();
            var dataset = new DatasetIM { Id = 2, Name = "Test2", Username = "testuser", Is_Dataset = "S" };
            datasetService.Setup(s => s.GetDatasetIMByIdForEditAsyncSinToken(2)).ReturnsAsync(dataset);
            var controller = GetController(datasetService: datasetService.Object);
            var result = await controller.GetDatasetByIdSinToken(2);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<DatasetIM>(okResult.Value);
            Assert.Equal(2, returned.Id);
        }

        [Fact]
        public async Task GetDatasetByIdSinToken_NegativeId_ReturnsNotFound()
        {
            var controller = GetController();
            var result = await controller.GetDatasetByIdSinToken(-1);
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        /* Tests sobre UpdateDataset */
        [Fact]
        public async Task UpdateDataset_ReturnsOk_WhenSuccess()
        {
            var datasetService = new Mock<IDatasetService>();
            var datasetUMService = new Mock<IDatasetUMService>();
            var dataset = new DatasetIM { Id = 1, Name = "Old", Username = "testuser", Is_Dataset = "S", DatasetId = 10 };
            var request = new CreateDatasetIMRequest { Name = "New", IsDataset = "S" };
            var updated = new DatasetIM { Id = 1, Name = "New", Username = "testuser", Is_Dataset = "S", DatasetId = 10 };
            var datasets = new Datasets { Id = 10, Username = "testuser", NameDataset = "New", TipoDataset = ModuleType.InsightMonitor };

            datasetService.Setup(s => s.GetDatasetIMByIdForEditAsync(1, "testuser")).ReturnsAsync(dataset);
            datasetUMService.Setup(s => s.ValidateDatasetNameAsync("New", "testuser", ModuleType.InsightMonitor, 10)).Returns(Task.CompletedTask);
            datasetService.Setup(s => s.UpdateDatasetIMAsync(dataset, request, "testuser")).ReturnsAsync(updated);
            datasetUMService.Setup(s => s.UpdateDatasetAsyncIM(10, It.IsAny<CreateDatasetRequest>(), updated)).ReturnsAsync(datasets);

            var controller = GetController(datasetService: datasetService.Object, datasetUMService: datasetUMService.Object);

            var result = await controller.UpdateDataset(1, request);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<DatasetIM>(okResult.Value);
            Assert.Equal("New", returned.Name);
        }

        [Fact]
        public async Task UpdateDataset_ReturnsNotFound_WhenNotFound()
        {
            var datasetService = new Mock<IDatasetService>();
            var request = new CreateDatasetIMRequest { Name = "New", IsDataset = "S" };
            datasetService.Setup(s => s.GetDatasetIMByIdForEditAsync(99, "testuser")).ReturnsAsync((DatasetIM?)null);

            var controller = GetController(datasetService: datasetService.Object);

            var result = await controller.UpdateDataset(99, request);

            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task UpdateDataset_ReturnsBadRequest_OnArgumentException()
        {
            var datasetService = new Mock<IDatasetService>();
            var datasetUMService = new Mock<IDatasetUMService>();
            var dataset = new DatasetIM { Id = 1, Name = "Old", Username = "testuser", Is_Dataset = "S", DatasetId = 10 };
            var request = new CreateDatasetIMRequest { Name = "New", IsDataset = "S" };

            datasetService.Setup(s => s.GetDatasetIMByIdForEditAsync(1, "testuser")).ReturnsAsync(dataset);
            datasetUMService.Setup(s => s.ValidateDatasetNameAsync("New", "testuser", ModuleType.InsightMonitor, 10)).ThrowsAsync(new ArgumentException("bad"));

            var controller = GetController(datasetService: datasetService.Object, datasetUMService: datasetUMService.Object);

            var result = await controller.UpdateDataset(1, request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("bad", badRequest.Value.ToString());
        }

        [Fact]
        public async Task UpdateDataset_ReturnsBadRequest_OnInvalidModel()
        {
            var controller = GetController();
            var request = new CreateDatasetIMRequest { Name = "New", IsDataset = "S" };
            controller.ModelState.AddModelError("Name", "Required");

            var result = await controller.UpdateDataset(1, request);

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task UpdateDataset_Returns500_OnException()
        {
            var datasetService = new Mock<IDatasetService>();
            var datasetUMService = new Mock<IDatasetUMService>();
            var dataset = new DatasetIM { Id = 1, Name = "Old", Username = "testuser", Is_Dataset = "S", DatasetId = 10 };
            var request = new CreateDatasetIMRequest { Name = "New", IsDataset = "S" };

            datasetService.Setup(s => s.GetDatasetIMByIdForEditAsync(1, "testuser")).ReturnsAsync(dataset);
            datasetUMService.Setup(s => s.ValidateDatasetNameAsync("New", "testuser", ModuleType.InsightMonitor, 10)).ThrowsAsync(new Exception("fail"));

            var controller = GetController(datasetService: datasetService.Object, datasetUMService: datasetUMService.Object);

            var result = await controller.UpdateDataset(1, request);

            var status = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, status.StatusCode);
        }

        /* Tests sobre DeleteDataset */ 
        [Fact]
        public async Task DeleteDataset_ReturnsNoContent_WhenSuccess()
        {
            var datasetService = new Mock<IDatasetService>();
            var datasetUMService = new Mock<IDatasetUMService>();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            using var context = new ApplicationDbContext(options);

            var datasetIM = new DatasetIM { Id = 1, Name = "Test", Username = "testuser", Is_Dataset = "S", DatasetId = 10 };
            context.DatasetsIM.Add(datasetIM);
            context.SaveChanges();

            datasetService.Setup(s => s.GetDatasetIMByIdForEditAsync(1, "testuser")).ReturnsAsync(datasetIM);
            datasetService.Setup(s => s.DeleteDatasetIMAsync(1, "testuser")).Returns(Task.CompletedTask);
            datasetUMService.Setup(s => s.DeleteDatasetAsync(10, "testuser")).Returns(Task.CompletedTask);

            var controller = GetController(datasetService: datasetService.Object, datasetUMService: datasetUMService.Object, context: context);

            var result = await controller.DeleteDataset(1);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteDataset_ReturnsNotFound_WhenNotFound()
        {
            var datasetService = new Mock<IDatasetService>();
            datasetService.Setup(s => s.GetDatasetIMByIdForEditAsync(99, "testuser")).ReturnsAsync((DatasetIM?)null);

            var controller = GetController(datasetService: datasetService.Object);

            var result = await controller.DeleteDataset(99);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task DeleteDataset_Returns500_OnException()
        {
            var datasetService = new Mock<IDatasetService>();
            var datasetUMService = new Mock<IDatasetUMService>();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            using var context = new ApplicationDbContext(options);

            var datasetIM = new DatasetIM { Id = 1, Name = "Test", Username = "testuser", Is_Dataset = "S", DatasetId = 10 };
            context.DatasetsIM.Add(datasetIM);
            context.SaveChanges();

            datasetService.Setup(s => s.GetDatasetIMByIdForEditAsync(1, "testuser")).ReturnsAsync(datasetIM);
            datasetService.Setup(s => s.DeleteDatasetIMAsync(1, "testuser")).ThrowsAsync(new Exception("fail"));

            var controller = GetController(datasetService: datasetService.Object, datasetUMService: datasetUMService.Object, context: context);

            var result = await controller.DeleteDataset(1);

            var status = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, status.StatusCode);
        }

        [Fact]
        public async Task DeleteDataset_ReturnsNoContent_WhenDatasetIdIsValid()
        {
            var datasetService = new Mock<IDatasetService>();
            var datasetUMService = new Mock<IDatasetUMService>();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            using var context = new ApplicationDbContext(options);

            var datasetIM = new DatasetIM { Id = 2, Name = "Test2", Username = "testuser", Is_Dataset = "S", DatasetId = 20 };
            context.DatasetsIM.Add(datasetIM);
            context.SaveChanges();

            datasetService.Setup(s => s.GetDatasetIMByIdForEditAsync(2, "testuser")).ReturnsAsync(datasetIM);
            datasetService.Setup(s => s.DeleteDatasetIMAsync(2, "testuser")).Returns(Task.CompletedTask);
            datasetUMService.Setup(s => s.DeleteDatasetAsync(20, "testuser")).Returns(Task.CompletedTask);

            var controller = GetController(datasetService: datasetService.Object, datasetUMService: datasetUMService.Object, context: context);

            var result = await controller.DeleteDataset(2);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteDataset_Returns500_OnDeleteUMException()
        {
            var datasetService = new Mock<IDatasetService>();
            var datasetUMService = new Mock<IDatasetUMService>();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            using var context = new ApplicationDbContext(options);

            var datasetIM = new DatasetIM { Id = 3, Name = "Test3", Username = "testuser", Is_Dataset = "S", DatasetId = 30 };
            context.DatasetsIM.Add(datasetIM);
            context.SaveChanges();

            datasetService.Setup(s => s.GetDatasetIMByIdForEditAsync(3, "testuser")).ReturnsAsync(datasetIM);
            datasetService.Setup(s => s.DeleteDatasetIMAsync(3, "testuser")).Returns(Task.CompletedTask);
            datasetUMService.Setup(s => s.DeleteDatasetAsync(30, "testuser")).ThrowsAsync(new Exception("fail"));

            var controller = GetController(datasetService: datasetService.Object, datasetUMService: datasetUMService.Object, context: context);

            var result = await controller.DeleteDataset(3);

            var status = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, status.StatusCode);
        }

        /* Tests sobre GetSensorType */
        [Fact]
        public async Task GetSensorType_ReturnsOk_WhenSensorFound()
        {
            var datasetService = new Mock<IDatasetService>();
            var sondaIMService = new Mock<ISondaIMService>();
            var dataset = new DatasetIM { Id = 1, Id_Source = 10, SensorName = "Temp" };
            var source = new Source { Devices = new List<Device> { new Device { Id = 5 } } };
            var device = new Device { Id = 5, Sensors = new List<Sensor> { new Sensor { Name = "Temp", Type = "float" } } };

            datasetService.Setup(s => s.GetDatasetIMByIdAsync(1, "testuser")).ReturnsAsync(dataset);
            sondaIMService.Setup(s => s.GetSourceById(10, "testuser")).ReturnsAsync(source);
            sondaIMService.Setup(s => s.GetDeviceById(5, "testuser")).ReturnsAsync(device);

            var controller = GetController(datasetService: datasetService.Object, sondaIMService: sondaIMService.Object);

            var result = await controller.GetSensorType(1);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal("float", ok.Value);
        }

        [Fact]
        public async Task GetSensorType_ReturnsNotFound_WhenDatasetNotFound()
        {
            var datasetService = new Mock<IDatasetService>();
            datasetService.Setup(s => s.GetDatasetIMByIdAsync(99, "testuser")).ReturnsAsync((DatasetIM?)null);

            var controller = GetController(datasetService: datasetService.Object);

            var result = await controller.GetSensorType(99);

            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Contains("99", notFound.Value.ToString());
        }

        [Fact]
        public async Task GetSensorType_ReturnsBadRequest_WhenSourceOrSensorNameMissing()
        {
            var datasetService = new Mock<IDatasetService>();
            var dataset = new DatasetIM { Id = 1, Id_Source = null, SensorName = null };
            datasetService.Setup(s => s.GetDatasetIMByIdAsync(1, "testuser")).ReturnsAsync(dataset);

            var controller = GetController(datasetService: datasetService.Object);

            var result = await controller.GetSensorType(1);

            var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("información suficiente", bad.Value.ToString());
        }

        [Fact]
        public async Task GetSensorType_ReturnsNotFound_WhenSourceNotFound()
        {
            var datasetService = new Mock<IDatasetService>();
            var sondaIMService = new Mock<ISondaIMService>();
            var dataset = new DatasetIM { Id = 1, Id_Source = 10, SensorName = "Temp" };

            datasetService.Setup(s => s.GetDatasetIMByIdAsync(1, "testuser")).ReturnsAsync(dataset);
            sondaIMService.Setup(s => s.GetSourceById(10, "testuser")).ReturnsAsync((Source?)null);

            var controller = GetController(datasetService: datasetService.Object, sondaIMService: sondaIMService.Object);

            var result = await controller.GetSensorType(1);

            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Contains("Source", notFound.Value.ToString());
        }

        [Fact]
        public async Task GetSensorType_ReturnsNotFound_WhenSensorNotFoundInDevices()
        {
            var datasetService = new Mock<IDatasetService>();
            var sondaIMService = new Mock<ISondaIMService>();
            var dataset = new DatasetIM { Id = 1, Id_Source = 10, SensorName = "Temp" };
            var source = new Source { Devices = new List<Device> { new Device { Id = 5 } } };
            var device = new Device { Id = 5, Sensors = new List<Sensor> { new Sensor { Name = "Other", Type = "int" } } };

            datasetService.Setup(s => s.GetDatasetIMByIdAsync(1, "testuser")).ReturnsAsync(dataset);
            sondaIMService.Setup(s => s.GetSourceById(10, "testuser")).ReturnsAsync(source);
            sondaIMService.Setup(s => s.GetDeviceById(5, "testuser")).ReturnsAsync(device);

            var controller = GetController(datasetService: datasetService.Object, sondaIMService: sondaIMService.Object);

            var result = await controller.GetSensorType(1);

            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Contains("sensor", notFound.Value.ToString());
        }

        /* Tests sobre GetDatasetModule */
        [Fact]
        public async Task GetDatasetModule_ReturnsOk_WhenModuleFound()
        {
            var datasetService = new Mock<IDatasetService>();
            datasetService.Setup(s => s.IdentifyDatasetModuleAsync(1, "testuser")).ReturnsAsync("Insight Monitor");

            var controller = GetController(datasetService: datasetService.Object);

            var result = await controller.GetDatasetModule(1);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal("Insight Monitor", ok.Value);
        }

        [Fact]
        public async Task GetDatasetModule_ReturnsNotFound_WhenModuleIsNull()
        {
            var datasetService = new Mock<IDatasetService>();
            datasetService.Setup(s => s.IdentifyDatasetModuleAsync(99, "testuser")).ReturnsAsync((string?)null);

            var controller = GetController(datasetService: datasetService.Object);

            var result = await controller.GetDatasetModule(99);

            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Contains("99", notFound.Value.ToString());
        }

        [Fact]
        public async Task GetDatasetModule_Returns500_OnException()
        {
            var datasetService = new Mock<IDatasetService>();
            datasetService.Setup(s => s.IdentifyDatasetModuleAsync(1, "testuser")).ThrowsAsync(new Exception("fail"));

            var controller = GetController(datasetService: datasetService.Object);

            var result = await controller.GetDatasetModule(1);

            var status = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, status.StatusCode);
        }

        [Fact]
        public async Task GetDatasetModule_ReturnsOk_WithDifferentModule()
        {
            var datasetService = new Mock<IDatasetService>();
            datasetService.Setup(s => s.IdentifyDatasetModuleAsync(2, "testuser")).ReturnsAsync("Asset Manager");

            var controller = GetController(datasetService: datasetService.Object);

            var result = await controller.GetDatasetModule(2);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal("Asset Manager", ok.Value);
        }

        [Fact]
        public async Task GetDatasetModule_ReturnsOk_WithUrbanMonitor()
        {
            var datasetService = new Mock<IDatasetService>();
            datasetService.Setup(s => s.IdentifyDatasetModuleAsync(3, "testuser")).ReturnsAsync("Urban Monitor");

            var controller = GetController(datasetService: datasetService.Object);

            var result = await controller.GetDatasetModule(3);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal("Urban Monitor", ok.Value);
        }
    }
}
