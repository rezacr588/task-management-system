using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using TechTalk.SpecFlow;
using TodoApi.WebApi;

namespace TodoApi.Tests.BDD.Steps;

[Binding]
public class UserManagementSteps
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;
    private HttpResponseMessage? _response;
    private string? _userToken;
    private dynamic? _loginResult;
    private dynamic? _userProfile;

    public UserManagementSteps(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [When(@"I register with email ""(.*)"" and password ""(.*)""")]
    public async Task WhenIRegisterWithEmailAndPassword(string email, string password)
    {
        var registerRequest = new
        {
            Email = email,
            Password = password,
            Name = "Test User"
        };

        _response = await _client.PostAsJsonAsync("/api/users/register", registerRequest);
    }

    [Then(@"the user should be registered successfully")]
    public void ThenTheUserShouldBeRegisteredSuccessfully()
    {
        _response.Should().NotBeNull();
        _response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Then(@"I should receive a user ID")]
    public async Task ThenIShouldReceiveAUserId()
    {
        _response.Should().NotBeNull();
        var result = await _response.Content.ReadFromJsonAsync<dynamic>();
        result.Should().NotBeNull();
        // Assuming the response contains an id field
        ((int)result.id).Should().BeGreaterThan(0);
    }

    [Given(@"I have registered with email ""(.*)"" and password ""(.*)""")]
    public async Task GivenIHaveRegisteredWithEmailAndPassword(string email, string password)
    {
        await WhenIRegisterWithEmailAndPassword(email, password);
        _response.Should().NotBeNull();
        _response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [When(@"I login with email ""(.*)"" and password ""(.*)""")]
    public async Task WhenILoginWithEmailAndPassword(string email, string password)
    {
        var loginRequest = new
        {
            Email = email,
            Password = password
        };

        _response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        if (_response.IsSuccessStatusCode)
        {
            _loginResult = await _response.Content.ReadFromJsonAsync<dynamic>();
            _userToken = _loginResult.token.ToString();
        }
    }

    [Then(@"I should be logged in successfully")]
    public void ThenIShouldBeLoggedInSuccessfully()
    {
        _response.Should().NotBeNull();
        _response.StatusCode.Should().Be(HttpStatusCode.OK);
        _loginResult.Should().NotBeNull();
    }

    [Then(@"I should receive an authentication token")]
    public void ThenIShouldReceiveAnAuthenticationToken()
    {
        _userToken.Should().NotBeNullOrEmpty();
    }

    [Then(@"the login should fail")]
    public void ThenTheLoginShouldFail()
    {
        _response.Should().NotBeNull();
        _response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Then(@"I should receive an unauthorized error")]
    public void ThenIShouldReceiveAnUnauthorizedError()
    {
        _response.Should().NotBeNull();
        _response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Given(@"I am logged in as a registered user")]
    public async Task GivenIAmLoggedInAsARegisteredUser()
    {
        await GivenIHaveRegisteredWithEmailAndPassword("testuser@example.com", "password123");
        await WhenILoginWithEmailAndPassword("testuser@example.com", "password123");
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _userToken);
    }

    [When(@"I update my profile with name ""(.*)""")]
    public async Task WhenIUpdateMyProfileWithName(string name)
    {
        var updateRequest = new
        {
            Name = name,
            Email = "testuser@example.com"
        };

        _response = await _client.PutAsJsonAsync("/api/users/profile", updateRequest);
    }

    [Then(@"my profile should be updated successfully")]
    public void ThenMyProfileShouldBeUpdatedSuccessfully()
    {
        _response!.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Then(@"the profile should have the new name")]
    public async Task ThenTheProfileShouldHaveTheNewName()
    {
        var profile = await _response!.Content.ReadFromJsonAsync<dynamic>();
        profile!.name.ToString().Should().Be("John Doe");
    }

    [When(@"I request my profile")]
    public async Task WhenIRequestMyProfile()
    {
        _response = await _client.GetAsync("/api/users/profile");
        if (_response.IsSuccessStatusCode)
        {
            _userProfile = await _response.Content.ReadFromJsonAsync<dynamic>();
        }
    }

    [Then(@"I should receive my profile information")]
    public void ThenIShouldReceiveMyProfileInformation()
    {
        _response!.StatusCode.Should().Be(HttpStatusCode.OK);
        _userProfile.Should().NotBeNull();
    }

    [Then(@"the profile should contain my email and name")]
    public void ThenTheProfileShouldContainMyEmailAndName()
    {
        _userProfile!.email.ToString().Should().Be("testuser@example.com");
        _userProfile!.name.ToString().Should().NotBeNullOrEmpty();
    }
}