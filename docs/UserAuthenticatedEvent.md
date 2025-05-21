# UserAuthenticatedEvent
The `UserAuthenticatedEvent` type represents an event that occurs when a user successfully authenticates with the system. It provides information about the authentication event, including the user's identity, the client that initiated the authentication, and other relevant details. This type is used to track and respond to user authentication events within the `dotnet-auth-server` project.

## API
The `UserAuthenticatedEvent` type has the following public members:
* `EventId`: A unique identifier for the authentication event.
* `OccurredAt`: The date and time when the authentication event occurred.
* `RequestId`: An optional identifier for the request that initiated the authentication event.
* `UserId`: The identifier of the user who authenticated.
* `Username`: The username of the user who authenticated.
* `ClientId`: The identifier of the client that initiated the authentication event.
* `ClientIpAddress`: The IP address of the client that initiated the authentication event, if available.
* `AuthenticationMethod`: The method used to authenticate the user.
* `Email`: The email address of the user who authenticated, if available.

## Usage
Here are two examples of using the `UserAuthenticatedEvent` type:
```csharp
// Example 1: Creating a new UserAuthenticatedEvent instance
var @event = new UserAuthenticatedEvent
{
    EventId = Guid.NewGuid().ToString(),
    OccurredAt = DateTime.UtcNow,
    UserId = "user123",
    Username = "johnDoe",
    ClientId = "client456",
    AuthenticationMethod = "password"
};

// Example 2: Handling a UserAuthenticatedEvent in an event handler
void HandleUserAuthenticatedEvent(UserAuthenticatedEvent @event)
{
    Console.WriteLine($"User {@event.Username} authenticated at {@event.OccurredAt}");
    // Perform additional logic based on the event data
}
```

## Notes
When working with `UserAuthenticatedEvent` instances, consider the following edge cases and thread-safety remarks:
* The `RequestId` and `ClientIpAddress` properties are optional, so they may be null if not provided.
* The `Email` property is also optional, so it may be null if not available.
* The `OccurredAt` property represents the date and time in UTC, so be aware of potential timezone differences when comparing or processing this value.
* The `UserAuthenticatedEvent` type is a simple data container, so it does not have any inherent thread-safety concerns. However, when sharing instances across threads, ensure that the data is properly synchronized to avoid inconsistencies or data corruption.
