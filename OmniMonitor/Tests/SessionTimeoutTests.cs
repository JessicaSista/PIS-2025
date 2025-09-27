using Blazored.LocalStorage;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

public class SessionTimeoutTests : TestContext
{
    public SessionTimeoutTests()
    {
        // Registrar el fake de autorización de bUnit para todos los tests
        this.AddTestAuthorization();
    }

    [Fact]
    public void RedirigeAlLoginCuandoExpiraSesion()
    {
        // Arrange: simula token expirado en LocalStorage
        var localStorageMock = new Mock<ILocalStorageService>();
        localStorageMock.Setup(x => x.GetItemAsync<DateTime>("token_expires_at", default))
            .ReturnsAsync(DateTime.UtcNow.AddDays(-1)); // Expirado

        Services.AddSingleton(localStorageMock.Object);

        // Provee un AuthenticationStateProvider no autenticado
        Services.AddSingleton<AuthenticationStateProvider>(
            new TestAuthProvider(new ClaimsPrincipal(new ClaimsIdentity()))
        );

        // FakeNavigationManager ya está registrado por Bunit
        var navMan = Services.GetRequiredService<FakeNavigationManager>();

        // Act: renderiza el componente que debe redirigir
        var cut = RenderComponent<CascadingAuthenticationState>(parameters => parameters
            .AddChildContent<OmniMonitor.Client.Pages.TestPermissions>());

        // Assert: espera la redirección al login
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("No hay usuario autenticado", cut.Markup);
            Assert.Contains("Iniciar Sesión", cut.Markup);
        }, timeout: TimeSpan.FromSeconds(5));
    }

    // Proveedor de autenticación de test
    public class TestAuthProvider : AuthenticationStateProvider
    {
        private readonly ClaimsPrincipal _user;

        public TestAuthProvider(ClaimsPrincipal user)
        {
            _user = user;
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(_user));
    }
}