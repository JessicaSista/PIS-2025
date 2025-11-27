using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Moq;

using OmniMonitor.Client.Shared;
using OmniMonitor.Server.Context;

using OmniMonitor.Server.Controllers;
using OmniMonitor.Server.Models;
using OmniMonitor.Server.Services;
using OmniMonitor.Shared.Dtos;

using Org.BouncyCastle.Asn1.Crmf;

using Xunit;

namespace QA.Tests
{
    public class DashboardControllerTests
    {
        private ClaimsPrincipal GetUser(string username = "testuser")
        {
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, username) }, "TestAuth");
            return new ClaimsPrincipal(identity);
        }

        private DashboardController GetController(
            Mock<IDashboardService>? dashboardServiceMock = null,
            Mock<ISondaAuthService>? authMock = null,
            ClaimsPrincipal? user = null)
        {
            dashboardServiceMock ??= new Mock<IDashboardService>();
            authMock ??= new Mock<ISondaAuthService>();
            var controller = new DashboardController(dashboardServiceMock.Object, authMock.Object);
            if (user != null)
            {
                controller.ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = user }
                };
            }
            return controller;
        }

        private ApplicationDbContext BuildInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new ApplicationDbContext(options);
        }

        /* Tests sobre CreateDashboard */

        [Fact]
        public async Task CreateDashboard_ReturnsCreated_WhenValid()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            var request = new CreateDashboardRequest { Nombre = "Dash1" };
            var response = new DashboardResponse { IdDashboard = 1, Nombre = "Dash1", Username = "testuser" };
            dashboardServiceMock.Setup(s => s.CreateDashboardAsync(request, "testuser")).ReturnsAsync(response);

            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.CreateDashboard(request);

            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(response, created.Value);
        }

        [Fact]
        public async Task CreateDashboard_ReturnsBadRequest_WhenModelStateInvalid()
        {
            var controller = GetController(user: GetUser());
            controller.ModelState.AddModelError("Nombre", "Required");

            var result = await controller.CreateDashboard(new CreateDashboardRequest());

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task CreateDashboard_ReturnsBadRequest_OnArgumentException()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            dashboardServiceMock.Setup(s => s.CreateDashboardAsync(It.IsAny<CreateDashboardRequest>(), It.IsAny<string>()))
                .ThrowsAsync(new ArgumentException("Nombre duplicado"));

            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.CreateDashboard(new CreateDashboardRequest { Nombre = "Dash1" });

            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("Nombre duplicado", badRequest.Value.ToString());
        }

        [Fact]
        public async Task CreateDashboard_ReturnsServerError_OnException()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            dashboardServiceMock.Setup(s => s.CreateDashboardAsync(It.IsAny<CreateDashboardRequest>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("DB error"));

            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.CreateDashboard(new CreateDashboardRequest { Nombre = "Dash1" });

            var serverError = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, serverError.StatusCode);
            Assert.Contains("DB error", serverError.Value.ToString());
        }

        [Fact]
        public async Task CreateDashboard_ReturnsCreated_WithCorrectLocation()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            var request = new CreateDashboardRequest { Nombre = "Dash2" };
            var response = new DashboardResponse { IdDashboard = 2, Nombre = "Dash2", Username = "testuser" };
            dashboardServiceMock.Setup(s => s.CreateDashboardAsync(request, "testuser")).ReturnsAsync(response);

            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.CreateDashboard(request);

            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(nameof(controller.GetDashboard), created.ActionName);
            Assert.Equal(response.IdDashboard, ((DashboardResponse)created.Value).IdDashboard);
        }

        [Fact]
        public async Task CreateDashboard_ReturnsBadRequest_WhenUserIsNull()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            var controller = GetController(dashboardServiceMock, null, new ClaimsPrincipal());

            dashboardServiceMock.Setup(s => s.CreateDashboardAsync(It.IsAny<CreateDashboardRequest>(), null))
                .ThrowsAsync(new ArgumentException("Usuario requerido"));

            var result = await controller.CreateDashboard(new CreateDashboardRequest { Nombre = "Dash3" });
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        /* Tests sobre GetDashboard */

        [Fact]
        public async Task GetDashboard_ReturnsOk_WhenFound()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            var response = new DashboardResponse { IdDashboard = 1, Nombre = "Dash1" };
            dashboardServiceMock.Setup(s => s.GetDashboardByIdAsync(1, "testuser")).ReturnsAsync(response);

            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.GetDashboard(1);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(response, ok.Value);
        }

        [Fact]
        public async Task GetDashboard_ReturnsNotFound_WhenNotFound()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            dashboardServiceMock.Setup(s => s.GetDashboardByIdAsync(1, "testuser")).ReturnsAsync((DashboardResponse)null);

            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.GetDashboard(1);

            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Contains("No se encontró", notFound.Value.ToString());
        }

        [Fact]
        public async Task GetDashboard_ReturnsServerError_OnException()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            dashboardServiceMock.Setup(s => s.GetDashboardByIdAsync(1, "testuser"))
                .ThrowsAsync(new Exception("Error interno"));

            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.GetDashboard(1);

            var serverError = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, serverError.StatusCode);
        }

        [Fact]
        public async Task GetDashboard_ReturnsOk_WithMultipleCards()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            var response = new DashboardResponse
            {
                IdDashboard = 2,
                Nombre = "Dash2",
                Tarjetas = new List<DashboardCardResponse>
                {
                    new DashboardCardResponse { CardId = 1, TipoCard = 1 },
                    new DashboardCardResponse { CardId = 2, TipoCard = 2 }
                }
            };
            dashboardServiceMock.Setup(s => s.GetDashboardByIdAsync(2, "testuser")).ReturnsAsync(response);

            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.GetDashboard(2);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(response, ok.Value);
        }

        [Fact]
        public async Task GetDashboard_ReturnsOk_WhenUserIsNull()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            dashboardServiceMock.Setup(s => s.GetDashboardByIdAsync(1, null)).ReturnsAsync((DashboardResponse)null);

            var controller = GetController(dashboardServiceMock, null, new ClaimsPrincipal());
            var result = await controller.GetDashboard(1);

            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetDashboard_ReturnsOk_WhenDashboardHasLayout()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            var response = new DashboardResponse
            {
                IdDashboard = 3,
                Nombre = "Dash3",
                Layout = new DashboardLayout()
            };
            dashboardServiceMock.Setup(s => s.GetDashboardByIdAsync(3, "testuser")).ReturnsAsync(response);

            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.GetDashboard(3);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(response, ok.Value);
        }

        /* Tests sobre GetDashboardSinToken */
        [Fact]
        public async Task GetDashboardSinToken_ReturnsOk_WhenFound()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            var response = new DashboardResponse { IdDashboard = 1, Nombre = "Dash1" };
            dashboardServiceMock.Setup(s => s.GetDashboardByIdAsyncSinToken(1)).ReturnsAsync(response);

            var controller = GetController(dashboardServiceMock);
            var result = await controller.GetDashboardSinToken(1);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(response, ok.Value);
        }

        [Fact]
        public async Task GetDashboardSinToken_ReturnsNotFound_WhenNotFound()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            dashboardServiceMock.Setup(s => s.GetDashboardByIdAsyncSinToken(1)).ReturnsAsync((DashboardResponse)null);

            var controller = GetController(dashboardServiceMock);
            var result = await controller.GetDashboardSinToken(1);

            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Contains("No se encontró", notFound.Value.ToString());
        }

        [Fact]
        public async Task GetDashboardSinToken_ReturnsServerError_OnException()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            dashboardServiceMock.Setup(s => s.GetDashboardByIdAsyncSinToken(1))
                .ThrowsAsync(new Exception("Error interno"));

            var controller = GetController(dashboardServiceMock);
            var result = await controller.GetDashboardSinToken(1);

            var serverError = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, serverError.StatusCode);
        }

        [Fact]
        public async Task GetDashboardSinToken_ReturnsOk_WithMultipleCards()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            var response = new DashboardResponse
            {
                IdDashboard = 2,
                Nombre = "Dash2",
                Tarjetas = new List<DashboardCardResponse>
                {
                    new DashboardCardResponse { CardId = 1, TipoCard = 1 },
                    new DashboardCardResponse { CardId = 2, TipoCard = 2 }
                }
            };
            dashboardServiceMock.Setup(s => s.GetDashboardByIdAsyncSinToken(2)).ReturnsAsync(response);

            var controller = GetController(dashboardServiceMock);
            var result = await controller.GetDashboardSinToken(2);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(response, ok.Value);
        }

        [Fact]
        public async Task GetDashboardSinToken_ReturnsOk_WhenDashboardHasLayout()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            var response = new DashboardResponse
            {
                IdDashboard = 3,
                Nombre = "Dash3",
                Layout = new DashboardLayout()
            };
            dashboardServiceMock.Setup(s => s.GetDashboardByIdAsyncSinToken(3)).ReturnsAsync(response);

            var controller = GetController(dashboardServiceMock);
            var result = await controller.GetDashboardSinToken(3);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(response, ok.Value);
        }

        [Fact]
        public async Task GetDashboardSinToken_ReturnsOk_WhenDashboardIsEmpty()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            var response = new DashboardResponse { IdDashboard = 4, Nombre = "EmptyDash" };
            dashboardServiceMock.Setup(s => s.GetDashboardByIdAsyncSinToken(4)).ReturnsAsync(response);

            var controller = GetController(dashboardServiceMock);
            var result = await controller.GetDashboardSinToken(4);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(response, ok.Value);
        }

        /* Tests sobre GetAllDashboards */
        [Fact]
        public async Task GetAllDashboards_ReturnsOk_WithDashboards()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            var dashboards = new List<DashboardSummaryResponse> { new DashboardSummaryResponse { IdDashboard = 1, Nombre = "Dash1" } };
            dashboardServiceMock.Setup(s => s.GetAllDashboardsAsync("testuser", null)).ReturnsAsync(dashboards);

            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.GetAllDashboards(null);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(dashboards, ok.Value);
        }

        [Fact]
        public async Task GetAllDashboards_ReturnsServerError_OnException()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            dashboardServiceMock.Setup(s => s.GetAllDashboardsAsync("testuser", null))
                .ThrowsAsync(new Exception("DB error"));

            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.GetAllDashboards(null);

            var serverError = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, serverError.StatusCode);
        }

        [Fact]
        public async Task GetAllDashboards_ReturnsOk_EmptyList()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            dashboardServiceMock.Setup(s => s.GetAllDashboardsAsync("testuser", null)).ReturnsAsync(new List<DashboardSummaryResponse>());

            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.GetAllDashboards(null);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Empty((List<DashboardSummaryResponse>)ok.Value);
        }

        [Fact]
        public async Task GetAllDashboards_ReturnsOk_WithQuery()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            var dashboards = new List<DashboardSummaryResponse> { new DashboardSummaryResponse { IdDashboard = 2, Nombre = "Dash2" } };
            dashboardServiceMock.Setup(s => s.GetAllDashboardsAsync("testuser", "Dash2")).ReturnsAsync(dashboards);

            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.GetAllDashboards("Dash2");

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(dashboards, ok.Value);
        }

        [Fact]
        public async Task GetAllDashboards_ReturnsOk_WhenUserIsNull()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            dashboardServiceMock.Setup(s => s.GetAllDashboardsAsync(null, null)).ReturnsAsync(new List<DashboardSummaryResponse>());

            var controller = GetController(dashboardServiceMock, null, new ClaimsPrincipal());
            var result = await controller.GetAllDashboards(null);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Empty((List<DashboardSummaryResponse>)ok.Value);
        }

        [Fact]
        public async Task GetAllDashboards_ReturnsOk_MultipleDashboards()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            var dashboards = new List<DashboardSummaryResponse>
            {
                new DashboardSummaryResponse { IdDashboard = 1, Nombre = "Dash1" },
                new DashboardSummaryResponse { IdDashboard = 2, Nombre = "Dash2" }
            };
            dashboardServiceMock.Setup(s => s.GetAllDashboardsAsync("testuser", null)).ReturnsAsync(dashboards);

            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.GetAllDashboards(null);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(dashboards, ok.Value);
        }

        /* Tests sobre GetAllDashboardsPaginated */
        [Fact]
        public async Task GetAllDashboardsPaginated_ReturnsOk_WithData()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            dashboardServiceMock.Setup(s => s.GetAllDashboardsPaginatedAsync("testuser", null, 1, 9))
                .ReturnsAsync(new List<DashboardSummaryResponse> { new DashboardSummaryResponse { IdDashboard = 1 } });
            dashboardServiceMock.Setup(s => s.GetDashboardsCount("testuser", null)).ReturnsAsync(1);

            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.GetAllDashboardsPaginated(1, 9, null);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task GetAllDashboardsPaginated_ReturnsBadRequest_WhenPageInvalid()
        {
            var controller = GetController(null, null, GetUser());
            var result = await controller.GetAllDashboardsPaginated(0, 9, null);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("mayores a 0", badRequest.Value.ToString());
        }

        [Fact]
        public async Task GetAllDashboardsPaginated_ReturnsServerError_OnException()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            dashboardServiceMock.Setup(s => s.GetAllDashboardsPaginatedAsync("testuser", null, 1, 9))
                .ThrowsAsync(new Exception("DB error"));

            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.GetAllDashboardsPaginated(1, 9, null);
            var serverError = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, serverError.StatusCode);
        }

        [Fact]
        public async Task GetAllDashboardsPaginated_ReturnsOk_Empty()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            dashboardServiceMock.Setup(s => s.GetAllDashboardsPaginatedAsync("testuser", null, 1, 9))
                .ReturnsAsync(new List<DashboardSummaryResponse>());
            dashboardServiceMock.Setup(s => s.GetDashboardsCount("testuser", null)).ReturnsAsync(0);

            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.GetAllDashboardsPaginated(1, 9, null);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task GetAllDashboardsPaginated_ReturnsOk_PageOutOfRange()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            dashboardServiceMock.Setup(s => s.GetAllDashboardsPaginatedAsync("testuser", null, 2, 9))
                .ReturnsAsync(new List<DashboardSummaryResponse>());
            dashboardServiceMock.Setup(s => s.GetDashboardsCount("testuser", null)).ReturnsAsync(1);

            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.GetAllDashboardsPaginated(2, 9, null);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task GetAllDashboardsPaginated_ReturnsOk_WithQuery()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            dashboardServiceMock.Setup(s => s.GetAllDashboardsPaginatedAsync("testuser", "Dash2", 1, 9))
                .ReturnsAsync(new List<DashboardSummaryResponse> { new DashboardSummaryResponse { IdDashboard = 2 } });
            dashboardServiceMock.Setup(s => s.GetDashboardsCount("testuser", "Dash2")).ReturnsAsync(1);

            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.GetAllDashboardsPaginated(1, 9, "Dash2");

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.NotNull(ok.Value);
        }

        /* Tests sobre ValidateCardIds */
        [Fact]
        public async Task ValidateCardIds_ReturnsOk_WhenValid()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            dashboardServiceMock.Setup(s => s.ValidateCardIdsAsync(It.IsAny<List<int>>())).ReturnsAsync(true);

            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.ValidateCardIds(new List<int> { 1, 2 });

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.True((bool)ok.Value.GetType().GetProperty("isValid")!.GetValue(ok.Value));
        }

        [Fact]
        public async Task ValidateCardIds_ReturnsBadRequest_WhenListIsNull()
        {
            var controller = GetController(null, null, GetUser());
            var result = await controller.ValidateCardIds(null);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("no puede estar vacía", badRequest.Value.ToString());
        }

        [Fact]
        public async Task ValidateCardIds_ReturnsBadRequest_WhenListIsEmpty()
        {
            var controller = GetController(null, null, GetUser());
            var result = await controller.ValidateCardIds(new List<int>());
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("no puede estar vacía", badRequest.Value.ToString());
        }

        [Fact]
        public async Task ValidateCardIds_ReturnsOk_WhenInvalid()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            dashboardServiceMock.Setup(s => s.ValidateCardIdsAsync(It.IsAny<List<int>>())).ReturnsAsync(false);

            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.ValidateCardIds(new List<int> { 99 });

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.False((bool)ok.Value.GetType().GetProperty("isValid")!.GetValue(ok.Value));
        }

        [Fact]
        public async Task ValidateCardIds_ReturnsServerError_OnException()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            dashboardServiceMock.Setup(s => s.ValidateCardIdsAsync(It.IsAny<List<int>>()))
                .ThrowsAsync(new Exception("Error interno"));

            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.ValidateCardIds(new List<int> { 1 });

            var serverError = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, serverError.StatusCode);
        }

        [Fact]
        public async Task ValidateCardIds_ReturnsOk_WithMixedIds()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            dashboardServiceMock.Setup(s => s.ValidateCardIdsAsync(It.IsAny<List<int>>())).ReturnsAsync(false);

            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.ValidateCardIds(new List<int> { 1, 99 });

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.False((bool)ok.Value.GetType().GetProperty("isValid")!.GetValue(ok.Value));
        }

        /* Tests sobre DeleteDashboard */

        [Fact]
        public async Task DeleteDashboard_ReturnsNoContent_WhenSuccessful()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            dashboardServiceMock.Setup(s => s.DeleteDashboardAsync(1, "testuser")).ReturnsAsync(true);
            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.DeleteDashboard(1);
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteDashboard_ReturnsNotFound_WhenDashboardDoesNotExist()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            dashboardServiceMock.Setup(s => s.DeleteDashboardAsync(1, "testuser")).ReturnsAsync(false);
            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.DeleteDashboard(1);
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("No se encontró", notFound.Value.ToString());
        }

        [Fact]
        public async Task DeleteDashboard_ReturnsServerError_OnException()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            dashboardServiceMock.Setup(s => s.DeleteDashboardAsync(1, "testuser"))
                .ThrowsAsync(new Exception("DB error"));
            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.DeleteDashboard(1);
            var serverError = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, serverError.StatusCode);
        }

        [Fact]
        public async Task DeleteDashboard_ReturnNoContext_AndRemoveDashboardFromDb() 
        {
            var dbName = Guid.NewGuid().ToString();
            using var context = BuildInMemoryContext(dbName);
            context.Dashboards.Add(new DashboardDto { IdDashboard = 1, Nombre = "Dash1", Username = "testuser" });
            await context.SaveChangesAsync();
            var dashboardServiceMock = new Mock<IDashboardService>();
            dashboardServiceMock.Setup(s => s.DeleteDashboardAsync(1, "testuser")).ReturnsAsync(true);
            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.DeleteDashboard(1);
            Assert.IsType<NoContentResult>(result);
            var dashboardInDb = await context.Dashboards.FindAsync(1);
            Assert.Null(dashboardInDb);
        }

        [Fact]
        public async Task DeleteDashboard_ReturnNotFound_WhenUserIsNull()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            dashboardServiceMock.Setup(s => s.DeleteDashboardAsync(1, null)).ReturnsAsync(false);
            var controller = GetController(dashboardServiceMock, null, new ClaimsPrincipal());
            var result = await controller.DeleteDashboard(1);
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("No se encontró", notFound.Value.ToString());
        }

        /* Tests sobre UpdateDashboardConfig */

        [Fact]
        public async Task UpdateDashboardConfig_ReturnsOk_WhenSuccessful()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            var response = new DashboardResponse { IdDashboard = 1, Nombre = "Dash1" };
            dashboardServiceMock.Setup(s => s.UpdateDashboardConfigAsync(1, "testuser", "{NewDesign}")).ReturnsAsync(true);
            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.UpdateDashboardConfig(1, "{NewDesign}");
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task UpdateDashboardConfig_ReturnsNotFound_WhenDashboardDoesNotExist()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            dashboardServiceMock.Setup(s => s.UpdateDashboardConfigAsync(1, "testuser", "{NewDesign}")).ReturnsAsync(false);
            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.UpdateDashboardConfig(1, "{NewDesign}");
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("No se encontró", notFound.Value.ToString());
        }

        [Fact]
        public async Task UpdateDashboardConfig_ReturnsServerError_OnException()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            dashboardServiceMock.Setup(s => s.UpdateDashboardConfigAsync(1, "testuser", "{NewDesign}"))
            .ThrowsAsync(new Exception("DB error"));
            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.UpdateDashboardConfig(1, "{NewDesign}");
            var serverError = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, serverError.StatusCode);
        }

        [Fact]
        public async Task UpdateDashboardConfig_ReturnsNotFound_WhenUserIsNull()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            dashboardServiceMock.Setup(s => s.UpdateDashboardConfigAsync(1, null, "{NewDesign}")).ReturnsAsync(false);
            var controller = GetController(dashboardServiceMock, null, new ClaimsPrincipal());
            var result = await controller.UpdateDashboardConfig(1, "{NewDesign}");
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("No se encontró", notFound.Value.ToString());
        }

        [Fact]
        public async Task UpdateDashboardConfig_ReturnUnauthorizedAccessException_WhenUnauthorizedUser() 
        {             
            var dashboardServiceMock = new Mock<IDashboardService>();
            dashboardServiceMock.Setup(s => s.UpdateDashboardConfigAsync(1, "testuser", "{NewDesign}"))
                .ThrowsAsync(new UnauthorizedAccessException("No autorizado"));
            var controller = GetController(dashboardServiceMock, null, GetUser());
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await controller.UpdateDashboardConfig(1, "{NewDesign}"));
        }

        /* Tests sobre AddDashboardCard */

        [Fact]
        public async Task AddDashboardCard_ReturnsCreated_WhenSuccessful()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            var request = new OmniMonitor.Shared.Dtos.DashboardCard { CardId = 1, TipoCard = 1 };
            var response = new DashboardCardResponse { CardId = 1, TipoCard = 1 };
            dashboardServiceMock.Setup(s => s.AddDashboardCardAsync(1, "testuser", "{}", request)).ReturnsAsync(true);
            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.AddDashboardCard(1, "{}", request);
            var created = Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task AddDashboardCard_ReturnsNotFound_WhenDashboardDoesNotExist()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            var request = new OmniMonitor.Shared.Dtos.DashboardCard { CardId = 1, TipoCard = 1 };
            dashboardServiceMock.Setup(s => s.AddDashboardCardAsync(1, "testuser", "{}", request)).ReturnsAsync(false);
            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.AddDashboardCard(1, "{}", request);
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("No se encontró", notFound.Value.ToString());
        }

        [Fact]
        public async Task AddDashboardCard_ReturnsServerError_OnException()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            var request = new OmniMonitor.Shared.Dtos.DashboardCard { CardId = 1, TipoCard = 1 };
            dashboardServiceMock.Setup(s => s.AddDashboardCardAsync(1, "testuser", "{}", request))
            .ThrowsAsync(new Exception("DB error"));
            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.AddDashboardCard(1, "{}", request);
            var serverError = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, serverError.StatusCode);
        }

        [Fact]
        public async Task AddDashboardCard_ReturnsNotFound_WhenUserIsNull()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            var request = new OmniMonitor.Shared.Dtos.DashboardCard { CardId = 1, TipoCard = 1 };
            dashboardServiceMock.Setup(s => s.AddDashboardCardAsync(1, null, "{}", request)).ReturnsAsync(false);
            var controller = GetController(dashboardServiceMock, null, new ClaimsPrincipal());
            var result = await controller.AddDashboardCard(1, "{}", request);
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("No se encontró", notFound.Value.ToString());
        }

        [Fact]
        public async Task AddDashboardCard_Return500_WhenUnauthorizedUser() 
        {             
            var dashboardServiceMock = new Mock<IDashboardService>();
            var request = new OmniMonitor.Shared.Dtos.DashboardCard { CardId = 1, TipoCard = 1 };
            dashboardServiceMock.Setup(s => s.AddDashboardCardAsync(1, "testuser", "{}", request))
                .ThrowsAsync(new UnauthorizedAccessException("No autorizado"));
            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.AddDashboardCard(1, "{}", request);
            var serverError = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, serverError.StatusCode);
        }

        /* Tests sobre ReorderDashboardCards */

        [Fact]
        public async Task ReorderDashboardCards_ReturnOk_WhenSuccessful() 
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            var request = new List<OmniMonitor.Shared.Dtos.DashboardCard>
            {
                new OmniMonitor.Shared.Dtos.DashboardCard { CardId = 1, TipoCard = 1 },
                new OmniMonitor.Shared.Dtos.DashboardCard { CardId = 2, TipoCard = 2 }

            };
            dashboardServiceMock.Setup(s => s.ReorderDashboardCardsAsync(1, "testuser", "{}", request)).ReturnsAsync(true);
            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.ReorderDashboardCards(1, "{}", request);
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task ReorderDashboardCards_ReturnsNotFound_WhenDashboardDoesNotExist()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            var request = new List<OmniMonitor.Shared.Dtos.DashboardCard>
            {
                new OmniMonitor.Shared.Dtos.DashboardCard { CardId = 1, TipoCard = 1 },
                new OmniMonitor.Shared.Dtos.DashboardCard { CardId = 2, TipoCard = 2 }
            };
            dashboardServiceMock.Setup(s => s.ReorderDashboardCardsAsync(1, "testuser", "{}", request)).ReturnsAsync(false);
            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.ReorderDashboardCards(1, "{}", request);
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("No se encontró", notFound.Value.ToString());
        }

        [Fact]
        public async Task ReorderDashboardCards_ReturnsServerError_OnException()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            var request = new List<OmniMonitor.Shared.Dtos.DashboardCard>
            {
                new OmniMonitor.Shared.Dtos.DashboardCard { CardId = 1, TipoCard = 1 }
            };
            dashboardServiceMock.Setup(s => s.ReorderDashboardCardsAsync(1, "testuser", "{}", request)).ThrowsAsync(new Exception("DB error"));
            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.ReorderDashboardCards(1, "{}", request);
            var serverError = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, serverError.StatusCode);
        }

        [Fact]
        public async Task ReorderDashboardCards_ReturnsNotFound_WhenUserIsNull()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            var request = new List<OmniMonitor.Shared.Dtos.DashboardCard>
            {
                new OmniMonitor.Shared.Dtos.DashboardCard { CardId = 1, TipoCard = 1 }
            };
            dashboardServiceMock.Setup(s => s.ReorderDashboardCardsAsync(1, null, "{}", request)).ReturnsAsync(false);
            var controller = GetController(dashboardServiceMock, null, new ClaimsPrincipal());
            var result = await controller.ReorderDashboardCards(1, "{}", request);
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("No se encontró", notFound.Value.ToString());
        }

        [Fact]
        public async Task ReorderDashboardCards_ReturnAuthorizationAccessException_WhenUnauthorizedUser() 
        {             
            var dashboardServiceMock = new Mock<IDashboardService>();
            var request = new List<OmniMonitor.Shared.Dtos.DashboardCard>
            {
                new OmniMonitor.Shared.Dtos.DashboardCard { CardId = 1, TipoCard = 1 }
            };
            dashboardServiceMock.Setup(s => s.ReorderDashboardCardsAsync(1, "testuser", "{}", request))
                .ThrowsAsync(new UnauthorizedAccessException("No autorizado"));
            var controller = GetController(dashboardServiceMock, null, GetUser());
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await controller.ReorderDashboardCards(1, "{}", request));
        }

        /* Tests sobre DeleteDashboardCard */

        [Fact]
        public async Task DeleteDashboardCard_ReturnsOk_WhenSuccessful()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            dashboardServiceMock.Setup(s => s.DeleteDashboardCardAsync(1, "testuser", 1, 1)).ReturnsAsync(true);
            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.DeleteDashboardCard(1, 1, 1);
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task DeleteDashboardCard_ReturnsNotFound_WhenDashboardDoesNotExist()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            dashboardServiceMock.Setup(s => s.DeleteDashboardCardAsync(1, "testuser", 1, 1)).ReturnsAsync(false);
            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.DeleteDashboardCard(1, 1, 1);
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("No se encontró", notFound.Value.ToString());
        }

        [Fact]
        public async Task DeleteDashboardCard_ReturnsServerError_OnException()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            dashboardServiceMock.Setup(s => s.DeleteDashboardCardAsync(1, "testuser", 1, 1))
                .ThrowsAsync(new Exception("DB error"));
            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.DeleteDashboardCard(1, 1, 1);
            var serverError = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, serverError.StatusCode);
        }

        [Fact]
        public async Task DeleteDashboardCard_ReturnsNotFound_WhenUserIsNull()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            dashboardServiceMock.Setup(s => s.DeleteDashboardCardAsync(1, null, 1, 1)).ReturnsAsync(false);
            var controller = GetController(dashboardServiceMock, null, new ClaimsPrincipal());
            var result = await controller.DeleteDashboardCard(1, 1, 1);
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("No se encontró", notFound.Value.ToString());
        }

        [Fact]
        public async Task DeleteDashboardCard_ReturnAuthorizationAccessException_WhenUnauthorizedUser() 
        {             
            var dashboardServiceMock = new Mock<IDashboardService>();
            dashboardServiceMock.Setup(s => s.DeleteDashboardCardAsync(1, "testuser", 1, 1))
                .ThrowsAsync(new UnauthorizedAccessException("No autorizado"));
            var controller = GetController(dashboardServiceMock, null, GetUser());
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await controller.DeleteDashboardCard(1, 1, 1));
        }

        /* Tests sobre EditDashboardCard */

        [Fact]
        public async Task EditDashboardCard_ReturnsOk_WhenSuccessful()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            var request = new CreateVisualizacionRequest { Nombre = "Card" };
            dashboardServiceMock.Setup(s => s.EditDashboardCard(1, "testuser", "{}", 1, request)).ReturnsAsync(true);
            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.EditDashboardCard(1, "{}", 1, request);
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task EditDashboardCard_ReturnsNotFound_WhenDashboardDoesNotExist()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            var request = new CreateVisualizacionRequest { Nombre = "Card" };
            dashboardServiceMock.Setup(s => s.EditDashboardCard(1,"testuser", "{}", 1, request)).ReturnsAsync(false);
            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.EditDashboardCard(1, "testuser",1, request);
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("No se encontró", notFound.Value.ToString());
        }

        [Fact]
        public async Task EditDashboardCard_ReturnNotFound_WhenDashboardCardDoesNotExist()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            var request = new CreateVisualizacionRequest { Nombre = "Card" };
            dashboardServiceMock.Setup(s => s.EditDashboardCard(1, "testuser", "{}", 1, request))
                .ReturnsAsync(false);
            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.EditDashboardCard(1, "{}", 1, request);
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task EditDashboardCard_ReturnsServerError_OnException()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            var request = new CreateVisualizacionRequest { Nombre = "Card" };
            dashboardServiceMock.Setup(s => s.EditDashboardCard(1, "testuser", "{}", 1, request))
                .ThrowsAsync(new Exception("DB error"));
            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.EditDashboardCard(1, "{}", 1, request);
            var serverError = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, serverError.StatusCode);
        }

        [Fact]
        public async Task EditDashboardCard_ReturnsNotFound_WhenUserIsNull()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            var request = new CreateVisualizacionRequest { Nombre = "Card" };
            dashboardServiceMock.Setup(s => s.EditDashboardCard(1, null, "{}", 1, request)).ReturnsAsync(false);
            var controller = GetController(dashboardServiceMock, null, new ClaimsPrincipal());
            var result = await controller.EditDashboardCard(1, "{}", 1, request);
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("No se encontró", notFound.Value.ToString());
        }

        [Fact]
        public async Task EditDashboardCard_ReturnAuthorizationAccessException_WhenUnauthorizedUser() 
        {             
            var dashboardServiceMock = new Mock<IDashboardService>();
            var request = new CreateVisualizacionRequest { Nombre = "Card" };
            dashboardServiceMock.Setup(s => s.EditDashboardCard(1, "testuser", "{}", 1, request))
                .ThrowsAsync(new UnauthorizedAccessException("No autorizado"));
            var controller = GetController(dashboardServiceMock, null, GetUser());
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await controller.EditDashboardCard(1, "{}", 1, request));
        }

        /* Tests sobre CreateShareLink */

        [Fact]
        public async Task CreateShareLink_ReturnsOk_WithData()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            var request = new ShareRequestDto { ExpiresAt = DateTime.Now.AddDays(10), Password = "secret" };
            var response = new ShareResponseDto { CreatedAt = DateTime.Now, ExpiresAt = DateTime.Now.AddDays(10), Slug = "abc123", Status = "active", Visibility = "public", dashBoardId = 1 };
            dashboardServiceMock.Setup(s => s.CreateShareLinkAsync(1, request, "testuser")).ReturnsAsync(response);
            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.CreateShareLink(1, request);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(response, ok.Value);
        }

        [Fact]
        public async Task CreateShareLink_ReturnsBadRequest_WhenModelInvalid()
        {
            var controller = GetController(null, null, GetUser());
            controller.ModelState.AddModelError("ExpiresAt", "El campo ExpiresAt es obligatorio.");
            var request = new ShareRequestDto { Password = "secret" };
            var result = await controller.CreateShareLink(1, request);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequest.Value);
        }

        [Fact]
        public async Task CreateShareLink_ReturnsUnauthorized_WhenUserIsNull()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            var request = new ShareRequestDto { ExpiresAt = DateTime.Now.AddDays(10), Password = "secret" };
            var controller = GetController(dashboardServiceMock, null, new ClaimsPrincipal());
            var result = await controller.CreateShareLink(1, request);
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            Assert.Contains("Token inválido", unauthorized.Value.ToString());
        }

        [Fact]
        public async Task CreateShareLink_ReturnsNotFound_WhenDashboardDoesNotExist()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            var request = new ShareRequestDto { ExpiresAt = DateTime.Now.AddDays(10), Password = "secret" };
            dashboardServiceMock.Setup(s => s.CreateShareLinkAsync(1, request, "testuser"))
                .ThrowsAsync(new KeyNotFoundException("No se encontró el tablero."));
            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.CreateShareLink(1, request);
            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Contains("No se encontró el tablero", notFound.Value.ToString());
        }

        [Fact]
        public async Task CreateShareLink_ReturnsServerError_OnException()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            var request = new ShareRequestDto { ExpiresAt = DateTime.Now.AddDays(10), Password = "secret" };
            dashboardServiceMock.Setup(s => s.CreateShareLinkAsync(1, request, "testuser"))
                .ThrowsAsync(new Exception("DB error"));
            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.CreateShareLink(1, request);
            var serverError = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, serverError.StatusCode);
        }

        /* Tests sobre GetShareLinksForDashboard */

        [Fact]
        public async Task GetSharedLinksForDashboard_ReturnsOk_WithData()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            var response = new List<ShareResponseDto>
            {
                new ShareResponseDto { CreatedAt = DateTime.Now, ExpiresAt = DateTime.Now.AddDays(10), Slug = "abc123", Status = "active", Visibility = "public", dashBoardId = 1 }
            };
            dashboardServiceMock.Setup(s => s.GetAllByDashboardAsync(1, "testuser")).ReturnsAsync(response);
            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.GetShareLinksForDashboard(1);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(response, ok.Value);
        }

        [Fact]
        public async Task GetSharedLinksForDashboard_ReturnsUnauthorized_WhenUserIsNull()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            var controller = GetController(dashboardServiceMock, null, new ClaimsPrincipal());
            var result = await controller.GetShareLinksForDashboard(1);
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            Assert.Contains("Token inválido", unauthorized.Value.ToString());
        }

        [Fact]
        public async Task GetSharedLinksForDashboard_ReturnsServerError_OnException()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            dashboardServiceMock.Setup(s => s.GetAllByDashboardAsync(1, "testuser"))
                .ThrowsAsync(new Exception("DB error"));
            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.GetShareLinksForDashboard(1);
            var serverError = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, serverError.StatusCode);
        }

        [Fact]
        public async Task GetSharedLinksForDashboard_ReturnsEmptyList_WhenNoSharesExist()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            var response = new List<ShareResponseDto>();
            dashboardServiceMock.Setup(s => s.GetAllByDashboardAsync(1, "testuser")).ReturnsAsync(response);
            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.GetShareLinksForDashboard(1);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(response, ok.Value);
        }

        [Fact]
        public async Task GetSharedLinksForDashboard_ReturnsNotFound_WhenDashboardDoesNotExist()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            dashboardServiceMock.Setup(s => s.GetAllByDashboardAsync(1, "testuser"))
                .ReturnsAsync((List<ShareResponseDto>?)null);
            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.GetShareLinksForDashboard(1);
            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        /* GetPublicShareLink */

        [Fact]
        public async Task GetPublicShareLink_ReturnsOk_WithData()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            var response = new ShareResponseDto { CreatedAt = DateTime.Now, ExpiresAt = DateTime.Now.AddDays(10), Slug = "abc123", Status = "active", Visibility = "public", dashBoardId = 1 };
            dashboardServiceMock.Setup(s => s.GetBySlugAsync("abc123")).ReturnsAsync(response);
            var controller = GetController(dashboardServiceMock, null, null);
            var result = await controller.GetPublicShareLink("abc123");
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(response, ok.Value);
        }

        [Fact]
        public async Task GetPublicShareLink_ReturnsNotFound_WhenShareDoesNotExist()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            dashboardServiceMock.Setup(s => s.GetBySlugAsync("abc123")).ReturnsAsync((ShareResponseDto?)null);
            var controller = GetController(dashboardServiceMock, null, null);
            var result = await controller.GetPublicShareLink("abc123");
            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Contains("Enlace no encontrado", notFound.Value.ToString());
        }

        [Fact]
        public async Task GetPublicShareLink_ReturnsServerError_OnException()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            dashboardServiceMock.Setup(s => s.GetBySlugAsync("abc123"))
                .ThrowsAsync(new Exception("DB error"));
            var controller = GetController(dashboardServiceMock, null, null);
            var result = await controller.GetPublicShareLink("abc123");
            var serverError = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, serverError.StatusCode);
        }

        [Fact]
        public async Task GetPublicShareLink_ReturnsNotFound_WhenDashboardDoesNotExist()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            dashboardServiceMock.Setup(s => s.GetBySlugAsync("abc123"))
                .ReturnsAsync((ShareResponseDto?)null);
            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.GetPublicShareLink("abc123");
            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        /* Tests para ValidateSharePassword */

        [Fact]
        public async Task ValidateSharePassword_ReturnsOk_WhenPasswordIsValid()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            var response = new ValidateSharePasswordResponseDto { IsValid = true, DashboardId = 1 };
            dashboardServiceMock.Setup(s => s.ValidatePasswordAsync("abc123", "correct_password")).ReturnsAsync(response);
            var controller = GetController(dashboardServiceMock, null, null);
            var request = new ValidateSharePasswordRequestDto { Password = "correct_password" };
            var result = await controller.ValidateSharePassword("abc123", request);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task ValidateSharePassword_ReturnsUnauthorized_WhenPasswordIsInvalid()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            var response = new ValidateSharePasswordResponseDto { IsValid = false, DashboardId = null };
            dashboardServiceMock.Setup(s => s.ValidatePasswordAsync("abc123", "wrong_password")).ReturnsAsync(response);
            var controller = GetController(dashboardServiceMock, null, null);
            var request = new ValidateSharePasswordRequestDto { Password = "wrong_password" };
            var result = await controller.ValidateSharePassword("abc123", request);
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        }

        [Fact]
        public async Task ValidateSharePassword_ReturnsBadRequest_WhenModelInvalid()
        {
            var controller = GetController(null, null, null);
            controller.ModelState.AddModelError("Password", "El campo Password es obligatorio.");
            var request = new ValidateSharePasswordRequestDto { };
            var result = await controller.ValidateSharePassword("abc123", request);
            var badRequest = Assert.IsType<BadRequestResult>(result.Result);
        }

        [Fact]
        public async Task ValidateSharePassword_ReturnsServerError_OnException()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            dashboardServiceMock.Setup(s => s.ValidatePasswordAsync("abc123", "any_password"))
                .ThrowsAsync(new Exception("DB error"));
            var controller = GetController(dashboardServiceMock, null, null);
            var request = new ValidateSharePasswordRequestDto { Password = "any_password" };
            var result = await controller.ValidateSharePassword("abc123", request);
            var serverError = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, serverError.StatusCode);
        }

        /* Tests sobre UpdateShareLink */

        [Fact]
        public async Task UpdateShareLink_ReturnsOk_WithData()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            var request = new ShareRequestDto { ExpiresAt = DateTime.Now.AddDays(20), Password = "new_secret" };
            var response = new ShareResponseDto { CreatedAt = DateTime.Now, ExpiresAt = DateTime.Now.AddDays(20), Slug = "abc123", Status = "active", Visibility = "public", dashBoardId = 1 };
            dashboardServiceMock.Setup(s => s.UpdateShareLinkAsync("abc123", request, "testuser")).ReturnsAsync(response);
            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.UpdateShareLink("abc123", request);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(response, ok.Value);
        }

        [Fact]
        public async Task UpdateShareLink_ReturnsNotFound_WhenShareDoesNotExist()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            var request = new ShareRequestDto { ExpiresAt = DateTime.Now.AddDays(20), Password = "new_secret" };
            dashboardServiceMock.Setup(s => s.UpdateShareLinkAsync("abc123", request, "testuser")).ReturnsAsync((ShareResponseDto?)null);
            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.UpdateShareLink("abc123", request);
            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Contains("Enlace no encontrado", notFound.Value.ToString());
        }

        [Fact]
        public async Task UpdateShareLink_ReturnsBadRequest_WhenModelInvalid()
        {
            var controller = GetController(null, null, GetUser());
            controller.ModelState.AddModelError("ExpiresAt", "El campo ExpiresAt es obligatorio.");
            var request = new ShareRequestDto { Password = "new_secret" };
            var result = await controller.UpdateShareLink("abc123", request);
            var badRequest = Assert.IsType<BadRequestResult>(result.Result);
        }

        [Fact]
        public async Task UpdateShareLink_ReturnsUnauthorized_WhenUserIsNull()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            var request = new ShareRequestDto { ExpiresAt = DateTime.Now.AddDays(20), Password = "new_secret" };
            var controller = GetController(dashboardServiceMock, null, new ClaimsPrincipal());
            var result = await controller.UpdateShareLink("abc123", request);
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            Assert.Contains("Token inválido", unauthorized.Value.ToString());
        }

        [Fact]
        public async Task UpdateShareLink_ReturnsServerError_OnException()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            var request = new ShareRequestDto { ExpiresAt = DateTime.Now.AddDays(20), Password = "new_secret" };
            dashboardServiceMock.Setup(s => s.UpdateShareLinkAsync("abc123", request, "testuser"))
                .ThrowsAsync(new Exception("DB error"));
            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.UpdateShareLink("abc123", request);
            var serverError = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, serverError.StatusCode);
        }

        /* Tests sobre DeleteShareLink */

        [Fact]
        public async Task DeleteShareLink_ReturnsNoContent_WhenSuccessful()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            dashboardServiceMock.Setup(s => s.DeleteShareLinkAsync("abc123", "testuser")).ReturnsAsync(true);
            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.DeleteShareLink("abc123");
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteShareLink_ReturnsNotFound_WhenShareDoesNotExist()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            dashboardServiceMock.Setup(s => s.DeleteShareLinkAsync("abc123", "testuser")).ReturnsAsync(false);
            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.DeleteShareLink("abc123");
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("Enlace no encontrado", notFound.Value.ToString());
        }

        [Fact]
        public async Task DeleteShareLink_ReturnsUnauthorized_WhenUserIsNull()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            var controller = GetController(dashboardServiceMock, null, new ClaimsPrincipal());
            var result = await controller.DeleteShareLink("abc123");
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Contains("Token inválido", unauthorized.Value.ToString());
        }

        [Fact]
        public async Task DeleteShareLink_ReturnsServerError_OnException()
        {
            var dashboardServiceMock = new Mock<IDashboardService>();
            dashboardServiceMock.Setup(s => s.DeleteShareLinkAsync("abc123", "testuser"))
                .ThrowsAsync(new Exception("DB error"));
            var controller = GetController(dashboardServiceMock, null, GetUser());
            var result = await controller.DeleteShareLink("abc123");
            var serverError = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, serverError.StatusCode);
        }
    }
}
