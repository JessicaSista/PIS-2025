using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OmniMonitor.Server.Context;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using OmniMonitor.Client.Pages;
using Xunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components.Authorization;
using OmniMonitor.Shared.Dtos;
using Moq.Protected;
using System.Net.Http;
using System.Net;
using Moq;

namespace OmniMonitor.Client.Tests
{
    public class TestPermissions : TestContext
    {
        private readonly TestAuthorizationContext _authContext;

        public TestPermissions()
        {
            _authContext = this.AddTestAuthorization();
        }

        [Fact]
        public async Task RenderizaPermisosDelUsuario()
        {
            var dbName = $"TestDbLocal_{System.Guid.NewGuid()}";
            var localDbSettings = new Dictionary<string, string> {
                {"ConnectionStrings:DefaultConnection", $"Server=(localdb)\\mssqllocaldb;Database={dbName};Trusted_Connection=True;"}
            };
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(localDbSettings)
                .Build();

            var dbContext = new ApplicationDbContext(configuration);
            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.EnsureCreatedAsync();

            var permiso1 = new Permission { Name = "apis:read", Description = "Permite leer APIs" };
            var permiso2 = new Permission { Name = "dashboards:write", Description = "Permite editar dashboards" };
            var permisoVerUsuarios = new Permission { Name = "Ver Usuarios", Description = "Permite ver usuarios" };

            var rol = new Role { Name = "Administrador" };
            dbContext.Permissions.AddRange(permiso1, permiso2, permisoVerUsuarios);
            dbContext.Roles.Add(rol);
            dbContext.SaveChanges();

            dbContext.RolePermissions.Add(new RolePermission { RoleId = rol.Id, PermissionId = permiso1.Id });
            dbContext.RolePermissions.Add(new RolePermission { RoleId = rol.Id, PermissionId = permiso2.Id });
            dbContext.RolePermissions.Add(new RolePermission { RoleId = rol.Id, PermissionId = permisoVerUsuarios.Id });
            dbContext.SaveChanges();

            var user = new User { Username = "testuser", Password = "test" };
            dbContext.Users.Add(user);
            dbContext.SaveChanges();

            dbContext.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = rol.Id });
            dbContext.SaveChanges();

            Services.AddSingleton<ApplicationDbContext>(dbContext);

            _authContext.SetAuthorized("testuser");
            _authContext.SetClaims(
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, "Administrador")
            );

            // Mock necesario para Blazored.LocalStorage
            var localStorageMock = new Moq.Mock<Blazored.LocalStorage.ILocalStorageService>();
            Services.AddSingleton(localStorageMock.Object);

            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("[{\"Name\":\"apis:read\"},{\"Name\":\"dashboards:write\"}]"),
                });

            var httpClient = new HttpClient(handler.Object)
            {
                BaseAddress = new Uri("http://localhost")
            };
            Services.AddSingleton<HttpClient>(httpClient);

            var cut = RenderComponent<CascadingAuthenticationState>(parameters => parameters
                .AddChildContent<Pages.TestPermissions>());

            cut.WaitForAssertion(() =>
            {
                Assert.DoesNotContain("Cargando permisos...", cut.Markup);
                Assert.Contains("apis:read", cut.Markup);
                Assert.Contains("dashboards:write", cut.Markup);
                Assert.Contains("Administrador", cut.Markup);
                Assert.Contains("testuser", cut.Markup);
            }, timeout: System.TimeSpan.FromSeconds(30));
        }

        [Fact]
        public void RenderizaNoAutenticado()
        {
            _authContext.SetNotAuthorized();

            // Mock necesario para Blazored.LocalStorage
            var localStorageMock = new Moq.Mock<Blazored.LocalStorage.ILocalStorageService>();
            Services.AddSingleton(localStorageMock.Object);

            var cut = RenderComponent<CascadingAuthenticationState>(parameters => parameters
                .AddChildContent<Pages.TestPermissions>());

            cut.WaitForAssertion(() =>
            {
                Assert.Contains("No hay usuario autenticado", cut.Markup);
                Assert.Contains("Iniciar Sesión", cut.Markup);
            }, timeout: System.TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task DatosSeed_PublicadosCorrectamente()
        {
            var localDbSettings = new Dictionary<string, string> {
                {"ConnectionStrings:DefaultConnection", "Server=(localdb)\\mssqllocaldb;Database=TestDbLocal;Trusted_Connection=True;"}
            };
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(localDbSettings)
                .Build();

            var dbContext = new ApplicationDbContext(configuration);

            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.EnsureCreatedAsync();

            var adminRole = await dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "Administrador");
            Assert.NotNull(adminRole);

            var adminUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Username == "admin");
            Assert.NotNull(adminUser);

            var permisos = await dbContext.Permissions.ToListAsync();
            Assert.True(permisos.Count > 0);
        }
    }
}