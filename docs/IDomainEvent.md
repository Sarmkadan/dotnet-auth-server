# IDomainEvent

`IDomainEvent` defines the metadata shared by domain events published throughout the authorization server. Implementations describe a specific event instance and provide identifiers that consumers can use for deduplication, request tracing, and event-type routing.

- **Namespace:** `DotnetAuthServer.Events`
- **Source:** `src/Events/IDomainEvent.cs`

## API

### `string EventId { get; }`

Gets the unique identifier for this event instance. Consumers can use this value to track the event and detect duplicate deliveries.

### `DateTime OccurredAt { get; }`

Gets the UTC date and time at which the event occurred. Implementations are responsible for supplying a UTC timestamp.

### `string? RequestId { get; }`

Gets the optional identifier of the HTTP request that triggered the event. This value can correlate the event with request logs and distributed traces, and may be `null` when no request is associated with the event.

### `string EventType { get; }`

Gets the human-readable name of the event type. Consumers can use this value to identify or categorize the event.

## Implementation example

```csharp
using DotnetAuthServer.Events;

public sealed record UserAuthenticatedEvent(
    string EventId,
    DateTime OccurredAt,
    string? RequestId) : IDomainEvent
{
    public string EventType => "UserAuthenticated";
}
```

The interface exposes read-only properties only. It does not generate identifiers or timestamps, validate values, or publish events; each implementation and the surrounding event infrastructure provide those behaviors.
