using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OmniMonitor.Server.Configuration;
using OmniMonitor.Server.Context;
using OmniMonitor.Server.Services;
using System.Text;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

ConfigurationManager configuration = builder.Configuration;

// --- Logging Configuration ---
builder.Logging.ClearProviders();
builder.Logging.AddDebug();
builder.Logging.AddConsole();
builder.Configuration.AddJsonFile("ApiConfig.json", optional: false, reloadOnChange: true);
builder.Services.Configure<ApiConfig>(builder.Configuration);

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

// --- Add services to the container ---
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

// --- JWT AUTHENTICATION SERVICES WITH DEBUGGING ---
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        // --- DEBUGGING EVENTS ---
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("--- Token validation SUCCEEDED for user: {User}", context.Principal.Identity.Name);
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError(context.Exception, "--- Token validation FAILED ---");
                return Task.CompletedTask;
            }
        };
        // --- END OF DEBUGGING EVENTS ---
    });
// --- END OF JWT SECTION ---


builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;
    });
builder.Services.AddRazorPages();

builder.Services.AddScoped<ISondaAuthService, SondaAuthService>();
builder.Services.AddScoped<ISondaIMService, SondaIMService>();
builder.Services.AddScoped<ISondaUMService, SondaUMService>();
builder.Services.AddScoped<ISondaAMService, SondaAMService>();
builder.Services.AddScoped<ISondaEMService, SondaEMService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();
builder.Services.AddScoped<IDatasetService, DatasetService>();
builder.Services.AddScoped<IDatasetUMService, DatasetUMService>();
builder.Services.AddScoped<IVisualizacionService, VisualizacionService>();
builder.Services.AddHttpClient();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "OmniMonitor", Version = builder.Configuration["Version"] });
});

var app = builder.Build();

// --- Configure the HTTP request pipeline ---
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

if (configuration.GetValue<bool>("EnableHttpsRedirection"))
{
    app.UseHttpsRedirection();
}

app.UseBlazorFrameworkFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
        ctx.Context.Response.Headers.Append("Pragma", "no-cache");
        ctx.Context.Response.Headers.Append("Expires", "0");
    }
});

app.UseRouting();
app.UseCors(corsPolicy);

// --- ADD AUTHENTICATION MIDDLEWARE (ORDER IS CRITICAL) ---
app.UseAuthentication();
app.UseAuthorization();
// --- END OF MIDDLEWARE SECTION ---

app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();

