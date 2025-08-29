using OmniMonitor.Client;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// --- CHANGE THIS LINE ---
// Replace the old BaseAddress with your server's actual IP and port.
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri("http://190.134.208.255:5228")
});
// ------------------------

builder.Services.AddMudServices();

await builder.Build().RunAsync();