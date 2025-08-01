// existing content ...

## TokenIssuedEvent

The `TokenIssuedEvent` class represents an event published when an access token is successfully issued. It provides details about the token issuance, including the user, client, grant type, scopes, and token lifetime.

### Usage Example

```csharp
try
{
    // Simulate a successful token issuance
    var tokenIssuedEvent = new TokenIssuedEvent
    {
        UserId = "user123",
        ClientId = "client123",
        GrantType = "authorization_code",
        Scopes = new[] { "openid", "profile" },
        ExpiresInSeconds = 3600,
        ClientIpAddress = "192.168.1.100"
    };

    // Publish the event
    var eventPublisher = new EventPublisher();
    await eventPublisher.PublishAsync(tokenIssuedEvent);

    // Process the event
}
catch (Exception ex)
{
    // Handle any exceptions
}

// Output:
// EventId: a unique identifier for the event
// OccurredAt: the timestamp when the event occurred
// RequestId: the request ID associated with the event (optional)
// UserId: the user for whom the token was issued
// ClientId: the client application requesting the token
// GrantType: the grant type used (e.g. authorization_code, refresh_token)
// Scopes: the scopes granted in the token
// ExpiresInSeconds: the token lifetime in seconds
// ClientIpAddress: the client IP address for audit/security purposes (optional)
```

// ... rest of content ...
