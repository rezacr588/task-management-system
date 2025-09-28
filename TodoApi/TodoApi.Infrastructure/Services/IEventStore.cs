using TodoApi.Domain.Events;

namespace TodoApi.Infrastructure.Services;

public interface IEventStore
{
    // Store events
    Task SaveEventsAsync(string streamName, IEnumerable<DomainEvent> events, int expectedVersion = -1);
    Task<EventStream> GetEventsAsync(string streamName, int fromVersion = 0);
    
    // Snapshots for performance
    Task SaveSnapshotAsync<T>(string streamName, T snapshot, int version) where T : class;
    Task<T?> GetSnapshotAsync<T>(string streamName) where T : class;
    
    // Event projections
    Task<IEnumerable<EventStream>> GetEventsByEventTypeAsync(Type eventType, DateTime? from = null, DateTime? to = null);
    Task<IEnumerable<EventStream>> GetEventsByAggregateAsync(string aggregateType, DateTime? from = null, DateTime? to = null);
    
    // Replay and rebuilding
    Task<IEnumerable<DomainEvent>> GetAllEventsAsync(DateTime? from = null, DateTime? to = null);
    Task<long> GetLastEventNumberAsync();
    
    // Event metadata and querying
    Task<EventMetadata?> GetEventMetadataAsync(Guid eventId);
    Task<IEnumerable<EventStream>> SearchEventsAsync(EventSearchCriteria criteria);
}

public class EventStream
{
    public string StreamName { get; set; } = string.Empty;
    public int Version { get; set; }
    public List<StoredEvent> Events { get; set; } = new();
}

public class StoredEvent
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EventData { get; set; } = string.Empty;
    public string? Metadata { get; set; }
    public DateTime Timestamp { get; set; }
    public int Version { get; set; }
    public string StreamName { get; set; } = string.Empty;
}

public class EventMetadata
{
    public Guid EventId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string? CausationId { get; set; }
    public Dictionary<string, string> AdditionalData { get; set; } = new();
    public DateTime Timestamp { get; set; }
}

public class EventSearchCriteria
{
    public string? StreamName { get; set; }
    public string? EventType { get; set; }
    public string? AggregateType { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? UserId { get; set; }
    public string? CorrelationId { get; set; }
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 100;
    public Dictionary<string, string> MetadataFilters { get; set; } = new();
}