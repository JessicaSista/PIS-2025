using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using OmniMonitor.Client;
using OmniMonitor.Client.Auth;
using System.Net.Http;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// --- REMOVE THE OLD HTTPCLIENT REGISTRATION ---
// builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });


// --- ADD THE NEW HTTPCLIENT FACTORY CONFIGURATION ---

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

await builder.Build().RunAsync();
