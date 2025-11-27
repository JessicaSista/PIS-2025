using Microsoft.AspNetCore.Mvc;
using Moq;
using OmniMonitor.Server.Controllers;
using OmniMonitor.Server.Context;
using OmniMonitor.Server.Services;
using OmniMonitor.Shared.Dtos;
using OmniMonitor.Shared.Dtos.EM;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Diagnostics;

namespace QA.Tests
{
    public class DatasetEMControllerTests
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

        private static DatasetEMController GetController(
            IDatasetEMService? datasetEMService = null,
            ISondaAuthService? sondaAuthService = null,
            IDatasetUMService? datasetUMService = null,
            ISondaEMService? sondaEMService = null,
            IKpiService? kpiService = null,
            ApplicationDbContext? context = null,
            string username = "testuser")
        {
            var ctrl = new DatasetEMController(
                datasetEMService ?? Mock.Of<IDatasetEMService>(),
                sondaAuthService ?? Mock.Of<ISondaAuthService>(),
                datasetUMService ?? Mock.Of<IDatasetUMService>(),
                sondaEMService ?? Mock.Of<ISondaEMService>(),
                kpiService ?? Mock.Of<IKpiService>(),
                context ?? new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options)
            );
            ctrl.ControllerContext = GetControllerContext(username);
            return ctrl;
        }

        /* Tests sobre Create y Update */

        [Fact]
        public async Task CreateDatasetEMFiltered_SinFiltros_RetornaValido()
        {
            // Arrange
            var datasetEmService = new Mock<IDatasetEMService>();
            var datasetUMService = new Mock<IDatasetUMService>();
            var sondaEmService = new Mock<ISondaEMService>();

            var username = "validNoFiltersEM";
            var controller = GetController(datasetEmService.Object, null, datasetUMService.Object, sondaEmService.Object, username: username);

            // UM creates dataset and returns id
            datasetUMService.Setup(s => s.CreateDatasetAsync(It.IsAny<CreateDatasetRequest>()))
                .ReturnsAsync(new Datasets { Id = 200 });

            // Sonda returns an empty list so StaticFilterObjects doesn't receive null
            sondaEmService.Setup(s => s.GetEvents(null, null, null, null, username))
                .ReturnsAsync(new List<EventDto>());

            // EM service returns created DatasetEM with empty groups
            datasetEmService.Setup(s => s.CreateDatasetEMWithFiltersAsync(It.IsAny<CreateDatasetEMRequest>(), 200, It.IsAny<List<FilterCondition>>()))
                .ReturnsAsync(new DatasetEM
                {
                    Id = 200,
                    Username = username,
                    Name = "Dataset Válido Sin Filtros",
                    DatasetEvents = new List<DatasetEventEM>(),
                    DatasetAlerts = new List<DatasetAlert>(),
                    DatasetExtensions = new List<DatasetExtension>()
                });

            // UM update after creation
            datasetUMService.Setup(s => s.UpdateDatasetAsyncEM(200, It.IsAny<CreateDatasetRequest>(), It.IsAny<DatasetEM>()))
                .ReturnsAsync(new Datasets { Id = 200 });

            var req = new CreateDatasetEMFilteredRequest
            {
                DatasetRequest = new CreateDatasetEMRequest
                {
                    Username = username,
                    Name = "Dataset Válido Sin Filtros",
                    IsDataset = "S",
                    ContentType = "2"
                },
                Filters = new List<FilterCondition>() // empty = interpreted as no filtering
            };

            // Act
            var result = await controller.CreateDatasetEMFiltered(req);

            // Assert
            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returned = Assert.IsType<DatasetEM>(created.Value);

            Assert.Equal(200, returned.Id);
            Assert.Equal(username, returned.Username);
            Assert.Equal("Dataset Válido Sin Filtros", returned.Name);
            Assert.NotNull(returned.DatasetEvents);
            Assert.Empty(returned.DatasetEvents);

            datasetUMService.Verify(s => s.CreateDatasetAsync(It.IsAny<CreateDatasetRequest>()), Times.Once);
            datasetEmService.Verify(s => s.CreateDatasetEMWithFiltersAsync(It.IsAny<CreateDatasetEMRequest>(), 200, It.IsAny<List<FilterCondition>>()), Times.Once);
            datasetUMService.Verify(s => s.UpdateDatasetAsyncEM(200, It.IsAny<CreateDatasetRequest>(), It.IsAny<DatasetEM>()), Times.Once);
        }

        [Fact]
        public async Task CreateDatasetEMFiltered_FiltersNull_ReturnsBadRequest()
        {
            var svc = new Mock<IDatasetEMService>();
            var um = new Mock<IDatasetUMService>();
            var sonda = new Mock<ISondaEMService>();
            var controller = GetController(svc.Object, null, um.Object, sonda.Object, username: "user_null");
            var req = new CreateDatasetEMFilteredRequest
            {
                DatasetRequest = new CreateDatasetEMRequest { Name = "X", IsDataset = "S", ContentType = "1" },
                Filters = null
            };

            var result = await controller.CreateDatasetEMFiltered(req);
            var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(bad.Value);
            svc.Verify(s => s.CreateDatasetEMWithFiltersAsync(It.IsAny<CreateDatasetEMRequest>(), It.IsAny<int>(), It.IsAny<List<FilterCondition>>()), Times.Never);
        }

        [Fact]
        public async Task CreateDatasetEMFiltered_InvalidSondaNull_Returns500_AndDoesNotCallService()
        {
            var svc = new Mock<IDatasetEMService>();
            var um = new Mock<IDatasetUMService>();
            var sonda = new Mock<ISondaEMService>();
            var username = "sonda_null_em";

            um.Setup(u => u.CreateDatasetAsync(It.IsAny<CreateDatasetRequest>())).ReturnsAsync(new Datasets { Id = 77 });
            sonda.Setup(s => s.GetEvents(null, null, null, null, username)).ThrowsAsync(new Exception("sonda fail"));

            var controller = GetController(svc.Object, null, um.Object, sonda.Object, username: username);

            var req = new CreateDatasetEMFilteredRequest
            {
                DatasetRequest = new CreateDatasetEMRequest { Name = "X", IsDataset = "S", ContentType = "2" },
                Filters = new List<FilterCondition> { new() { AttributeName = "Name", Type = FilterType.Contains, ValueType = FilterValueType.String, Condition = "X" } }
            };

            var result = await controller.CreateDatasetEMFiltered(req);
            var obj = Assert.IsType<ObjectResult>(result.Result);
            Assert.InRange(obj.StatusCode ?? 0, 500, 599);
            svc.Verify(s => s.CreateDatasetEMWithFiltersAsync(It.IsAny<CreateDatasetEMRequest>(), It.IsAny<int>(), It.IsAny<List<FilterCondition>>()), Times.Never);
        }

        [Fact]
        public async Task CreateDatasetEMFiltered_FiltraAlertasPorEstado()
        {
            var svc = new Mock<IDatasetEMService>();
            var um = new Mock<IDatasetUMService>();
            var sonda = new Mock<ISondaEMService>();
            var username = "alertUser";

            var allAlerts = new List<AlertDto>
            {
                new() { AlertId = 1, AlertName = "Incendio", AlertState = "Activa" },
                new() { AlertId = 2, AlertName = "Robo", AlertState = "Cerrada" }
            };
            sonda.Setup(s => s.GetAlerts(null, null, null, null, null, null, null, null, null, username)).ReturnsAsync(allAlerts);

            var filters = new List<FilterCondition>
            {
                new() { AttributeName = "AlertState", Type = FilterType.Equals, ValueType = FilterValueType.String, Condition = "Activa" }
            };
            var req = new CreateDatasetEMFilteredRequest
            {
                DatasetRequest = new CreateDatasetEMRequest
                {
                    Username = username,
                    Name = "Solo Activas",
                    IsDataset = "S",
                    ContentType = "1"
                },
                Filters = filters
            };

            um.Setup(u => u.CreateDatasetAsync(It.IsAny<CreateDatasetRequest>())).ReturnsAsync(new Datasets { Id = 1 });
            svc.Setup(s => s.CreateDatasetEMWithFiltersAsync(It.IsAny<CreateDatasetEMRequest>(), 1, filters))
                .ReturnsAsync(new DatasetEM
                {
                    Id = 1,
                    Username = username,
                    Name = "Solo Activas",
                    DatasetAlerts = new List<DatasetAlert> { new() { Id_alert = 1 } }
                });
            um.Setup(u => u.UpdateDatasetAsyncEM(1, It.IsAny<CreateDatasetRequest>(), It.IsAny<DatasetEM>())).ReturnsAsync(new Datasets { Id = 1 });

            var controller = GetController(svc.Object, null, um.Object, sonda.Object, username: username);

            var result = await controller.CreateDatasetEMFiltered(req);
            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returned = Assert.IsType<DatasetEM>(created.Value);
            Assert.Single(returned.DatasetAlerts);
            Assert.Equal(1, returned.DatasetAlerts.First().Id_alert);
        }

        [Fact]
        public async Task CreateDatasetEMFiltered_FiltraEventosPorNombre()
        {
            var svc = new Mock<IDatasetEMService>();
            var um = new Mock<IDatasetUMService>();
            var sonda = new Mock<ISondaEMService>();
            var username = "eventUser";

            var allEvents = new List<EventDto>
            {
                new() { Id = 10, Name = "Corte de luz" },
                new() { Id = 11, Name = "Incendio" }
            };
            sonda.Setup(s => s.GetEvents(null, null, null, null, username)).ReturnsAsync(allEvents);

            var filters = new List<FilterCondition>
            {
                new() { AttributeName = "Name", Type = FilterType.Contains, ValueType = FilterValueType.String, Condition = "Incendio" }
            };
            var req = new CreateDatasetEMFilteredRequest
            {
                DatasetRequest = new CreateDatasetEMRequest
                {
                    Username = username,
                    Name = "Solo Incendios",
                    IsDataset = "S",
                    ContentType = "2"
                },
                Filters = filters
            };

            um.Setup(u => u.CreateDatasetAsync(It.IsAny<CreateDatasetRequest>())).ReturnsAsync(new Datasets { Id = 2 });
            svc.Setup(s => s.CreateDatasetEMWithFiltersAsync(It.IsAny<CreateDatasetEMRequest>(), 2, filters))
                .ReturnsAsync(new DatasetEM
                {
                    Id = 2,
                    Username = username,
                    Name = "Solo Incendios",
                    DatasetEvents = new List<DatasetEventEM> { new() { Id_event = 11 } }
                });
            um.Setup(u => u.UpdateDatasetAsyncEM(2, It.IsAny<CreateDatasetRequest>(), It.IsAny<DatasetEM>())).ReturnsAsync(new Datasets { Id = 2 });

            var controller = GetController(svc.Object, null, um.Object, sonda.Object, username: username);

            var result = await controller.CreateDatasetEMFiltered(req);
            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returned = Assert.IsType<DatasetEM>(created.Value);
            Assert.Single(returned.DatasetEvents);
            Assert.Equal(11, returned.DatasetEvents.First().Id_event);
        }

        [Fact]
        public async Task CreateDatasetEMFiltered_FiltraExtensionesPorEstado()
        {
            var svc = new Mock<IDatasetEMService>();
            var um = new Mock<IDatasetUMService>();
            var sonda = new Mock<ISondaEMService>();
            var username = "extUser";

            var allExtensions = new List<ExtensionDto>
            {
                new() { ExtensionId = 100, ExtensionState = "Abierta" },
                new() { ExtensionId = 101, ExtensionState = "Cerrada" }
            };
            sonda.Setup(s => s.GetExtensions(null, null, null, null, null, null, null, null, null, username)).ReturnsAsync(allExtensions);

            var filters = new List<FilterCondition>
            {
                new() { AttributeName = "ExtensionState", Type = FilterType.Equals, ValueType = FilterValueType.String, Condition = "Abierta" }
            };
            var req = new CreateDatasetEMFilteredRequest
            {
                DatasetRequest = new CreateDatasetEMRequest
                {
                    Username = username,
                    Name = "Solo Abiertas",
                    IsDataset = "S",
                    ContentType = "3"
                },
                Filters = filters
            };

            um.Setup(u => u.CreateDatasetAsync(It.IsAny<CreateDatasetRequest>())).ReturnsAsync(new Datasets { Id = 3 });
            svc.Setup(s => s.CreateDatasetEMWithFiltersAsync(It.IsAny<CreateDatasetEMRequest>(), 3, filters))
                .ReturnsAsync(new DatasetEM
                {
                    Id = 3,
                    Username = username,
                    Name = "Solo Abiertas",
                    DatasetExtensions = new List<DatasetExtension> { new() { Id_extension = 100 } }
                });
            um.Setup(u => u.UpdateDatasetAsyncEM(3, It.IsAny<CreateDatasetRequest>(), It.IsAny<DatasetEM>())).ReturnsAsync(new Datasets { Id = 3 });

            var controller = GetController(svc.Object, null, um.Object, sonda.Object, username: username);

            var result = await controller.CreateDatasetEMFiltered(req);
            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returned = Assert.IsType<DatasetEM>(created.Value);
            Assert.Single(returned.DatasetExtensions);
            Assert.Equal(100, returned.DatasetExtensions.First().Id_extension);
        }

        [Fact]
        public async Task UpdateDatasetEMFiltered_AgregaEventosFiltrados()
        {
            var svc = new Mock<IDatasetEMService>();
            var um = new Mock<IDatasetUMService>();
            var sonda = new Mock<ISondaEMService>();
            var username = "updateUser";
            var existing = new DatasetEM
            {
                Id = 10,
                Username = username,
                Name = "Dataset Actualizable",
                DatasetEvents = new List<DatasetEventEM> { new() { Id_event = 100 } }
            };

            svc.Setup(s => s.GetDatasetEMByIdForEditAsync(10, username)).ReturnsAsync(existing);
            um.Setup(u => u.ValidateDatasetNameAsync(It.IsAny<string>(), username, ModuleType.EventManager, existing.DatasetId)).Returns(Task.CompletedTask);

            var allEvents = new List<EventDto>
            {
                new() { Id = 100, Name = "Evento 1" },
                new() { Id = 101, Name = "Evento 2" }
            };
            sonda.Setup(s => s.GetEvents(null, null, null, null, username)).ReturnsAsync(allEvents);

            var filters = new List<FilterCondition>
            {
                new() { AttributeName = "Name", Type = FilterType.Contains, ValueType = FilterValueType.String, Condition = "Evento" }
            };

            svc.Setup(s => s.UpdateDatasetEMWithFiltersAsync(10, It.IsAny<CreateDatasetEMRequest>(), filters))
                .ReturnsAsync(new DatasetEM
                {
                    Id = 10,
                    Username = username,
                    Name = "Dataset Actualizable",
                    DatasetEvents = new List<DatasetEventEM>
                    {
                        new() { Id_event = 100 },
                        new() { Id_event = 101 }
                    }
                });
            um.Setup(u => u.UpdateDatasetAsyncEM(existing.DatasetId, It.IsAny<CreateDatasetRequest>(), It.IsAny<DatasetEM>())).ReturnsAsync(new Datasets { Id = 10 });

            var req = new CreateDatasetEMFilteredRequest
            {
                DatasetRequest = new CreateDatasetEMRequest
                {
                    Username = username,
                    Name = "Dataset Actualizable",
                    IsDataset = "S",
                    ContentType = "2"
                },
                Filters = filters
            };

            var controller = GetController(svc.Object, null, um.Object, sonda.Object, username: username);

            var result = await controller.UpdateDatasetEMFiltered(10, req);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<DatasetEM>(ok.Value);
            Assert.Equal(2, returned.DatasetEvents.Count);
            Assert.Contains(returned.DatasetEvents, e => e.Id_event == 101);
        }

        [Fact]
        public async Task CreateDatasetEMFiltered_ModelStateInvalido_RetornaBadRequest()
        {
            var svc = new Mock<IDatasetEMService>();
            var um = new Mock<IDatasetUMService>();
            var sonda = new Mock<ISondaEMService>();
            var controller = GetController(svc.Object, null, um.Object, sonda.Object, username: "u1");
            controller.ModelState.AddModelError("DatasetRequest.Name", "required");

            var req = new CreateDatasetEMFilteredRequest
            {
                DatasetRequest = new CreateDatasetEMRequest { IsDataset = "S", ContentType = "1" },
                Filters = new List<FilterCondition>()
            };

            var result = await controller.CreateDatasetEMFiltered(req);
            var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
            svc.Verify(s => s.CreateDatasetEMWithFiltersAsync(It.IsAny<CreateDatasetEMRequest>(), It.IsAny<int>(), It.IsAny<List<FilterCondition>>()), Times.Never);
        }

        [Fact]
        public async Task UpdateDatasetEMFiltered_NoExiste_RetornaNotFound()
        {
            var svc = new Mock<IDatasetEMService>();
            var um = new Mock<IDatasetUMService>();
            var sonda = new Mock<ISondaEMService>();
            var username = "noUser";
            svc.Setup(s => s.GetDatasetEMByIdForEditAsync(99, username)).ReturnsAsync((DatasetEM?)null);

            var req = new CreateDatasetEMFilteredRequest
            {
                DatasetRequest = new CreateDatasetEMRequest { Username = username, Name = "Inexistente", IsDataset = "S", ContentType = "2" },
                Filters = new List<FilterCondition>()
            };
            var controller = GetController(svc.Object, null, um.Object, sonda.Object, username: username);

            var result = await controller.UpdateDatasetEMFiltered(99, req);
            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal("No se encontró el DatasetEM con ID 99 para el usuario noUser.", notFound.Value);
        }

        [Fact]
        public async Task CreateDatasetEMFiltered_SondaLanzaExcepcion_Retorna500()
        {
            var svc = new Mock<IDatasetEMService>();
            var um = new Mock<IDatasetUMService>();
            var sonda = new Mock<ISondaEMService>();
            var username = "sondaFail";

            um.Setup(u => u.CreateDatasetAsync(It.IsAny<CreateDatasetRequest>())).ReturnsAsync(new Datasets { Id = 77 });
            sonda.Setup(s => s.GetExtensions(null, null, null, null, null, null, null, null, null, username))
                 .ThrowsAsync(new Exception("sonda down"));

            var controller = GetController(svc.Object, null, um.Object, sonda.Object, username: username);

            var req = new CreateDatasetEMFilteredRequest
            {
                DatasetRequest = new CreateDatasetEMRequest { Name = "X", IsDataset = "S", ContentType = "3" },
                Filters = new List<FilterCondition>()
            };

            var result = await controller.CreateDatasetEMFiltered(req);
            var obj = Assert.IsType<ObjectResult>(result.Result);
            Assert.InRange(obj.StatusCode ?? 0, 500, 599);
            svc.Verify(s => s.CreateDatasetEMWithFiltersAsync(It.IsAny<CreateDatasetEMRequest>(), It.IsAny<int>(), It.IsAny<List<FilterCondition>>()), Times.Never);
        }

        [Fact]
        public async Task CreateDatasetEMFiltered_SondaRetornaVacio_CreaDatasetVacio()
        {
            var svc = new Mock<IDatasetEMService>();
            var um = new Mock<IDatasetUMService>();
            var sonda = new Mock<ISondaEMService>();
            var username = "emptyU";

            um.Setup(u => u.CreateDatasetAsync(It.IsAny<CreateDatasetRequest>())).ReturnsAsync(new Datasets { Id = 66 });
            sonda.Setup(s => s.GetEvents(null, null, null, null, username)).ReturnsAsync(new List<EventDto>());

            svc.Setup(s => s.CreateDatasetEMWithFiltersAsync(It.IsAny<CreateDatasetEMRequest>(), 66, It.IsAny<List<FilterCondition>>()))
               .ReturnsAsync(new DatasetEM { Id = 66, Username = username, Name = "Empty", DatasetEvents = new List<DatasetEventEM>() });
            um.Setup(u => u.UpdateDatasetAsyncEM(66, It.IsAny<CreateDatasetRequest>(), It.IsAny<DatasetEM>())).ReturnsAsync(new Datasets { Id = 66 });

            var controller = GetController(svc.Object, null, um.Object, sonda.Object, username: username);

            var req = new CreateDatasetEMFilteredRequest
            {
                DatasetRequest = new CreateDatasetEMRequest { Name = "Empty", IsDataset = "S", ContentType = "2" },
                Filters = new List<FilterCondition>()
            };

            var result = await controller.CreateDatasetEMFiltered(req);
            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returned = Assert.IsType<DatasetEM>(created.Value);
            Assert.NotNull(returned.DatasetEvents);
            Assert.Empty(returned.DatasetEvents);
        }

        [Fact]
        public async Task UpdateDatasetEMFiltered_ServiceLanzaExcepcion_RetornaBadRequest()
        {
            var svc = new Mock<IDatasetEMService>();
            var um = new Mock<IDatasetUMService>();
            var sonda = new Mock<ISondaEMService>();
            var username = "svcThrow";

            var existing = new DatasetEM { Id = 600, Username = username, Name = "Exist" };
            svc.Setup(s => s.GetDatasetEMByIdForEditAsync(600, username)).ReturnsAsync(existing);
            um.Setup(u => u.ValidateDatasetNameAsync(It.IsAny<string>(), username, ModuleType.EventManager, existing.DatasetId)).Returns(Task.CompletedTask);

            sonda.Setup(s => s.GetEvents(null, null, null, null, username)).ReturnsAsync(new List<EventDto>());

            svc.Setup(s => s.UpdateDatasetEMWithFiltersAsync(600, It.IsAny<CreateDatasetEMRequest>(), It.IsAny<List<FilterCondition>>()))
               .ThrowsAsync(new InvalidOperationException("Name conflict"));

            var controller = GetController(svc.Object, null, um.Object, sonda.Object, username: username);

            var req = new CreateDatasetEMFilteredRequest
            {
                DatasetRequest = new CreateDatasetEMRequest { Name = "X", IsDataset = "S", ContentType = "2" },
                Filters = new List<FilterCondition>()
            };

            var result = await controller.UpdateDatasetEMFiltered(600, req);
            var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Name conflict", bad.Value);
        }

        [Fact]
        public async Task UpdateDatasetEMFiltered_SinUsuarioEnContexto_RetornaBadRequest()
        {
            var svc = new Mock<IDatasetEMService>();
            var um = new Mock<IDatasetUMService>();
            var sonda = new Mock<ISondaEMService>();

            var controller = GetController(svc.Object, null, um.Object, sonda.Object);
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

            var req = new CreateDatasetEMFilteredRequest
            {
                DatasetRequest = new CreateDatasetEMRequest { Name = "NoUser", IsDataset = "S", ContentType = "1" },
                Filters = new List<FilterCondition>()
            };

            var result = await controller.UpdateDatasetEMFiltered(123, req);
            var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Usuario no encontrado.", bad.Value);
        }

        /* Tests sobre GetAllDatasets */

        [Fact]
        public async Task GetAllDatasets_Success_ReturnsOkWithDatasets()
        {
            // Arrange
            var svc = new Mock<IDatasetEMService>();
            var um = new Mock<IDatasetUMService>();
            var sonda = new Mock<ISondaEMService>();

            var username = "felizUser";
            var expected = new List<DatasetEM>
    {
        new DatasetEM
        {
            Id = 1,
            Username = username,
            Name = "Dataset Feliz 1",
            Description = "Descripción corta",
            DatasetEvents = new List<DatasetEventEM> { new() { Id_event = 10 } },
            DatasetAlerts = new List<DatasetAlert> { new() { Id_alert = 20 } },
            DatasetExtensions = new List<DatasetExtension> { new() { Id_extension = 30 } }
        },
        new DatasetEM
        {
            Id = 2,
            Username = username,
            Name = "Dataset Feliz 2",
            Description = "Otra descripción",
            DatasetEvents = new List<DatasetEventEM>(),
            DatasetAlerts = new List<DatasetAlert>(),
            DatasetExtensions = new List<DatasetExtension>()
        }
    };

            svc.Setup(s => s.GetAllDatasetsEMAsync(username)).ReturnsAsync(expected);

            var controller = GetController(svc.Object, null, um.Object, sonda.Object, username: username);

            // Act
            var result = await controller.GetAllDatasets();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<List<DatasetEM>>(ok.Value);
            Assert.Equal(2, returned.Count);

            // Verificaciones puntuales del contenido
            var first = returned.First();
            Assert.Equal(1, first.Id);
            Assert.Equal("Dataset Feliz 1", first.Name);
            Assert.Equal("Descripción corta", first.Description);
            Assert.Single(first.DatasetEvents);
            Assert.Single(first.DatasetAlerts);
            Assert.Single(first.DatasetExtensions);

            svc.Verify(s => s.GetAllDatasetsEMAsync(username), Times.Once);

        }

        [Fact]
        public async Task GetAllDatasets_UsernameEmptyString_PassesEmptyToService()
        {
            // Arrange
            var svc = new Mock<IDatasetEMService>();
            var um = new Mock<IDatasetUMService>();
            var sonda = new Mock<ISondaEMService>();

            // create controller but override ControllerContext to supply empty username
            var controller = GetController(svc.Object, null, um.Object, sonda.Object, username: "willBeOverwritten");
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "") }, "mock"));
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

            svc.Setup(s => s.GetAllDatasetsEMAsync("")).ReturnsAsync(new List<DatasetEM>());

            // Act
            var result = await controller.GetAllDatasets();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<List<DatasetEM>>(ok.Value);
            Assert.Empty(returned);
            svc.Verify(s => s.GetAllDatasetsEMAsync(""), Times.Once);
        }

        [Fact]
        public async Task GetAllDatasets_UsernameWhitespace_PassesWhitespaceToService()
        {
            // Arrange
            var svc = new Mock<IDatasetEMService>();
            var um = new Mock<IDatasetUMService>();
            var sonda = new Mock<ISondaEMService>();

            var controller = GetController(svc.Object, null, um.Object, sonda.Object, username: "orig");
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "   ") }, "mock"));
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

            svc.Setup(s => s.GetAllDatasetsEMAsync("   ")).ReturnsAsync(new List<DatasetEM>());

            // Act
            var result = await controller.GetAllDatasets();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            svc.Verify(s => s.GetAllDatasetsEMAsync("   "), Times.Once);
        }

        [Fact]
        public async Task GetAllDatasets_EntriesWithNullOptionalFields_AreReturnedIntact()
        {
            // Arrange
            var svc = new Mock<IDatasetEMService>();
            var um = new Mock<IDatasetUMService>();
            var sonda = new Mock<ISondaEMService>();

            var username = "nullFields";
            var ds = new DatasetEM
            {
                Id = 10,
                Username = username,
                Name = null!,
                Description = null,
                DatasetEvents = null!,
                DatasetAlerts = null!,
                DatasetExtensions = null!
            };
            svc.Setup(s => s.GetAllDatasetsEMAsync(username)).ReturnsAsync(new List<DatasetEM> { ds });

            var controller = GetController(svc.Object, null, um.Object, sonda.Object, username: username);

            // Act
            var result = await controller.GetAllDatasets();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<List<DatasetEM>>(ok.Value);
            Assert.Single(returned);
            Assert.Null(returned[0].Name);
            Assert.Null(returned[0].DatasetEvents);
            svc.Verify(s => s.GetAllDatasetsEMAsync(username), Times.Once);
        }

        [Fact]
        public async Task GetAllDatasets_DuplicatedIds_AreReturnedAsProvided()
        {
            // Arrange
            var svc = new Mock<IDatasetEMService>();
            var um = new Mock<IDatasetUMService>();
            var sonda = new Mock<ISondaEMService>();

            var username = "dupIds";
            var a = new DatasetEM { Id = 5, Username = username, Name = "A" };
            var b = new DatasetEM { Id = 5, Username = username, Name = "B" };
            svc.Setup(s => s.GetAllDatasetsEMAsync(username)).ReturnsAsync(new List<DatasetEM> { a, b });

            var controller = GetController(svc.Object, null, um.Object, sonda.Object, username: username);

            // Act
            var result = await controller.GetAllDatasets();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<List<DatasetEM>>(ok.Value);
            Assert.Equal(2, returned.Count);
            Assert.Equal("A", returned[0].Name);
            Assert.Equal("B", returned[1].Name);
            svc.Verify(s => s.GetAllDatasetsEMAsync(username), Times.Once);
        }

        [Fact]
        public async Task GetAllDatasets_NegativeAndZeroIds_AreHandled()
        {
            // Arrange
            var svc = new Mock<IDatasetEMService>();
            var um = new Mock<IDatasetUMService>();
            var sonda = new Mock<ISondaEMService>();

            var username = "negIds";
            var items = new List<DatasetEM>
            {
                new DatasetEM { Id = 0, Username = username, Name = "Zero" },
                new DatasetEM { Id = -10, Username = username, Name = "Negative" }
            };
            svc.Setup(s => s.GetAllDatasetsEMAsync(username)).ReturnsAsync(items);

            var controller = GetController(svc.Object, null, um.Object, sonda.Object, username: username);

            // Act
            var result = await controller.GetAllDatasets();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<List<DatasetEM>>(ok.Value);
            Assert.Equal(2, returned.Count);
            Assert.Contains(returned, d => d.Id == 0);
            Assert.Contains(returned, d => d.Id == -10);
            svc.Verify(s => s.GetAllDatasetsEMAsync(username), Times.Once);
        }

        [Fact]
        public async Task GetAllDatasets_VeryLongStrings_AreReturnedWithoutTruncation()
        {
            // Arrange
            var svc = new Mock<IDatasetEMService>();
            var um = new Mock<IDatasetUMService>();
            var sonda = new Mock<ISondaEMService>();

            var username = "longStrings";
            var longName = new string('x', 10000);
            var longDesc = new string('y', 20000);
            var ds = new DatasetEM { Id = 77, Username = username, Name = longName, Description = longDesc };
            svc.Setup(s => s.GetAllDatasetsEMAsync(username)).ReturnsAsync(new List<DatasetEM> { ds });

            var controller = GetController(svc.Object, null, um.Object, sonda.Object, username: username);

            // Act
            var result = await controller.GetAllDatasets();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<List<DatasetEM>>(ok.Value);
            Assert.Single(returned);
            Assert.Equal(10000, returned[0].Name.Length);
            Assert.Equal(20000, returned[0].Description!.Length);
            svc.Verify(s => s.GetAllDatasetsEMAsync(username), Times.Once);
        }

        [Fact]
        public async Task GetAllDatasets_DoesNotCallOtherServices_UnlessNeeded()
        {
            // Arrange
            var svc = new Mock<IDatasetEMService>();
            var um = new Mock<IDatasetUMService>();
            var sonda = new Mock<ISondaEMService>();

            var username = "isolation";
            svc.Setup(s => s.GetAllDatasetsEMAsync(username)).ReturnsAsync(new List<DatasetEM>());

            var controller = GetController(svc.Object, null, um.Object, sonda.Object, username: username);

            // Act
            var result = await controller.GetAllDatasets();

            // Assert
            Assert.IsType<OkObjectResult>(result.Result);
            // Verify that UM and Sonda services are not invoked by this endpoint
            um.VerifyNoOtherCalls();
            sonda.VerifyNoOtherCalls();
            svc.Verify(s => s.GetAllDatasetsEMAsync(username), Times.Once);
        }

        /* Tests sobre GetDatasetById */

        [Fact]
        public async Task GetDatasetById_Feliz_ReturnsOkWithDataset()
        {
            // Arrange
            var svc = new Mock<IDatasetEMService>();
            var um = new Mock<IDatasetUMService>();
            var sonda = new Mock<ISondaEMService>();

            var username = "felizUser";
            var dataset = new DatasetEM
            {
                Id = 11,
                Username = username,
                Name = "Dataset Feliz",
                Description = "Descripción",
                DatasetEvents = new List<DatasetEventEM> { new() { Id_event = 100 } },
                DatasetAlerts = new List<DatasetAlert> { new() { Id_alert = 200 } },
                DatasetExtensions = new List<DatasetExtension> { new() { Id_extension = 300 } }
            };

            svc.Setup(s => s.GetDatasetEMByIdAsync(11, username)).ReturnsAsync(dataset);

            var controller = GetController(svc.Object, null, um.Object, sonda.Object, username: username);

            // Act
            var result = await controller.GetDatasetById(11);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<DatasetEM>(ok.Value);
            Assert.Equal(11, returned.Id);
            Assert.Equal("Dataset Feliz", returned.Name);
            Assert.Single(returned.DatasetEvents);
            svc.Verify(s => s.GetDatasetEMByIdAsync(11, username), Times.Once);
        }

        [Fact]
        public async Task GetDatasetById_NotFound_Returns404()
        {
            // Arrange
            var svc = new Mock<IDatasetEMService>();
            var username = "userX";
            svc.Setup(s => s.GetDatasetEMByIdAsync(999, username)).ReturnsAsync((DatasetEM?)null);

            var controller = GetController(svc.Object, null, Mock.Of<IDatasetUMService>(), Mock.Of<ISondaEMService>(), username: username);

            // Act
            var result = await controller.GetDatasetById(999);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal($"No se encontró el dataset con ID 999 para el usuario {username}.", notFound.Value);
            svc.Verify(s => s.GetDatasetEMByIdAsync(999, username), Times.Once);
        }

        [Fact]
        public async Task GetDatasetById_ServiceThrows_Returns500()
        {
            // Arrange
            var svc = new Mock<IDatasetEMService>();
            var username = "errUser";
            svc.Setup(s => s.GetDatasetEMByIdAsync(5, username)).ThrowsAsync(new Exception("boom"));

            var controller = GetController(svc.Object, null, Mock.Of<IDatasetUMService>(), Mock.Of<ISondaEMService>(), username: username);

            // Act
            var result = await controller.GetDatasetById(5);

            // Assert
            var obj = Assert.IsType<ObjectResult>(result.Result);
            Assert.InRange(obj.StatusCode ?? 0, 500, 599);
            Assert.Contains("boom", obj.Value?.ToString() ?? string.Empty);
            svc.Verify(s => s.GetDatasetEMByIdAsync(5, username), Times.Once);
        }

        [Fact]
        public async Task GetDatasetById_NoUserInContext_PassesNullToService_AndHandlesNotFound()
        {
            // Arrange
            var svc = new Mock<IDatasetEMService>();
            svc.Setup(s => s.GetDatasetEMByIdAsync(7, null)).ReturnsAsync((DatasetEM?)null);

            var controller = GetController(svc.Object, null, Mock.Of<IDatasetUMService>(), Mock.Of<ISondaEMService>(), username: "willRemove");
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }; // remove user

            // Act
            var result = await controller.GetDatasetById(7);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            // username becomes empty string in message
            Assert.Equal($"No se encontró el dataset con ID 7 para el usuario .", notFound.Value);
            svc.Verify(s => s.GetDatasetEMByIdAsync(7, null), Times.Once);
        }

        [Fact]
        public async Task GetDatasetById_NegativeAndZeroIds_AreHandledAsNotFound()
        {
            // Arrange
            var svc = new Mock<IDatasetEMService>();
            var username = "negUser";
            svc.Setup(s => s.GetDatasetEMByIdAsync(0, username)).ReturnsAsync((DatasetEM?)null);
            svc.Setup(s => s.GetDatasetEMByIdAsync(-1, username)).ReturnsAsync((DatasetEM?)null);

            var controller = GetController(svc.Object, null, Mock.Of<IDatasetUMService>(), Mock.Of<ISondaEMService>(), username: username);

            // Act & Assert for zero
            var result0 = await controller.GetDatasetById(0);
            var notFound0 = Assert.IsType<NotFoundObjectResult>(result0.Result);
            Assert.Equal($"No se encontró el dataset con ID 0 para el usuario {username}.", notFound0.Value);

            // Act & Assert for negative
            var resultNeg = await controller.GetDatasetById(-1);
            var notFoundNeg = Assert.IsType<NotFoundObjectResult>(resultNeg.Result);
            Assert.Equal($"No se encontró el dataset con ID -1 para el usuario {username}.", notFoundNeg.Value);

            svc.Verify(s => s.GetDatasetEMByIdAsync(0, username), Times.Once);
            svc.Verify(s => s.GetDatasetEMByIdAsync(-1, username), Times.Once);
        }

        [Fact]
        public async Task GetDatasetById_ServiceReturnsDatasetWithNullFields_ControllerReturnsIt()
        {
            // Arrange
            var svc = new Mock<IDatasetEMService>();
            var username = "nullFieldsUser";
            var ds = new DatasetEM
            {
                Id = 55,
                Username = username,
                Name = null!, // simulate malformed service response
                Description = null,
                DatasetEvents = null!, // null collections
                DatasetAlerts = null!,
                DatasetExtensions = null!
            };
            svc.Setup(s => s.GetDatasetEMByIdAsync(55, username)).ReturnsAsync(ds);

            var controller = GetController(svc.Object, null, Mock.Of<IDatasetUMService>(), Mock.Of<ISondaEMService>(), username: username);

            // Act
            var result = await controller.GetDatasetById(55);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<DatasetEM>(ok.Value);
            Assert.Equal(55, returned.Id);
            Assert.Null(returned.Name);
            Assert.Null(returned.DatasetEvents);
            svc.Verify(s => s.GetDatasetEMByIdAsync(55, username), Times.Once);
        }

        [Fact]
        public async Task GetDatasetById_LargeCollections_ReturnsPayloadIntact()
        {
            // Arrange
            var svc = new Mock<IDatasetEMService>();
            var username = "bigUser";
            var big = new DatasetEM
            {
                Id = 700,
                Username = username,
                Name = "Big",
                DatasetEvents = Enumerable.Range(0, 1000).Select(i => new DatasetEventEM { Id_event = i }).ToList(),
                DatasetAlerts = Enumerable.Range(0, 500).Select(i => new DatasetAlert { Id_alert = i }).ToList(),
                DatasetExtensions = Enumerable.Range(0, 200).Select(i => new DatasetExtension { Id_extension = i }).ToList()
            };
            svc.Setup(s => s.GetDatasetEMByIdAsync(700, username)).ReturnsAsync(big);

            var controller = GetController(svc.Object, null, Mock.Of<IDatasetUMService>(), Mock.Of<ISondaEMService>(), username: username);

            // Act
            var result = await controller.GetDatasetById(700);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<DatasetEM>(ok.Value);
            Assert.Equal(1000, returned.DatasetEvents.Count);
            Assert.Equal(500, returned.DatasetAlerts.Count);
            Assert.Equal(200, returned.DatasetExtensions.Count);
            svc.Verify(s => s.GetDatasetEMByIdAsync(700, username), Times.Once);
        }

        [Fact]
        public async Task GetDatasetById_DatasetOwnedByDifferentUser_ServiceMayReturnIt_ControllerJustReturnsIt()
        {
            // Arrange
            var svc = new Mock<IDatasetEMService>();
            var caller = "callerUser";
            var owner = "ownerUser";
            var ds = new DatasetEM { Id = 88, Username = owner, Name = "OwnedByOther" };

            svc.Setup(s => s.GetDatasetEMByIdAsync(88, caller)).ReturnsAsync(ds);

            var controller = GetController(svc.Object, null, Mock.Of<IDatasetUMService>(), Mock.Of<ISondaEMService>(), username: caller);

            // Act
            var result = await controller.GetDatasetById(88);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<DatasetEM>(ok.Value);
            Assert.Equal(owner, returned.Username);
            Assert.Equal("OwnedByOther", returned.Name);
            svc.Verify(s => s.GetDatasetEMByIdAsync(88, caller), Times.Once);
        }

        [Fact]
        public async Task GetDatasetById_ServiceReturnsNullExplicitly_TreatedAsNotFound()
        {
            // Arrange
            var svc = new Mock<IDatasetEMService>();
            var username = "nullReturn";
            svc.Setup(s => s.GetDatasetEMByIdAsync(1234, username)).ReturnsAsync((DatasetEM?)null);

            var controller = GetController(svc.Object, null, Mock.Of<IDatasetUMService>(), Mock.Of<ISondaEMService>(), username: username);

            // Act
            var result = await controller.GetDatasetById(1234);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal($"No se encontró el dataset con ID 1234 para el usuario {username}.", notFound.Value);
            svc.Verify(s => s.GetDatasetEMByIdAsync(1234, username), Times.Once);
        }

        /* Tests sobre DeleteDataset */

        [Fact]
        public async Task DeleteDataset_Success_CallsServicesAndReturnsNoContent()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            using var context = new ApplicationDbContext(options);

            var username = "deleter_ok";
            var ds = new DatasetEM
            {
                Id = 101,
                Username = username,
                Name = "ToDelete",
                DatasetId = 1001
            };
            context.DatasetsEM.Add(ds);
            await context.SaveChangesAsync();

            var svc = new Mock<IDatasetEMService>();
            var um = new Mock<IDatasetUMService>();
            var sonda = new Mock<ISondaEMService>();

            svc.Setup(s => s.DeleteDatasetEMAsync(101, username)).Returns(Task.CompletedTask).Verifiable();
            um.Setup(u => u.DeleteDatasetAsync(1001, username)).Returns(Task.CompletedTask).Verifiable();

            var controller = GetController(svc.Object, null, um.Object, sonda.Object, null, context, username: username);

            // Act
            var result = await controller.DeleteDataset(101);

            // Assert
            Assert.IsType<NoContentResult>(result);
            svc.Verify();
            um.Verify();
        }

        [Fact]
        public async Task DeleteDataset_NotFound_ReturnsNotFound_AndNoServiceCalls()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            using var context = new ApplicationDbContext(options);

            var username = "no_exist";
            var svc = new Mock<IDatasetEMService>();
            var um = new Mock<IDatasetUMService>();
            var sonda = new Mock<ISondaEMService>();

            var controller = GetController(svc.Object, null, um.Object, sonda.Object, null, context, username: username);

            // Act
            var result = await controller.DeleteDataset(9999);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal($"No se encontró el dataset con ID 9999 para el usuario {username}.", notFound.Value);
            svc.Verify(s => s.DeleteDatasetEMAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
            um.Verify(u => u.DeleteDatasetAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task DeleteDataset_ExistsButOtherUser_ReturnsNotFound_NoServiceCalls()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            using var context = new ApplicationDbContext(options);

            var owner = "owner";
            var caller = "caller";
            var ds = new DatasetEM { Id = 202, Username = owner, Name = "OwnerDataset", DatasetId = 2002 };
            context.DatasetsEM.Add(ds);
            await context.SaveChangesAsync();

            var svc = new Mock<IDatasetEMService>();
            var um = new Mock<IDatasetUMService>();
            var sonda = new Mock<ISondaEMService>();

            var controller = GetController(svc.Object, null, um.Object, sonda.Object, null, context, username: caller);

            // Act
            var result = await controller.DeleteDataset(202);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal($"No se encontró el dataset con ID 202 para el usuario {caller}.", notFound.Value);
            svc.Verify(s => s.DeleteDatasetEMAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
            um.Verify(u => u.DeleteDatasetAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task DeleteDataset_EmServiceThrows_ControllerReturnsBadRequest_AndUmNotCalled()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            using var context = new ApplicationDbContext(options);

            var username = "svc_throw";
            var ds = new DatasetEM { Id = 404, Username = username, Name = "WillThrow", DatasetId = 4004 };
            context.DatasetsEM.Add(ds);
            await context.SaveChangesAsync();

            var svc = new Mock<IDatasetEMService>();
            var um = new Mock<IDatasetUMService>();
            var sonda = new Mock<ISondaEMService>();

            svc.Setup(s => s.DeleteDatasetEMAsync(404, username)).ThrowsAsync(new Exception("EM delete failed"));
            um.Setup(u => u.DeleteDatasetAsync(It.IsAny<int>(), It.IsAny<string>())).Verifiable();

            var controller = GetController(svc.Object, null, um.Object, sonda.Object, null, context, username: username);

            // Act
            var result = await controller.DeleteDataset(404);

            // Assert
            var obj = Assert.IsType<ObjectResult>(result);
            Assert.NotNull(obj.Value);
            Assert.InRange(obj.StatusCode ?? 0, 500, 599);
            svc.Verify(s => s.DeleteDatasetEMAsync(404, username), Times.Once);
            um.Verify(u => u.DeleteDatasetAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task DeleteDataset_UmServiceThrows_ControllerReturnsBadRequest()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            using var context = new ApplicationDbContext(options);

            var username = "um_throw";
            var ds = new DatasetEM { Id = 505, Username = username, Name = "UMFail", DatasetId = 5005 };
            context.DatasetsEM.Add(ds);
            await context.SaveChangesAsync();

            var svc = new Mock<IDatasetEMService>();
            var um = new Mock<IDatasetUMService>();
            var sonda = new Mock<ISondaEMService>();

            svc.Setup(s => s.DeleteDatasetEMAsync(505, username)).Returns(Task.CompletedTask);
            um.Setup(u => u.DeleteDatasetAsync(5005, username)).ThrowsAsync(new Exception("UM delete failed"));

            var controller = GetController(svc.Object, null, um.Object, sonda.Object, null, context, username: username);

            // Act
            var result = await controller.DeleteDataset(505);

            // Assert
            var obj = Assert.IsType<ObjectResult>(result);
            Assert.NotNull(obj.Value);
            Assert.InRange(obj.StatusCode ?? 0, 500, 599);
            svc.Verify(s => s.DeleteDatasetEMAsync(505, username), Times.Once);
            um.Verify(u => u.DeleteDatasetAsync(5005, username), Times.Once);
        }
    }
}
