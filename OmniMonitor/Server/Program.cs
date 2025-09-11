using OmniMonitor.Server.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

ConfigurationManager configuration = builder.Configuration;

builder.Logging.ClearProviders();
builder.Logging.AddDebug();
builder.Logging.AddConsole();
if (OperatingSystem.IsWindows())
{
    builder.Logging.AddEventLog(eventLogSettings =>
    {
        if (OperatingSystem.IsWindows())
        {
            eventLogSettings.LogName = "SONDA";
            eventLogSettings.SourceName = "OmniMonitor";
        }
    });
}
builder.Logging.AddAzureWebAppDiagnostics();

// Add services to the container.

// https://learn.microsoft.com/en-us/ef/
// https://www.entityframeworktutorial.net/efcore/entity-framework-core.aspx
builder.Services.AddDbContext<ApplicationDbContext>();

string corsPolicy = "CORSPolicy";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: corsPolicy,
    policy =>
    {
        policy.WithOrigins(builder.Configuration.GetSection("CORS").Get<string[]>()!)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddScoped<ISondaAuthService, SondaAuthService>();
//builder.Services.AddSingleton<ISondaApiGetDevicesService, SondaApiGetDevicesService>();
builder.Services.AddScoped<ISondaApiGetDevicesService, SondaApiGetDevicesService>();
builder.Services.AddHttpClient();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "OmniMonitor", Version = builder.Configuration["Version"] });
});

WebApplication app = builder.Build();

// Corre automáticamente las migraciones en local, se hace de esta manera para mantener la misma "lógica" que el ambiente remoto
if (configuration.GetValue<bool>("Development") || app.Environment.IsDevelopment())
{
    using IServiceScope scope = app.Services.CreateScope();
    ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();

    app.UseWebAssemblyDebugging();
    app.UseSwagger();
    app.UseSwaggerUI();
}


// Configure the HTTP request pipeline.
if (configuration.GetValue<bool>("Development") || app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

if (configuration.GetValue<bool>("EnableHttpsRedirection"))
{
    app.UseHttpsRedirection();
}

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();
app.UseCors(corsPolicy);

app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
