using Microsoft.EntityFrameworkCore;
using Moq;
using TodoApi.Domain.Events;
using TodoApi.Infrastructure.Data;
using TodoApi.Infrastructure.Logging;
using TodoApi.Infrastructure.Services;
using Xunit;
using FluentAssertions;

namespace TodoApi.Tests.Unit.Services;

public class PostgreSqlEventStoreTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IStructuredLogger<PostgreSqlEventStore>> _loggerMock;
    private readonly PostgreSqlEventStore _eventStore;

    public PostgreSqlEventStoreTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _loggerMock = new Mock<IStructuredLogger<PostgreSqlEventStore>>();
        _eventStore = new PostgreSqlEventStore(_context, _loggerMock.Object);
    }

    [Fact]
    public async Task SaveEventsAsync_WithValidEvents_ShouldSaveSuccessfully()
    {
        // Arrange
        var streamName = "todoitem-123";
        var events = new List<DomainEvent>
        {
            new TodoItemCreatedEvent(123, 1, "Test Task", "Description", null, 1, DateTime.UtcNow)
        };

        // Act
        await _eventStore.SaveEventsAsync(streamName, events);

        // Assert
        var savedEvents = await _context.EventStore.ToListAsync();
        savedEvents.Should().HaveCount(1);
        savedEvents[0].StreamName.Should().Be(streamName);
        savedEvents[0].EventType.Should().Be("TodoItemCreatedEvent");
        savedEvents[0].Version.Should().Be(1);
    }

    [Fact]
    public async Task SaveEventsAsync_WithConcurrencyConflict_ShouldThrowException()
    {
        // Arrange
        var streamName = "todoitem-123";
        var event1 = new TodoItemCreatedEvent(123, 1, "Test Task", "Description", null, 1, DateTime.UtcNow);
        
        // Save first event
        await _eventStore.SaveEventsAsync(streamName, new[] { event1 });

        var event2 = new TodoItemUpdatedEvent(123, 1, "Old", "New", null, null, null, null, null, null, null, null, DateTime.UtcNow);

        // Act & Assert
        var act = () => _eventStore.SaveEventsAsync(streamName, new[] { event2 }, 0); // Expected version 0, but actual is 1
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Concurrency conflict*");
    }

    [Fact]
    public async Task GetEventsAsync_WithExistingStream_ShouldReturnEvents()
    {
        // Arrange
        var streamName = "todoitem-456";
        var events = new List<DomainEvent>
        {
            new TodoItemCreatedEvent(456, 1, "Task 1", "Desc 1", null, 1, DateTime.UtcNow),
            new TodoItemUpdatedEvent(456, 1, "Task 1", "Updated Task", null, null, null, null, null, null, null, null, DateTime.UtcNow.AddMinutes(1))
        };

        await _eventStore.SaveEventsAsync(streamName, events);

        // Act
        var eventStream = await _eventStore.GetEventsAsync(streamName);

        // Assert
        eventStream.Should().NotBeNull();
        eventStream.StreamName.Should().Be(streamName);
        eventStream.Events.Should().HaveCount(2);
        eventStream.Version.Should().Be(2);
    }

    [Fact]
    public async Task GetEventsAsync_WithFromVersion_ShouldReturnEventsFromVersion()
    {
        // Arrange
        var streamName = "todoitem-789";
        var events = new List<DomainEvent>
        {
            new TodoItemCreatedEvent(789, 1, "Task 1", "Desc 1", null, 1, DateTime.UtcNow),
            new TodoItemUpdatedEvent(789, 1, null, "Updated", null, null, null, null, null, null, null, null, DateTime.UtcNow.AddMinutes(1)),
            new TodoItemCompletedEvent(789, 1, DateTime.UtcNow.AddMinutes(2), DateTime.UtcNow.AddMinutes(2))
        };

        await _eventStore.SaveEventsAsync(streamName, events);

        // Act
        var eventStream = await _eventStore.GetEventsAsync(streamName, 1);

        // Assert
        eventStream.Events.Should().HaveCount(2); // Should get events from version 2 and 3
        eventStream.Events.First().Version.Should().Be(2);
    }

    [Fact]
    public async Task SaveSnapshotAsync_ShouldSaveSnapshot()
    {
        // Arrange
        var streamName = "todoitem-snapshot";
        var snapshot = new { Id = 123, Title = "Snapshot Task", IsComplete = false };
        var version = 5;

        // Act
        await _eventStore.SaveSnapshotAsync(streamName, snapshot, version);

        // Assert
        var savedSnapshots = await _context.SnapshotStore.ToListAsync();
        savedSnapshots.Should().HaveCount(1);
        savedSnapshots[0].StreamName.Should().Be(streamName);
        savedSnapshots[0].Version.Should().Be(version);
    }

    [Fact]
    public async Task GetSnapshotAsync_WithExistingSnapshot_ShouldReturnSnapshot()
    {
        // Arrange
        var streamName = "todoitem-snapshot";
        var originalSnapshot = new { Id = 123, Title = "Snapshot Task", IsComplete = false };
        await _eventStore.SaveSnapshotAsync(streamName, originalSnapshot, 5);

        // Act
        var retrievedSnapshot = await _eventStore.GetSnapshotAsync<dynamic>(streamName);

        // Assert
        retrievedSnapshot.Should().NotBeNull();
        // Note: Actual JSON deserialization testing would require more specific type handling
    }

    [Fact]
    public async Task GetEventsByEventTypeAsync_ShouldReturnMatchingEvents()
    {
        // Arrange
        var events1 = new List<DomainEvent>
        {
            new TodoItemCreatedEvent(1, 1, "Task 1", "Desc 1", null, 1, DateTime.UtcNow)
        };
        var events2 = new List<DomainEvent>
        {
            new TodoItemCreatedEvent(2, 1, "Task 2", "Desc 2", null, 1, DateTime.UtcNow),
            new TodoItemCompletedEvent(2, 1, DateTime.UtcNow, DateTime.UtcNow)
        };

        await _eventStore.SaveEventsAsync("todoitem-1", events1);
        await _eventStore.SaveEventsAsync("todoitem-2", events2);

        // Act
        var eventStreams = await _eventStore.GetEventsByEventTypeAsync(typeof(TodoItemCreatedEvent));

        // Assert
        var streams = eventStreams.ToList();
        streams.Should().HaveCount(2); // Two streams with TodoItemCreatedEvent
        streams.SelectMany(s => s.Events).Count(e => e.EventType == "TodoItemCreatedEvent").Should().Be(2);
    }

    [Fact]
    public async Task GetLastEventNumberAsync_ShouldReturnLastVersion()
    {
        // Arrange
        var events = new List<DomainEvent>
        {
            new TodoItemCreatedEvent(1, 1, "Task 1", "Desc 1", null, 1, DateTime.UtcNow),
            new TodoItemCompletedEvent(1, 1, DateTime.UtcNow, DateTime.UtcNow)
        };

        await _eventStore.SaveEventsAsync("todoitem-1", events);

        // Act
        var lastEventNumber = await _eventStore.GetLastEventNumberAsync();

        // Assert
        lastEventNumber.Should().Be(2);
    }

    [Fact]
    public async Task GetEventMetadataAsync_WithValidEventId_ShouldReturnMetadata()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var todoEvent = new TodoItemCreatedEvent(1, 1, "Task", "Desc", null, 1, DateTime.UtcNow);
        typeof(DomainEvent).GetProperty("Id")?.SetValue(todoEvent, eventId);

        await _eventStore.SaveEventsAsync("todoitem-1", new[] { todoEvent });

        // Act
        var metadata = await _eventStore.GetEventMetadataAsync(eventId);

        // Assert
        metadata.Should().NotBeNull();
        metadata!.EventId.Should().Be(eventId);
    }

    [Fact]
    public async Task SearchEventsAsync_WithCriteria_ShouldReturnMatchingEvents()
    {
        // Arrange
        var events = new List<DomainEvent>
        {
            new TodoItemCreatedEvent(1, 1, "Task 1", "Desc 1", null, 1, DateTime.UtcNow),
            new TodoItemCreatedEvent(2, 1, "Task 2", "Desc 2", null, 1, DateTime.UtcNow.AddMinutes(1))
        };

        await _eventStore.SaveEventsAsync("todoitem-1", events.Take(1));
        await _eventStore.SaveEventsAsync("todoitem-2", events.Skip(1));

        var criteria = new EventSearchCriteria
        {
            EventType = "TodoItemCreatedEvent",
            Take = 10
        };

        // Act
        var results = await _eventStore.SearchEventsAsync(criteria);

        // Assert
        var streams = results.ToList();
        streams.Should().HaveCount(2);
        streams.SelectMany(s => s.Events).Should().AllSatisfy(e => e.EventType.Should().Be("TodoItemCreatedEvent"));
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}