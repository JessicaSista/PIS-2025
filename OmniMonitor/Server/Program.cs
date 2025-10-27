using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OmniMonitor.Server.Configuration;
using OmniMonitor.Server.Context;
using OmniMonitor.Server.Services;
using OmniMonitor.Shared.Dtos;
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
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. ASP.NET Core Identity Configuration (AÑADIDO)
builder.Services.AddIdentity<User, IdentityRole<int>>(options => {
    options.Password.RequireDigit = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

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
builder.Services.AddScoped<IDatasetService, DatasetIMService>();
builder.Services.AddScoped<IDatasetAmService, DatasetAmService>();
builder.Services.AddScoped<IDatasetUMService, DatasetUMService>();
builder.Services.AddScoped<IDatasetEMService, DatasetEMService>();
builder.Services.AddScoped<IVisualizacionService, VisualizacionService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IApiDataService, ApiDataService>();
builder.Services.AddScoped<IJoinConfigurationService, JoinConfigurationService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IKpiService, KpiService>();
builder.Services.AddScoped<IKpiAMService, KpiAMService>();
builder.Services.AddHttpClient();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "OmniMonitor", Version = builder.Configuration["Version"] });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var userManager = services.GetRequiredService<UserManager<User>>();
        var context = services.GetRequiredService<ApplicationDbContext>();

        logger.LogInformation("Iniciando seeding de usuarios...");

        await context.Database.MigrateAsync();

        // --- Crear usuario 'admin' ---
        string adminUsername = "admin";
        string adminPassword = "adminadmin";
        if (await userManager.FindByNameAsync(adminUsername) == null)
        {
            var adminUser = new User
            {
                UserName = adminUsername,
                Email = "IgnacioLavagnino@omnimonitor.com",
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                logger.LogInformation($"Usuario '{adminUsername}' creado exitosamente.");
            }
            else
            {
                logger.LogError($"Error al crear usuario '{adminUsername}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
        else { logger.LogInformation($"Usuario '{adminUsername}' ya existe."); }

        // --- Crear usuario 'visitante' ---
        string visitorUsername = "visitante";
        string visitorPassword = "visitante";
        if (await userManager.FindByNameAsync(visitorUsername) == null)
        {
            var visitorUser = new User
            {
                UserName = visitorUsername,
                Email = "visitante@omnimonitor.com",
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(visitorUser, visitorPassword);
            if (result.Succeeded)
            {
                logger.LogInformation($"Usuario '{visitorUsername}' creado exitosamente.");
            }
            else
            {
                logger.LogError($"Error al crear usuario '{visitorUsername}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
        else { logger.LogInformation($"Usuario '{visitorUsername}' ya existe."); }

        logger.LogInformation("Seeding de usuarios completado.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Ocurrió un error durante el seeding de la base de datos.");
    }
}

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

