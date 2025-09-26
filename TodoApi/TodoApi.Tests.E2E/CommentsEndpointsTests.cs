using System.Collections.Generic;
using System.Net.Http.Json;
using FluentAssertions;
using TodoApi.Application.DTOs;
using TodoApi.Domain.Enums;
using TodoApi.Tests.E2E.Infrastructure;

namespace TodoApi.Tests.E2E
{
    public class CommentsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public CommentsEndpointsTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task PostComment_ThenRetrieve_ShouldRoundTrip()
        {
            var payload = new CommentCreateRequest
            {
                TodoItemId = 1,
                AuthorId = 1,
                Content = "E2E integration comment",
                AuthorDisplayName = "Integration User",
                EventType = ActivityEventType.CommentCreated
            };

            var response = await _client.PostAsJsonAsync("/api/Comments", payload);
            response.EnsureSuccessStatusCode();

            var created = await response.Content.ReadFromJsonAsync<CommentDto>();
            created.Should().NotBeNull();
            created!.Content.Should().Be("E2E integration comment");

            var list = await _client.GetFromJsonAsync<List<CommentDto>>("/api/Comments/todo/1");
            list.Should().NotBeNull();
            list!.Should().Contain(c => c.Content == "E2E integration comment");

            var activity = await _client.GetFromJsonAsync<List<ActivityLogDto>>("/api/Comments/todo/1/activity");
            activity.Should().NotBeNull();
            activity!.Should().NotBeEmpty();
        }
    }
}
