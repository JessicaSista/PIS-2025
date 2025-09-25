using OmniMonitor.Client;
using OmniMonitor.Client.Shared;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Localization;
using System.Globalization;
using MudBlazor.Services;
using Blazored.LocalStorage;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddMudServices();

builder.Services.AddBlazoredLocalStorage();

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// Agrega una instancia del servicio de inicialización de cultura
builder.Services.AddScoped<CultureInitializer>();

var app = builder.Build();

// Obtiene el servicio de inicialización y lo llama para configurar la cultura al inicio
var cultureInitializer = app.Services.GetRequiredService<CultureInitializer>();
await cultureInitializer.InitializeCultureAsync();

await app.RunAsync();