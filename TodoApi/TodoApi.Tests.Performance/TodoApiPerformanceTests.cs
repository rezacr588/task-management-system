using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using NBomber.Contracts;
using NBomber.CSharp;
using TodoApi.Application.DTOs;
using TodoApi.Domain.Enums;
using TodoApi.WebApi;

namespace TodoApi.Tests.Performance;

public class TodoApiPerformanceTests
{
    private static string? _userToken;
    private static int _userId;

    [Fact]
    public void TodoApiLoadTest()
    {
        // Setup test application factory
        var factory = new WebApplicationFactory<Program>();

        // Register and login once for all scenarios
        SetupTestUser(factory).GetAwaiter().GetResult();

        // Define the load test scenario
        var scenario = Scenario.Create("todo_operations_load_test", async context =>
        {
            using var client = factory.CreateClient();

            // Set authorization header
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _userToken);

            // Create a new todo item
            var createRequest = new
            {
                Title = $"Load Test Task {Guid.NewGuid()}",
                Description = "Performance testing task creation"
            };

            var createResponse = await client.PostAsJsonAsync("/api/v1/TodoItems", createRequest);

            if (!createResponse.IsSuccessStatusCode)
            {
                return Response.Fail($"Create failed: {createResponse.StatusCode}");
            }

            var createdTask = await createResponse.Content.ReadFromJsonAsync<dynamic>();
            var taskId = (int)createdTask!.id;

            // Read the created task
            var getResponse = await client.GetAsync($"/api/v1/TodoItems/{taskId}");
            if (!getResponse.IsSuccessStatusCode)
            {
                return Response.Fail($"Get failed: {getResponse.StatusCode}");
            }

            // Update the task to mark as completed
            var updateRequest = new
            {
                Title = $"Load Test Task {taskId}",
                Description = "Performance testing task creation",
                IsComplete = true
            };

            var updateResponse = await client.PutAsJsonAsync($"/api/v1/TodoItems/{taskId}", updateRequest);
            if (!updateResponse.IsSuccessStatusCode)
            {
                return Response.Fail($"Update failed: {updateResponse.StatusCode}");
            }

            // Delete the task
            var deleteResponse = await client.DeleteAsync($"/api/v1/TodoItems/{taskId}");
            if (!deleteResponse.IsSuccessStatusCode)
            {
                return Response.Fail($"Delete failed: {deleteResponse.StatusCode}");
            }

            return Response.Ok();
        })
        .WithoutWarmUp()
        .WithLoadSimulations(
            Simulation.Inject(rate: 10, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
            Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
            Simulation.Inject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30))
        );

        // Run the load test
        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        // Assert performance requirements
        var scenarioStats = stats.ScenarioStats[0];

        // Less than 5% failure rate
        Assert.True(scenarioStats.Fail.Request.Count / (double)scenarioStats.AllRequestCount < 0.05);

        // Average response time under 200ms
        Assert.True(scenarioStats.Ok.Request.RPS.Avg < 200);

        // 95th percentile under 500ms
        Assert.True(scenarioStats.Ok.Latency.Percent95 < 500);
    }

    private static async Task SetupTestUser(WebApplicationFactory<Program> factory)
    {
        using var client = factory.CreateClient();

        // Register a test user
        var registerRequest = new UserRegistrationDto
        {
            Email = $"performance-test-{Guid.NewGuid()}@example.com",
            Password = "PerformanceTest123!",
            Name = "Performance Test User",
            BiometricToken = "perf-test-token",
            Role = UserRoles.User
        };

        var registerResponse = await client.PostAsJsonAsync("/api/v1/User/register", registerRequest);
        registerResponse.EnsureSuccessStatusCode();

        var userResult = await registerResponse.Content.ReadFromJsonAsync<UserDto>();
        _userId = userResult!.Id;

        // Login to get token
        var loginRequest = new
        {
            Email = registerRequest.Email,
            Password = registerRequest.Password
        };

        var loginResponse = await client.PostAsJsonAsync("/api/v1/User/login", loginRequest);
        loginResponse.EnsureSuccessStatusCode();

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<dynamic>();
        _userToken = loginResult!.token.ToString();
    }
}
