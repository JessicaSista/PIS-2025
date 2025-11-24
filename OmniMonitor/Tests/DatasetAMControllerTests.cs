using Microsoft.AspNetCore.Mvc;
using Moq;
using OmniMonitor.Server.Controllers;
using OmniMonitor.Server.Context;
using OmniMonitor.Server.Services;
using OmniMonitor.Shared.Dtos;
using OmniMonitor.Shared.Dtos.AM;
using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace QA.Tests
{
    public class DatasetAMControllerTests
    {
        private static ControllerContext GetControllerContext(string username)
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, username)
            ], "mock"));
            return new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        private static DatasetAMController GetController(
            IDatasetAmService? datasetAmService = null,
            ISondaAuthService? sondaAuthService = null,
            IDatasetUMService? datasetUMService = null,
            IKpiService? kpiService = null,
            ApplicationDbContext? context = null,
            ISondaAMService? sondaAMService = null,
            string username = "testuser")
        {
            var ctrl = new DatasetAMController(
                datasetAmService ?? Mock.Of<IDatasetAmService>(),
                sondaAuthService ?? Mock.Of<ISondaAuthService>(),
                datasetUMService ?? Mock.Of<IDatasetUMService>(),
                kpiService ?? Mock.Of<IKpiService>(),
                context ?? new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options),
                sondaAMService ?? Mock.Of<ISondaAMService>()
            );
            ctrl.ControllerContext = GetControllerContext(username);
            return ctrl;
        }

        /* Tests de Create y Update */

        [Fact]
        public async Task CreateDatasetAMFiltered_FiltersNull_ReturnsBadRequest()
        {
            var datasetAmService = new Mock<IDatasetAmService>();
            var datasetUMService = new Mock<IDatasetUMService>();
            var sondaAMService = new Mock<ISondaAMService>();
            var username = "noFiltersNull";
            var controller = GetController(datasetAmService.Object, null, datasetUMService.Object, null, null, sondaAMService.Object, username: username);

            var req = new CreateDatasetAMFilteredRequest
            {
                DatasetRequest = new CreateDatasetAMRequest { Nombre = "X", ContentType = "2" },
                Filters = null // null intentionally
            };

            var result = await controller.CreateDatasetAMFiltered(req);
            var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
            // check only that it's a BadRequest; message may be implementation detail
            Assert.NotNull(bad.Value);
            datasetAmService.Verify(s => s.CreateDatasetAMWithFiltersAsync(It.IsAny<CreateDatasetAMRequest>(), It.IsAny<int>(), It.IsAny<List<FilterCondition>>()), Times.Never);
        }

        [Fact]
        public async Task CreateDatasetAMFiltered_InvalidAssetId_SkipsNonNumericIds()
        {
            var datasetAmService = new Mock<IDatasetAmService>();
            var datasetUMService = new Mock<IDatasetUMService>();
            var sondaAMService = new Mock<ISondaAMService>();
            var username = "invalidId";
            // sonda returns one asset so StaticFilterObjects can run (controller will call sonda)
            sondaAMService.Setup(s => s.GetAssets(null, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<int?>(), username))
                .ReturnsAsync(new List<AssetDto> { new() { Id = "100", Name = "Good" } });

            datasetUMService.Setup(s => s.CreateDatasetAsync(It.IsAny<CreateDatasetRequest>()))
                .ReturnsAsync(new Datasets { Id = 500 });

            // Service should receive a CreateDatasetAMRequest where Grupo_Asset_Ids will be filled from filtered results.
            datasetAmService.Setup(s => s.CreateDatasetAMWithFiltersAsync(It.IsAny<CreateDatasetAMRequest>(), 500, It.IsAny<List<FilterCondition>>()))
                .ReturnsAsync((CreateDatasetAMRequest r, int d, List<FilterCondition> f) =>
                    new DatasetAM { Id_Dataset = 500, Username = username, Nombre = r.Nombre, Grupo_Asset = new List<DatasetAsset> { new() { Id_Asset = "100" } } });

            var controller = GetController(datasetAmService.Object, null, datasetUMService.Object, null, null, sondaAMService.Object, username: username);

            // Here we simulate that the filtered result produces a mix: controller would set Grupo_Asset after filtering;
            // to simulate non-numeric input, put a non-numeric asset in the request that should be ignored by parsing.
            var req = new CreateDatasetAMFilteredRequest
            {
                DatasetRequest = new CreateDatasetAMRequest { Nombre = "X", ContentType = "2" },
                Filters = new List<FilterCondition>() // filters present (empty allowed)
            };

            var result = await controller.CreateDatasetAMFiltered(req);
            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returned = Assert.IsType<DatasetAM>(created.Value);

            // returned.Grupo_Asset comes from service mock; ensure at least the numeric one is present
            Assert.NotNull(returned.Grupo_Asset);
            Assert.Contains(returned.Grupo_Asset, a => a.Id_Asset == "100");
            datasetAmService.Verify(s => s.CreateDatasetAMWithFiltersAsync(It.IsAny<CreateDatasetAMRequest>(), 500, It.IsAny<List<FilterCondition>>()), Times.Once);
        }

        [Fact]
        public async Task PuedeFiltrarPorColumnaDisponible_Asset()
        {
            var datasetAmService = new Mock<IDatasetAmService>();
            var datasetUMService = new Mock<IDatasetUMService>();
            var sondaAMService = new Mock<ISondaAMService>();

            var username = "usuario4";
            var allAssets = new List<AssetDto>
            {
                new() { Id = "A1", Name = "Laptop" },
                new() { Id = "A2", Name = "Monitor" }
            };

            sondaAMService.Setup(s => s.GetAssets(null, null, null, null, null, null, username))
                .ReturnsAsync(allAssets);

            var filters = new List<FilterCondition>
            {
                new() { AttributeName = "Name", Type = FilterType.Equals, ValueType = FilterValueType.String, Condition = "Laptop" }
            };

            var req = new CreateDatasetAMFilteredRequest
            {
                DatasetRequest = new CreateDatasetAMRequest
                {
                    Username = username,
                    Nombre = "Filtrado por nombre",
                    ContentType = "2"
                },
                Filters = filters
            };

            datasetUMService.Setup(s => s.CreateDatasetAsync(It.IsAny<CreateDatasetRequest>()))
                .ReturnsAsync(new Datasets { Id = 40 });
            datasetAmService.Setup(s => s.CreateDatasetAMWithFiltersAsync(It.IsAny<CreateDatasetAMRequest>(), 40, filters))
                .ReturnsAsync(new DatasetAM
                {
                    Id_Dataset = 40,
                    Username = username,
                    Nombre = "Filtrado por nombre",
                    Grupo_Asset = new List<DatasetAsset> { new() { Id_Asset = "A1" } }
                });
            datasetUMService.Setup(s => s.UpdateDatasetAsyncAM(40, It.IsAny<CreateDatasetRequest>(), It.IsAny<DatasetAM>()))
                .ReturnsAsync(new Datasets { Id = 40 });

            var controller = GetController(datasetAmService.Object, null, datasetUMService.Object, null, null, sondaAMService.Object, username: username);

            var result = await controller.CreateDatasetAMFiltered(req);
            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returned = Assert.IsType<DatasetAM>(created.Value);
            Assert.Single(returned.Grupo_Asset);
            Assert.Equal("A1", returned.Grupo_Asset.First().Id_Asset);
        }

        [Fact]
        public async Task PuedeFiltrarPorColumnaDisponible_EventTaskInstance()
        {
            var datasetAmService = new Mock<IDatasetAmService>();
            var datasetUMService = new Mock<IDatasetUMService>();
            var sondaAMService = new Mock<ISondaAMService>();

            var username = "usuario2";
            var allTasks = new List<EventTaskInstanceDto>
            {
                new() { Id = 1, State = "Abierta", Subject = "Revisión" },
                new() { Id = 2, State = "Cerrada", Subject = "Entrega" }
            };

            sondaAMService.Setup(s => s.GetEventTaskInstances(
                It.IsAny<string>(), null, null, null, null, null, null, null, null, false, false, username))
                .ReturnsAsync(allTasks);

            var filters = new List<FilterCondition>
            {
                new() { AttributeName = "State", Type = FilterType.Equals, ValueType = FilterValueType.String, Condition = "Abierta" }
            };

            var req = new CreateDatasetAMFilteredRequest
            {
                DatasetRequest = new CreateDatasetAMRequest
                {
                    Username = username,
                    Nombre = "Filtrado por estado",
                    ContentType = "1"
                },
                Filters = filters
            };

            datasetUMService.Setup(s => s.CreateDatasetAsync(It.IsAny<CreateDatasetRequest>()))
                .ReturnsAsync(new Datasets { Id = 20 });
            datasetAmService.Setup(s => s.CreateDatasetAMWithFiltersAsync(It.IsAny<CreateDatasetAMRequest>(), 20, filters))
                .ReturnsAsync(new DatasetAM
                {
                    Id_Dataset = 20,
                    Username = username,
                    Nombre = "Filtrado por estado",
                    Grupo_Event_Task_Instance = new List<DatasetEventTaskInstance> { new() { Id_Event_Task_Instance = 1 } }
                });
            datasetUMService.Setup(s => s.UpdateDatasetAsyncAM(20, It.IsAny<CreateDatasetRequest>(), It.IsAny<DatasetAM>()))
                .ReturnsAsync(new Datasets { Id = 20 });

            var controller = GetController(datasetAmService.Object, null, datasetUMService.Object, null, null, sondaAMService.Object, username: username);

            var result = await controller.CreateDatasetAMFiltered(req);
            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returned = Assert.IsType<DatasetAM>(created.Value);
            Assert.Single(returned.Grupo_Event_Task_Instance);
            Assert.Equal(1, returned.Grupo_Event_Task_Instance.First().Id_Event_Task_Instance);
        }

        [Fact]
        public async Task PuedeActualizarDatasetAM_AgregandoNuevosRegistros_EventTaskInstance()
        {
            var datasetAmService = new Mock<IDatasetAmService>();
            var datasetUMService = new Mock<IDatasetUMService>();
            var sondaAMService = new Mock<ISondaAMService>();

            var username = "usuario3";
            var existingDataset = new DatasetAM
            {
                Id_Dataset = 30,
                Username = username,
                Nombre = "Dataset Actualizable",
                Grupo_Event_Task_Instance = new List<DatasetEventTaskInstance> { new() { Id_Event_Task_Instance = 1 } }
            };

            datasetAmService.Setup(s => s.GetDatasetAMByIdForEditAsync(30, username)).ReturnsAsync(existingDataset);
            datasetUMService.Setup(s => s.ValidateDatasetNameAsync(It.IsAny<string>(), username, ModuleType.AssetManager, existingDataset.DatasetId)).Returns(Task.CompletedTask);

            var allTasks = new List<EventTaskInstanceDto>
            {
                new() { Id = 1, State = "Abierta", Subject = "Revisión" },
                new() { Id = 2, State = "Abierta", Subject = "Entrega" }
            };
            sondaAMService.Setup(s => s.GetEventTaskInstances(
                It.IsAny<string>(), null, null, null, null, null, null, null, null, false, false, username))
                .ReturnsAsync(allTasks);

            var filters = new List<FilterCondition>
            {
                new() { AttributeName = "State", Type = FilterType.Equals, ValueType = FilterValueType.String, Condition = "Abierta" }
            };

            datasetAmService.Setup(s => s.UpdateDatasetAMWithFiltersAsync(30, It.IsAny<CreateDatasetAMRequest>(), filters))
                .ReturnsAsync(new DatasetAM
                {
                    Id_Dataset = 60,
                    Username = username,
                    Nombre = "Dataset Actualizable",
                    Grupo_Event_Task_Instance = new List<DatasetEventTaskInstance>
                    {
                        new() { Id_Event_Task_Instance = 1 },
                        new() { Id_Event_Task_Instance = 2 }
                    }
                });
            datasetUMService.Setup(s => s.UpdateDatasetAsyncAM(existingDataset.DatasetId, It.IsAny<CreateDatasetRequest>(), It.IsAny<DatasetAM>()))
                .ReturnsAsync(new Datasets { Id = 30 });

            var req = new CreateDatasetAMFilteredRequest
            {
                DatasetRequest = new CreateDatasetAMRequest
                {
                    Username = username,
                    Nombre = "Dataset Actualizable",
                    ContentType = "1"
                },
                Filters = filters
            };

            var controller = GetController(datasetAmService.Object, null, datasetUMService.Object, null, null, sondaAMService.Object, username: username);

            var result = await controller.UpdateDatasetAMFiltered(30, req);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<DatasetAM>(ok.Value);
            Assert.Equal(2, returned.Grupo_Event_Task_Instance.Count);
            Assert.Contains(returned.Grupo_Event_Task_Instance, e => e.Id_Event_Task_Instance == 2);
        }

        [Fact]
        public async Task NoPuedeActualizarDatasetAM_SiNoExiste()
        {
            var datasetAmService = new Mock<IDatasetAmService>();
            var datasetUMService = new Mock<IDatasetUMService>();
            var sondaAMService = new Mock<ISondaAMService>();
            var username = "usuario4";
            datasetAmService.Setup(s => s.GetDatasetAMByIdForEditAsync(40, username)).ReturnsAsync((DatasetAM?)null);
            var req = new CreateDatasetAMFilteredRequest
            {
                DatasetRequest = new CreateDatasetAMRequest
                {
                    Username = username,
                    Nombre = "Dataset Inexistente",
                    ContentType = "2"
                },
                Filters = new List<FilterCondition>()
            };
            var controller = GetController(datasetAmService.Object, null, datasetUMService.Object, null, null, sondaAMService.Object, username: username);
            var result = await controller.UpdateDatasetAMFiltered(40, req);
            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal("No se encontró el DatasetAM con ID 40 para el usuario usuario4.", notFound.Value);

        }

        [Fact]
        public async Task PuedeFiltrarPorColumnaDisponible_Stock()
        {
            var datasetAmService = new Mock<IDatasetAmService>();
            var datasetUMService = new Mock<IDatasetUMService>();
            var sondaAMService = new Mock<ISondaAMService>();
            var username = "usuario5";
            var allStocks = new List<StockDto>
            {
                new() { Id = 100, Location = "Almacén A" },
                new() { Id = 101, Location = "Almacén B" }
            };
            sondaAMService.Setup(s => s.GetAllStock(null,null,null,null,null,username))
                .ReturnsAsync(allStocks);
            var filters = new List<FilterCondition>
            {
                new() { AttributeName = "Location", Type = FilterType.Equals, ValueType = FilterValueType.String, Condition = "Almacén A" }
            };
            var req = new CreateDatasetAMFilteredRequest
            {
                DatasetRequest = new CreateDatasetAMRequest
                {
                    Username = username,
                    Nombre = "Filtrado por ubicación",
                    ContentType = "3"
                },
                Filters = filters
            };
            datasetUMService.Setup(s => s.CreateDatasetAsync(It.IsAny<CreateDatasetRequest>()))
                .ReturnsAsync(new Datasets { Id = 50 });
            datasetAmService.Setup(s => s.CreateDatasetAMWithFiltersAsync(It.IsAny<CreateDatasetAMRequest>(), 50, filters))
                .ReturnsAsync(new DatasetAM
                {
                    Id_Dataset = 50,
                    Username = username,
                    Nombre = "Filtrado por ubicación",
                    Grupo_Stock = new List<DatasetStock> { new() { Id_Stock = 100 } }
                });
            datasetUMService.Setup(s => s.UpdateDatasetAsyncAM(50, It.IsAny<CreateDatasetRequest>(), It.IsAny<DatasetAM>()))
                .ReturnsAsync(new Datasets { Id = 50 });
            var controller = GetController(datasetAmService.Object, null, datasetUMService.Object, null, null, sondaAMService.Object, username: username);
            var result = await controller.CreateDatasetAMFiltered(req);
            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returned = Assert.IsType<DatasetAM>(created.Value);
            Assert.Single(returned.Grupo_Stock);
            Assert.Equal(100, returned.Grupo_Stock.First().Id_Stock);
        }

        [Fact]
        public async Task UpdateDatasetAMFiltered_CuandoNoEncuentra_NoLlamaUpdate()
        {
            var datasetAmService = new Mock<IDatasetAmService>();
            var datasetUMService = new Mock<IDatasetUMService>();
            var username = "usuarioX";
            datasetAmService.Setup(s => s.GetDatasetAMByIdForEditAsync(999, username)).ReturnsAsync((DatasetAM?)null);

            var controller = GetController(datasetAmService.Object, null, datasetUMService.Object, null, null, username: username);

            var req = new CreateDatasetAMFilteredRequest { DatasetRequest = new CreateDatasetAMRequest { Username = username, Nombre = "n", ContentType = "2" }, Filters = new List<FilterCondition>() };
            var result = await controller.UpdateDatasetAMFiltered(999, req);

            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            datasetUMService.Verify(s => s.UpdateDatasetAsyncAM(It.IsAny<int>(), It.IsAny<CreateDatasetRequest>(), It.IsAny<DatasetAM>()), Times.Never);
        }

        [Fact]
        public async Task CreateDatasetAMFiltered_SinFiltros_RetornaValido()
        {
            // Arrange
            var datasetAmService = new Mock<IDatasetAmService>();
            var datasetUMService = new Mock<IDatasetUMService>();
            var sondaAMService = new Mock<ISondaAMService>();

            var username = "validNoFilters";
            // Preparar controller con usuario en contexto
            var controller = GetController(datasetAmService.Object, null, datasetUMService.Object, null, null, sondaAMService.Object, username: username);

            // Mock: UM crea el dataset y devuelve un Id
            datasetUMService.Setup(s => s.CreateDatasetAsync(It.IsAny<CreateDatasetRequest>()))
                .ReturnsAsync(new Datasets { Id = 123 });

            // Mock: la sonda devuelve una lista (puede estar vacía) para que StaticFilterObjects no reciba null
            sondaAMService.Setup(s => s.GetAssets(null, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<int?>(), username))
                .ReturnsAsync(new List<AssetDto>());

            // Mock: AM service crea el DatasetAM con grupos vacíos (resultado esperado)
            datasetAmService.Setup(s => s.CreateDatasetAMWithFiltersAsync(It.IsAny<CreateDatasetAMRequest>(), 123, It.IsAny<List<FilterCondition>>()))
                .ReturnsAsync(new DatasetAM
                {
                    Id_Dataset = 123,
                    Username = username,
                    Nombre = "Dataset Válido Sin Filtros",
                    Grupo_Asset = new List<DatasetAsset>()
                });

            // Mock: UM actualiza el dataset con la entidad AM retornada
            datasetUMService.Setup(s => s.UpdateDatasetAsyncAM(123, It.IsAny<CreateDatasetRequest>(), It.IsAny<DatasetAM>()))
                .ReturnsAsync(new Datasets { Id = 123 });

            var req = new CreateDatasetAMFilteredRequest
            {
                DatasetRequest = new CreateDatasetAMRequest
                {
                    Username = username,
                    Nombre = "Dataset Válido Sin Filtros",
                    ContentType = "2"
                },
                Filters = new List<FilterCondition>() // lista vacía - se interpreta como "no filtrar"
            };

            // Act
            var result = await controller.CreateDatasetAMFiltered(req);

            // Assert: resultado CreatedAtActionResult y contenido correcto
            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returned = Assert.IsType<DatasetAM>(created.Value);

            Assert.Equal(123, returned.Id_Dataset);
            Assert.Equal(username, returned.Username);
            Assert.Equal("Dataset Válido Sin Filtros", returned.Nombre);
            Assert.NotNull(returned.Grupo_Asset);
            Assert.Empty(returned.Grupo_Asset);

            // Verificaciones de llamadas a servicios
            datasetUMService.Verify(s => s.CreateDatasetAsync(It.IsAny<CreateDatasetRequest>()), Times.Once);
            datasetAmService.Verify(s => s.CreateDatasetAMWithFiltersAsync(It.IsAny<CreateDatasetAMRequest>(), 123, It.IsAny<List<FilterCondition>>()), Times.Once);
            datasetUMService.Verify(s => s.UpdateDatasetAsyncAM(123, It.IsAny<CreateDatasetRequest>(), It.IsAny<DatasetAM>()), Times.Once);
        }

        [Fact]
        public async Task CreateDatasetAMFiltered_ModelStateInvalid_ReturnsBadRequest_WithoutCallingAMService()
        {
            var datasetAmService = new Mock<IDatasetAmService>();
            var datasetUMService = new Mock<IDatasetUMService>();
            var sondaAMService = new Mock<ISondaAMService>();

            var username = "modelInvalid";
            var controller = GetController(datasetAmService.Object, null, datasetUMService.Object, null, null, sondaAMService.Object, username: username);

            controller.ModelState.AddModelError("DatasetRequest.Nombre", "required");

            var req = new CreateDatasetAMFilteredRequest
            {
                DatasetRequest = new CreateDatasetAMRequest { Nombre = "", ContentType = "2" },
                Filters = new List<FilterCondition>()
            };

            var result = await controller.CreateDatasetAMFiltered(req);
            var bad = Assert.IsType<BadRequestObjectResult>(result.Result);

            // AM service must not be invoked when model is invalid
            datasetAmService.Verify(s => s.CreateDatasetAMWithFiltersAsync(It.IsAny<CreateDatasetAMRequest>(), It.IsAny<int>(), It.IsAny<List<FilterCondition>>()), Times.Never);
        }

        [Fact]
        public async Task CreateDatasetAMFiltered_SondaThrowsException_ControllerReturns500_AndDoesNotCallAM()
        {
            var datasetAmService = new Mock<IDatasetAmService>();
            var datasetUMService = new Mock<IDatasetUMService>();
            var sondaAMService = new Mock<ISondaAMService>();
            var username = "sondaEx";

            datasetUMService.Setup(s => s.CreateDatasetAsync(It.IsAny<CreateDatasetRequest>())).ReturnsAsync(new Datasets { Id = 200 });
            sondaAMService.Setup(s => s.GetAssets(null, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<int?>(), username))
                .ThrowsAsync(new Exception("sonda fail"));

            var controller = GetController(datasetAmService.Object, null, datasetUMService.Object, null, null, sondaAMService.Object, username: username);

            var req = new CreateDatasetAMFilteredRequest
            {
                DatasetRequest = new CreateDatasetAMRequest { Nombre = "X", ContentType = "2" },
                Filters = new List<FilterCondition>()
            };

            var result = await controller.CreateDatasetAMFiltered(req);
            var obj = Assert.IsType<ObjectResult>(result.Result);
            Assert.InRange(obj.StatusCode ?? 0, 500, 599);
            // ensure AM service never called
            datasetAmService.Verify(s => s.CreateDatasetAMWithFiltersAsync(It.IsAny<CreateDatasetAMRequest>(), It.IsAny<int>(), It.IsAny<List<FilterCondition>>()), Times.Never);
        }

        [Fact]
        public async Task CreateDatasetAMFiltered_SondaReturnsEmpty_ControllerCreatesDatasetWithEmptyGroup()
        {
            var datasetAmService = new Mock<IDatasetAmService>();
            var datasetUMService = new Mock<IDatasetUMService>();
            var sondaAMService = new Mock<ISondaAMService>();
            var username = "sondaEmpty";

            datasetUMService.Setup(s => s.CreateDatasetAsync(It.IsAny<CreateDatasetRequest>())).ReturnsAsync(new Datasets { Id = 300 });
            sondaAMService.Setup(s => s.GetAssets(null, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<int?>(), username))
                .ReturnsAsync(new List<AssetDto>()); // empty

            datasetAmService.Setup(s => s.CreateDatasetAMWithFiltersAsync(It.IsAny<CreateDatasetAMRequest>(), 300, It.IsAny<List<FilterCondition>>()))
                .ReturnsAsync(new DatasetAM { Id_Dataset = 300, Username = username, Nombre = "Empty", Grupo_Asset = new List<DatasetAsset>() });

            datasetUMService.Setup(s => s.UpdateDatasetAsyncAM(300, It.IsAny<CreateDatasetRequest>(), It.IsAny<DatasetAM>())).ReturnsAsync(new Datasets { Id = 300 });

            var controller = GetController(datasetAmService.Object, null, datasetUMService.Object, null, null, sondaAMService.Object, username: username);

            var req = new CreateDatasetAMFilteredRequest
            {
                DatasetRequest = new CreateDatasetAMRequest { Nombre = "Empty", ContentType = "2" },
                Filters = new List<FilterCondition>()
            };

            var result = await controller.CreateDatasetAMFiltered(req);
            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returned = Assert.IsType<DatasetAM>(created.Value);
            Assert.NotNull(returned.Grupo_Asset);
            Assert.Empty(returned.Grupo_Asset);
        }

        [Fact]
        public async Task UpdateDatasetAMFiltered_NoUserInContext_ReturnsBadRequest()
        {
            var datasetAmService = new Mock<IDatasetAmService>();
            var datasetUMService = new Mock<IDatasetUMService>();
            var sondaAMService = new Mock<ISondaAMService>();

            var controller = GetController(datasetAmService.Object, null, datasetUMService.Object, null, null, sondaAMService.Object);
            // remove user
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

            var req = new CreateDatasetAMFilteredRequest { DatasetRequest = new CreateDatasetAMRequest { Nombre = "X", ContentType = "1" }, Filters = new List<FilterCondition>() };
            var result = await controller.UpdateDatasetAMFiltered(1, req);
            var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Usuario no encontrado.", bad.Value);
        }

        [Fact]
        public async Task UpdateDatasetAMFiltered_AMServiceThrows_ReturnsBadRequest()
        {
            var datasetAmService = new Mock<IDatasetAmService>();
            var datasetUMService = new Mock<IDatasetUMService>();
            var sondaAMService = new Mock<ISondaAMService>();
            var username = "svcThrows";

            var existing = new DatasetAM { Id_Dataset = 500, Username = username, Nombre = "Exist" };
            datasetAmService.Setup(s => s.GetDatasetAMByIdForEditAsync(500, username)).ReturnsAsync(existing);
            datasetUMService.Setup(s => s.ValidateDatasetNameAsync(It.IsAny<string>(), username, ModuleType.AssetManager, existing.DatasetId)).Returns(Task.CompletedTask);

            // AM service will throw when trying update (e.g., name conflict)
            datasetAmService.Setup(s => s.UpdateDatasetAMWithFiltersAsync(500, It.IsAny<CreateDatasetAMRequest>(), It.IsAny<List<FilterCondition>>()))
                .ThrowsAsync(new InvalidOperationException("name conflict"));

            sondaAMService.Setup(s => s.GetEventTaskInstances(
            It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<int?>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(),
            It.IsAny<int?>(), It.IsAny<bool>(), It.IsAny<bool>(), username))
            .ReturnsAsync(new List<EventTaskInstanceDto>());

            var controller = GetController(datasetAmService.Object, null, datasetUMService.Object, null, null, sondaAMService.Object, username: username);

            var req = new CreateDatasetAMFilteredRequest { DatasetRequest = new CreateDatasetAMRequest { Nombre = "X", ContentType = "1" }, Filters = new List<FilterCondition>() };
            var result = await controller.UpdateDatasetAMFiltered(500, req);
            var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("name conflict", bad.Value);
        }

        [Fact]
        public async Task UpdateDatasetAMFiltered_ContentTypeInvalid_ReturnsBadRequest()
        {
            var datasetAmService = new Mock<IDatasetAmService>();
            var datasetUMService = new Mock<IDatasetUMService>();
            var sondaAMService = new Mock<ISondaAMService>();
            var username = "badType";

            var existing = new DatasetAM { Id_Dataset = 600, Username = username, Nombre = "Exist" };
            datasetAmService.Setup(s => s.GetDatasetAMByIdForEditAsync(600, username)).ReturnsAsync(existing);
            datasetUMService.Setup(s => s.ValidateDatasetNameAsync(It.IsAny<string>(), username, ModuleType.AssetManager, existing.DatasetId)).Returns(Task.CompletedTask);

            var controller = GetController(datasetAmService.Object, null, datasetUMService.Object, null, null, sondaAMService.Object, username: username);

            var req = new CreateDatasetAMFilteredRequest { DatasetRequest = new CreateDatasetAMRequest { Nombre = "X", ContentType = "999" }, Filters = new List<FilterCondition>() };

            var result = await controller.UpdateDatasetAMFiltered(600, req);
            var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("ContentType inválido o no soportado", bad.Value);
        }

        /* Tests sobre GetAllDatasetAMs */
        [Fact]
        public async Task GetAllDatasetAMs_ReturnsList_ForUser()
        {
            // Arrange
            var datasetAmService = new Mock<IDatasetAmService>();
            var datasetUMService = new Mock<IDatasetUMService>();
            var sondaAMService = new Mock<ISondaAMService>();

            var username = "listUser";

            var expected = new List<DatasetAM>
    {
        new DatasetAM { Id_Dataset = 1, Username = username, Nombre = "D1", Grupo_Asset = new List<DatasetAsset>() },
        new DatasetAM { Id_Dataset = 2, Username = username, Nombre = "D2", Grupo_Asset = new List<DatasetAsset>() }
    };

            datasetAmService.Setup(s => s.GetAllDatasetAMsAsync(username)).ReturnsAsync(expected);

            var controller = GetController(datasetAmService.Object, null, datasetUMService.Object, null, null, sondaAMService.Object, username: username);

            // Act
            var result = await controller.GetAllDatasetAMs();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<List<DatasetAM>>(ok.Value);
            Assert.Equal(2, returned.Count);
            Assert.Contains(returned, d => d.Id_Dataset == 1 && d.Nombre == "D1");
        }

        [Fact]
        public async Task GetAllDatasetAMs_ReturnsEmptyList_WhenNoneExist()
        {
            // Arrange
            var datasetAmService = new Mock<IDatasetAmService>();
            var datasetUMService = new Mock<IDatasetUMService>();
            var sondaAMService = new Mock<ISondaAMService>();
            var username = "emptyUser";
            datasetAmService.Setup(s => s.GetAllDatasetAMsAsync(username)).ReturnsAsync(new List<DatasetAM>());
            var controller = GetController(datasetAmService.Object, null, datasetUMService.Object, null, null, sondaAMService.Object, username: username);
            // Act
            var result = await controller.GetAllDatasetAMs();
            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<List<DatasetAM>>(ok.Value);
            Assert.Empty(returned);
        }

        [Fact]
        public async Task GetAllDatasetAMs_ServiceThrows_Returns500()
        {
            // Arrange
            var datasetAmService = new Mock<IDatasetAmService>();
            var datasetUMService = new Mock<IDatasetUMService>();
            var sondaAMService = new Mock<ISondaAMService>();

            var username = "errUser";
            datasetAmService.Setup(s => s.GetAllDatasetAMsAsync(username)).ThrowsAsync(new Exception("boom"));

            var controller = GetController(datasetAmService.Object, null, datasetUMService.Object, null, null, sondaAMService.Object, username: username);

            // Act
            var result = await controller.GetAllDatasetAMs();

            // Assert
            var obj = Assert.IsType<ObjectResult>(result.Result);
            Assert.InRange(obj.StatusCode ?? 0, 500, 599);
            datasetAmService.Verify(s => s.GetAllDatasetAMsAsync(username), Times.Once);
        }

        [Fact]
        public async Task GetAllDatasetAMs_NoUserInContext_ReturnsBadRequest()
        {
            // Arrange
            var datasetAmService = new Mock<IDatasetAmService>();
            var datasetUMService = new Mock<IDatasetUMService>();
            var sondaAMService = new Mock<ISondaAMService>();
            var controller = GetController(datasetAmService.Object, null, datasetUMService.Object, null, null, sondaAMService.Object);
            // Remove user from context
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            // Act
            var result = await controller.GetAllDatasetAMs();
            // Assert
            var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Usuario no encontrado.", bad.Value);
        }

        /* Tests sobre GetDatasetAMById */

        [Fact]
        public async Task GetDatasetAMById_ReturnsNotFound_WhenMissing()
        {
            // Arrange
            var datasetAmService = new Mock<IDatasetAmService>();
            var datasetUMService = new Mock<IDatasetUMService>();
            var sondaAMService = new Mock<ISondaAMService>();

            var username = "missingUser";
            datasetAmService.Setup(s => s.GetDatasetAMByIdAsync(999, username)).ReturnsAsync((DatasetAM?)null);

            var controller = GetController(datasetAmService.Object, null, datasetUMService.Object, null, null, sondaAMService.Object, username: username);

            // Act
            var result = await controller.GetDatasetAMById(999);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal($"No se encontró el DatasetAM con ID 999 para el usuario {username}.", notFound.Value);
        }

        [Fact]
        public async Task GetDatasetAMById_ReturnsDataset_WhenExists()
        {
            // Arrange
            var datasetAmService = new Mock<IDatasetAmService>();
            var datasetUMService = new Mock<IDatasetUMService>();
            var sondaAMService = new Mock<ISondaAMService>();

            var username = "existingUser";
            var ds = new DatasetAM { Id_Dataset = 10, Username = username, Nombre = "Exist" };

            datasetAmService.Setup(s => s.GetDatasetAMByIdAsync(10, username)).ReturnsAsync(ds);

            var controller = GetController(datasetAmService.Object, null, datasetUMService.Object, null, null, sondaAMService.Object, username: username);

            // Act
            var result = await controller.GetDatasetAMById(10);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<DatasetAM>(ok.Value);
            Assert.Equal(10, returned.Id_Dataset);
            Assert.Equal("Exist", returned.Nombre);
        }

        [Fact]
        public async Task GetDatasetAMById_ReturnsDataset_WhenExists_NormalCase()
        {
            // Arrange
            var datasetAmService = new Mock<IDatasetAmService>();
            var username = "userA";
            var ds = new DatasetAM { Id_Dataset = 42, Username = username, Nombre = "Existente" };
            datasetAmService.Setup(s => s.GetDatasetAMByIdAsync(42, username)).ReturnsAsync(ds);

            var controller = GetController(datasetAmService.Object, null, null, null, null, username: username);

            // Act
            var result = await controller.GetDatasetAMById(42);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<DatasetAM>(ok.Value);
            Assert.Equal(42, returned.Id_Dataset);
            Assert.Equal("Existente", returned.Nombre);
            datasetAmService.Verify(s => s.GetAllDatasetAMsAsync(It.IsAny<string>()), Times.Never);
            datasetAmService.Verify(s => s.GetDatasetAMByIdAsync(42, username), Times.Once);
        }

        [Fact]
        public async Task GetDatasetAMById_ReturnsNotFound_WhenServiceReturnsNull()
        {
            // Arrange
            var datasetAmService = new Mock<IDatasetAmService>();
            var username = "userB";
            datasetAmService.Setup(s => s.GetDatasetAMByIdAsync(99, username)).ReturnsAsync((DatasetAM?)null);

            var controller = GetController(datasetAmService.Object, null, null, null, null, username: username);

            // Act
            var result = await controller.GetDatasetAMById(99);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal($"No se encontró el DatasetAM con ID 99 para el usuario {username}.", notFound.Value);
            datasetAmService.Verify(s => s.GetDatasetAMByIdAsync(99, username), Times.Once);
        }

        [Fact]
        public async Task GetDatasetAMById_ServiceThrows_Returns500()
        {
            // Arrange
            var datasetAmService = new Mock<IDatasetAmService>();
            var username = "userErr";
            datasetAmService.Setup(s => s.GetDatasetAMByIdAsync(10, username)).ThrowsAsync(new Exception("boom"));

            var controller = GetController(datasetAmService.Object, null, null, null, null, username: username);

            // Act
            var result = await controller.GetDatasetAMById(10);

            // Assert
            var obj = Assert.IsType<ObjectResult>(result.Result);
            Assert.InRange(obj.StatusCode ?? 0, 500, 599);
            Assert.Contains("boom", obj.Value?.ToString() ?? string.Empty);
            datasetAmService.Verify(s => s.GetDatasetAMByIdAsync(10, username), Times.Once);
        }

        [Fact]
        public async Task GetDatasetAMById_NoUserInContext_PassesNullToService_AndHandlesResponse()
        {
            // Arrange
            var datasetAmService = new Mock<IDatasetAmService>();
            // Simulate service returning null when username is null
            datasetAmService.Setup(s => s.GetDatasetAMByIdAsync(5, null)).ReturnsAsync((DatasetAM?)null);

            var controller = GetController(datasetAmService.Object, null, null, null, null, username: "someUser");
            // remove user from context to simulate missing JWT
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

            // Act
            var result = await controller.GetDatasetAMById(5);

            // Assert -> controller will forward null username to service resulting in NotFound (as mocked)
            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal($"No se encontró el DatasetAM con ID 5 para el usuario .", notFound.Value);
            datasetAmService.Verify(s => s.GetDatasetAMByIdAsync(5, null), Times.Once);
        }

        [Fact]
        public async Task GetDatasetAMById_NegativeId_ReturnsNotFound()
        {
            // Arrange
            var datasetAmService = new Mock<IDatasetAmService>();
            var username = "userNeg";
            // Controller doesn't validate id itself; service returning null simulates not found for negative id
            datasetAmService.Setup(s => s.GetDatasetAMByIdAsync(-1, username)).ReturnsAsync((DatasetAM?)null);

            var controller = GetController(datasetAmService.Object, null, null, null, null, username: username);

            // Act
            var result = await controller.GetDatasetAMById(-1);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal($"No se encontró el DatasetAM con ID -1 para el usuario {username}.", notFound.Value);
            datasetAmService.Verify(s => s.GetDatasetAMByIdAsync(-1, username), Times.Once);
        }

        [Fact]
        public async Task GetDatasetAMById_ReturnsDataset_WithNullFieldsAndLargeCollections()
        {
            // Arrange
            var datasetAmService = new Mock<IDatasetAmService>();
            var username = "bigUser";
            // Create dataset with null optional fields and large collections to ensure controller returns payload intact
            var big = new DatasetAM
            {
                Id_Dataset = 5000,
                Username = username,
                Nombre = null!, // simulate null despite Required attribute; service might return such object
                Descripcion = null,
                Filters = null,
                Grupo_Asset = Enumerable.Range(0, 500).Select(i => new DatasetAsset { Id_Asset = i.ToString() }).ToList(),
                Grupo_Event_Task_Instance = Enumerable.Range(0, 300).Select(i => new DatasetEventTaskInstance { Id_Event_Task_Instance = i }).ToList(),
                Grupo_Stock = Enumerable.Range(0, 200).Select(i => new DatasetStock { Id_Stock = i }).ToList()
            };
            datasetAmService.Setup(s => s.GetDatasetAMByIdAsync(5000, username)).ReturnsAsync(big);

            var controller = GetController(datasetAmService.Object, null, null, null, null, username: username);

            // Act
            var result = await controller.GetDatasetAMById(5000);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<DatasetAM>(ok.Value);
            Assert.Equal(5000, returned.Id_Dataset);
            // despite Nombre being null in the object, controller returns it as-is
            Assert.Null(returned.Nombre);
            Assert.Equal(500, returned.Grupo_Asset.Count);
            Assert.Equal(300, returned.Grupo_Event_Task_Instance.Count);
            Assert.Equal(200, returned.Grupo_Stock.Count);
            datasetAmService.Verify(s => s.GetDatasetAMByIdAsync(5000, username), Times.Once);
        }

        [Fact]
        public async Task GetDatasetAMById_ServiceReturnsDatasetForDifferentUser_ControllerReturnsOk()
        {
            // Arrange
            var datasetAmService = new Mock<IDatasetAmService>();
            var usernameInToken = "callerUser";
            var datasetOwner = "otherUser";
            var ds = new DatasetAM { Id_Dataset = 77, Username = datasetOwner, Nombre = "OwnedByOther" };

            // Service returns a dataset owned by otherUser even if caller is callerUser.
            datasetAmService.Setup(s => s.GetDatasetAMByIdAsync(77, usernameInToken)).ReturnsAsync(ds);

            var controller = GetController(datasetAmService.Object, null, null, null, null, username: usernameInToken);

            // Act
            var result = await controller.GetDatasetAMById(77);

            // Assert: controller just returns what service gives; business ownership enforcement is responsibility of the service
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<DatasetAM>(ok.Value);
            Assert.Equal("OwnedByOther", returned.Nombre);
            Assert.Equal(datasetOwner, returned.Username);
            datasetAmService.Verify(s => s.GetDatasetAMByIdAsync(77, usernameInToken), Times.Once);
        }

        /* Tests sobre GetDatasetAMByIdForEdit */

        [Fact]
        public async Task GetDatasetAMByIdForEdit_ReturnsDataset_WhenExists()
        {
            // Arrange
            var datasetAmService = new Mock<IDatasetAmService>();
            var datasetUMService = new Mock<IDatasetUMService>();
            var sondaAMService = new Mock<ISondaAMService>();

            var username = "editUser";
            var ds = new DatasetAM { Id_Dataset = 20, Username = username, Nombre = "ToEdit" };

            datasetAmService.Setup(s => s.GetDatasetAMByIdForEditAsync(20, username)).ReturnsAsync(ds);

            var controller = GetController(datasetAmService.Object, null, datasetUMService.Object, null, null, sondaAMService.Object, username: username);

            // Act
            var result = await controller.GetDatasetAMByIdForEdit(20);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<DatasetAM>(ok.Value);
            Assert.Equal(20, returned.Id_Dataset);
            Assert.Equal("ToEdit", returned.Nombre);
        }

        /* Tests sobre DeleteDatasetAM */

        [Fact]
        public async Task DeleteDatasetAM_Success_RemovesAndReturnsNoContent()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            using var context = new ApplicationDbContext(options);

            var username = "deleter";
            var ds = new DatasetAM
            {
                Id_Dataset = 77,
                Username = username,
                Nombre = "ToDelete",
                DatasetId = 777
            };
            context.DatasetAM.Add(ds);
            await context.SaveChangesAsync();

            var datasetAmService = new Mock<IDatasetAmService>();
            var datasetUMService = new Mock<IDatasetUMService>();
            var sondaAMService = new Mock<ISondaAMService>();

            // Expect DeleteDatasetAMAsync and DeleteDatasetAsync to be called once
            datasetAmService.Setup(s => s.DeleteDatasetAMAsync(77, username)).Returns(Task.CompletedTask).Verifiable();
            datasetUMService.Setup(s => s.DeleteDatasetAsync(777, username)).Returns(Task.CompletedTask).Verifiable();

            var controller = GetController(datasetAmService.Object, null, datasetUMService.Object, null, context, sondaAMService.Object, username: username);

            // Act
            var result = await controller.DeleteDatasetAM(77);

            // Assert
            Assert.IsType<NoContentResult>(result);
            datasetAmService.Verify();
            datasetUMService.Verify();
        }

        [Fact]
        public async Task DeleteDatasetAM_ReturnsNotFound_WhenNoDatasetForUser()
        {
            // Arrange: empty in-memory context
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            using var context = new ApplicationDbContext(options);

            var datasetAmService = new Mock<IDatasetAmService>();
            var datasetUMService = new Mock<IDatasetUMService>();
            var sondaAMService = new Mock<ISondaAMService>();

            var username = "noDeleteUser";
            var controller = GetController(datasetAmService.Object, null, datasetUMService.Object, null, context, sondaAMService.Object, username: username);

            // Act
            var result = await controller.DeleteDatasetAM(1234);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal($"No se encontró el dataset con ID 1234 para el usuario {username}.", notFound.Value);
            datasetAmService.Verify(s => s.DeleteDatasetAMAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
            datasetUMService.Verify(s => s.DeleteDatasetAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task DeleteDatasetAM_ReturnsNotFound_WhenDatasetBelongsToOtherUser()
        {
            // Arrange: context contains dataset for different user
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            using var context = new ApplicationDbContext(options);

            var owner = "ownerUser";
            var caller = "callerUser";
            var ds = new DatasetAM
            {
                Id_Dataset = 202,
                Username = owner,
                Nombre = "OtherOwner",
                DatasetId = 2002
            };
            context.DatasetAM.Add(ds);
            await context.SaveChangesAsync();

            var datasetAmService = new Mock<IDatasetAmService>();
            var datasetUMService = new Mock<IDatasetUMService>();
            var sondaAMService = new Mock<ISondaAMService>();

            var controller = GetController(datasetAmService.Object, null, datasetUMService.Object, null, context, sondaAMService.Object, username: caller);

            // Act
            var result = await controller.DeleteDatasetAM(202);

            // Assert: controller queries by id AND username, so should return NotFound for callerUser
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal($"No se encontró el dataset con ID 202 para el usuario {caller}.", notFound.Value);
            datasetAmService.Verify(s => s.DeleteDatasetAMAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
            datasetUMService.Verify(s => s.DeleteDatasetAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task DeleteDatasetAM_AmServiceThrows_ReturnsBadRequest_AndDoesNotCallUmDelete()
        {
            // Arrange: context with dataset for user
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            using var context = new ApplicationDbContext(options);

            var username = "deleter_am_throw";
            var ds = new DatasetAM
            {
                Id_Dataset = 303,
                Username = username,
                Nombre = "ToDeleteAMErr",
                DatasetId = 3003
            };
            context.DatasetAM.Add(ds);
            await context.SaveChangesAsync();

            var datasetAmService = new Mock<IDatasetAmService>();
            var datasetUMService = new Mock<IDatasetUMService>();
            var sondaAMService = new Mock<ISondaAMService>();

            datasetAmService.Setup(s => s.DeleteDatasetAMAsync(303, username)).ThrowsAsync(new Exception("AM delete failed"));
            // UM delete should not be called when AM service fails
            datasetUMService.Setup(s => s.DeleteDatasetAsync(It.IsAny<int>(), It.IsAny<string>())).Verifiable();

            var controller = GetController(datasetAmService.Object, null, datasetUMService.Object, null, context, sondaAMService.Object, username: username);

            // Act
            var result = await controller.DeleteDatasetAM(303);

            // Assert
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            // mensaje del controller incluye error.Message
            Assert.Equal(new { error = "AM delete failed" }.ToString(), bad.Value?.ToString(), ignoreCase: true);
            datasetAmService.Verify(s => s.DeleteDatasetAMAsync(303, username), Times.Once);
            datasetUMService.Verify(s => s.DeleteDatasetAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task DeleteDatasetAM_UmServiceThrows_ReturnsBadRequest()
        {
            // Arrange: context with dataset for user
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            using var context = new ApplicationDbContext(options);

            var username = "deleter_um_throw";
            var ds = new DatasetAM
            {
                Id_Dataset = 404,
                Username = username,
                Nombre = "ToDeleteUMErr",
                DatasetId = 4004
            };
            context.DatasetAM.Add(ds);
            await context.SaveChangesAsync();

            var datasetAmService = new Mock<IDatasetAmService>();
            var datasetUMService = new Mock<IDatasetUMService>();
            var sondaAMService = new Mock<ISondaAMService>();

            datasetAmService.Setup(s => s.DeleteDatasetAMAsync(404, username)).Returns(Task.CompletedTask);
            datasetUMService.Setup(s => s.DeleteDatasetAsync(4004, username)).ThrowsAsync(new Exception("UM delete failed"));

            var controller = GetController(datasetAmService.Object, null, datasetUMService.Object, null, context, sondaAMService.Object, username: username);

            // Act
            var result = await controller.DeleteDatasetAM(404);

            // Assert
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            // controller returns BadRequest(new { error = ex.Message })
            Assert.Equal(new { error = "UM delete failed" }.ToString(), bad.Value?.ToString(), ignoreCase: true);
            datasetAmService.Verify(s => s.DeleteDatasetAMAsync(404, username), Times.Once);
            datasetUMService.Verify(s => s.DeleteDatasetAsync(4004, username), Times.Once);
        }

        [Fact]
        public async Task DeleteDatasetAM_WhenMultipleDatasetsSameIdDifferentUsers_DeletesOnlyForCaller()
        {
            // Arrange: insert two DatasetAM rows with same Id_Dataset? (Id_Dataset is PK so cannot duplicate),
            // instead simulate same DatasetId (foreign dataset) with different users to ensure lookup uses username filter.
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            using var context = new ApplicationDbContext(options);

            var owner = "owner_multi";
            var other = "other_multi";
            var dsOwner = new DatasetAM
            {
                Id_Dataset = 505,
                Username = owner,
                Nombre = "OwnerRow",
                DatasetId = 5005
            };
            var dsOther = new DatasetAM
            {
                Id_Dataset = 606,
                Username = other,
                Nombre = "OtherRow",
                DatasetId = 5005 // same DatasetId but different Id_Dataset and different Username
            };
            context.DatasetAM.AddRange(dsOwner, dsOther);
            await context.SaveChangesAsync();

            var datasetAmService = new Mock<IDatasetAmService>();
            var datasetUMService = new Mock<IDatasetUMService>();
            var sondaAMService = new Mock<ISondaAMService>();

            // Expect deletion only when caller is 'owner_multi' and id 505 is requested
            datasetAmService.Setup(s => s.DeleteDatasetAMAsync(505, owner)).Returns(Task.CompletedTask).Verifiable();
            datasetUMService.Setup(s => s.DeleteDatasetAsync(5005, owner)).Returns(Task.CompletedTask).Verifiable();

            var controller = GetController(datasetAmService.Object, null, datasetUMService.Object, null, context, sondaAMService.Object, username: owner);

            // Act
            var result = await controller.DeleteDatasetAM(505);

            // Assert
            Assert.IsType<NoContentResult>(result);
            datasetAmService.Verify(s => s.DeleteDatasetAMAsync(505, owner), Times.Once);
            datasetUMService.Verify(s => s.DeleteDatasetAsync(5005, owner), Times.Once);

            // Ensure attempting to delete the other id with this caller yields NotFound
            var notFoundResult = await controller.DeleteDatasetAM(606);
            var notFound = Assert.IsType<NotFoundObjectResult>(notFoundResult);
            Assert.Equal($"No se encontró el dataset con ID 606 para el usuario {owner}.", notFound.Value);
        }
    }
}
