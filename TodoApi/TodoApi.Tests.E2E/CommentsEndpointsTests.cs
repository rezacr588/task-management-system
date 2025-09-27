using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TodoApi.Application.DTOs;
using TodoApi.Domain.Enums;
using TodoApi.Tests.E2E.Infrastructure;
using Xunit;

namespace TodoApi.Tests.E2E
{
    [Collection("E2E")]
    public class CommentsEndpointsTests : EndpointTestBase
    {
        public CommentsEndpointsTests(CustomWebApplicationFactory factory)
            : base(factory)
        {
        }

        [Fact]
        public async Task PostComment_ThenRetrieve_ShouldRoundTrip()
        {
            var client = await CreateFreshClientAsync();

            var payload = new CommentCreateRequest
            {
                TodoItemId = 1,
                AuthorId = 1,
                Content = "E2E integration comment",
                AuthorDisplayName = "Integration User",
                EventType = ActivityEventType.CommentCreated
            };

            var response = await client.PostAsJsonAsync("/api/Comments", payload);
            response.EnsureSuccessStatusCode();

            var created = await response.Content.ReadFromJsonAsync<CommentDto>();
            created.Should().NotBeNull();
            created!.Content.Should().Be("E2E integration comment");

            var list = await client.GetFromJsonAsync<List<CommentDto>>("/api/Comments/todo/1");
            list.Should().NotBeNull();
            list!.Should().Contain(c => c.Content == "E2E integration comment");

            var activity = await client.GetFromJsonAsync<List<ActivityLogDto>>("/api/Comments/todo/1/activity");
            activity.Should().NotBeNull();
            activity!.Should().NotBeEmpty();
            activity!.Should().Contain(a => a.EventType == ActivityEventType.CommentCreated);
        }

        [Fact]
        public async Task PostComment_ForMissingTodo_ShouldReturnNotFound()
        {
            var client = await CreateFreshClientAsync();

            var payload = new CommentCreateRequest
            {
                TodoItemId = 999,
                AuthorId = 1,
                Content = "Ghost comment",
                AuthorDisplayName = "Integration User"
            };

            var response = await client.PostAsJsonAsync("/api/Comments", payload);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
