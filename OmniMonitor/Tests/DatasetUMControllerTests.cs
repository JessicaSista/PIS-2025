using System.Security.Claims;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Moq;

using OmniMonitor.Server.Context;
using OmniMonitor.Server.Controllers;
using OmniMonitor.Server.Services;
using OmniMonitor.Shared.Dtos;
using OmniMonitor.Shared.Dtos.EM;

namespace QA.Tests
{
    public class DatasetUMControllerTests
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

        private static DatasetUMController GetController(
            IDatasetUMService? datasetUMService = null,
            ISondaAuthService? sondaAuthService = null,
            ApplicationDbContext? context = null,
            ISondaUMService? sondaUMService = null,
            IKpiService? kpiService = null,
            string username = "testuser")
        {
            var ctrl = new DatasetUMController(
                datasetUMService ?? Mock.Of<IDatasetUMService>(),
                sondaAuthService ?? Mock.Of<ISondaAuthService>(),
                context ?? new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options),
                sondaUMService ?? Mock.Of<ISondaUMService>(),
                kpiService ?? Mock.Of<IKpiService>()

            );
            ctrl.ControllerContext = GetControllerContext(username);
            return ctrl;
        }

        private static DatasetUMController GetControllerWithDb(
            ApplicationDbContext context,
            string username = "testuser")
        {
            var ctrl = new DatasetUMController(
                Mock.Of<IDatasetUMService>(),
                Mock.Of<ISondaAuthService>(),
                context,
                Mock.Of<ISondaUMService>(),
                Mock.Of<IKpiService>()
            );
            ctrl.ControllerContext = GetControllerContext(username);
            return ctrl;
        }

        /* Tests sobre GetAllDatasetsDtoPaginated, CreateDatasetFiltered y UpdateDatasetWithFilters */

        [Fact]
        public async Task GetAllDatasetsDtoPaginated_ReturnsPagedResults()
        {
            // Arrange
            var username = "user1";
            var context = new ApplicationDbContext(
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase("PagedDb1")
                    .Options);

            // Agregar datasets UM
            for (int i = 1; i <= 15; i++)
            {
                var ds = new Datasets
                {
                    NameDataset = $"Dataset {i}",
                    TipoDataset = ModuleType.UrbanMonitor,
                    Username = username,
                    DatasetUM = new List<DatasetUM>
                    {
                        new DatasetUM { Name = $"Dataset {i}", Username = username }
                    }
                };
                context.Datasets.Add(ds);
            }
            context.SaveChanges();

            var controller = GetControllerWithDb(context, username);

            // Act
            var result = await controller.GetAllDatasetsDtoPaginated(page: 2, pageSize: 5, search: null);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var paged = Assert.IsType<PaginatedDatasetDto>(ok.Value);
            Assert.Equal(5, paged.Items.Count);
            Assert.Equal(2, paged.Page);
            Assert.Equal(5, paged.PageSize);
            Assert.Equal(15, paged.TotalCount);
            Assert.Equal(3, paged.TotalPages);
            Assert.True(paged.HasPreviousPage);
            Assert.True(paged.HasNextPage);
        }

        [Fact]
        public async Task GetAllDatasetsDtoPaginated_SearchFiltersResults()
        {
            // Arrange
            var username = "user2";
            var context = new ApplicationDbContext(
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase("PagedDb2")
                    .Options);

            context.Datasets.Add(new Datasets
            {
                NameDataset = "Alpha",
                TipoDataset = ModuleType.UrbanMonitor,
                Username = username,
                DatasetUM = new List<DatasetUM> { new DatasetUM { Name = "Alpha", Username = username } }
            });
            context.Datasets.Add(new Datasets
            {
                NameDataset = "Beta",
                TipoDataset = ModuleType.UrbanMonitor,
                Username = username,
                DatasetUM = new List<DatasetUM> { new DatasetUM { Name = "Beta", Username = username } }
            });
            context.Datasets.Add(new Datasets
            {
                NameDataset = "Gamma",
                TipoDataset = ModuleType.UrbanMonitor,
                Username = username,
                DatasetUM = new List<DatasetUM> { new DatasetUM { Name = "Gamma", Username = username } }
            });
            context.SaveChanges();

            var controller = GetControllerWithDb(context, username);

            // Act
            var result = await controller.GetAllDatasetsDtoPaginated(page: 1, pageSize: 10, search: "Beta");

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var paged = Assert.IsType<PaginatedDatasetDto>(ok.Value);
            Assert.Single(paged.Items);
            Assert.Equal("Beta", paged.Items.First().Nombre);
        }

        [Fact]
        public async Task GetAllDatasetsDtoPaginated_PageOutOfRange_ReturnsLastPage()
        {
            // Arrange
            var username = "user3";
            var context = new ApplicationDbContext(
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase("PagedDb3")
                    .Options);

            for (int i = 1; i <= 7; i++)
            {
                context.Datasets.Add(new Datasets
                {
                    NameDataset = $"DS{i}",
                    TipoDataset = ModuleType.UrbanMonitor,
                    Username = username,
                    DatasetUM = new List<DatasetUM> { new DatasetUM { Name = $"DS{i}", Username = username } }
                });
            }
            context.SaveChanges();

            var controller = GetControllerWithDb(context, username);

            // Act: pedir página 5 cuando solo hay 2 páginas
            var result = await controller.GetAllDatasetsDtoPaginated(page: 5, pageSize: 4, search: null);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var paged = Assert.IsType<PaginatedDatasetDto>(ok.Value);
            Assert.Equal(2, paged.Page); // última página
            Assert.Equal(3, paged.Items.Count); // 7 - 4 = 3
        }

        [Fact]
        public async Task GetAllDatasetsDtoPaginated_InvalidPageSize_ReturnsBadRequest()
        {
            var username = "user4";
            var context = new ApplicationDbContext(
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase("PagedDb4")
                    .Options);

            var controller = GetControllerWithDb(context, username);

            var result = await controller.GetAllDatasetsDtoPaginated(page: 1, pageSize: 0, search: null);
            var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("tamaño de página", bad.Value.ToString());
        }

        [Fact]
        public async Task GetAllDatasetsDtoPaginated_InvalidPage_ReturnsBadRequest()
        {
            var username = "user5";
            var context = new ApplicationDbContext(
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase("PagedDb5")
                    .Options);

            var controller = GetControllerWithDb(context, username);

            var result = await controller.GetAllDatasetsDtoPaginated(page: 0, pageSize: 10, search: null);
            var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("número de página", bad.Value.ToString());
        }

        [Fact]
        public async Task CreateDatasetFiltered_FiltersNull_ReturnsBadRequest()
        {
            var svc = new Mock<IDatasetUMService>();
            var sonda = new Mock<ISondaUMService>();
            var controller = GetController(svc.Object, null, null, sonda.Object, username: "um_null");
            var req = new CreateDatasetUMFilteredRequest
            {
                DatasetRequest = new CreateDatasetUMRequest { Username = "um_null", Name = "X", ContentType = "1" },
                Filters = new List<FilterCondition>()
            };

            var result = await controller.CreateDatasetFiltered(req);
            var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(bad.Value);
            svc.Verify(s => s.CreateDatasetUMWithFiltersAsync(It.IsAny<CreateDatasetUMRequest>(), It.IsAny<int>(), It.IsAny<List<FilterCondition>>()), Times.Never);
        }

        [Fact]
        public async Task CreateDatasetFiltered_SondaThrows_Returns500_AndDoesNotCallService()
        {
            var svc = new Mock<IDatasetUMService>();
            var sonda = new Mock<ISondaUMService>();
            var username = "sonda_fail_um";

            svc.Setup(u => u.CreateDatasetAsync(It.IsAny<CreateDatasetRequest>())).ReturnsAsync(new Datasets { Id = 77 });
            sonda.Setup(s => s.GetAllNews(username, 1, null, null, 1000)).ThrowsAsync(new Exception("sonda down"));

            var controller = GetController(svc.Object, null, null, sonda.Object, username: username);

            var req = new CreateDatasetUMFilteredRequest
            {
                DatasetRequest = new CreateDatasetUMRequest { Username = username, Name = "X", ContentType = "2" },
                Filters = new List<FilterCondition> { new() { AttributeName = "Title", Type = FilterType.Contains, ValueType = FilterValueType.String, Condition = "X" } }
            };

            var result = await controller.CreateDatasetFiltered(req);
            var obj = Assert.IsType<ObjectResult>(result.Result);
            Assert.InRange(obj.StatusCode ?? 0, 500, 599);
            svc.Verify(s => s.CreateDatasetUMWithFiltersAsync(It.IsAny<CreateDatasetUMRequest>(), It.IsAny<int>(), It.IsAny<List<FilterCondition>>()), Times.Never);
        }


        [Fact]
        public async Task CreateDatasetFiltered_FiltraEventosPorEstado()
        {
            var svc = new Mock<IDatasetUMService>();
            var sonda = new Mock<ISondaUMService>();
            var username = "eventUser";

            var allEvents = new List<Event>
            {
                new() { Id = 1, Name = "Ev1", Description = "Activo" },
                new() { Id = 2, Name = "Ev2", Description = "Cerrado" }
            };
            sonda.Setup(s => s.GetAllEvents(username)).ReturnsAsync(allEvents);

            var filters = new List<FilterCondition>
            {
                new() { AttributeName = "Description", Type = FilterType.Equals, ValueType = FilterValueType.String, Condition = "Activo" }
            };
            var req = new CreateDatasetUMFilteredRequest
            {
                DatasetRequest = new CreateDatasetUMRequest
                {
                    Username = username,
                    Name = "Solo Activos",
                    ContentType = "1"
                },
                Filters = filters
            };

            svc.Setup(s => s.CreateDatasetUMWithFiltersAsync(It.IsAny<CreateDatasetUMRequest>(), It.IsAny<int>(), filters))
                .ReturnsAsync(new DatasetUM
                {
                    Id = 1,
                    Username = username,
                    Name = "Solo Activos",
                    DatasetEvents = new List<DatasetEvent> { new() { Id_event = 1 } }
                });
            svc.Setup(s => s.CreateDatasetAsync(It.IsAny<CreateDatasetRequest>())).ReturnsAsync(new Datasets { Id = 1 });
            svc.Setup(s => s.UpdateDatasetAsyncUM(1, It.IsAny<CreateDatasetRequest>(), It.IsAny<DatasetUM>())).ReturnsAsync(new Datasets { Id = 1 });

            var controller = GetController(svc.Object, null, null, sonda.Object, null, username);

            var result = await controller.CreateDatasetFiltered(req);
            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returned = Assert.IsType<DatasetUM>(created.Value);
            Assert.Single(returned.DatasetEvents);
            Assert.Equal(1, returned.DatasetEvents.First().Id_event);
        }

        [Fact]
        public async Task CreateDatasetFiltered_FiltraNoticiasPorTitulo()
        {
            var svc = new Mock<IDatasetUMService>();
            var sonda = new Mock<ISondaUMService>();
            var username = "newsUser";

            var allNews = new List<News>
            {
                new() { Id = 1, Title = "Alerta importante" },
                new() { Id = 2, Title = "Noticia menor" }
            };
            sonda.Setup(s => s.GetAllNews(username, 1, null, null, 1000)).ReturnsAsync(allNews);

            var filters = new List<FilterCondition>
            {
                new() { AttributeName = "Title", Type = FilterType.Contains, ValueType = FilterValueType.String, Condition = "Alerta" }
            };
            var req = new CreateDatasetUMFilteredRequest
            {
                DatasetRequest = new CreateDatasetUMRequest
                {
                    Username = username,
                    Name = "Solo Alertas",
                    ContentType = "2"
                },
                Filters = filters
            };

            svc.Setup(s => s.CreateDatasetUMWithFiltersAsync(It.IsAny<CreateDatasetUMRequest>(), It.IsAny<int>(), filters))
                .ReturnsAsync(new DatasetUM
                {
                    Id = 2,
                    Username = username,
                    Name = "Solo Alertas",
                    DatasetNews = new List<DatasetNews> { new() { Id_news = 1 } }
                });
            svc.Setup(s => s.CreateDatasetAsync(It.IsAny<CreateDatasetRequest>())).ReturnsAsync(new Datasets { Id = 2 });
            svc.Setup(s => s.UpdateDatasetAsyncUM(2, It.IsAny<CreateDatasetRequest>(), It.IsAny<DatasetUM>())).ReturnsAsync(new Datasets { Id = 2 });

            var controller = GetController(svc.Object, null, null, sonda.Object, null, username);

            var result = await controller.CreateDatasetFiltered(req);
            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returned = Assert.IsType<DatasetUM>(created.Value);
            Assert.Single(returned.DatasetNews);
            Assert.Equal(1, returned.DatasetNews.First().Id_news);
        }

        [Fact]
        public async Task UpdateDatasetWithFilters_AgregaEventosFiltrados()
        {
            var svc = new Mock<IDatasetUMService>();
            var sonda = new Mock<ISondaUMService>();
            var username = "updateUser";
            var existing = new DatasetUM
            {
                Id = 10,
                Username = username,
                Name = "Dataset Actualizable",
                DatasetEvents = new List<DatasetEvent> { new() { Id_event = 100 } }
            };

            svc.Setup(s => s.GetDatasetUMByIdForEditAsync(10, username)).ReturnsAsync(existing);
            svc.Setup(s => s.ValidateDatasetNameAsync(It.IsAny<string>(), username, ModuleType.UrbanMonitor, existing.DatasetId)).Returns(Task.CompletedTask);

            var allEvents = new List<Event>
            {
                new() { Id = 100, Name = "Evento 1" },
                new() { Id = 101, Name = "Evento 2" }
            };
            sonda.Setup(s => s.GetAllEvents(username)).ReturnsAsync(allEvents);

            var filters = new List<FilterCondition>
            {
                new() { AttributeName = "Name", Type = FilterType.Contains, ValueType = FilterValueType.String, Condition = "Evento" }
            };

            svc.Setup(s => s.UpdateDatasetUMWithFiltersAsync(10, It.IsAny<CreateDatasetUMRequest>(), filters))
                .ReturnsAsync(new DatasetUM
                {
                    Id = 10,
                    Username = username,
                    Name = "Dataset Actualizable",
                    DatasetEvents = new List<DatasetEvent>
                    {
                        new() { Id_event = 100 },
                        new() { Id_event = 101 }
                    }
                });
            svc.Setup(s => s.UpdateDatasetAsyncUM(existing.DatasetId, It.IsAny<CreateDatasetRequest>(), It.IsAny<DatasetUM>())).ReturnsAsync(new Datasets { Id = 10 });

            var req = new CreateDatasetUMFilteredRequest
            {
                DatasetRequest = new CreateDatasetUMRequest
                {
                    Username = username,
                    Name = "Dataset Actualizable",
                    ContentType = "1"
                },
                Filters = filters
            };

            var controller = GetController(svc.Object, null, null, sonda.Object, null, username);

            var result = await controller.UpdateDatasetWithFilters(10, req);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<DatasetUM>(ok.Value);
            Assert.Equal(2, returned.DatasetEvents.Count);
            Assert.Contains(returned.DatasetEvents, e => e.Id_event == 101);
        }

        [Fact]
        public async Task CreateDatasetUMFiltered_SondaRetornaVacio_RetornaBadRequest()
        {
            var svc = new Mock<IDatasetUMService>();
            var sonda = new Mock<ISondaUMService>();
            var username = "emptyUser";
            sonda.Setup(s => s.GetAllEvents(username)).ReturnsAsync(new List<Event>());
            var filters = new List<FilterCondition>
            {
                new() { AttributeName = "State", Type = FilterType.Equals, ValueType = FilterValueType.String, Condition = "Activo" }
            };
            var req = new CreateDatasetUMFilteredRequest
            {
                DatasetRequest = new CreateDatasetUMRequest
                {
                    Username = username,
                    Name = "Dataset Vacío",
                    ContentType = "1"
                },
                Filters = filters
            };
            svc.Setup(s => s.CreateDatasetUMWithFiltersAsync(It.IsAny<CreateDatasetUMRequest>(), It.IsAny<int>(), filters))
                .ReturnsAsync(new DatasetUM
                {
                    Id = 3,
                    Username = username,
                    Name = "Dataset Vacío",
                    DatasetEvents = new List<DatasetEvent>()
                });
            svc.Setup(s => s.CreateDatasetAsync(It.IsAny<CreateDatasetRequest>())).ReturnsAsync(new Datasets { Id = 3 });
            svc.Setup(s => s.UpdateDatasetAsyncUM(3, It.IsAny<CreateDatasetRequest>(), It.IsAny<DatasetUM>())).ReturnsAsync(new Datasets { Id = 3 });
            var controller = GetController(svc.Object, null, null, sonda.Object, null, username);
            var result = await controller.CreateDatasetFiltered(req);
            var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task CreateDatasetFiltered_ModelStateInvalido_RetornaBadRequest()
        {
            var svc = new Mock<IDatasetUMService>();
            var sonda = new Mock<ISondaUMService>();
            var controller = GetController(svc.Object, null, null, sonda.Object, username: "u1");
            controller.ModelState.AddModelError("DatasetRequest.Name", "required");

            var req = new CreateDatasetUMFilteredRequest
            {
                DatasetRequest = new CreateDatasetUMRequest { ContentType = "1" },
                Filters = new List<FilterCondition>()
            };

            var result = await controller.CreateDatasetFiltered(req);
            var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
            svc.Verify(s => s.CreateDatasetUMWithFiltersAsync(It.IsAny<CreateDatasetUMRequest>(), It.IsAny<int>(), It.IsAny<List<FilterCondition>>()), Times.Never);
        }

        [Fact]
        public async Task CreateDatasetUMFiltered_SinFiltros_RetornaValido()
        {
            // Arrange
            var datasetUmService = new Mock<IDatasetUMService>();
            var sondaUmService = new Mock<ISondaUMService>();

            var username = "validNoFiltersUM";
            var controller = GetController(datasetUmService.Object, null, null, sondaUmService.Object, username: username);

            // UM service will create dataset and return id
            datasetUmService.Setup(s => s.CreateDatasetAsync(It.IsAny<CreateDatasetRequest>()))
                .ReturnsAsync(new Datasets { Id = 300 });

            // Sonda returns empty events list to avoid null source
            sondaUmService.Setup(s => s.GetAllEvents(username))
                .ReturnsAsync(new List<Event>());

            // UM service returns created DatasetUM with empty groups
            datasetUmService.Setup(s => s.CreateDatasetUMWithFiltersAsync(It.IsAny<CreateDatasetUMRequest>(), 300, It.IsAny<List<FilterCondition>>()))
                .ReturnsAsync(new DatasetUM
                {
                    Id = 300,
                    Username = username,
                    Name = "Dataset Válido Sin Filtros",
                    DatasetEvents = new List<DatasetEvent>(),
                    DatasetNews = new List<DatasetNews>()
                });

            // UM update after creation
            datasetUmService.Setup(s => s.UpdateDatasetAsyncUM(300, It.IsAny<CreateDatasetRequest>(), It.IsAny<DatasetUM>()))
                .ReturnsAsync(new Datasets { Id = 300 });

            var req = new CreateDatasetUMFilteredRequest
            {
                DatasetRequest = new CreateDatasetUMRequest
                {
                    Username = username,
                    Name = "Dataset Válido Sin Filtros",
                    ContentType = "1"
                },
                Filters = new List<FilterCondition>() // empty interpreted as no filtering
            };

            // Act
            var result = await controller.CreateDatasetFiltered(req);

            // Assert
            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returned = Assert.IsType<DatasetUM>(created.Value);

            Assert.Equal(300, returned.Id);
            Assert.Equal(username, returned.Username);
            Assert.Equal("Dataset Válido Sin Filtros", returned.Name);
            Assert.NotNull(returned.DatasetEvents);
            Assert.Empty(returned.DatasetEvents);

            datasetUmService.Verify(s => s.CreateDatasetAsync(It.IsAny<CreateDatasetRequest>()), Times.Once);
            datasetUmService.Verify(s => s.CreateDatasetUMWithFiltersAsync(It.IsAny<CreateDatasetUMRequest>(), 300, It.IsAny<List<FilterCondition>>()), Times.Once);
            datasetUmService.Verify(s => s.UpdateDatasetAsyncUM(300, It.IsAny<CreateDatasetRequest>(), It.IsAny<DatasetUM>()), Times.Once);
        }

        [Fact]
        public async Task CreateDatasetFiltered_SondaLanzaExcepcion_Retorna500()
        {
            var svc = new Mock<IDatasetUMService>();
            var sonda = new Mock<ISondaUMService>();
            var username = "sondaFail";

            svc.Setup(s => s.CreateDatasetAsync(It.IsAny<CreateDatasetRequest>())).ReturnsAsync(new Datasets { Id = 77 });
            sonda.Setup(s => s.GetAllNews(username, 1, null, null, 1000))
                 .ThrowsAsync(new Exception("sonda down"));

            var controller = GetController(svc.Object, null, null, sonda.Object, username: username);

            var req = new CreateDatasetUMFilteredRequest
            {
                DatasetRequest = new CreateDatasetUMRequest { Name = "X", ContentType = "2" },
                Filters = new List<FilterCondition>()
            };

            var result = await controller.CreateDatasetFiltered(req);
            var obj = Assert.IsType<ObjectResult>(result.Result);
            Assert.InRange(obj.StatusCode ?? 0, 500, 599);
            svc.Verify(s => s.CreateDatasetUMWithFiltersAsync(It.IsAny<CreateDatasetUMRequest>(), It.IsAny<int>(), It.IsAny<List<FilterCondition>>()), Times.Never);
        }

        [Fact]
        public async Task CreateDatasetFiltered_SinFiltros_RetornaBadRequest()
        {
            var svc = new Mock<IDatasetUMService>();
            var sonda = new Mock<ISondaUMService>();
            var controller = GetController(svc.Object, null, null, sonda.Object, username: "u2");
            var req = new CreateDatasetUMFilteredRequest
            {
                DatasetRequest = new CreateDatasetUMRequest { Username = "u2", Name = "Dataset Sin Filtros", ContentType = "1"},
                Filters = new List<FilterCondition>()
            };
            var result = await controller.CreateDatasetFiltered(req);
            var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
            svc.Verify(s => s.CreateDatasetUMWithFiltersAsync(It.IsAny<CreateDatasetUMRequest>(), It.IsAny<int>(), It.IsAny<List<FilterCondition>>()), Times.Never);
        }

        /* Tests sobre GetAllDatasets */

        [Fact]
        public async Task GetAllDatasets_Feliz_ReturnsOkWithList()
        {
            // Arrange
            var svc = new Mock<IDatasetUMService>();
            var sonda = new Mock<ISondaUMService>();

            var username = "happyUser";
            var expected = new List<DatasetUM>
    {
        new DatasetUM { Id = 1, Username = username, Name = "D1" },
        new DatasetUM { Id = 2, Username = username, Name = "D2" }
    };

            svc.Setup(s => s.GetAllDatasetsUMAsync(username)).ReturnsAsync(expected);

            var controller = GetController(svc.Object, null, null, sonda.Object, username: username);

            // Act
            var result = await controller.GetAllDatasets();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<List<DatasetUM>>(ok.Value);
            Assert.Equal(expected, returned);
            svc.Verify(s => s.GetAllDatasetsUMAsync(username), Times.Once);
        }

        [Fact]
        public async Task GetAllDatasets_ServiceReturnsNull_ReturnsOkWithNullBody()
        {
            // Arrange
            var svc = new Mock<IDatasetUMService>();
            var sonda = new Mock<ISondaUMService>();

            var username = "nullUser";
            svc.Setup(s => s.GetAllDatasetsUMAsync(username)).ReturnsAsync((List<DatasetUM>?)null);

            var controller = GetController(svc.Object, null, null, sonda.Object, username: username);

            // Act
            var result = await controller.GetAllDatasets();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Null(ok.Value);
            svc.Verify(s => s.GetAllDatasetsUMAsync(username), Times.Once);
        }

        [Fact]
        public async Task GetAllDatasets_ServiceThrows_Returns500()
        {
            // Arrange
            var svc = new Mock<IDatasetUMService>();
            var sonda = new Mock<ISondaUMService>();

            var username = "errUser";
            svc.Setup(s => s.GetAllDatasetsUMAsync(username)).ThrowsAsync(new Exception("boom um"));

            var controller = GetController(svc.Object, null, null, sonda.Object, username: username);

            // Act
            var result = await controller.GetAllDatasets();

            // Assert
            var obj = Assert.IsType<ObjectResult>(result.Result);
            Assert.InRange(obj.StatusCode ?? 0, 500, 599);
            Assert.Contains("boom um", obj.Value?.ToString() ?? string.Empty);
            svc.Verify(s => s.GetAllDatasetsUMAsync(username), Times.Once);
        }

        [Fact]
        public async Task GetAllDatasets_NoUserInContext_PassesNullToService()
        {
            // Arrange
            var svc = new Mock<IDatasetUMService>();
            var sonda = new Mock<ISondaUMService>();

            svc.Setup(s => s.GetAllDatasetsUMAsync(null)).ReturnsAsync(new List<DatasetUM>());

            var controller = GetController(svc.Object, null, null, sonda.Object, username: "willRemove");
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }; // remove user

            // Act
            var result = await controller.GetAllDatasets();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            // controller returns Ok(datasets) — service returned empty list
            var returned = Assert.IsType<List<DatasetUM>>(ok.Value);
            Assert.Empty(returned);
            svc.Verify(s => s.GetAllDatasetsUMAsync(null), Times.Once);
        }

        [Fact]
        public async Task GetAllDatasets_ListContainsNullElements_ControllerReturnsSameList()
        {
            // Arrange
            var svc = new Mock<IDatasetUMService>();
            var sonda = new Mock<ISondaUMService>();

            var username = "nullElems";
            var listWithNulls = new List<DatasetUM?> { new DatasetUM { Id = 1, Username = username, Name = "OK" }, null, null };
            // Cast to List<DatasetUM> for the service signature but preserve nulls
            svc.Setup(s => s.GetAllDatasetsUMAsync(username)).ReturnsAsync(listWithNulls.Cast<DatasetUM>().ToList());

            var controller = GetController(svc.Object, null, null, sonda.Object, username: username);

            // Act
            var result = await controller.GetAllDatasets();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<List<DatasetUM>>(ok.Value);
            Assert.Equal(1, returned[0].Id);
            svc.Verify(s => s.GetAllDatasetsUMAsync(username), Times.Once);
        }

        [Fact]
        public async Task GetAllDatasets_LargePayload_ReturnsAllItems()
        {
            // Arrange
            var svc = new Mock<IDatasetUMService>();
            var sonda = new Mock<ISondaUMService>();

            var username = "bigUser";
            var large = Enumerable.Range(0, 500)
                .Select(i => new DatasetUM
                {
                    Id = i + 1,
                    Username = username,
                    Name = $"Name_{i}",
                    DatasetEvents = Enumerable.Range(0, 5).Select(e => new DatasetEvent { Id_event = e }).ToList(),
                    DatasetNews = Enumerable.Range(0, 3).Select(n => new DatasetNews { Id_news = n }).ToList()
                })
                .ToList();

            svc.Setup(s => s.GetAllDatasetsUMAsync(username)).ReturnsAsync(large);

            var controller = GetController(svc.Object, null, null, sonda.Object, username: username);

            // Act
            var result = await controller.GetAllDatasets();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<List<DatasetUM>>(ok.Value);
            Assert.Equal(500, returned.Count);
            Assert.Equal(5, returned[0].DatasetEvents.Count);
            Assert.Equal(3, returned[0].DatasetNews.Count);
            svc.Verify(s => s.GetAllDatasetsUMAsync(username), Times.Once);
        }

        /* Tests sobre GetDatasetById */

        [Fact]
        public async Task GetDatasetById_Feliz_ReturnsOkWithDataset()
        {
            // Arrange
            var svc = new Mock<IDatasetUMService>();
            var sonda = new Mock<ISondaUMService>();

            var username = "happyUser";
            var ds = new DatasetUM
            {
                Id = 42,
                Username = username,
                Name = "MiDataset",
                Description = "Desc"
            };

            svc.Setup(s => s.GetDatasetUMByIdForEditAsync(42, username)).ReturnsAsync(ds);

            var controller = GetController(svc.Object, null, null, sonda.Object, username: username);

            // Act
            var result = await controller.GetDatasetById(42);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<DatasetUM>(ok.Value);
            Assert.Equal(42, returned.Id);
            Assert.Equal("MiDataset", returned.Name);
            svc.Verify(s => s.GetDatasetUMByIdForEditAsync(42, username), Times.Once);
        }

        [Fact]
        public async Task GetDatasetById_NotFound_Returns404()
        {
            // Arrange
            var svc = new Mock<IDatasetUMService>();
            var username = "noUser";
            svc.Setup(s => s.GetDatasetUMByIdForEditAsync(999, username)).ReturnsAsync((DatasetUM?)null);

            var controller = GetController(svc.Object, null, null, Mock.Of<ISondaUMService>(), username: username);

            // Act
            var result = await controller.GetDatasetById(999);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal($"No se encontró el dataset con ID 999 para el usuario {username}.", notFound.Value);
            svc.Verify(s => s.GetDatasetUMByIdForEditAsync(999, username), Times.Once);
        }

        [Fact]
        public async Task GetDatasetById_ServiceThrows_Returns500()
        {
            // Arrange
            var svc = new Mock<IDatasetUMService>();
            var username = "errUser";
            svc.Setup(s => s.GetDatasetUMByIdForEditAsync(7, username)).ThrowsAsync(new Exception("boom um"));

            var controller = GetController(svc.Object, null, null, Mock.Of<ISondaUMService>(), username: username);

            // Act
            var result = await controller.GetDatasetById(7);

            // Assert
            var obj = Assert.IsType<ObjectResult>(result.Result);
            Assert.InRange(obj.StatusCode ?? 0, 500, 599);
            Assert.Contains("boom um", obj.Value?.ToString() ?? string.Empty);
            svc.Verify(s => s.GetDatasetUMByIdForEditAsync(7, username), Times.Once);
        }

        [Fact]
        public async Task GetDatasetById_NoUserInContext_PassesNullUsernameToService()
        {
            // Arrange
            var svc = new Mock<IDatasetUMService>();
            svc.Setup(s => s.GetDatasetUMByIdForEditAsync(5, null)).ReturnsAsync((DatasetUM?)null);

            var controller = GetController(svc.Object, null, null, Mock.Of<ISondaUMService>(), username: "willRemove");
            // remove user from context
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

            // Act
            var result = await controller.GetDatasetById(5);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal($"No se encontró el dataset con ID 5 para el usuario .", notFound.Value);
            svc.Verify(s => s.GetDatasetUMByIdForEditAsync(5, null), Times.Once);
        }

        [Fact]
        public async Task GetDatasetById_ZeroAndNegativeIds_TreatedAsNotFoundIfServiceReturnsNull()
        {
            // Arrange
            var svc = new Mock<IDatasetUMService>();
            var username = "negUser";
            svc.Setup(s => s.GetDatasetUMByIdForEditAsync(0, username)).ReturnsAsync((DatasetUM?)null);
            svc.Setup(s => s.GetDatasetUMByIdForEditAsync(-1, username)).ReturnsAsync((DatasetUM?)null);

            var controller = GetController(svc.Object, null, null, Mock.Of<ISondaUMService>(), username: username);

            // Act & Assert for zero
            var result0 = await controller.GetDatasetById(0);
            var notFound0 = Assert.IsType<NotFoundObjectResult>(result0.Result);
            Assert.Equal($"No se encontró el dataset con ID 0 para el usuario {username}.", notFound0.Value);

            // Act & Assert for negative
            var resultNeg = await controller.GetDatasetById(-1);
            var notFoundNeg = Assert.IsType<NotFoundObjectResult>(resultNeg.Result);
            Assert.Equal($"No se encontró el dataset con ID -1 para el usuario {username}.", notFoundNeg.Value);

            svc.Verify(s => s.GetDatasetUMByIdForEditAsync(0, username), Times.Once);
            svc.Verify(s => s.GetDatasetUMByIdForEditAsync(-1, username), Times.Once);
        }

        [Fact]
        public async Task GetDatasetById_ServiceReturnsDatasetWithNullFields_ControllerReturnsIt()
        {
            // Arrange
            var svc = new Mock<IDatasetUMService>();
            var username = "nullFieldsUser";
            var ds = new DatasetUM
            {
                Id = 77,
                Username = username,
                Name = null!, // simulate malformed service response
                Description = null,
                DatasetEvents = null!, // null collections
                DatasetNews = null!
            };
            svc.Setup(s => s.GetDatasetUMByIdForEditAsync(77, username)).ReturnsAsync(ds);

            var controller = GetController(svc.Object, null, null, Mock.Of<ISondaUMService>(), username: username);

            // Act
            var result = await controller.GetDatasetById(77);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<DatasetUM>(ok.Value);
            Assert.Equal(77, returned.Id);
            Assert.Null(returned.Name);
            Assert.Null(returned.DatasetEvents);
            svc.Verify(s => s.GetDatasetUMByIdForEditAsync(77, username), Times.Once);
        }

        [Fact]
        public async Task GetDatasetById_LargeCollections_ReturnsPayloadIntact()
        {
            // Arrange
            var svc = new Mock<IDatasetUMService>();
            var username = "bigUser";
            var big = new DatasetUM
            {
                Id = 999,
                Username = username,
                Name = "BigDataset",
                DatasetEvents = Enumerable.Range(0, 1000).Select(i => new DatasetEvent { Id_event = i }).ToList(),
                DatasetNews = Enumerable.Range(0, 500).Select(i => new DatasetNews { Id_news = i }).ToList()
            };
            svc.Setup(s => s.GetDatasetUMByIdForEditAsync(999, username)).ReturnsAsync(big);

            var controller = GetController(svc.Object, null, null, Mock.Of<ISondaUMService>(), username: username);

            // Act
            var result = await controller.GetDatasetById(999);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<DatasetUM>(ok.Value);
            Assert.Equal(1000, returned.DatasetEvents.Count);
            Assert.Equal(500, returned.DatasetNews.Count);
            svc.Verify(s => s.GetDatasetUMByIdForEditAsync(999, username), Times.Once);
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
            var ds = new DatasetUM
            {
                Id = 101,
                Username = username,
                Name = "ToDelete",
                DatasetId = 1001
            };
            context.DatasetsUM.Add(ds);
            await context.SaveChangesAsync();

            var svc = new Mock<IDatasetUMService>();
            var sonda = new Mock<ISondaUMService>();
            var auth = Mock.Of<ISondaAuthService>();

            svc.Setup(s => s.GetDatasetUMByIdForEditAsync(101, username)).ReturnsAsync(ds);
            svc.Setup(s => s.DeleteDatasetUMAsync(101, username)).Returns(Task.CompletedTask).Verifiable();
            svc.Setup(s => s.DeleteDatasetAsync(1001, username)).Returns(Task.CompletedTask).Verifiable();

            var controller = GetController(svc.Object, auth, context, sonda.Object, username: username);

            // Act
            var result = await controller.DeleteDataset(101);

            // Assert
            Assert.IsType<NoContentResult>(result);
            svc.Verify(s => s.DeleteDatasetUMAsync(101, username), Times.Once);
            svc.Verify(s => s.DeleteDatasetAsync(1001, username), Times.Once);
        }

        [Fact]
        public async Task DeleteDataset_ServiceGetReturnsNull_ReturnsNotFound_AndNoDeleteCalls()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            using var context = new ApplicationDbContext(options);

            var username = "no_exist";
            var svc = new Mock<IDatasetUMService>();
            var sonda = new Mock<ISondaUMService>();
            var auth = Mock.Of<ISondaAuthService>();

            svc.Setup(s => s.GetDatasetUMByIdForEditAsync(9999, username)).ReturnsAsync((DatasetUM?)null);

            var controller = GetController(svc.Object, auth, context, sonda.Object, username: username);

            // Act
            var result = await controller.DeleteDataset(9999);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal($"No se encontró el dataset con ID 9999 para el usuario {username}.", notFound.Value);
            svc.Verify(s => s.DeleteDatasetUMAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
            svc.Verify(s => s.DeleteDatasetAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task DeleteDataset_EmServiceThrows_Returns500_AndUmDeleteNotCalled()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            using var context = new ApplicationDbContext(options);

            var username = "svc_throw";
            var ds = new DatasetUM { Id = 404, Username = username, Name = "WillThrow", DatasetId = 4004 };
            context.DatasetsUM.Add(ds);
            await context.SaveChangesAsync();

            var svc = new Mock<IDatasetUMService>();
            var sonda = new Mock<ISondaUMService>();
            var auth = Mock.Of<ISondaAuthService>();

            svc.Setup(s => s.GetDatasetUMByIdForEditAsync(404, username)).ReturnsAsync(ds);
            svc.Setup(s => s.DeleteDatasetUMAsync(404, username)).ThrowsAsync(new Exception("UM delete failed"));

            var controller = GetController(svc.Object, auth, context, sonda.Object, username: username);

            // Act
            var result = await controller.DeleteDataset(404);

            // Assert
            var obj = Assert.IsType<ObjectResult>(result);
            Assert.InRange(obj.StatusCode ?? 0, 500, 599);
            Assert.Contains("UM delete failed", obj.Value?.ToString() ?? string.Empty);
            svc.Verify(s => s.DeleteDatasetUMAsync(404, username), Times.Once);
            svc.Verify(s => s.DeleteDatasetAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task DeleteDataset_DbRowMissing_IdNull_LeadsTo500()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            using var context = new ApplicationDbContext(options);

            // Note: we DO NOT add a row to context.DatasetsUM, but mock service GetDatasetUMByIdForEditAsync to return a DatasetUM
            // This reproduces the scenario where the service finds the dataset but the general table row is missing => id == null -> NRE
            var username = "mismatch";
            var dsFromService = new DatasetUM { Id = 555, Username = username, Name = "ExistsInServiceOnly", DatasetId = 5005 };

            var svc = new Mock<IDatasetUMService>();
            var sonda = new Mock<ISondaUMService>();
            var auth = Mock.Of<ISondaAuthService>();

            svc.Setup(s => s.GetDatasetUMByIdForEditAsync(555, username)).ReturnsAsync(dsFromService);
            svc.Setup(s => s.DeleteDatasetUMAsync(555, username)).Returns(Task.CompletedTask);

            var controller = GetController(svc.Object, auth, context, sonda.Object, username: username);

            // Act
            var result = await controller.DeleteDataset(555);

            // Assert: because controller does FirstOrDefaultAsync on context (returns null) and then uses id!.DatasetId, an exception occurs -> 500
            var obj = Assert.IsType<ObjectResult>(result);
            Assert.InRange(obj.StatusCode ?? 0, 500, 599);
            svc.Verify(s => s.DeleteDatasetUMAsync(555, username), Times.Once);
            // DeleteDatasetAsync cannot be called because id was null and access triggers exception before call
            svc.Verify(s => s.DeleteDatasetAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        /* Tests sobre GetAllDatasetsDto y GetAllGenericDatasetsDto */

        [Fact]
        public async Task GetAllDatasetsDto_ReturnsEmpty_WhenChildCollectionsEmpty()
        {
            // Arrange
            var username = "emptyChildren";
            var context = new ApplicationDbContext(
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .Options);

            // Agregar fila Datasets con Tipo UM pero DatasetUM vacío
            context.Datasets.Add(new Datasets
            {
                NameDataset = "UM empty child",
                TipoDataset = ModuleType.UrbanMonitor,
                Username = username,
                DatasetUM = new List<DatasetUM>() // empty child collection
            });
            context.SaveChanges();

            var controller = GetControllerWithDb(context, username);

            // Act
            var result = await controller.GetAllDatasetsDto();

            // Assert -> no debe incluir entradas sin DatasetUM
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var list = Assert.IsType<List<DatasetDto>>(ok.Value);
            Assert.Empty(list);
        }

        [Fact]
        public async Task GetAllDatasetsDto_SearchIsCaseInsensitiveAndRemovesSpaces()
        {
            // Arrange
            var username = "caseUser";
            var context = new ApplicationDbContext(
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .Options);

            context.Datasets.Add(new Datasets
            {
                NameDataset = "Mi Dataset Especial",
                TipoDataset = ModuleType.InsightMonitor,
                Username = username,
                DatasetIM = new List<DatasetIM> { new DatasetIM { Name = "Mi Dataset Especial" } }
            });
            context.SaveChanges();

            var controller = GetControllerWithDb(context, username);

            // Act (search with different case and spaces removed)
            var result = await controller.GetAllDatasetsDto(search: "midatasetespecial");

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var list = Assert.IsType<List<DatasetDto>>(ok.Value);
            Assert.Single(list);
            Assert.Equal("Mi Dataset Especial", list[0].Nombre);
        }

        [Fact]
        public async Task GetAllDatasetsDto_LargeMixedModules_PerformanceSmoke()
        {
            // Arrange
            var username = "bigMix";
            var context = new ApplicationDbContext(
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .Options);

            for (int i = 0; i < 200; i++)
            {
                context.Datasets.Add(new Datasets
                {
                    NameDataset = $"IM {i}",
                    TipoDataset = ModuleType.InsightMonitor,
                    Username = username,
                    DatasetIM = new List<DatasetIM> { new DatasetIM { Name = $"IM {i}" } }
                });
                context.Datasets.Add(new Datasets
                {
                    NameDataset = $"UM {i}",
                    TipoDataset = ModuleType.UrbanMonitor,
                    Username = username,
                    DatasetUM = new List<DatasetUM> { new DatasetUM { Name = $"UM {i}" } }
                });
            }
            context.SaveChanges();

            var controller = GetControllerWithDb(context, username);

            // Act
            var result = await controller.GetAllDatasetsDto();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var list = Assert.IsType<List<DatasetDto>>(ok.Value);
            // Esperamos 400 (200 IM + 200 UM) total
            Assert.Equal(400, list.Count);
        }

        [Fact]
        public async Task GetAllDatasetsDto_ReturnsAllModules_AsDatasetDtoList()
        {
            // Arrange
            var username = "mixUser";
            var context = new ApplicationDbContext(
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .Options);

            // IM
            context.Datasets.Add(new Datasets
            {
                NameDataset = "IMds",
                TipoDataset = ModuleType.InsightMonitor,
                Username = username,
                DatasetIM = new List<DatasetIM> { new DatasetIM { Name = "IM name", ContentType = "imct" } }
            });

            // UM
            context.Datasets.Add(new Datasets
            {
                NameDataset = "UMds",
                TipoDataset = ModuleType.UrbanMonitor,
                Username = username,
                DatasetUM = new List<DatasetUM> { new DatasetUM { Name = "UM name", ContentType = "umct" } }
            });

            // AM
            context.Datasets.Add(new Datasets
            {
                NameDataset = "AMds",
                TipoDataset = ModuleType.AssetManager,
                Username = username,
                DatasetAM = new List<DatasetAM> { new DatasetAM { Nombre = "AM name", ContentType = "amct" } }
            });

            // EM
            context.Datasets.Add(new Datasets
            {
                NameDataset = "EMds",
                TipoDataset = ModuleType.EventManager,
                Username = username,
                DatasetEM = new List<DatasetEM> { new DatasetEM { Name = "EM name", ContentType = "emct" } }
            });

            context.SaveChanges();

            var controller = GetControllerWithDb(context, username);

            // Act
            var result = await controller.GetAllDatasetsDto();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var list = Assert.IsType<List<DatasetDto>>(ok.Value);
            // Expect 4 entries (one per module)
            Assert.Equal(4, list.Count);
            Assert.Contains(list, d => d.Module == "Insight Monitor" && d.ContentType == "imct");
            Assert.Contains(list, d => d.Module == "Urban Monitor" && d.ContentType == "umct");
            Assert.Contains(list, d => d.Module == "Asset Manager" && d.ContentType == "amct");
            Assert.Contains(list, d => d.Module == "Event Manager" && d.ContentType == "emct");
        }

        [Fact]
        public async Task GetAllDatasetsDto_NoDatasets_ReturnsEmptyList()
        {
            // Arrange
            var username = "emptyUser";
            var context = new ApplicationDbContext(
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .Options);

            // no seed
            context.SaveChanges();

            var controller = GetControllerWithDb(context, username);

            // Act
            var result = await controller.GetAllDatasetsDto();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var list = Assert.IsType<List<DatasetDto>>(ok.Value);
            Assert.Empty(list);
        }

        [Fact]
        public async Task GetAllGenericDatasetDtos_Success_ReturnsExpectedEntries()
        {
                // Arrange
                var username = "gen_happy";
                var context = new ApplicationDbContext(
                    new DbContextOptionsBuilder<ApplicationDbContext>()
                        .UseInMemoryDatabase(Guid.NewGuid().ToString())
                        .Options);

                context.Datasets.Add(new Datasets
                {
                    NameDataset = "UM Generic 1",
                    TipoDataset = ModuleType.UrbanMonitor,
                    Username = username,
                    DatasetUM = new List<DatasetUM>
        {
            new DatasetUM { Name = "UM Generic 1", DatasetId = 11 } // <- asignar DatasetId explícito
        }
                });

                context.Datasets.Add(new Datasets
                {
                    NameDataset = "EM Generic 1",
                    TipoDataset = ModuleType.EventManager,
                    Username = username,
                    DatasetEM = new List<DatasetEM>
        {
            new DatasetEM { Name = "EM Generic 1", DatasetId = 22 } // <- asignar DatasetId explícito
        }
                });

                context.SaveChanges();

                var controller = GetControllerWithDb(context, username);

                // Act
                var result = await controller.GetAllGenericDatasetDtos();

                // Assert
                var ok = Assert.IsType<OkObjectResult>(result.Result);
                var list = Assert.IsType<List<DatasetDtoGenerico>>(ok.Value);
                Assert.Equal(2, list.Count);
            }

        [Fact]
        public async Task GetAllGenericDatasetDtos_EmptyResult_ReturnsEmptyList()
        {
            // Arrange
            var username = "gen_empty";
            var context = new ApplicationDbContext(
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .Options);
            context.SaveChanges();

            var controller = GetControllerWithDb(context, username);

            // Act
            var result = await controller.GetAllGenericDatasetDtos();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var list = Assert.IsType<List<DatasetDtoGenerico>>(ok.Value);
            Assert.Empty(list);
        }

        [Fact]
        public async Task GetAllGenericDatasetDtos_SearchFiltersByNombre()
        {
            // Arrange
            var username = "gen_search";
            var context = new ApplicationDbContext(
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .Options);

            context.Datasets.Add(new Datasets
            {
                NameDataset = "Public UM",
                TipoDataset = ModuleType.UrbanMonitor,
                Username = username,
                DatasetUM = new List<DatasetUM> { new DatasetUM { Name = "Public UM", DatasetId = 123 } }
            });

            context.Datasets.Add(new Datasets
            {
                NameDataset = "Private IM",
                TipoDataset = ModuleType.InsightMonitor,
                Username = username,
                DatasetIM = new List<DatasetIM> { new DatasetIM { Name = "Private IM", ContentType = "x", DatasetId = 456 } }
            });

            context.SaveChanges();

            var controller = GetControllerWithDb(context, username);

            // Act
            var result = await controller.GetAllGenericDatasetDtos(search: "public");

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var list = Assert.IsType<List<DatasetDtoGenerico>>(ok.Value);
            Assert.Single(list);
            Assert.Equal("Public UM", list[0].Nombre);
        }

        [Fact]
        public async Task GetAllGenericDatasetDtos_HandlesMissingChildCollectionGracefully()
        {
            // Arrange
            var username = "gen_child_missing";
            var context = new ApplicationDbContext(
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .Options);

            // Add a Datasets row with TipoDataset = InsightMonitor but DatasetIM empty -> should not add entry
            context.Datasets.Add(new Datasets
            {
                NameDataset = "IM empty",
                TipoDataset = ModuleType.InsightMonitor,
                Username = username,
                DatasetIM = new List<DatasetIM>() // empty
            });
            context.SaveChanges();

            var controller = GetControllerWithDb(context, username);

            // Act
            var result = await controller.GetAllGenericDatasetDtos();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var list = Assert.IsType<List<DatasetDtoGenerico>>(ok.Value);
            Assert.Empty(list);
        }
    }
}
