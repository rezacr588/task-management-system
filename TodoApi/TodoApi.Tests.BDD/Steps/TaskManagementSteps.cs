using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using TechTalk.SpecFlow;
using TodoApi.Application.DTOs;
using TodoApi.WebApi;

namespace TodoApi.Tests.BDD.Steps;

[Binding]
public class TaskManagementSteps
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;
    private HttpResponseMessage? _response;
    private TodoItemDto? _createdTask;
    private List<TodoItemDto>? _tasks;
    private string? _userToken;

    public TaskManagementSteps(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Given(@"I am a registered user")]
    public async Task GivenIAmARegisteredUser()
    {
        // Register a test user and get token
        var registerRequest = new UserRegistrationDto
        {
            Email = "test@example.com",
            Password = "Password123!",
            Name = "Test User",
            BiometricToken = "test-biometric-token",
            Role = "User"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/v1/User/register", registerRequest);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Login to get token
        var loginRequest = new { Email = "test@example.com", Password = "Password123!" };
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/User/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<dynamic>();
        _userToken = loginResult.token.ToString();

        // Set authorization header for subsequent requests
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _userToken);
    }

    [When(@"I create a task with title ""(.*)"" and description ""(.*)""")]
    public async Task WhenICreateATaskWithTitleAndDescription(string title, string description)
    {
        var createRequest = new
        {
            Title = title,
            Description = description
        };

        _response = await _client.PostAsJsonAsync("/api/v1/TodoItems", createRequest);
        if (_response.IsSuccessStatusCode)
        {
            _createdTask = await _response.Content.ReadFromJsonAsync<TodoItemDto>();
        }
    }

    [Then(@"the task should be created successfully")]
    public void ThenTheTaskShouldBeCreatedSuccessfully()
    {
        _response!.StatusCode.Should().Be(HttpStatusCode.Created);
        _createdTask.Should().NotBeNull();
        _createdTask!.Id.Should().NotBe(0);
    }

    [Then(@"the task should have the correct title and description")]
    public void ThenTheTaskShouldHaveTheCorrectTitleAndDescription()
    {
        _createdTask!.Title.Should().Be("Buy groceries");
        _createdTask!.Description.Should().Be("Weekly shopping");
    }

    [Given(@"I have created tasks")]
    public async Task GivenIHaveCreatedTasks()
    {
        // Create a few test tasks
        await WhenICreateATaskWithTitleAndDescription("Task 1", "Description 1");
        await WhenICreateATaskWithTitleAndDescription("Task 2", "Description 2");
        await WhenICreateATaskWithTitleAndDescription("Task 3", "Description 3");
    }

    [When(@"I request all my tasks")]
    public async Task WhenIRequestAllMyTasks()
    {
        _response = await _client.GetAsync("/api/v1/TodoItems");
        if (_response.IsSuccessStatusCode)
        {
            _tasks = await _response.Content.ReadFromJsonAsync<List<TodoItemDto>>();
        }
    }

    [Then(@"I should receive a list of all my tasks")]
    public void ThenIShouldReceiveAListOfAllMyTasks()
    {
        _response.StatusCode.Should().Be(HttpStatusCode.OK);
        _tasks.Should().NotBeNull();
        _tasks.Should().HaveCountGreaterThanOrEqualTo(3);
    }

    [Given(@"I have a task with title ""(.*)""")]
    public async Task GivenIHaveATaskWithTitle(string title)
    {
        await WhenICreateATaskWithTitleAndDescription(title, "Test description");
    }

    [When(@"I update the task title to ""(.*)""")]
    public async Task WhenIUpdateTheTaskTitleTo(string newTitle)
    {
        var updateRequest = new
        {
            Title = newTitle,
            Description = _createdTask!.Description,
            IsComplete = _createdTask!.IsComplete
        };

        _response = await _client.PutAsJsonAsync($"/api/v1/TodoItems/{_createdTask.Id}", updateRequest);
        if (_response.IsSuccessStatusCode)
        {
            _createdTask = await _response.Content.ReadFromJsonAsync<TodoItemDto>();
        }
    }

    [Then(@"the task should be updated successfully")]
    public void ThenTheTaskShouldBeUpdatedSuccessfully()
    {
        _response!.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Then(@"the task should have the new title")]
    public void ThenTheTaskShouldHaveTheNewTitle()
    {
        _createdTask!.Title.Should().Be("Updated task");
    }

    [Given(@"I have an incomplete task")]
    public async Task GivenIHaveAnIncompleteTask()
    {
        await WhenICreateATaskWithTitleAndDescription("Incomplete Task", "This task is not done");
    }

    [When(@"I mark the task as completed")]
    public async Task WhenIMarkTheTaskAsCompleted()
    {
        var updateRequest = new
        {
            Title = _createdTask!.Title,
            Description = _createdTask!.Description,
            IsComplete = true
        };

        _response = await _client.PutAsJsonAsync($"/api/v1/TodoItems/{_createdTask!.Id}", updateRequest);
        if (_response.IsSuccessStatusCode)
        {
            _createdTask = await _response.Content.ReadFromJsonAsync<TodoItemDto>();
        }
    }

    [Then(@"the task should be marked as completed")]
    public void ThenTheTaskShouldBeMarkedAsCompleted()
    {
        _createdTask!.IsComplete.Should().BeTrue();
    }

    [Given(@"I have a task")]
    public async Task GivenIHaveATask()
    {
        await WhenICreateATaskWithTitleAndDescription("Test Task", "Test Description");
    }

    [When(@"I delete the task")]
    public async Task WhenIDeleteTheTask()
    {
        _response = await _client.DeleteAsync($"/api/v1/TodoItems/{_createdTask!.Id}");
    }

    [Then(@"the task should be deleted successfully")]
    public void ThenTheTaskShouldBeDeletedSuccessfully()
    {
        _response!.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Then(@"the task should no longer exist")]
    public async Task ThenTheTaskShouldNoLongerExist()
    {
        var getResponse = await _client.GetAsync($"/api/v1/TodoItems/{_createdTask!.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Given(@"I have created tags ""(.*)"" and ""(.*)""")]
    public async Task GivenIHaveCreatedTags(string tag1, string tag2)
    {
        // Create tags
        var tag1Request = new { Name = tag1 };
        var tag2Request = new { Name = tag2 };

        await _client.PostAsJsonAsync("/api/v1/Tag", tag1Request);
        await _client.PostAsJsonAsync("/api/v1/Tag", tag2Request);
    }

    [When(@"I assign the tags to the task")]
    public async Task WhenIAssignTheTagsToTheTask()
    {
        var updateRequest = new
        {
            Title = _createdTask!.Title,
            Description = _createdTask!.Description,
            IsComplete = _createdTask!.IsComplete,
            TagIds = new[] { 1, 2 }
        };

        _response = await _client.PutAsJsonAsync($"/api/v1/TodoItems/{_createdTask!.Id}", updateRequest);
        if (_response.IsSuccessStatusCode)
        {
            _createdTask = await _response.Content.ReadFromJsonAsync<TodoItemDto>();
        }
    }

    [Then(@"the task should have the assigned tags")]
    public void ThenTheTaskShouldHaveTheAssignedTags()
    {
        _createdTask!.Tags.Should().NotBeNull();
        _createdTask!.Tags.Should().HaveCount(2);
    }

    [Given(@"I have both completed and incomplete tasks")]
    public async Task GivenIHaveBothCompletedAndIncompleteTasks()
    {
        // Create completed task
        await WhenICreateATaskWithTitleAndDescription("Completed Task", "This is done");
        await WhenIMarkTheTaskAsCompleted();

        // Create incomplete task
        await WhenICreateATaskWithTitleAndDescription("Incomplete Task", "This is not done");
    }

    [When(@"I filter tasks by completion status ""(.*)""")]
    public async Task WhenIFilterTasksByCompletionStatus(string status)
    {
        var completed = status == "completed";
        _response = await _client.GetAsync($"/api/todoitems?completed={completed}");
        if (_response.IsSuccessStatusCode)
        {
            _tasks = await _response.Content.ReadFromJsonAsync<List<TodoItemDto>>();
        }
    }

    [Then(@"I should only see completed tasks")]
    public void ThenIShouldOnlySeeCompletedTasks()
    {
        _tasks!.Should().AllSatisfy(task => task.IsComplete.Should().BeTrue());
    }
}