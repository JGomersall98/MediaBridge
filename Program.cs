using System.IO;
using System.Security.Claims;
using System.Text;
using MediaBridge.Database;
using MediaBridge.Models.Authentication;
using MediaBridge.Services.Admin;
using MediaBridge.Services.Authentication;
using MediaBridge.Services.Background;
using MediaBridge.Services.Dashboard;
using MediaBridge.Services.Helpers;
using MediaBridge.Services.Media;
using MediaBridge.Services.Media.Downloads;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Infrastructure", LogEventLevel.Warning)
    .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/application-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] [PID:{ProcessId}] [TID:{ThreadId}] {Message:lj}{NewLine}{Exception}",
        restrictedToMinimumLevel: LogEventLevel.Information)
    .WriteTo.Logger(sqlLogger => sqlLogger
    .Filter.ByIncludingOnly(evt => evt.Properties.ContainsKey("SourceContext") &&
        evt.Properties["SourceContext"].ToString().Contains("Microsoft.EntityFrameworkCore"))
    .WriteTo.File("logs/sql-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [SQL] [PID:{ProcessId}] [TID:{ThreadId}] {Message:lj}{NewLine}{Exception}",
            restrictedToMinimumLevel: LogEventLevel.Information))
    .Enrich.WithProcessId()
    .Enrich.WithThreadId()
    .CreateLogger();

// Clear default providers and use Serilog
builder.Host.UseSerilog();

// Add services to the container.
IConfiguration configuration = builder.Configuration;

builder.Services.AddControllers();

var jwtSection = builder.Configuration.GetSection("Jwt");
builder.Services.Configure<JwtOptions>(jwtSection);
var key = Encoding.UTF8.GetBytes(jwtSection["Key"]!);

builder.Services
 .AddAuthentication(options =>
 {
     options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
     options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
 })
 .AddJwtBearer(options =>
 {
     options.TokenValidationParameters = new TokenValidationParameters
     {
         ValidateIssuer = true,
         ValidateAudience = true,
         ValidateLifetime = true,
         ValidateIssuerSigningKey = true,
         ValidIssuer = jwtSection["Issuer"],
         ValidAudience = jwtSection["Audience"],
         IssuerSigningKey = new SymmetricSecurityKey(key),
         ClockSkew = TimeSpan.Zero,
         RoleClaimType = ClaimTypes.Role
     };
 });

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
var connectionString = configuration.GetConnectionString("MediaBridgeDb");
builder.Services.AddDbContext<MediaBridgeDbContext>(options =>
{
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
    // Enable SQL logging for Entity Framework
    options.LogTo(Log.Logger.Information, LogLevel.Information);
});

// Health Checks
builder.Services.AddHealthChecks()
 .AddCheck("self", () => HealthCheckResult.Healthy("Application is running"))
 .AddDbContextCheck<MediaBridgeDbContext>("database", failureStatus: HealthStatus.Degraded);

// Services
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<IMediaService, MediaService>();
builder.Services.AddScoped<IDownloadProcessorService, DownloadProcessorService>();
builder.Services.AddScoped<IRequestDownloadStatusService, RequestDownloadStatusService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddTransient<IUtilService, UtilService>();

builder.Services.AddTransient<ICaching, Caching>();
builder.Services.AddTransient<IHttpClientService, HttpClientService>();
builder.Services.AddTransient<IGetConfig, GetConfig>();
builder.Services.AddHostedService<DownloadQueueBackgroundService>();

var applyMigrations = builder.Configuration.GetValue<bool>("ApplyDbMigrations", false);

// HttpClient
builder.Services.AddHttpClient();

var app = builder.Build();

try
{
    if (applyMigrations)
    {
        using (var scope = app.Services.CreateScope())
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            try
            {
                logger.LogWarning("ApplyDbMigrations={ApplyDbMigrations}", applyMigrations);
                logger.LogInformation("ApplyDbMigrations enabled - applying pending EF Core migrations...");
                var db = scope.ServiceProvider.GetRequiredService<MediaBridgeDbContext>();
                db.Database.Migrate();
                logger.LogInformation("Database migrations applied successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while applying database migrations.");
                throw;
            }
        }
    }
}
catch (Exception ex)
{
    Log.Fatal(ex, "Error while attempting to apply migrations");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

try
{
    Log.Information("Starting MediaBridge application");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
