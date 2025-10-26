using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using OmniMonitor.Client;
using OmniMonitor.Client.Auth;
using OmniMonitor.Client.Services;
using System.Net.Http;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");


// --- ADD THE HTTPCLIENT FACTORY CONFIGURATION ---

// 1. Register the AuthHeaderHandler from the Canvas
builder.Services.AddScoped<AuthHeaderHandler>();

// 2. Configure a named HttpClient that automatically uses the handler
builder.Services.AddHttpClient("API", client =>
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
    .AddHttpMessageHandler<AuthHeaderHandler>();

// 3. Make the configured HttpClient the default one for injection
builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("API"));

// --- END OF HTTPCLIENT CONFIGURATION ---


builder.Services.AddMudServices();

builder.Services.AddBlazoredLocalStorage();

builder.Services.AddAuthorizationCore();

builder.Services.AddScoped<AuthenticationStateProvider, ApiAuthenticationStateProvider>();

builder.Services.AddScoped<ThemeService>();

builder.Services.AddScoped<VisualizationDraftService>();

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");


// Agrega una instancia del servicio de inicializacion de cultura
builder.Services.AddScoped<CultureInitializer>();

var app = builder.Build();

// Obtiene el servicio de inicializacion y lo llama para configurar la cultura al inicio
var cultureInitializer = app.Services.GetRequiredService<CultureInitializer>();
await cultureInitializer.InitializeCultureAsync();

await app.RunAsync();
