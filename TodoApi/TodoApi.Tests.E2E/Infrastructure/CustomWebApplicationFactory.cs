using System;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TodoApi.Domain.Entities;
using TodoApi.Domain.Enums;
using TodoApi.Infrastructure.Data;

namespace TodoApi.Tests.E2E.Infrastructure
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                var dbContextDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (dbContextDescriptor != null)
                {
                    services.Remove(dbContextDescriptor);
                }

                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase($"E2E-Db-{Guid.NewGuid()}");
                });

                using var scope = services.BuildServiceProvider().CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Database.EnsureCreated();
                SeedData(db);
            });
        }

        private static void SeedData(ApplicationDbContext context)
        {
            if (context.Users.Any())
            {
                return;
            }

            var user = new User
            {
                Id = 1,
                Name = "E2E User",
                Email = "e2e@example.com",
                PasswordHash = "hash",
                BiometricToken = "token",
                Role = "Admin"
            };

            var todo = new TodoItem
            {
                Id = 1,
                Title = "E2E Todo",
                Description = "End to end task",
                AssignedToUser = user,
                AssignedToUserId = user.Id,
                Priority = PriorityLevel.High
            };

            context.Users.Add(user);
            context.TodoItems.Add(todo);
            context.SaveChanges();
        }
    }
}
