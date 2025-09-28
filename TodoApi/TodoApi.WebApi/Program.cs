using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using TodoApi.Infrastructure.Data;
using TodoApi.Application.Interfaces;
using TodoApi.Application.Services;
using TodoApi.Infrastructure.Services;
using TodoApi.Infrastructure.Repositories;
using TodoApi.Application.Mappers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using TodoApi.WebApi.Filters;
using FluentValidation.AspNetCore;
using FluentValidation;
using TodoApi.WebApi.HealthChecks;
using AspNetCoreRateLimit;
using Microsoft.OpenApi;
using System.IO;
using Microsoft.AspNetCore.Identity;
using TodoApi.Domain.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TodoApi.Infrastructure.Logging;
using StackExchange.Redis;
using Serilog;
using Serilog.Events;
using System.Diagnostics.Metrics;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog for structured logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("System", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "TodoApi")
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File("logs/todoapi-.log", 
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// Validate required configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Database connection string 'DefaultConnection' is required but not configured.");
}

// Add database context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString);

    // Enable sensitive data logging only in development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Add JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtKey))
{
    throw new InvalidOperationException("JWT Key is required but not configured. Please set 'Jwt:Key' in configuration.");
}

builder.Services.AddAuthentication(options =>
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
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Add caching services
builder.Services.AddMemoryCache();

// Configure Redis cache (with fallback to in-memory)
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrEmpty(redisConnectionString))
{
    try
    {
        builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var configuration = ConfigurationOptions.Parse(redisConnectionString);
            return ConnectionMultiplexer.Connect(configuration);
        });
        builder.Services.AddScoped<ICacheService, RedisCacheService>();
        Log.Information("Redis cache configured successfully");
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Failed to configure Redis cache, falling back to in-memory cache");
        builder.Services.AddScoped<ICacheService, InMemoryCacheService>();
    }
}
else
{
    Log.Information("No Redis connection string found, using in-memory cache");
    builder.Services.AddScoped<ICacheService, InMemoryCacheService>();
}

// Add metrics and observability
builder.Services.AddSingleton<IMetricsCollector, MetricsCollector>();
builder.Services.AddScoped(typeof(IStructuredLogger<>), typeof(StructuredLogger<>));

// Add health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "database", tags: new[] { "db", "sql" })
    .AddCheck<ApplicationHealthCheck>("application", tags: new[] { "app" });

// Add Redis health check if Redis is configured
if (!string.IsNullOrEmpty(redisConnectionString))
{
    builder.Services.AddHealthChecks().AddRedis(redisConnectionString, name: "redis", tags: new[] { "cache", "redis" });
}

// Add rate limiting
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();

// Add API versioning
builder.Services.AddApiVersioning(options =>
{
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.ReportApiVersions = true;
});

// Add services to DI container
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITodoItemService, TodoItemService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IActivityLogService, ActivityLogService>();
builder.Services.AddScoped<ITagService, TagService>();

// CQRS Services
builder.Services.AddScoped<ITodoItemQueryService, TodoItemQueryService>();
builder.Services.AddScoped<ITodoItemCommandService, TodoItemCommandService>();

// Event Sourcing
builder.Services.AddScoped<IEventStore, PostgreSqlEventStore>();

// Search Services
builder.Services.AddScoped<ISearchService, PostgreSqlFullTextSearchService>();

// File Storage
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();

// Authentication and Authorization
builder.Services.AddScoped<ITokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<ITokenValidator, BiometricTokenValidator>();
builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITodoItemRepository, TodoItemRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<IActivityLogRepository, ActivityLogRepository>();
builder.Services.AddScoped<ITagRepository, TagRepository>();
builder.Services.AddScoped<TodoApi.Domain.Services.IActivityLogger, TodoApi.Domain.Services.ActivityLogger>();

// Background Jobs
builder.Services.AddScoped<IBackgroundJobService, HangfireBackgroundJobService>();
builder.Services.AddScoped<INotificationJobService, NotificationJobService>();
builder.Services.AddScoped<IMaintenanceJobService, MaintenanceJobService>();

// Real-time notifications
builder.Services.AddScoped<ITodoItemNotificationService, TodoItemNotificationService>();

// Add SignalR
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
});

// Add Hangfire
builder.Services.AddHangfire(config =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    config.UseNpgsqlStorage(connectionString, new Hangfire.PostgreSql.PostgreSqlStorageOptions
    {
        SchemaName = "hangfire"
    });
});

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = Environment.ProcessorCount;
    options.Queues = new[] { "default", "notifications", "maintenance", "reports" };
});

// Register the new TagSuggestionService
builder.Services.AddSingleton<ITagSuggestionService>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var endpoint = config["TextAnalytics:Endpoint"];
    var apiKey = config["TextAnalytics:ApiKey"];

    if (string.IsNullOrEmpty(endpoint) || endpoint.Contains("YOUR_"))
    {
        // Return a dummy implementation if not configured
        return new TagSuggestionService(null, null, useDummy: true);
    }

    return new TagSuggestionService(endpoint, apiKey);
});

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(CollaborationProfile).Assembly);

// Add FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssembly(typeof(TodoApi.Application.Validators.TodoItemDtoValidator).Assembly);

// Add MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(TodoApi.Domain.Events.DomainEvent).Assembly));

// Add controllers with API behavior
builder.Services.AddControllers(options =>
{
    options.SuppressAsyncSuffixInActionNames = false;
    options.Filters.Add<GlobalExceptionFilter>();
})
.AddXmlDataContractSerializerFormatters(); // Add XML content negotiation

// Add response caching
builder.Services.AddResponseCaching();

// Configure API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Todo API",
        Version = "v1",
        Description = "A production-ready Todo API with full CRUD operations, JWT authentication, and comprehensive task management features",
        Contact = new OpenApiContact
        {
            Name = "Todo API Support",
            Email = "support@todoapi.com"
        },
        License = new OpenApiLicense
        {
            Name = "MIT License"
        }
    });

    // Include XML comments
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Add JWT Bearer token support
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Todo API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at root
    });
}

// Add request/response logging middleware
app.UseMiddleware<RequestResponseLoggingMiddleware>();

// Security headers
app.UseHttpsRedirection();

// Rate limiting
app.UseIpRateLimiting();

// HTTPS enforcement
app.UseHttpsRedirection();
app.UseHsts();

// Response caching
app.UseResponseCaching();

// Health check endpoint
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/database", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("db")
});
app.MapHealthChecks("/health/application", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("app")
});
app.MapHealthChecks("/health/cache", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("cache")
});

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

// Map SignalR hub
app.MapHub<TodoItemHub>("/hubs/todoitems");

// Setup Hangfire dashboard (only in development or with proper authentication)
if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new HangfireAuthorizationFilter() }
    });
}

// Configure recurring jobs
ConfigureRecurringJobs(app.Services);

// Ensure database is created in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await context.Database.EnsureCreatedAsync();
}

// Log startup completion
Log.Information("TodoApi started successfully");

app.Run();

// Ensure Serilog flushes on shutdown
Log.CloseAndFlush();

public partial class Program { }

// Helper method to configure recurring jobs
static void ConfigureRecurringJobs(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var backgroundJobService = scope.ServiceProvider.GetRequiredService<IBackgroundJobService>();
    
    // Schedule recurring jobs
    backgroundJobService.AddOrUpdateRecurringJob<INotificationJobService>(
        "overdue-notifications", 
        service => service.SendOverdueNotificationsAsync(), 
        "0 8 * * *"); // Daily at 8 AM
    
    backgroundJobService.AddOrUpdateRecurringJob<INotificationJobService>(
        "due-today-notifications", 
        service => service.SendDueTodayNotificationsAsync(), 
        "0 7 * * *"); // Daily at 7 AM
    
    backgroundJobService.AddOrUpdateRecurringJob<INotificationJobService>(
        "weekly-digest", 
        service => service.SendWeeklyDigestAsync(), 
        "0 9 * * 1"); // Monday at 9 AM
    
    backgroundJobService.AddOrUpdateRecurringJob<INotificationJobService>(
        "email-queue-processing", 
        service => service.ProcessEmailQueueAsync(), 
        "*/5 * * * *"); // Every 5 minutes
    
    backgroundJobService.AddOrUpdateRecurringJob<INotificationJobService>(
        "data-cleanup", 
        service => service.CleanupOldDataAsync(), 
        "0 2 * * 0"); // Sunday at 2 AM
    
    backgroundJobService.AddOrUpdateRecurringJob<INotificationJobService>(
        "generate-reports", 
        service => service.GenerateReportsAsync(), 
        "0 3 1 * *"); // First day of month at 3 AM
    
    backgroundJobService.AddOrUpdateRecurringJob<IMaintenanceJobService>(
        "database-maintenance", 
        service => service.OptimizeDatabaseAsync(), 
        "0 1 * * 0"); // Sunday at 1 AM
}

// Hangfire authorization filter for development
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // In development, allow all access
        // In production, implement proper authorization
        return true;
    }
}
