# ConsentGrantedEvent
The `ConsentGrantedEvent` type represents an event that occurs when a user grants consent to a client, allowing the client to access specific scopes on behalf of the user. This event is crucial in authentication and authorization workflows, providing a record of when and how consent was granted.

## API
The `ConsentGrantedEvent` type exposes the following public members:
- `EventId`: A unique identifier for the event.
- `OccurredAt`: The date and time when the event occurred.
- `RequestId`: The identifier of the request that triggered the consent grant, or `null` if not applicable.
- `UserId`: The identifier of the user who granted consent.
- `ClientId`: The identifier of the client to which consent was granted.
- `GrantedScopes`: A collection of scopes that the client is now authorized to access on behalf of the user.
- `IsPermanent`: A boolean indicating whether the granted consent is permanent or not.
- `ClientIpAddress`: The IP address of the client that requested consent, or `null` if not available.

## Usage
Here are examples of how to use the `ConsentGrantedEvent` type:
```csharp
// Example 1: Creating a new ConsentGrantedEvent
var @event = new ConsentGrantedEvent
{
    EventId = Guid.NewGuid().ToString(),
    OccurredAt = DateTime.UtcNow,
    RequestId = "request-123",
    UserId = "user-123",
    ClientId = "client-123",
    GrantedScopes = new[] { "scope1", "scope2" },
    IsPermanent = true,
    ClientIpAddress = "192.168.1.100"
};

// Example 2: Handling a ConsentGrantedEvent in an event handler
void HandleConsentGrantedEvent(ConsentGrantedEvent @event)
{
    Console.WriteLine($"Consent granted to client {@event.ClientId} for user {@event.UserId} at {@event.OccurredAt}");
    foreach (var scope in @event.GrantedScopes)
    {
        Console.WriteLine($"  - {scope}");
    }
}
```

## Notes
When working with `ConsentGrantedEvent`, consider the following:
- The `EventId` should be unique to prevent event duplication or confusion.
- The `OccurredAt` timestamp is crucial for auditing and logging purposes.
- The `IsPermanent` flag determines whether the consent needs to be periodically renewed or is valid indefinitely.
- The `ClientIpAddress` may be `null` if the client's IP address is not available or cannot be determined.
- This type is designed to be thread-safe, as it is primarily a data container. However, when creating or manipulating instances of `ConsentGrantedEvent` in a multi-threaded environment, ensure that access to shared instances is properly synchronized to prevent data corruption or inconsistencies.
