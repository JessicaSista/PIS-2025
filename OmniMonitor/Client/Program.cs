using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using OmniMonitor.Client;
using OmniMonitor.Client.Auth;
using OmniMonitor.Client.Services;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Asegúrate de que el nivel mínimo global esté configurado en Warning
builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Warning);
// Silenciar warnings relacionadas con solicitudes HTTP
builder.Logging.AddFilter(
    "Microsoft.AspNetCore.Components.WebAssembly.Http",
    Microsoft.Extensions.Logging.LogLevel.Warning);

// 1. Register the AuthHeaderHandler from the Canvas
builder.Services.AddScoped<AuthHeaderHandler>();

// 2. Configure a named HttpClient that automatically uses the handler
builder.Services.AddHttpClient("API", client =>
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
    .AddHttpMessageHandler<AuthHeaderHandler>();

// 3. Make the configured HttpClient the default one for injection
builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("API"));

// Configure JSON serialization options globally for HttpClient operations
builder.Services.Configure<JsonSerializerOptions>("HttpClientJsonOptions", options =>
{
    options.PropertyNameCaseInsensitive = true;
    options.Converters.Add(new JsonStringEnumConverter());
});



builder.Services.AddMudServices();

builder.Services.AddBlazoredLocalStorage();

builder.Services.AddAuthorizationCore();

builder.Services.AddScoped<AuthenticationStateProvider, ApiAuthenticationStateProvider>();

builder.Services.AddScoped<ThemeService>();

builder.Services.AddScoped<VisualizationDraftService>();

builder.Services.AddScoped<ShareLinkService>();

builder.Services.AddScoped<OmniMonitor.Client.Services.SignalR.TelemetryHubClient>();
builder.Services.AddScoped<OmniMonitor.Client.Services.SignalR.KpiHubClient>();

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// Configure JSON serialization options to handle enums as strings
builder.Services.Configure<JsonSerializerOptions>(options =>
{
    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.Converters.Add(new JsonStringEnumConverter());
});

// Agrega una instancia del servicio de inicializacion de cultura
builder.Services.AddScoped<CultureInitializer>();

var app = builder.Build();

// Obtiene el servicio de inicializacion y lo llama para configurar la cultura al inicio
var cultureInitializer = app.Services.GetRequiredService<CultureInitializer>();
await cultureInitializer.InitializeCultureAsync();

await app.RunAsync();
