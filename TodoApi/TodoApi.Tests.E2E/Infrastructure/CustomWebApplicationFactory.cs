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
using TodoApi.Infrastructure.Data;

namespace TodoApi.Tests.E2E.Infrastructure
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        private const string TestDatabaseName = "todoapi_test";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            // Use the testing configuration which has the test database connection
            builder.ConfigureAppConfiguration((context, config) =>
            {
                // This will load appsettings.Testing.json
                config.AddJsonFile("appsettings.Testing.json", optional: false, reloadOnChange: true);
            });
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            var host = base.CreateHost(builder);

            // Ensure test database is created and seeded
            using var scope = host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Ensure database exists and apply migrations
            db.Database.EnsureCreated();

            // Seed test data
            DbSeeder.SeedAsync(db).GetAwaiter().GetResult();

            return host;
        }

        public async Task ResetDatabaseAsync()
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Clean all data from tables but keep schema
            await db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"ActivityLogEntries\", \"Comments\", \"TodoItems\", \"Tags\", \"Users\" RESTART IDENTITY CASCADE");

            // Reseed the database
            await DbSeeder.SeedAsync(db);
        }
    }
}
