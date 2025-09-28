using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TodoApi.Domain.Events;
using TodoApi.Infrastructure.Data;
using TodoApi.Infrastructure.Logging;

namespace TodoApi.Infrastructure.Services;

public class PostgreSqlEventStore : IEventStore
{
    private readonly ApplicationDbContext _context;
    private readonly IStructuredLogger<PostgreSqlEventStore> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public PostgreSqlEventStore(ApplicationDbContext context, IStructuredLogger<PostgreSqlEventStore> logger)
    {
        _context = context;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task SaveEventsAsync(string streamName, IEnumerable<DomainEvent> events, int expectedVersion = -1)
    {
        using var scope = _logger.BeginScope("SaveEvents", new { StreamName = streamName, ExpectedVersion = expectedVersion });
        
        try
        {
            var eventsList = events.ToList();
            if (!eventsList.Any())
            {
                return;
            }

            // Get current version
            var currentVersion = await GetStreamVersionAsync(streamName);
            
            // Check version for optimistic concurrency
            if (expectedVersion != -1 && currentVersion != expectedVersion)
            {
                throw new InvalidOperationException($"Concurrency conflict: Expected version {expectedVersion}, but current version is {currentVersion}");
            }

            // Create stored events
            var storedEvents = new List<EventStoreEntry>();
            var version = currentVersion;

            foreach (var domainEvent in eventsList)
            {
                version++;
                
                var eventData = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), _jsonOptions);
                var metadata = JsonSerializer.Serialize(new EventMetadata
                {
                    EventId = domainEvent.Id,
                    UserId = domainEvent.UserId?.ToString() ?? "system",
                    CorrelationId = domainEvent.CorrelationId ?? Guid.NewGuid().ToString(),
                    CausationId = domainEvent.CausationId,
                    Timestamp = domainEvent.OccurredAt,
                    AdditionalData = domainEvent.Metadata ?? new Dictionary<string, string>()
                }, _jsonOptions);

                storedEvents.Add(new EventStoreEntry
                {
                    Id = domainEvent.Id,
                    StreamName = streamName,
                    Version = version,
                    EventType = domainEvent.GetType().Name,
                    EventData = eventData,
                    Metadata = metadata,
                    Timestamp = domainEvent.OccurredAt
                });
            }

            // Save to database
            await _context.EventStore.AddRangeAsync(storedEvents);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Saved {EventCount} events to stream {StreamName} at version {Version}", 
                eventsList.Count, streamName, version);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to save events to stream {StreamName}: {Error}", streamName, ex, ex.Message);
            throw;
        }
    }

    public async Task<EventStream> GetEventsAsync(string streamName, int fromVersion = 0)
    {
        using var scope = _logger.BeginScope("GetEvents", new { StreamName = streamName, FromVersion = fromVersion });

        try
        {
            var events = await _context.EventStore
                .Where(e => e.StreamName == streamName && e.Version > fromVersion)
                .OrderBy(e => e.Version)
                .Select(e => new StoredEvent
                {
                    Id = e.Id,
                    EventType = e.EventType,
                    EventData = e.EventData,
                    Metadata = e.Metadata,
                    Timestamp = e.Timestamp,
                    Version = e.Version,
                    StreamName = e.StreamName
                })
                .ToListAsync();

            var maxVersion = events.Any() ? events.Max(e => e.Version) : fromVersion;

            _logger.LogTrace("Retrieved {EventCount} events from stream {StreamName} from version {FromVersion}", 
                events.Count, streamName, fromVersion);

            return new EventStream
            {
                StreamName = streamName,
                Version = maxVersion,
                Events = events
            };
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to get events from stream {StreamName}: {Error}", streamName, ex, ex.Message);
            throw;
        }
    }

    public async Task SaveSnapshotAsync<T>(string streamName, T snapshot, int version) where T : class
    {
        using var scope = _logger.BeginScope("SaveSnapshot", new { StreamName = streamName, Version = version });

        try
        {
            var snapshotData = JsonSerializer.Serialize(snapshot, _jsonOptions);
            
            var snapshotEntry = new SnapshotStoreEntry
            {
                Id = Guid.NewGuid(),
                StreamName = streamName,
                Version = version,
                SnapshotType = typeof(T).Name,
                SnapshotData = snapshotData,
                Timestamp = DateTime.UtcNow
            };

            // Remove old snapshots for this stream
            var oldSnapshots = await _context.SnapshotStore
                .Where(s => s.StreamName == streamName)
                .ToListAsync();
                
            if (oldSnapshots.Any())
            {
                _context.SnapshotStore.RemoveRange(oldSnapshots);
            }

            await _context.SnapshotStore.AddAsync(snapshotEntry);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Saved snapshot for stream {StreamName} at version {Version}", streamName, version);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to save snapshot for stream {StreamName}: {Error}", streamName, ex, ex.Message);
            throw;
        }
    }

    public async Task<T?> GetSnapshotAsync<T>(string streamName) where T : class
    {
        using var scope = _logger.BeginScope("GetSnapshot", new { StreamName = streamName });

        try
        {
            var snapshot = await _context.SnapshotStore
                .Where(s => s.StreamName == streamName)
                .OrderByDescending(s => s.Version)
                .FirstOrDefaultAsync();

            if (snapshot == null)
            {
                return null;
            }

            var result = JsonSerializer.Deserialize<T>(snapshot.SnapshotData, _jsonOptions);
            
            _logger.LogTrace("Retrieved snapshot for stream {StreamName} from version {Version}", 
                streamName, snapshot.Version);
                
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to get snapshot for stream {StreamName}: {Error}", streamName, ex, ex.Message);
            return null;
        }
    }

    public async Task<IEnumerable<EventStream>> GetEventsByEventTypeAsync(Type eventType, DateTime? from = null, DateTime? to = null)
    {
        var query = _context.EventStore.Where(e => e.EventType == eventType.Name);
        
        if (from.HasValue)
            query = query.Where(e => e.Timestamp >= from.Value);
            
        if (to.HasValue)
            query = query.Where(e => e.Timestamp <= to.Value);

        var events = await query.OrderBy(e => e.Timestamp).ToListAsync();
        
        return events.GroupBy(e => e.StreamName)
            .Select(g => new EventStream
            {
                StreamName = g.Key,
                Version = g.Max(e => e.Version),
                Events = g.Select(e => new StoredEvent
                {
                    Id = e.Id,
                    EventType = e.EventType,
                    EventData = e.EventData,
                    Metadata = e.Metadata,
                    Timestamp = e.Timestamp,
                    Version = e.Version,
                    StreamName = e.StreamName
                }).ToList()
            });
    }

    public async Task<IEnumerable<EventStream>> GetEventsByAggregateAsync(string aggregateType, DateTime? from = null, DateTime? to = null)
    {
        var query = _context.EventStore.Where(e => e.StreamName.StartsWith(aggregateType + "-"));
        
        if (from.HasValue)
            query = query.Where(e => e.Timestamp >= from.Value);
            
        if (to.HasValue)
            query = query.Where(e => e.Timestamp <= to.Value);

        var events = await query.OrderBy(e => e.Timestamp).ToListAsync();
        
        return events.GroupBy(e => e.StreamName)
            .Select(g => new EventStream
            {
                StreamName = g.Key,
                Version = g.Max(e => e.Version),
                Events = g.Select(e => new StoredEvent
                {
                    Id = e.Id,
                    EventType = e.EventType,
                    EventData = e.EventData,
                    Metadata = e.Metadata,
                    Timestamp = e.Timestamp,
                    Version = e.Version,
                    StreamName = e.StreamName
                }).ToList()
            });
    }

    public async Task<IEnumerable<DomainEvent>> GetAllEventsAsync(DateTime? from = null, DateTime? to = null)
    {
        var query = _context.EventStore.AsQueryable();
        
        if (from.HasValue)
            query = query.Where(e => e.Timestamp >= from.Value);
            
        if (to.HasValue)
            query = query.Where(e => e.Timestamp <= to.Value);

        var events = await query.OrderBy(e => e.Timestamp).ToListAsync();
        
        var domainEvents = new List<DomainEvent>();
        
        foreach (var evt in events)
        {
            try
            {
                var eventType = Type.GetType($"TodoApi.Domain.Events.{evt.EventType}");
                if (eventType != null)
                {
                    var domainEvent = JsonSerializer.Deserialize(evt.EventData, eventType, _jsonOptions) as DomainEvent;
                    if (domainEvent != null)
                    {
                        domainEvents.Add(domainEvent);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to deserialize event {EventId}: {Error}", evt.Id, ex.Message);
            }
        }
        
        return domainEvents;
    }

    public async Task<long> GetLastEventNumberAsync()
    {
        var lastEvent = await _context.EventStore
            .OrderByDescending(e => e.Timestamp)
            .FirstOrDefaultAsync();
            
        return lastEvent?.Version ?? 0;
    }

    public async Task<EventMetadata?> GetEventMetadataAsync(Guid eventId)
    {
        var evt = await _context.EventStore
            .Where(e => e.Id == eventId)
            .FirstOrDefaultAsync();
            
        if (evt?.Metadata == null)
            return null;
            
        return JsonSerializer.Deserialize<EventMetadata>(evt.Metadata, _jsonOptions);
    }

    public async Task<IEnumerable<EventStream>> SearchEventsAsync(EventSearchCriteria criteria)
    {
        var query = _context.EventStore.AsQueryable();
        
        if (!string.IsNullOrEmpty(criteria.StreamName))
            query = query.Where(e => e.StreamName == criteria.StreamName);
            
        if (!string.IsNullOrEmpty(criteria.EventType))
            query = query.Where(e => e.EventType == criteria.EventType);
            
        if (!string.IsNullOrEmpty(criteria.AggregateType))
            query = query.Where(e => e.StreamName.StartsWith(criteria.AggregateType + "-"));
            
        if (criteria.FromDate.HasValue)
            query = query.Where(e => e.Timestamp >= criteria.FromDate.Value);
            
        if (criteria.ToDate.HasValue)
            query = query.Where(e => e.Timestamp <= criteria.ToDate.Value);

        var events = await query
            .OrderBy(e => e.Timestamp)
            .Skip(criteria.Skip)
            .Take(criteria.Take)
            .ToListAsync();
        
        return events.GroupBy(e => e.StreamName)
            .Select(g => new EventStream
            {
                StreamName = g.Key,
                Version = g.Max(e => e.Version),
                Events = g.Select(e => new StoredEvent
                {
                    Id = e.Id,
                    EventType = e.EventType,
                    EventData = e.EventData,
                    Metadata = e.Metadata,
                    Timestamp = e.Timestamp,
                    Version = e.Version,
                    StreamName = e.StreamName
                }).ToList()
            });
    }

    private async Task<int> GetStreamVersionAsync(string streamName)
    {
        var lastEvent = await _context.EventStore
            .Where(e => e.StreamName == streamName)
            .OrderByDescending(e => e.Version)
            .FirstOrDefaultAsync();
            
        return lastEvent?.Version ?? 0;
    }
}

// EF Core entities for event store
public class EventStoreEntry
{
    public Guid Id { get; set; }
    public string StreamName { get; set; } = string.Empty;
    public int Version { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EventData { get; set; } = string.Empty;
    public string? Metadata { get; set; }
    public DateTime Timestamp { get; set; }
}

public class SnapshotStoreEntry
{
    public Guid Id { get; set; }
    public string StreamName { get; set; } = string.Empty;
    public int Version { get; set; }
    public string SnapshotType { get; set; } = string.Empty;
    public string SnapshotData { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}