using Blazored.LocalStorage;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;
using Moq;
using Moq.Protected;
using MudBlazor;
using MudBlazor.Services;
using OmniMonitor.Client.Auth;
using OmniMonitor.Client.Pages;
using OmniMonitor.Server.Context;
using OmniMonitor.Shared;
using OmniMonitor.Shared.Dtos;
using System.Net;
using System.Net.Http.Json;

public class LoginTest : TestContext
{
    

    public LoginTest()
    {
        Services.AddAuthorization();
        Services.AddMudServices();
        JSInterop.SetupVoid(_ => true);

        // Mock de ILocalStorageService
        var localStorageMock = new MockLocalStorageService();
        Services.AddSingleton<ILocalStorageService>(localStorageMock);

        // Mock de HttpClient
        var handler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("http://localhost")
        };
        Services.AddSingleton<HttpClient>(httpClient);

        // Registrar el provider DESPUÉS de los mocks
        Services.AddSingleton<AuthenticationStateProvider, ApiAuthenticationStateProvider>();

        Services.AddSingleton<ISnackbar>(new MockSnackbar());
        Services.AddSingleton<NavigationManager>(new FakeNavigationManager());
        Services.AddHttpClient();

        // Mock para el localizer específico del Login
        var localizerLoginMock = new Mock<IStringLocalizer<Login>>();
        localizerLoginMock
            .Setup(l => l["UsernameLabel"])
            .Returns(new LocalizedString("UsernameLabel", "Usuario o Email"));
        localizerLoginMock
            .Setup(l => l["PasswordLabel"])
            .Returns(new LocalizedString("PasswordLabel", "Contraseña"));
        localizerLoginMock
            .Setup(l => l["LoginButton"])
            .Returns(new LocalizedString("LoginButton", "Iniciar Sesión"));
        localizerLoginMock
            .Setup(l => l["RequiredUsername"])
            .Returns(new LocalizedString("RequiredUsername", "El usuario es obligatorio."));
        localizerLoginMock
            .Setup(l => l["Subtitle"])
            .Returns(new LocalizedString("Subtitle", "Ingrese sus credenciales para acceder."));
        localizerLoginMock
            .Setup(l => l["AuthenticationError"])
            .Returns(new LocalizedString("AuthenticationError", "Credenciales incorrectas"));
        localizerLoginMock
            .Setup(l => l["UnexpectedErrorPrefix"])
            .Returns(new LocalizedString("UnexpectedErrorPrefix", "Error: "));
        Services.AddSingleton<IStringLocalizer<Login>>(localizerLoginMock.Object);

        // Mock para el localizer compartido (SharedResource)
        var localizerSharedMock = new Mock<IStringLocalizer<SharedResource>>();
        localizerSharedMock
            .Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));
        Services.AddSingleton<IStringLocalizer<SharedResource>>(localizerSharedMock.Object);

        // Registrar el contexto en memoria
        var dbContext = CreateDbContext();
        Services.AddSingleton<ApplicationDbContext>(dbContext);
    }

    [Fact]
    public void Login_RenderizaCamposYBoton()
    {
        var cut = RenderComponent<Login>();
        Assert.Contains("Usuario o Email", cut.Markup);
        Assert.Contains("Contraseña", cut.Markup);
        Assert.Contains("Iniciar Sesión", cut.Markup);
    }

    [Fact]
    public void Login_NoPermiteEnvioConCamposVacios()
    {
        var cut = RenderComponent<Login>();
        var loginButton = cut.Find("button.mud-button");
        loginButton.Click();
        Assert.Contains("obligatorio", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Login_ContraseñaNoEsTextoPlano()
    {
        var cut = RenderComponent<Login>();
        var passwordInput = cut.Find("input[type=password]");
        Assert.Equal("password", passwordInput.GetAttribute("type"));
    }

    [Fact]
    public void Login_CredencialesCorrectas_RedireccionaADashboard()
    {
        // Mock de respuesta HTTP para éxito
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(new OmniMonitor.Shared.Dtos.LoginResponse
                {
                    Success = true,
                    Token = "fake-token"
                })
            });

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost")
        };

        // Mock para IStringLocalizer<Login>
        var localizerMock = new Mock<IStringLocalizer<Login>>();
        localizerMock.Setup(l => l["UnexpectedErrorPrefix"])
            .Returns(new LocalizedString("UnexpectedErrorPrefix", "Error: "));
        localizerMock.Setup(l => l["AuthenticationError"])
            .Returns(new LocalizedString("AuthenticationError", "Error de autenticación"));
        // Agrega más setups si usas más claves en la vista

        // Mock para IStringLocalizer<SharedResource>
        var sharedLocalizerMock = new Mock<IStringLocalizer<SharedResource>>();
        sharedLocalizerMock.Setup(l => l["LoginTitle"])
            .Returns(new LocalizedString("LoginTitle", "Iniciar sesión"));
        sharedLocalizerMock.Setup(l => l["Subtitle"])
            .Returns(new LocalizedString("Subtitle", "Introduce tus credenciales"));
        // Agrega más setups si usas más claves en la vista

        // Reemplaza el HttpClient
        Services.RemoveAll<HttpClient>();
        Services.AddSingleton<HttpClient>(httpClient);

        // Vuelve a registrar el provider para que use el nuevo HttpClient
        Services.RemoveAll<AuthenticationStateProvider>();
        Services.AddSingleton<AuthenticationStateProvider, ApiAuthenticationStateProvider>();

        // Registra los localizadores mockeados
        Services.RemoveAll<IStringLocalizer<Login>>();
        Services.AddSingleton<IStringLocalizer<Login>>(localizerMock.Object);
        Services.RemoveAll<IStringLocalizer<SharedResource>>();
        Services.AddSingleton<IStringLocalizer<SharedResource>>(sharedLocalizerMock.Object);

        var nav = Services.GetRequiredService<NavigationManager>() as FakeNavigationManager;
        var cut = RenderComponent<Login>();
        cut.Find("input[type=text]").Change("admin");
        cut.Find("input[type=password]").Change("admin");
        var loginButton = cut.Find("button.mud-button");
        loginButton.Click();
        cut.WaitForAssertion(() => Assert.Contains("/", nav?.Uri));
    }

    [Fact]
    public void Login_CredencialesIncorrectas_MuestraError()
    {
        // Mock de respuesta HTTP para credenciales incorrectas
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Unauthorized,
                Content = new StringContent("{\"success\":false,\"message\":\"Credenciales incorrectas\"}", System.Text.Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost")
        };

        // Mock para IStringLocalizer<Login>
        var localizerMock = new Mock<IStringLocalizer<Login>>();
        localizerMock
            .Setup(l => l["AuthenticationError"])
            .Returns(new LocalizedString("AuthenticationError", "Credenciales incorrectas"));
        localizerMock
            .Setup(l => l["UnexpectedErrorPrefix"])
            .Returns(new LocalizedString("UnexpectedErrorPrefix", "Error: "));

        // Mock para ISnackbar
        var snackbarMock = new MockSnackbar();

        // Reemplaza servicios relevantes
        Services.RemoveAll<HttpClient>();
        Services.AddSingleton<HttpClient>(httpClient);

        Services.RemoveAll<IStringLocalizer<Login>>();
        Services.AddSingleton<IStringLocalizer<Login>>(localizerMock.Object);

        Services.RemoveAll<ISnackbar>();
        Services.AddSingleton<ISnackbar>(snackbarMock);

        var cut = RenderComponent<Login>();
        cut.Find("input[type=text]").Change("usuario");
        cut.Find("input[type=password]").Change("incorrecta");
        var loginButton = cut.Find("button.mud-button");
        loginButton.Click();

        cut.WaitForAssertion(
            () => Assert.Contains("Credenciales incorrectas", cut.Markup, StringComparison.OrdinalIgnoreCase),
            timeout: TimeSpan.FromSeconds(10)
          );
    }

    [Fact]
    public void Login_ConsultaCredencialesEnBaseDeDatos()
    {
        // Arrange: obtener el contexto registrado
        var dbContext = Services.GetRequiredService<ApplicationDbContext>();

        // Prueba con credenciales válidas
        var usuarioValido = dbContext.Users
            .FirstOrDefault(u => u.Username == "testuser" && u.Password == "password123");
        Assert.NotNull(usuarioValido);

        // Prueba con credenciales inválidas
        var usuarioInvalido = dbContext.Users
            .AsEnumerable()
            .FirstOrDefault(u => u.Username == "testuser" && u.Password == "paSsword123");
        Assert.Null(usuarioInvalido);

        // Prueba con usuario inexistente
        var usuarioNoExiste = dbContext.Users
            .AsEnumerable()
            .FirstOrDefault(u => u.Username == "nouser" && u.Password == "password123");
        Assert.Null(usuarioNoExiste);
    }

    // Mock de ISnackbar
    private class MockSnackbar : ISnackbar
    {
        public List<string> Messages { get; } = new();
        public bool RequireInteraction { get; set; }
        public SnackbarConfiguration Configuration { get; } = new SnackbarConfiguration();
        public IEnumerable<Snackbar> ShownSnackbars => Array.Empty<Snackbar>();
        public event Action? OnSnackbarsUpdated;
        public void Add(string message, Severity severity = Severity.Normal) => Messages.Add(message);
        public void Add(RenderFragment message, Severity severity = Severity.Normal) { }
        public void Clear() { }
        public Snackbar? Add(string message, Severity severity = Severity.Normal, Action<SnackbarOptions>? configure = null, string? key = null) => null;
        public Snackbar? Add(MarkupString message, Severity severity = Severity.Normal, Action<SnackbarOptions>? configure = null, string? key = null) => null;
        public Snackbar? Add(RenderFragment message, Severity severity = Severity.Normal, Action<SnackbarOptions>? configure = null, string? key = null) => null;
        public Snackbar? Add<T>(Dictionary<string, object>? componentParameters = null, Severity severity = Severity.Normal, Action<SnackbarOptions>? configure = null, string? key = null) where T : IComponent => null;
        public void Remove(Snackbar snackbar) { }
        public void RemoveByKey(string key) { }
        public void Dispose() { }
    }

    // Mock de NavigationManager
    private class FakeNavigationManager : NavigationManager
    {
        public FakeNavigationManager() => Initialize("http://localhost/", "http://localhost/login");
        protected override void NavigateToCore(string uri, bool forceLoad) => Uri = ToAbsoluteUri(uri).ToString();
    }

    // Mock completo de ILocalStorageService para Blazored.LocalStorage v4.4.0+
    private class MockLocalStorageService : ILocalStorageService
    {
        private readonly Dictionary<string, object> _store = new();

        public ValueTask<T> GetItemAsync<T>(string key) =>
            new(_store.TryGetValue(key, out var value) ? (T)value : default!);

        public ValueTask<T> GetItemAsync<T>(string key, CancellationToken cancellationToken) =>
            GetItemAsync<T>(key);

        public ValueTask<string> GetItemAsStringAsync(string key) =>
            new(_store.TryGetValue(key, out var value) ? value?.ToString() ?? string.Empty : string.Empty);

        public ValueTask<string> GetItemAsStringAsync(string key, CancellationToken cancellationToken) =>
            GetItemAsStringAsync(key);

        public ValueTask SetItemAsync<T>(string key, T data)
        {
            _store[key] = data!;
            return ValueTask.CompletedTask;
        }

        public ValueTask SetItemAsync<T>(string key, T data, CancellationToken cancellationToken) =>
            SetItemAsync(key, data);

        public ValueTask SetItemAsStringAsync(string key, string data)
        {
            _store[key] = data;
            return ValueTask.CompletedTask;
        }

        public ValueTask SetItemAsStringAsync(string key, string data, CancellationToken cancellationToken) =>
            SetItemAsStringAsync(key, data);

        public ValueTask RemoveItemAsync(string key)
        {
            _store.Remove(key);
            return ValueTask.CompletedTask;
        }

        public ValueTask RemoveItemAsync(string key, CancellationToken cancellationToken) =>
            RemoveItemAsync(key);

        public ValueTask RemoveItemsAsync(IEnumerable<string> keys, CancellationToken cancellationToken)
        {
            foreach (var key in keys)
                _store.Remove(key);
            return ValueTask.CompletedTask;
        }

        public ValueTask ClearAsync()
        {
            _store.Clear();
            return ValueTask.CompletedTask;
        }

        public ValueTask ClearAsync(CancellationToken cancellationToken) =>
            ClearAsync();

        public ValueTask<bool> ContainKeyAsync(string key) =>
            new(_store.ContainsKey(key));

        public ValueTask<bool> ContainKeyAsync(string key, CancellationToken cancellationToken) =>
            ContainKeyAsync(key);

        public ValueTask<IEnumerable<string>> KeysAsync() =>
            new(_store.Keys.AsEnumerable());

        public ValueTask<IEnumerable<string>> KeysAsync(CancellationToken cancellationToken) =>
            KeysAsync();

        public ValueTask<string> KeyAsync(int index) =>
            new(_store.Keys.ElementAtOrDefault(index) ?? string.Empty);

        public ValueTask<string> KeyAsync(int index, CancellationToken cancellationToken) =>
            KeyAsync(index);

        public ValueTask<int> LengthAsync() =>
            new(_store.Count);

        public ValueTask<int> LengthAsync(CancellationToken cancellationToken) =>
            LengthAsync();

        // EVENTOS requeridos
        public event EventHandler<Blazored.LocalStorage.ChangedEventArgs>? Changed;
        public event EventHandler<Blazored.LocalStorage.ChangingEventArgs>? Changing;
    }

    // Agrega este método privado a la clase LoginTest para solucionar CS8801
    private static ApplicationDbContext CreateDbContext()
    {
        var inMemorySettings = new Dictionary<string, string?> {
            {"ConnectionStrings:DefaultConnection", "Server=localhost;Database=OmniMonitorTest;Trusted_Connection=True;TrustServerCertificate=True;"}
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var context = new ApplicationDbContext(configuration);

        context.Database.Migrate(); // <-- Aplica las migraciones aquí

        // Limpiar o preparar datos de prueba
        context.Users.RemoveRange(context.Users.ToList());
        context.Users.Add(new User
        {
            Username = "testuser",
            Password = "password123"
        });
        context.SaveChanges();

        return context;
    }
}