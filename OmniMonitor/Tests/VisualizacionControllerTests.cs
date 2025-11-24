using System.Data;
using System.Security.Claims;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Moq;

using OmniMonitor.Server.Context;
using OmniMonitor.Server.Controllers;
using OmniMonitor.Server.Services;
using OmniMonitor.Shared.Dtos;

namespace QA.Tests
{
    public class VisualizacionControllerTests
    {
        private ServiceProvider BuildServiceProviderWithInMemoryDb(string dbName)
        {
            var services = new ServiceCollection();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(dbName));
            return services.BuildServiceProvider();
        }

        private VisualizacionController GetController(
            Mock<IVisualizacionService>? serviceMock = null,
            Mock<ISondaAuthService>? authMock = null,
            ClaimsPrincipal? user = null)
        {
            serviceMock ??= new Mock<IVisualizacionService>();
            authMock ??= new Mock<ISondaAuthService>();
            var controller = new VisualizacionController(serviceMock.Object, authMock.Object);
            if (user != null)
            {
                controller.ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = user }
                };
            }
            return controller;
        }

        private ClaimsPrincipal GetUser(string username = "testuser")
        {
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, username) }, "TestAuth");
            return new ClaimsPrincipal(identity);
        }

        private void SeedVisualizacion(ApplicationDbContext db, int id, string username = "testuser")
        {
            var vis = new Visualizacion
            {
                IdVisualizacion = id,
                Nombre = $"Vis{id}",
                Username = username,
                JsonDesign = "{}",
                GrupoDatasets = new List<GrupoDataset>
                {
                    new GrupoDataset { DatasetId = 1, JsonDesign = "{}" }
                }
            };
            db.Visualizaciones.Add(vis);
            db.SaveChanges();
        }

        /* Tests sobre CreateVisualizacion */

        [Fact]
        public async Task CreateVisualizacion_ReturnsCreated_WhenValid()
        {
            var serviceMock = new Mock<IVisualizacionService>();
            var request = new CreateVisualizacionRequest { Nombre = "Vis1" };
            var visualizacion = new Visualizacion { IdVisualizacion = 1, Nombre = "Vis1", Username = "testuser" };
            serviceMock.Setup(s => s.CreateVisualizacionAsync(request, "testuser")).ReturnsAsync(visualizacion);

            var controller = GetController(serviceMock, null, GetUser());

            var result = await controller.CreateVisualizacion(request);

            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(visualizacion, created.Value);
        }

        [Fact]
        public async Task CreateVisualizacion_ReturnsBadRequest_WhenModelStateInvalid()
        {
            var controller = GetController(null, null, GetUser());
            controller.ModelState.AddModelError("Nombre", "Required");

            var result = await controller.CreateVisualizacion(new CreateVisualizacionRequest());

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task CreateVisualizacion_ReturnsBadRequest_OnArgumentException()
        {
            var serviceMock = new Mock<IVisualizacionService>();
            serviceMock.Setup(s => s.CreateVisualizacionAsync(It.IsAny<CreateVisualizacionRequest>(), It.IsAny<string>()))
                .ThrowsAsync(new ArgumentException("Nombre duplicado"));

            var controller = GetController(serviceMock, null, GetUser());

            var result = await controller.CreateVisualizacion(new CreateVisualizacionRequest { Nombre = "Vis1" });

            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("Nombre duplicado", badRequest.Value.ToString());
        }

        [Fact]
        public async Task CreateVisualizacion_ReturnsServerError_OnException()
        {
            var serviceMock = new Mock<IVisualizacionService>();
            serviceMock.Setup(s => s.CreateVisualizacionAsync(It.IsAny<CreateVisualizacionRequest>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("DB error"));

            var controller = GetController(serviceMock, null, GetUser());

            var result = await controller.CreateVisualizacion(new CreateVisualizacionRequest { Nombre = "Vis1" });

            var serverError = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, serverError.StatusCode);
            Assert.Contains("DB error", serverError.Value.ToString());
        }

        [Fact]
        public async Task CreateVisualizacion_ReturnsCreated_WithCorrectLocation()
        {
            var serviceMock = new Mock<IVisualizacionService>();
            var request = new CreateVisualizacionRequest { Nombre = "Vis2" };
            var visualizacion = new Visualizacion { IdVisualizacion = 2, Nombre = "Vis2", Username = "testuser" };
            serviceMock.Setup(s => s.CreateVisualizacionAsync(request, "testuser")).ReturnsAsync(visualizacion);

            var controller = GetController(serviceMock, null, GetUser());

            var result = await controller.CreateVisualizacion(request);

            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(nameof(controller.GetVisualizacionById), created.ActionName);
            Assert.Equal(visualizacion.IdVisualizacion, ((Visualizacion)created.Value).IdVisualizacion);
        }

        [Fact]
        public async Task CreateVisualizacion_ReturnsBadRequest_WhenUserIsNull()
        {
            var serviceMock = new Mock<IVisualizacionService>();
            var controller = GetController(serviceMock, null, new ClaimsPrincipal());

            controller.ModelState.Clear();
            serviceMock.Setup(s => s.CreateVisualizacionAsync(It.IsAny<CreateVisualizacionRequest>(), null))
                .ThrowsAsync(new ArgumentException("Usuario requerido"));

            var result = await controller.CreateVisualizacion(new CreateVisualizacionRequest { Nombre = "Vis3" });
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        /* Tests sobre GetAllVisualizaciones */

        [Fact]
        public async Task GetAllVisualizaciones_ReturnsOk_WithVisualizaciones()
        {
            var serviceMock = new Mock<IVisualizacionService>();
            var visualizaciones = new List<Visualizacion> { new Visualizacion { IdVisualizacion = 1, Nombre = "Vis1" } };
            serviceMock.Setup(s => s.GetAllVisualizacionesAsync("testuser")).ReturnsAsync(visualizaciones);

            var controller = GetController(serviceMock, null, GetUser());

            var result = await controller.GetAllVisualizaciones();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(visualizaciones, ok.Value);
        }

        [Fact]
        public async Task GetAllVisualizaciones_ReturnsServerError_OnException()
        {
            var serviceMock = new Mock<IVisualizacionService>();
            serviceMock.Setup(s => s.GetAllVisualizacionesAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception("DB error"));

            var controller = GetController(serviceMock, null, GetUser());

            var result = await controller.GetAllVisualizaciones();

            var serverError = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, serverError.StatusCode);
            Assert.Contains("DB error", serverError.Value.ToString());
        }

        [Fact]
        public async Task GetAllVisualizaciones_ReturnsOk_EmptyList()
        {
            var serviceMock = new Mock<IVisualizacionService>();
            serviceMock.Setup(s => s.GetAllVisualizacionesAsync("testuser")).ReturnsAsync(new List<Visualizacion>());

            var controller = GetController(serviceMock, null, GetUser());

            var result = await controller.GetAllVisualizaciones();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Empty((List<Visualizacion>)ok.Value);
        }

        [Fact]
        public async Task GetAllVisualizaciones_ReturnsOk_WhenUserIsNull()
        {
            var serviceMock = new Mock<IVisualizacionService>();
            serviceMock.Setup(s => s.GetAllVisualizacionesAsync(null)).ReturnsAsync(new List<Visualizacion>());

            var controller = GetController(serviceMock, null, new ClaimsPrincipal());

            var result = await controller.GetAllVisualizaciones();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Empty((List<Visualizacion>)ok.Value);
        }

        [Fact]
        public async Task GetAllVisualizaciones_ReturnsOk_MultipleVisualizaciones()
        {
            var serviceMock = new Mock<IVisualizacionService>();
            var visualizaciones = new List<Visualizacion>
            {
                new Visualizacion { IdVisualizacion = 1, Nombre = "Vis1" },
                new Visualizacion { IdVisualizacion = 2, Nombre = "Vis2" }
            };
            serviceMock.Setup(s => s.GetAllVisualizacionesAsync("testuser")).ReturnsAsync(visualizaciones);

            var controller = GetController(serviceMock, null, GetUser());

            var result = await controller.GetAllVisualizaciones();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(visualizaciones, ok.Value);
        }

        [Fact]
        public async Task GetAllVisualizaciones_ReturnsServerError_OnNullService()
        {
            var controller = new VisualizacionController(null, null);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = GetUser() }
            };
            var result = await controller.GetAllVisualizaciones();
            var serverError = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, serverError.StatusCode);
        }

        /* Tests sobre GetAllVisualizacionesPaginated */

        [Fact]
        public async Task GetAllVisualizacionesPaginated_ReturnsOk_WithData()
        {
            var serviceMock = new Mock<IVisualizacionService>();
            serviceMock.Setup(s => s.GetAllVisualizacionesPaginatedAsync("testuser", 1, 10, null))
                .ReturnsAsync(new List<Visualizacion> { new Visualizacion { IdVisualizacion = 1 } });
            serviceMock.Setup(s => s.GetVisualizacionesCountAsync("testuser", null)).ReturnsAsync(1);

            var controller = GetController(serviceMock, null, GetUser());

            var result = await controller.GetAllVisualizacionesPaginated(1, 10, null);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task GetAllVisualizacionesPaginated_ReturnsBadRequest_WhenUserIsNull()
        {
            var controller = GetController(null, null, new ClaimsPrincipal());
            var result = await controller.GetAllVisualizacionesPaginated(1, 10, null);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("Usuario no encontrado", badRequest.Value.ToString());
        }

        [Fact]
        public async Task GetAllVisualizacionesPaginated_ReturnsBadRequest_WhenPageInvalid()
        {
            var controller = GetController(null, null, GetUser());
            var result = await controller.GetAllVisualizacionesPaginated(0, 10, null);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("mayores a 0", badRequest.Value.ToString());
        }

        [Fact]
        public async Task GetAllVisualizacionesPaginated_ReturnsServerError_OnException()
        {
            var serviceMock = new Mock<IVisualizacionService>();
            serviceMock.Setup(s => s.GetAllVisualizacionesPaginatedAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("DB error"));

            var controller = GetController(serviceMock, null, GetUser());
            var result = await controller.GetAllVisualizacionesPaginated(1, 10, null);
            var serverError = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, serverError.StatusCode);
        }

        [Fact]
        public async Task GetAllVisualizacionesPaginated_ReturnsOk_Empty()
        {
            var serviceMock = new Mock<IVisualizacionService>();
            serviceMock.Setup(s => s.GetAllVisualizacionesPaginatedAsync("testuser", 1, 10, null))
                .ReturnsAsync(new List<Visualizacion>());
            serviceMock.Setup(s => s.GetVisualizacionesCountAsync("testuser", null)).ReturnsAsync(0);

            var controller = GetController(serviceMock, null, GetUser());
            var result = await controller.GetAllVisualizacionesPaginated(1, 10, null);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task GetAllVisualizacionesPaginated_ReturnsOk_PageOutOfRange()
        {
            var serviceMock = new Mock<IVisualizacionService>();
            serviceMock.Setup(s => s.GetAllVisualizacionesPaginatedAsync("testuser", 2, 10, null))
                .ReturnsAsync(new List<Visualizacion>());
            serviceMock.Setup(s => s.GetVisualizacionesCountAsync("testuser", null)).ReturnsAsync(1);

            var controller = GetController(serviceMock, null, GetUser());
            var result = await controller.GetAllVisualizacionesPaginated(2, 10, null);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.NotNull(ok.Value);
        }

        /* Tests sobre GetVisualizacionById */

        [Fact]
        public async Task GetVisualizacionById_ReturnsOk_WhenFound()
        {
            var serviceMock = new Mock<IVisualizacionService>();
            var visualizacion = new Visualizacion { IdVisualizacion = 1, Nombre = "Vis1" };
            serviceMock.Setup(s => s.GetVisualizacionByIdAsync(1, "testuser")).ReturnsAsync(visualizacion);

            var controller = GetController(serviceMock, null, GetUser());
            var result = await controller.GetVisualizacionById(1);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(visualizacion, ok.Value);
        }

        [Fact]
        public async Task GetVisualizacionById_ReturnsNotFound_WhenNotFound()
        {
            var serviceMock = new Mock<IVisualizacionService>();
            serviceMock.Setup(s => s.GetVisualizacionByIdAsync(1, "testuser")).ReturnsAsync((Visualizacion)null);

            var controller = GetController(serviceMock, null, GetUser());
            var result = await controller.GetVisualizacionById(1);

            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Contains("No se encontró", notFound.Value.ToString());
        }

        [Fact]
        public async Task GetVisualizacionById_ReturnsServerError_OnException()
        {
            var serviceMock = new Mock<IVisualizacionService>();
            serviceMock.Setup(s => s.GetVisualizacionByIdAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("DB error"));

            var controller = GetController(serviceMock, null, GetUser());
            var result = await controller.GetVisualizacionById(1);

            var serverError = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, serverError.StatusCode);
        }

        [Fact]
        public async Task GetVisualizacionById_ReturnsOk_WithDifferentUser()
        {
            var serviceMock = new Mock<IVisualizacionService>();
            var visualizacion = new Visualizacion { IdVisualizacion = 2, Nombre = "Vis2" };
            serviceMock.Setup(s => s.GetVisualizacionByIdAsync(2, "otheruser")).ReturnsAsync(visualizacion);

            var controller = GetController(serviceMock, null, GetUser("otheruser"));
            var result = await controller.GetVisualizacionById(2);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(visualizacion, ok.Value);
        }

        [Fact]
        public async Task GetVisualizacionById_ReturnsNotFound_WhenUserIsNull()
        {
            var serviceMock = new Mock<IVisualizacionService>();
            serviceMock.Setup(s => s.GetVisualizacionByIdAsync(1, null)).ReturnsAsync((Visualizacion)null);

            var controller = GetController(serviceMock, null, new ClaimsPrincipal());
            var result = await controller.GetVisualizacionById(1);

            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Contains("No se encontró", notFound.Value.ToString());
        }

        [Fact]
        public async Task GetVisualizacionById_ReturnsOk_WhenVisualizacionHasDatasets()
        {
            var serviceMock = new Mock<IVisualizacionService>();
            var visualizacion = new Visualizacion
            {
                IdVisualizacion = 3,
                Nombre = "Vis3",
                GrupoDatasets = new List<GrupoDataset>
                {
                    new GrupoDataset { DatasetId = 1, JsonDesign = "{}" }
                }
            };
            serviceMock.Setup(s => s.GetVisualizacionByIdAsync(3, "testuser")).ReturnsAsync(visualizacion);

            var controller = GetController(serviceMock, null, GetUser());
            var result = await controller.GetVisualizacionById(3);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(visualizacion, ok.Value);
        }

        /* Tests sobre GetVisualizacionByIdSinToken */

        [Fact]
        public async Task GetVisualizacionByIdSinToken_ReturnsOk_WhenFound()
        {
            var serviceMock = new Mock<IVisualizacionService>();
            var visualizacion = new Visualizacion { IdVisualizacion = 1, Nombre = "Vis1" };
            serviceMock.Setup(s => s.GetVisualizacionByIdAsyncSinToken(1)).ReturnsAsync(visualizacion);
            var controller = GetController(serviceMock);
            var result = await controller.GetVisualizacionByIdSinToken(1);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(visualizacion, ok.Value);
        }

        [Fact]
        public async Task GetVisualizacionByIdSinToken_ReturnsNotFound_WhenNotFound()
        {
            var serviceMock = new Mock<IVisualizacionService>();
            serviceMock.Setup(s => s.GetVisualizacionByIdAsyncSinToken(1)).ReturnsAsync((Visualizacion)null);
            var controller = GetController(serviceMock);
            var result = await controller.GetVisualizacionByIdSinToken(1);
            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Contains("No se encontró", notFound.Value.ToString());
        }

        [Fact]
        public async Task GetVisualizacionByIdSinToken_ReturnsServerError_OnException()
        {
            var serviceMock = new Mock<IVisualizacionService>();
            serviceMock.Setup(s => s.GetVisualizacionByIdAsyncSinToken(It.IsAny<int>()))
                .ThrowsAsync(new Exception("DB error"));
            var controller = GetController(serviceMock);
            var result = await controller.GetVisualizacionByIdSinToken(1);
            var serverError = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, serverError.StatusCode);
        }

        [Fact]
        public async Task GetVisualizacionByIdSinToken_ReturnsOk_WhenVisualizacionHasDatasets()
        {
            var serviceMock = new Mock<IVisualizacionService>();
            var visualizacion = new Visualizacion
            {
                IdVisualizacion = 3,
                Nombre = "Vis3",
                GrupoDatasets = new List<GrupoDataset>
                {
                    new GrupoDataset { DatasetId = 1, JsonDesign = "{}" }
                }
            };
            serviceMock.Setup(s => s.GetVisualizacionByIdAsyncSinToken(3)).ReturnsAsync(visualizacion);
            var controller = GetController(serviceMock);
            var result = await controller.GetVisualizacionByIdSinToken(3);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(visualizacion, ok.Value);
        }

        /* Tests sobre DeleteVisualizacion */

        [Fact]
        public async Task DeleteVisualizacion_ReturnsNoContent_WhenDeleted()
        {
            var dbName = Guid.NewGuid().ToString();
            var provider = BuildServiceProviderWithInMemoryDb(dbName);
            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            SeedVisualizacion(db, 1);

            var serviceMock = new Mock<IVisualizacionService>();
            var authMock = new Mock<ISondaAuthService>();
            var controller = new VisualizacionController(serviceMock.Object, authMock.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = GetUser() }
            };
            controller.HttpContext.RequestServices = provider;

            var result = await controller.DeleteVisualizacion(1);
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteVisualizacion_ReturnsUnauthorized_WhenUserIsNull()
        {
            var serviceMock = new Mock<IVisualizacionService>();
            var authMock = new Mock<ISondaAuthService>();
            var controller = new VisualizacionController(serviceMock.Object, authMock.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
            };

            var result = await controller.DeleteVisualizacion(1);
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Contains("Token inválido", unauthorized.Value.ToString());
        }

        [Fact]
        public async Task DeleteVisualizacion_ReturnsNotFound_WhenVisualizacionNotExists()
        {
            var dbName = Guid.NewGuid().ToString();
            var provider = BuildServiceProviderWithInMemoryDb(dbName);
            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var serviceMock = new Mock<IVisualizacionService>();
            var authMock = new Mock<ISondaAuthService>();
            var controller = new VisualizacionController(serviceMock.Object, authMock.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = GetUser() }
            };
            controller.HttpContext.RequestServices = provider;

            var result = await controller.DeleteVisualizacion(99);
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("No se encontró", notFound.Value.ToString());
        }

        [Fact]
        public async Task DeleteVisualizacion_RemovesGrupoVisualizacionAndGrupoDataset()
        {
            var dbName = Guid.NewGuid().ToString();
            var provider = BuildServiceProviderWithInMemoryDb(dbName);
            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            SeedVisualizacion(db, 2);

            db.GrupoVisualizaciones.Add(new GrupoVisualizacion { IdVisualizacion = 2 });
            db.SaveChanges();

            var serviceMock = new Mock<IVisualizacionService>();
            var authMock = new Mock<ISondaAuthService>();
            var controller = new VisualizacionController(serviceMock.Object, authMock.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = GetUser() }
            };
            controller.HttpContext.RequestServices = provider;

            var result = await controller.DeleteVisualizacion(2);
            Assert.IsType<NoContentResult>(result);

            Assert.Empty(db.GrupoVisualizaciones);
            Assert.Empty(db.GrupoDatasets);
            Assert.Empty(db.Visualizaciones);
        }

        [Fact]
        public async Task DeleteVisualizacion_ReturnsServerError_OnException()
        {
            var dbName = Guid.NewGuid().ToString();
            var provider = BuildServiceProviderWithInMemoryDb(dbName);
            using var scope = provider.CreateScope();

            var serviceMock = new Mock<IVisualizacionService>();
            var authMock = new Mock<ISondaAuthService>();
            var controller = new VisualizacionController(serviceMock.Object, authMock.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = GetUser() }
            };
            controller.HttpContext.RequestServices = provider;

            // Simular excepción lanzando desde el scope
            controller.HttpContext.RequestServices = new ServiceCollection().BuildServiceProvider();

            var result = await controller.DeleteVisualizacion(1);
            var serverError = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, serverError.StatusCode);
        }

        [Fact]
        public async Task DeleteVisualizacion_DeletesWithMultipleDatasets()
        {
            var dbName = Guid.NewGuid().ToString();
            var provider = BuildServiceProviderWithInMemoryDb(dbName);
            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var vis = new Visualizacion
            {
                IdVisualizacion = 3,
                Nombre = "Vis3",
                Username = "testuser",
                JsonDesign = "{}",
                GrupoDatasets = new List<GrupoDataset>
                {
                    new GrupoDataset { DatasetId = 1, JsonDesign = "{}" },
                    new GrupoDataset { DatasetId = 2, JsonDesign = "{}" }
                }
            };
            db.Visualizaciones.Add(vis);
            db.SaveChanges();

            var serviceMock = new Mock<IVisualizacionService>();
            var authMock = new Mock<ISondaAuthService>();
            var controller = new VisualizacionController(serviceMock.Object, authMock.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = GetUser() }
            };
            controller.HttpContext.RequestServices = provider;

            var result = await controller.DeleteVisualizacion(3);
            Assert.IsType<NoContentResult>(result);
            Assert.Empty(db.Visualizaciones);
        }

        /* Tests sobre getVisualizationData */

        [Fact]
        public async Task GetVisualizationData_ReturnsOk_WhenValid()
        {
            var serviceMock = new Mock<IVisualizacionService>();
            var request = new VisualizationRequest { column = "col1" };
            var response = new VisualizationResponse { Type = "int", Values = new List<VisualizationValue>() };
            serviceMock.Setup(s => s.GetVisualizationDataAsync(request, "testuser")).ReturnsAsync(response);

            var controller = GetController(serviceMock, null, GetUser());
            var result = await controller.GetVisualizationData(request);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(response, ok.Value);
        }

        [Fact]
        public async Task GetVisualizationData_ReturnsServerError_OnException()
        {
            var serviceMock = new Mock<IVisualizacionService>();
            var request = new VisualizationRequest { column = "col1" };
            serviceMock.Setup(s => s.GetVisualizationDataAsync(request, "testuser"))
                .ThrowsAsync(new Exception("DB error"));

            var controller = GetController(serviceMock, null, GetUser());
            var result = await controller.GetVisualizationData(request);

            var serverError = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, serverError.StatusCode);
        }

        [Fact]
        public async Task GetVisualizationData_ReturnsOk_EmptyValues()
        {
            var serviceMock = new Mock<IVisualizacionService>();
            var request = new VisualizationRequest { column = "col1" };
            var response = new VisualizationResponse { Type = "unknown", Values = new List<VisualizationValue>() };
            serviceMock.Setup(s => s.GetVisualizationDataAsync(request, "testuser")).ReturnsAsync(response);

            var controller = GetController(serviceMock, null, GetUser());
            var result = await controller.GetVisualizationData(request);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(response, ok.Value);
        }

        [Fact]
        public async Task GetVisualizationData_ReturnsOk_WithValues()
        {
            var serviceMock = new Mock<IVisualizacionService>();
            var request = new VisualizationRequest { column = "col1" };
            var response = new VisualizationResponse
            {
                Type = "string",
                Values = new List<VisualizationValue>
                {
                    new VisualizationValue { Name = "A", Value = 1 },
                    new VisualizationValue { Name = "B", Value = 2 }
                }
            };
            serviceMock.Setup(s => s.GetVisualizationDataAsync(request, "testuser")).ReturnsAsync(response);

            var controller = GetController(serviceMock, null, GetUser());
            var result = await controller.GetVisualizationData(request);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(response, ok.Value);
        }

        [Fact]
        public async Task GetVisualizationData_ReturnsOk_WhenUserIsNull()
        {
            var serviceMock = new Mock<IVisualizacionService>();
            var request = new VisualizationRequest { column = "col1" };
            var response = new VisualizationResponse { Type = "int", Values = new List<VisualizationValue>() };
            serviceMock.Setup(s => s.GetVisualizationDataAsync(request, null)).ReturnsAsync(response);

            var controller = GetController(serviceMock, null, new ClaimsPrincipal());
            var result = await controller.GetVisualizationData(request);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(response, ok.Value);
        }

        [Fact]
        public async Task GetVisualizationData_ReturnsOk_WithComplexValues()
        {
            var serviceMock = new Mock<IVisualizacionService>();
            var request = new VisualizationRequest { column = "col1" };
            var response = new VisualizationResponse
            {
                Type = "object",
                Values = new List<VisualizationValue>
                {
                    new VisualizationValue { Name = "X", Value = 10 },
                    new VisualizationValue { Name = "Y", Value = 20 }
                }
            };
            serviceMock.Setup(s => s.GetVisualizationDataAsync(request, "testuser")).ReturnsAsync(response);

            var controller = GetController(serviceMock, null, GetUser());
            var result = await controller.GetVisualizationData(request);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(response, ok.Value);
        }

        /* Tests sobre getVisualizationDataSinToken */

        [Fact]
        public async Task GetVisualizationDataSinToken_ReturnsOk_WhenValid()
        {
            var serviceMock = new Mock<IVisualizacionService>();
            var request = new VisualizationRequest { column = "col1" };
            var response = new VisualizationResponse { Type = "int", Values = new List<VisualizationValue>() };
            serviceMock.Setup(s => s.GetVisualizationDataSinTokenAsync(request)).ReturnsAsync(response);

            var controller = GetController(serviceMock);
            var result = await controller.GetVisualizationDataSinToken(request);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(response, ok.Value);
        }

        [Fact]
        public async Task GetVisualizationDataSinToken_ReturnsServerError_OnException()
        {
            var serviceMock = new Mock<IVisualizacionService>();
            var request = new VisualizationRequest { column = "col1" };
            serviceMock.Setup(s => s.GetVisualizationDataSinTokenAsync(request))
                .ThrowsAsync(new Exception("DB error"));

            var controller = GetController(serviceMock);
            var result = await controller.GetVisualizationDataSinToken(request);

            var serverError = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, serverError.StatusCode);
        }

        [Fact]
        public async Task GetVisualizationDataSinToken_ReturnsOk_EmptyValues()
        {
            var serviceMock = new Mock<IVisualizacionService>();
            var request = new VisualizationRequest { column = "col1" };
            var response = new VisualizationResponse { Type = "unknown", Values = new List<VisualizationValue>() };
            serviceMock.Setup(s => s.GetVisualizationDataSinTokenAsync(request)).ReturnsAsync(response);

            var controller = GetController(serviceMock);
            var result = await controller.GetVisualizationDataSinToken(request);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(response, ok.Value);
        }

        [Fact]
        public async Task GetVisualizationDataSinToken_ReturnsOk_WithValues()
        {
            var serviceMock = new Mock<IVisualizacionService>();
            var request = new VisualizationRequest { column = "col1" };
            var response = new VisualizationResponse
            {
                Type = "string",
                Values = new List<VisualizationValue>
                {
                    new VisualizationValue { Name = "A", Value = 1 },
                    new VisualizationValue { Name = "B", Value = 2 }
                }
            };
            serviceMock.Setup(s => s.GetVisualizationDataSinTokenAsync(request)).ReturnsAsync(response);

            var controller = GetController(serviceMock);
            var result = await controller.GetVisualizationDataSinToken(request);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(response, ok.Value);
        }

        [Fact]
        public async Task GetVisualizationDataSinToken_ReturnsOk_WithComplexValues()
        {
            var serviceMock = new Mock<IVisualizacionService>();
            var request = new VisualizationRequest { column = "col1" };
            var response = new VisualizationResponse
            {
                Type = "object",
                Values = new List<VisualizationValue>
                {
                    new VisualizationValue { Name = "X", Value = 10 },
                    new VisualizationValue { Name = "Y", Value = 20 }
                }
            };
            serviceMock.Setup(s => s.GetVisualizationDataSinTokenAsync(request)).ReturnsAsync(response);

            var controller = GetController(serviceMock);
            var result = await controller.GetVisualizationDataSinToken(request);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(response, ok.Value);
        }

        [Fact]
        public async Task GetVisualizationDataSinToken_ReturnsOk_WithNullRequest()
        {
            var serviceMock = new Mock<IVisualizacionService>();
            var response = new VisualizationResponse { Type = "null", Values = new List<VisualizationValue>() };
            serviceMock.Setup(s => s.GetVisualizationDataSinTokenAsync(null)).ReturnsAsync(response);

            var controller = GetController(serviceMock);
            var result = await controller.GetVisualizationDataSinToken(null);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(response, ok.Value);
        }

        /* Tests sobre UpdateVisualizacionLink */

        [Fact]
        public async Task UpdateVisualizacionLink_ReturnsOk_WhenUpdated()
        {
            var dbName = Guid.NewGuid().ToString();
            var provider = BuildServiceProviderWithInMemoryDb(dbName);
            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            SeedVisualizacion(db, 10);

            var serviceMock = new Mock<IVisualizacionService>();
            var authMock = new Mock<ISondaAuthService>();
            var controller = GetController(serviceMock, authMock, GetUser());
            controller.HttpContext.RequestServices = provider;

            var result = await controller.UpdateVisualizacionLink(10, "http://newlink.com");
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("Link actualizado", ok.Value.ToString());
        }

        [Fact]
        public async Task UpdateVisualizacionLink_ReturnsUnauthorized_WhenUserIsNull()
        {
            var serviceMock = new Mock<IVisualizacionService>();
            var authMock = new Mock<ISondaAuthService>();
            var controller = GetController(serviceMock, authMock);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
            };
            var result = await controller.UpdateVisualizacionLink(1, "http://newlink.com");
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Contains("Token inválido", unauthorized.Value.ToString());
        }

        [Fact]
        public async Task UpdateVisualizacionLink_ReturnsNotFound_WhenVisualizacionNotExists()
        {
            var dbName = Guid.NewGuid().ToString();
            var provider = BuildServiceProviderWithInMemoryDb(dbName);
            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var serviceMock = new Mock<IVisualizacionService>();
            var authMock = new Mock<ISondaAuthService>();
            var controller = GetController(serviceMock, authMock, GetUser());
            controller.HttpContext.RequestServices = provider;

            var result = await controller.UpdateVisualizacionLink(99, "http://newlink.com");
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("No se encontró", notFound.Value.ToString());
        }

        [Fact]
        public async Task UpdateVisualizacionLink_UpdatesLinkCorrectly()
        {
            var dbName = Guid.NewGuid().ToString();
            var provider = BuildServiceProviderWithInMemoryDb(dbName);
            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            SeedVisualizacion(db, 20);

            var serviceMock = new Mock<IVisualizacionService>();
            var authMock = new Mock<ISondaAuthService>();
            var controller = GetController(serviceMock, authMock, GetUser());
            controller.HttpContext.RequestServices = provider;


            var result = await controller.UpdateVisualizacionLink(20, "http://updated.com");
            var ok = Assert.IsType<OkObjectResult>(result);

            using var verifyScope = provider.CreateScope();
            var dbVerify = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var vis = await dbVerify.Visualizaciones.FirstOrDefaultAsync(v => v.IdVisualizacion == 20);
            Assert.Equal("http://updated.com", vis.Link);
        }

        [Fact]
        public async Task UpdateVisualizacionLink_ReturnsServerError_OnException()
        {
            var dbName = Guid.NewGuid().ToString();
            var provider = BuildServiceProviderWithInMemoryDb(dbName);
            using var scope = provider.CreateScope();

            var serviceMock = new Mock<IVisualizacionService>();
            var authMock = new Mock<ISondaAuthService>();
            var controller = GetController(serviceMock, authMock, GetUser());
            controller.HttpContext.RequestServices = new ServiceCollection().BuildServiceProvider();

            var result = await controller.UpdateVisualizacionLink(1, "http://fail.com");
            var serverError = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, serverError.StatusCode);
        }

        [Fact]
        public async Task UpdateVisualizacionLink_UpdatesLinkToNull()
        {
            var dbName = Guid.NewGuid().ToString();
            var provider = BuildServiceProviderWithInMemoryDb(dbName);
            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            SeedVisualizacion(db, 30);

            var serviceMock = new Mock<IVisualizacionService>();
            var authMock = new Mock<ISondaAuthService>();
            var controller = GetController(serviceMock, authMock, GetUser());
            controller.HttpContext.RequestServices = provider;

            var result = await controller.UpdateVisualizacionLink(30, null);
            var ok = Assert.IsType<OkObjectResult>(result);

            var vis = await db.Visualizaciones.FirstOrDefaultAsync(v => v.IdVisualizacion == 30);
            Assert.Null(vis.Link);
        }

        /* Tests sobre EditVisualizacion */


        [Fact]
        public async Task EditVisualizacion_ReturnsOk_WhenValid()
        {
            var dbName = Guid.NewGuid().ToString();
            var provider = BuildServiceProviderWithInMemoryDb(dbName);
            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var Dataset1 = new Datasets { Id = 1, NameDataset = "Dataset1", Username = "testuser" };
            db.Datasets.Add(Dataset1);
            db.SaveChanges();
            SeedVisualizacion(db, 100);

            var serviceMock = new Mock<IVisualizacionService>();
            var authMock = new Mock<ISondaAuthService>();
            var controller = GetController(serviceMock, authMock, GetUser());
            controller.HttpContext.RequestServices = provider;

            var request = new CreateVisualizacionRequest
            {
                Nombre = "Editada",
                FechaDesde = DateTime.Today,
                FechaHasta = DateTime.Today.AddDays(1),
                JsonDiseñoGeneral = "{}",
                Datasets = new List<DatasetConfig>
                {
                    new DatasetConfig { DatasetId = 1, JsonDiseño = "{}" }
                }
            };

            var result = await controller.EditVisualizacion(100, request);
            var ok = Assert.IsType<OkObjectResult>(result);
            using var verifyScope = provider.CreateScope();
            var dbVerify = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var vis = await dbVerify.Visualizaciones.FirstOrDefaultAsync(v => v.IdVisualizacion == 100);
            Assert.Equal("Editada", vis.Nombre);
        }

        [Fact]
        public async Task EditVisualizacion_ReturnsUnauthorized_WhenUserIsNull()
        {
            var dbName = Guid.NewGuid().ToString();
            var provider = BuildServiceProviderWithInMemoryDb(dbName);

            var serviceMock = new Mock<IVisualizacionService>();
            var authMock = new Mock<ISondaAuthService>();
            var controller = GetController(serviceMock, authMock);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
            };
            controller.HttpContext.RequestServices = provider;

            var request = new CreateVisualizacionRequest { Nombre = "Editada" };
            var result = await controller.EditVisualizacion(100, request);
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Contains("Token inválido", unauthorized.Value.ToString());
        }

        [Fact]
        public async Task EditVisualizacion_ReturnsBadRequest_WhenModelStateInvalid()
        {
            var dbName = Guid.NewGuid().ToString();
            var provider = BuildServiceProviderWithInMemoryDb(dbName);

            var serviceMock = new Mock<IVisualizacionService>();
            var authMock = new Mock<ISondaAuthService>();
            var controller = GetController(serviceMock, authMock, GetUser());
            controller.HttpContext.RequestServices = provider;
            controller.ModelState.AddModelError("Nombre", "Required");

            var request = new CreateVisualizacionRequest();
            var result = await controller.EditVisualizacion(100, request);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task EditVisualizacion_ReturnsNotFound_WhenVisualizacionNotExists()
        {
            var dbName = Guid.NewGuid().ToString();
            var provider = BuildServiceProviderWithInMemoryDb(dbName);

            var serviceMock = new Mock<IVisualizacionService>();
            var authMock = new Mock<ISondaAuthService>();
            var controller = GetController(serviceMock, authMock, GetUser());
            controller.HttpContext.RequestServices = provider;

            var request = new CreateVisualizacionRequest { Nombre = "Editada" };
            var result = await controller.EditVisualizacion(999, request);
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("No se encontró", notFound.Value.ToString());
        }

        [Fact]
        public async Task EditVisualizacion_ReturnsBadRequest_WhenFechasInvalidas()
        {
            var dbName = Guid.NewGuid().ToString();
            var provider = BuildServiceProviderWithInMemoryDb(dbName);
            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            SeedVisualizacion(db, 103);

            var serviceMock = new Mock<IVisualizacionService>();
            var authMock = new Mock<ISondaAuthService>();
            var controller = GetController(serviceMock, authMock, GetUser());
            controller.HttpContext.RequestServices = provider;

            var request = new CreateVisualizacionRequest
            {
                Nombre = "Editada",
                FechaDesde = DateTime.Today.AddDays(2),
                FechaHasta = DateTime.Today.AddDays(1)
            };
            var result = await controller.EditVisualizacion(103, request);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("fecha de inicio", badRequest.Value.ToString());
        }

        [Fact]
        public async Task EditVisualizacion_ReturnsServerError_OnException()
        {
            var dbName = Guid.NewGuid().ToString();
            var provider = BuildServiceProviderWithInMemoryDb(dbName);

            var serviceMock = new Mock<IVisualizacionService>();
            var authMock = new Mock<ISondaAuthService>();
            var controller = GetController(serviceMock, authMock, GetUser());
            controller.HttpContext.RequestServices = new ServiceCollection().BuildServiceProvider();

            var request = new CreateVisualizacionRequest { Nombre = "Editada" };
            var result = await controller.EditVisualizacion(100, request);
            var serverError = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, serverError.StatusCode);
        }

        [Fact]
        public async Task EditVisualizacion_ReturnBadRequest_WhenDatasetNotExist() 
        {
            var dbName = Guid.NewGuid().ToString();
            var provider = BuildServiceProviderWithInMemoryDb(dbName);
            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            SeedVisualizacion(db, 104);
            var serviceMock = new Mock<IVisualizacionService>();
            var authMock = new Mock<ISondaAuthService>();
            var controller = GetController(serviceMock, authMock, GetUser());
            controller.HttpContext.RequestServices = provider;
            var request = new CreateVisualizacionRequest
            {
                Nombre = "Editada",
                FechaDesde = DateTime.Today,
                FechaHasta = DateTime.Today.AddDays(1),
                JsonDiseñoGeneral = "{}",
                Datasets = new List<DatasetConfig>
                {
                    new DatasetConfig { DatasetId = 999, JsonDiseño = "{}" }
                }
            };
            var result = await controller.EditVisualizacion(104, request);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("no existe", badRequest.Value.ToString());
        }

        [Fact]
        public async Task EditVisualizacion_ReturnBadRequest_WhenNameIsRepeat()
        {
            var dbName = Guid.NewGuid().ToString();
            var provider = BuildServiceProviderWithInMemoryDb(dbName);
            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            SeedVisualizacion(db, 105);
            db.Visualizaciones.Add(new Visualizacion
            {
                IdVisualizacion = 200,
                Nombre = "NombreExistente",
                Username = "testuser",
                JsonDesign = "{}"
            });
            db.SaveChanges();
            var serviceMock = new Mock<IVisualizacionService>();
            var authMock = new Mock<ISondaAuthService>();
            var controller = GetController(serviceMock, authMock, GetUser());
            controller.HttpContext.RequestServices = provider;
            var request = new CreateVisualizacionRequest
            {
                Nombre = "NombreExistente", // Nombre que ya existe
                FechaDesde = DateTime.Today,
                FechaHasta = DateTime.Today.AddDays(1),
                JsonDiseñoGeneral = "{}",
                Datasets = new List<DatasetConfig>
                {
                    new DatasetConfig { DatasetId = 1, JsonDiseño = "{}" }
                }
            };
            var result = await controller.EditVisualizacion(105, request);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}
