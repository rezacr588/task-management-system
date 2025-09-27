using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using TodoApi.Infrastructure.Data;

namespace TodoApi.Tests.E2E.Infrastructure
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName;

        public CustomWebApplicationFactory()
        {
            // Generate a unique database name for each test run
            _databaseName = $"todoapi_test_{Guid.NewGuid().ToString("N")}";
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            // Override the connection string to use a unique database
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.Testing.json", optional: false, reloadOnChange: true);
            });

            builder.ConfigureServices(services =>
            {
                // Replace the DbContext with our unique database
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseNpgsql($"Host=localhost;Port=5432;Database={_databaseName};Username=postgres;Password=postgres");
                });

                // Disable response caching for tests
                services.AddControllers(options =>
                {
                    // Remove or modify response cache filters
                    var cacheFilters = options.Filters.Where(f => f is ResponseCacheAttribute).ToList();
                    foreach (var filter in cacheFilters)
                    {
                        options.Filters.Remove(filter);
                    }
                });

                // Register domain services for testing
                services.AddScoped<TodoApi.Domain.Services.IActivityLogger, TodoApi.Domain.Services.ActivityLogger>();
            });
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            var host = base.CreateHost(builder);

            // Ensure test database is created and seeded
            using var scope = host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Ensure database is clean and apply migrations
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            // Seed test data
            DbSeeder.SeedAsync(db).GetAwaiter().GetResult();

            return host;
        }

        public async Task ResetDatabaseAsync()
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Close all connections to the database
            await db.Database.ExecuteSqlRawAsync("SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = current_database() AND pid <> pg_backend_pid();");

            // Drop and recreate the database
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();

            // Reseed the database
            await DbSeeder.SeedAsync(db);
        }
    }
}
