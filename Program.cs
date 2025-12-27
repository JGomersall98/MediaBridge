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
using Microsoft.IdentityModel.Tokens;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;


var builder = WebApplication.CreateBuilder(args);

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

// DB migration flag (read from appsettings.*). We'll use this to decide whether to register the hosted service.
var applyMigrations = builder.Configuration.GetValue<bool>("ApplyDbMigrations", false);

// Background Services
if (!applyMigrations)
{
    builder.Services.AddHostedService<DownloadQueueBackgroundService>();
}

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
    Console.WriteLine("Error while attempting to apply migrations: " + ex);
    throw;
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

app.Run();
