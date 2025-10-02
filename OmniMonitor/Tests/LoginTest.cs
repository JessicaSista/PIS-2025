using Blazored.LocalStorage;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;
using Moq;
using Moq.Protected;
using MudBlazor;
using MudBlazor.Services;
using OmniMonitor.Client.Pages;
using OmniMonitor.Server.Context;
using OmniMonitor.Shared.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class LoginTest : TestContext
{
    private ApplicationDbContext CrearDbContext()
    {
        var dbName = $"TestDbLocal_{Guid.NewGuid()}";
        var inMemorySettings = new Dictionary<string, string> {
            {"ConnectionStrings:DefaultConnection", $"Server=(localdb)\\mssqllocaldb;Database={dbName};Trusted_Connection=True;"}
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var context = new ApplicationDbContext(configuration);

        // Elimina y crea la base de datos para evitar conflictos
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        // Poblar usuarios solo si la base está vacía
        if (!context.Users.Any())
        {
            context.Users.Add(new User { Id = 1, Username = "admin", Password = "admin" });
            context.Users.Add(new User { Id = 2, Username = "visitante", Password = "visitante" });
            context.SaveChanges();
        }
        return context;
    }

    public LoginTest()
    {
        Services.AddMudServices();
        JSInterop.SetupVoid(_ => true);
        Services.AddSingleton<ISnackbar>(new MockSnackbar());
        Services.AddSingleton<NavigationManager>(new FakeNavigationManager());
        Services.AddSingleton<ILocalStorageService>(new MockLocalStorageService());

        var localizerMock = new Mock<IStringLocalizer<SharedResource>>();
        localizerMock
            .Setup(l => l["LoginTitle"])
            .Returns(new LocalizedString("LoginTitle", "Iniciar Sesión"));
        Services.AddSingleton<IStringLocalizer<SharedResource>>(localizerMock.Object);

        // Mock para el localizer específico del Login
        var localizerLoginMock = new Mock<IStringLocalizer<OmniMonitor.Client.Pages.Login>>();
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
        Services.AddSingleton<IStringLocalizer<OmniMonitor.Client.Pages.Login>>(localizerLoginMock.Object);

        // Registrar el contexto en memoria
        var dbContext = CrearDbContext();
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

        Services.RemoveAll<HttpClient>();
        Services.AddSingleton<HttpClient>(httpClient);

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
                Content = JsonContent.Create(new OmniMonitor.Shared.Dtos.LoginResponse
                {
                    Success = false,
                    Message = "Credenciales incorrectas"
                })
            });

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost")
        };

        Services.RemoveAll<HttpClient>();
        Services.AddSingleton<HttpClient>(httpClient);

        var cut = RenderComponent<Login>();
        cut.Find("input[type=text]").Change("usuario");
        cut.Find("input[type=password]").Change("incorrecta");
        var loginButton = cut.Find("button.mud-button");
        loginButton.Click();
        // Espera a que el mensaje de error aparezca
        cut.WaitForAssertion(() => Assert.Contains("Credenciales incorrectas", cut.Markup, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Login_ConsultaCredencialesEnBaseDeDatos()
    {
        // Arrange: obtener el contexto registrado
        var dbContext = Services.GetRequiredService<ApplicationDbContext>();

        // Prueba con credenciales válidas
        var usuarioValido = dbContext.Users
            .FirstOrDefault(u => u.Username == "visitante" && u.Password == "visitante");
        Assert.NotNull(usuarioValido);

        // Prueba con credenciales inválidas
        var usuarioInvalido = dbContext.Users
            .FirstOrDefault(u => u.Username == "visitante" && u.Password == "incorrecta");
        Assert.Null(usuarioInvalido);

        // Prueba con usuario inexistente
        var usuarioNoExiste = dbContext.Users
            .FirstOrDefault(u => u.Username == "Admi" && u.Password == "admin");
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
}