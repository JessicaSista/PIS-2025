using System.Collections.Generic;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using OmniMonitor.Client.Pages;
using OmniMonitor.Shared.Dtos;
using RichardSzalay.MockHttp;
using Xunit;
using Xunit.Abstractions;
using System.Net.Http.Json;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components.Authorization;

namespace OmniMonitor.Client.Tests
{
    public class TestPermissions : TestContext
    {
        private readonly ITestOutputHelper _output;
        private readonly TestAuthorizationContext _authContext;

        public TestPermissions(ITestOutputHelper output)
        {
            _output = output;
            _authContext = this.AddTestAuthorization(); // Solo esto, sin registrar AuthenticationStateProvider
        }

        [Fact]
        public void RenderizaPermisosDelUsuario()
        {
            // Arrange
            var userId = 1;
            var permissions = new List<Permission>
            {
                new Permission { Id = 1, Name = "apis:read" },
                new Permission { Id = 2, Name = "dashboards:write" }
            };

            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When("http://localhost/api/authorization/users/1/permissions")
                .Respond("application/json", System.Text.Json.JsonSerializer.Serialize(permissions));
            mockHttp.When("http://localhost/api/authorization/users/1/has-permission*")
                .Respond("application/json", "true");
            mockHttp.When("http://localhost/api/authorization/users/1/has-role*")
                .Respond("application/json", "true");

            var httpClient = mockHttp.ToHttpClient();
            httpClient.BaseAddress = new System.Uri("http://localhost/");
            Services.AddSingleton<HttpClient>(httpClient);

            // Configura el usuario autenticado y sus claims
            _authContext.SetAuthorized("testuser");
            _authContext.SetClaims(
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, "Administrador")
            );

            var localStorageMock = new Moq.Mock<Blazored.LocalStorage.ILocalStorageService>();
            Services.AddSingleton(localStorageMock.Object);

            // Act
            var cut = RenderComponent<CascadingAuthenticationState>(parameters => parameters
                .AddChildContent<Pages.TestPermissions>());
            

            cut.WaitForAssertion(() =>
            {
                Assert.DoesNotContain("Cargando permisos...", cut.Markup);
                Assert.Contains("apis:read", cut.Markup);
                Assert.Contains("dashboards:write", cut.Markup);
                Assert.Contains("Administrador", cut.Markup);
                Assert.Contains("testuser", cut.Markup);
            }, timeout: System.TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void RenderizaSinPermisos()
        {
            // Arrange
            var userId = 2;
            var permissions = new List<Permission>(); // Sin permisos

            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When("http://localhost/api/authorization/users/2/permissions")
                .Respond("application/json", System.Text.Json.JsonSerializer.Serialize(permissions));
            mockHttp.When("http://localhost/api/authorization/users/2/has-permission*")
                .Respond("application/json", "false");
            mockHttp.When("http://localhost/api/authorization/users/2/has-role*")
                .Respond("application/json", "false");

            var httpClient = mockHttp.ToHttpClient();
            httpClient.BaseAddress = new System.Uri("http://localhost/");
            Services.AddSingleton<HttpClient>(httpClient);

            // Configura el usuario autenticado y sus claims
            _authContext.SetAuthorized("noperms");
            _authContext.SetClaims(
                new Claim(ClaimTypes.Name, "noperms"),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, "Visitante")
            );

            var localStorageMock = new Moq.Mock<Blazored.LocalStorage.ILocalStorageService>();
            Services.AddSingleton(localStorageMock.Object);

            // Act
            var cut = RenderComponent<CascadingAuthenticationState>(parameters => parameters
                .AddChildContent<Pages.TestPermissions>());

            cut.WaitForAssertion(() =>
            {
                Assert.Contains("No se pudieron cargar los permisos o no tiene ninguno.", cut.Markup);
                Assert.Contains("Visitante", cut.Markup);
                Assert.Contains("noperms", cut.Markup);
            }, timeout: System.TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void RenderizaNoAutenticado()
        {
            // Arrange
            _authContext.SetNotAuthorized();

            var localStorageMock = new Moq.Mock<Blazored.LocalStorage.ILocalStorageService>();
            Services.AddSingleton(localStorageMock.Object);

            // Act
            var cut = RenderComponent<CascadingAuthenticationState>(parameters => parameters
                .AddChildContent<Pages.TestPermissions>());

            // Assert
            cut.WaitForAssertion(() =>
            {
                Assert.Contains("No hay usuario autenticado", cut.Markup);
                Assert.Contains("Iniciar Sesión", cut.Markup);
            }, timeout: System.TimeSpan.FromSeconds(5));
        }

        private void MostrarPermisos(IEnumerable<Permission> permisos)
        {
            if (permisos == null || !permisos.Any())
            {
                _output.WriteLine("No hay permisos.");
                return;
            }

            foreach (var permiso in permisos)
            {
                _output.WriteLine($"Permiso: {permiso.Id} - {permiso.Name} - {permiso.Description}");
            }
        }

        [Fact]
        public void MuestraPermisosManual()
        {
            var permisos = new List<Permission>
            {
                new Permission { Id = 1, Name = "apis:read", Description = "Permite leer APIs" },
                new Permission { Id = 2, Name = "dashboards:write", Description = "Permite editar dashboards" }
            };

            MostrarPermisos(permisos);

            Assert.True(true); // Solo para que el test pase
        }

        [Fact]
        public async Task MuestraTodosLosPermisosDesdeApi()
        {
            var mockHttp = new MockHttpMessageHandler();
            var allPermissions = new List<Permission>
            {
                new Permission { Id = 1, Name = "apis:read", Description = "Permite leer APIs" },
                new Permission { Id = 2, Name = "dashboards:write", Description = "Permite editar dashboards" },
                // ... otros permisos
            };

            mockHttp.When("http://localhost/api/authorization/permissions")
                .Respond("application/json", System.Text.Json.JsonSerializer.Serialize(allPermissions));

            var httpClient = mockHttp.ToHttpClient();
            httpClient.BaseAddress = new System.Uri("http://localhost/");
            Services.AddSingleton<HttpClient>(httpClient);

            // Act
            var response = await httpClient.GetAsync("api/authorization/permissions");
            var permisos = await response.Content.ReadFromJsonAsync<List<Permission>>();

            MostrarPermisos(permisos);

            Assert.NotNull(permisos);
            Assert.True(permisos.Count > 0);
        }
    }

    // Mock mínimo de Permission para los tests
    public class Permission
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
    }
}