using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using OmniMonitor.Server.Configuration;
using OmniMonitor.Server.Context;
using OmniMonitor.Server.Models;
using OmniMonitor.Server.Security;
using OmniMonitor.Server.Services;
using OmniMonitor.Shared.Dtos;

var builder = WebApplication.CreateBuilder(args);

ConfigurationManager configuration = builder.Configuration;

builder.Logging.ClearProviders();
builder.Logging.AddDebug();
builder.Logging.AddConsole();
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

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ASP.NET Core Identity Configuration - Solo usuarios (sin roles de Identity)
builder.Services.AddIdentityCore<User>(options => {
    options.Password.RequireDigit = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddSignInManager<SignInManager<User>>()
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

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("Token validado para usuario: {User}", context.Principal.Identity.Name);
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError(context.Exception, "Error al validar token");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

builder.Services.AddAuthorization(options =>
{
    // Módulo Users
    options.AddPolicy("Users.View", policy => policy.Requirements.Add(new PermissionRequirement("Users.View")));
    options.AddPolicy("Users.Create", policy => policy.Requirements.Add(new PermissionRequirement("Users.Create")));
    options.AddPolicy("Users.Edit", policy => policy.Requirements.Add(new PermissionRequirement("Users.Edit")));
    options.AddPolicy("Users.Delete", policy => policy.Requirements.Add(new PermissionRequirement("Users.Delete")));

    // Módulo Dashboards
    options.AddPolicy("Dashboards.View", policy => policy.Requirements.Add(new PermissionRequirement("Dashboards.View")));
    options.AddPolicy("Dashboards.Create", policy => policy.Requirements.Add(new PermissionRequirement("Dashboards.Create")));
    options.AddPolicy("Dashboards.Edit", policy => policy.Requirements.Add(new PermissionRequirement("Dashboards.Edit")));
    options.AddPolicy("Dashboards.Delete", policy => policy.Requirements.Add(new PermissionRequirement("Dashboards.Delete")));
    options.AddPolicy("Dashboards.Share", policy => policy.Requirements.Add(new PermissionRequirement("Dashboards.Share")));

    // Módulo Datasets
    options.AddPolicy("Datasets.View", policy => policy.Requirements.Add(new PermissionRequirement("Datasets.View")));
    options.AddPolicy("Datasets.Create", policy => policy.Requirements.Add(new PermissionRequirement("Datasets.Create")));
    options.AddPolicy("Datasets.Edit", policy => policy.Requirements.Add(new PermissionRequirement("Datasets.Edit")));
    options.AddPolicy("Datasets.Delete", policy => policy.Requirements.Add(new PermissionRequirement("Datasets.Delete")));

    // Módulo Visualizations
    options.AddPolicy("Visualizations.View", policy => policy.Requirements.Add(new PermissionRequirement("Visualizations.View")));
    options.AddPolicy("Visualizations.Create", policy => policy.Requirements.Add(new PermissionRequirement("Visualizations.Create")));
    options.AddPolicy("Visualizations.Edit", policy => policy.Requirements.Add(new PermissionRequirement("Visualizations.Edit")));
    options.AddPolicy("Visualizations.Delete", policy => policy.Requirements.Add(new PermissionRequirement("Visualizations.Delete")));

    // Módulo Reports
    options.AddPolicy("Reports.View", policy => policy.Requirements.Add(new PermissionRequirement("Reports.View")));
    options.AddPolicy("Reports.Create", policy => policy.Requirements.Add(new PermissionRequirement("Reports.Create")));
    options.AddPolicy("Reports.Edit", policy => policy.Requirements.Add(new PermissionRequirement("Reports.Edit")));
    options.AddPolicy("Reports.Delete", policy => policy.Requirements.Add(new PermissionRequirement("Reports.Delete")));
    options.AddPolicy("Reports.Export", policy => policy.Requirements.Add(new PermissionRequirement("Reports.Export")));

    // Módulo Sensors
    options.AddPolicy("Sensors.View", policy => policy.Requirements.Add(new PermissionRequirement("Sensors.View")));
    options.AddPolicy("Sensors.Configure", policy => policy.Requirements.Add(new PermissionRequirement("Sensors.Configure")));

    // Módulo Devices
    options.AddPolicy("Devices.View", policy => policy.Requirements.Add(new PermissionRequirement("Devices.View")));
    options.AddPolicy("Devices.Manage", policy => policy.Requirements.Add(new PermissionRequirement("Devices.Manage")));

    // Módulo Assets
    options.AddPolicy("Assets.View", policy => policy.Requirements.Add(new PermissionRequirement("Assets.View")));
    options.AddPolicy("Assets.Create", policy => policy.Requirements.Add(new PermissionRequirement("Assets.Create")));
    options.AddPolicy("Assets.Edit", policy => policy.Requirements.Add(new PermissionRequirement("Assets.Edit")));
    options.AddPolicy("Assets.Delete", policy => policy.Requirements.Add(new PermissionRequirement("Assets.Delete")));

    // Módulo Tasks
    options.AddPolicy("Tasks.View", policy => policy.Requirements.Add(new PermissionRequirement("Tasks.View")));
    options.AddPolicy("Tasks.Create", policy => policy.Requirements.Add(new PermissionRequirement("Tasks.Create")));
    options.AddPolicy("Tasks.Edit", policy => policy.Requirements.Add(new PermissionRequirement("Tasks.Edit")));
    options.AddPolicy("Tasks.Delete", policy => policy.Requirements.Add(new PermissionRequirement("Tasks.Delete")));

    // Módulo Zones
    options.AddPolicy("Zones.View", policy => policy.Requirements.Add(new PermissionRequirement("Zones.View")));
    options.AddPolicy("Zones.Manage", policy => policy.Requirements.Add(new PermissionRequirement("Zones.Manage")));

    // Módulo Events
    options.AddPolicy("Events.View", policy => policy.Requirements.Add(new PermissionRequirement("Events.View")));
    options.AddPolicy("Events.Manage", policy => policy.Requirements.Add(new PermissionRequirement("Events.Manage")));

    // Módulo Alerts
    options.AddPolicy("Alerts.View", policy => policy.Requirements.Add(new PermissionRequirement("Alerts.View")));
    options.AddPolicy("Alerts.Manage", policy => policy.Requirements.Add(new PermissionRequirement("Alerts.Manage")));

    // Módulo System
    options.AddPolicy("System.ViewRoles", policy => policy.Requirements.Add(new PermissionRequirement("System.ViewRoles")));
    options.AddPolicy("System.ManageRoles", policy => policy.Requirements.Add(new PermissionRequirement("System.ManageRoles")));
    options.AddPolicy("System.ViewPermissions", policy => policy.Requirements.Add(new PermissionRequirement("System.ViewPermissions")));
    options.AddPolicy("System.ManagePermissions", policy => policy.Requirements.Add(new PermissionRequirement("System.ManagePermissions")));
    options.AddPolicy("System.ViewLogs", policy => policy.Requirements.Add(new PermissionRequirement("System.ViewLogs")));
    options.AddPolicy("System.ViewSettings", policy => policy.Requirements.Add(new PermissionRequirement("System.ViewSettings")));
    options.AddPolicy("System.ManageSettings", policy => policy.Requirements.Add(new PermissionRequirement("System.ManageSettings")));
});

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
builder.Services.AddScoped<IPermissionService, PermissionService>();
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
builder.Services.AddScoped<IPasswordHasher<SharedLink>, PasswordHasher<SharedLink>>();
builder.Services.AddHttpClient();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "OmniMonitor", Version = builder.Configuration["Version"] });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Por favor, introduce 'Bearer' seguido de un espacio y tu token",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });


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

        logger.LogInformation("Iniciando seeding de usuarios y roles...");

        await context.Database.MigrateAsync();

        string adminUsername = "admin";
        string adminPassword = "adminadmin";
        var adminUser = await userManager.FindByNameAsync(adminUsername);
        
        if (adminUser == null)
        {
            adminUser = new User
            {
                UserName = adminUsername,
                Email = "admin@omnimonitor.com",
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
        else 
        { 
            logger.LogInformation($"Usuario '{adminUsername}' ya existe."); 
        }

        if (adminUser != null)
        {
            var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
            if (adminRole != null)
            {
                var existingUserRole = await context.UserRoles
                    .FirstOrDefaultAsync(ur => ur.UserId == adminUser.Id && ur.RoleId == adminRole.Id);
                
                if (existingUserRole == null)
                {
                    var userRole = new UserRole
                    {
                        UserId = adminUser.Id,
                        RoleId = adminRole.Id
                    };
                    context.UserRoles.Add(userRole);
                    await context.SaveChangesAsync();
                    logger.LogInformation($"Rol 'Admin' asignado al usuario '{adminUsername}'.");
                }
                else
                {
                    logger.LogInformation($"Usuario '{adminUsername}' ya tiene el rol 'Admin'.");
                }
            }
            else
            {
                logger.LogWarning("Rol 'Admin' no encontrado en la base de datos. Asegúrate de que las migraciones se hayan ejecutado correctamente.");
            }
        }

        logger.LogInformation("Seeding de usuarios y roles completado.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Ocurri� un error durante el seeding de la base de datos.");
    }
}

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

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();

