// existing content ...

## ConsentGrantedEvent

The `ConsentGrantedEvent` class represents an event published when a user grants consent for a client application to access their data. It provides details about the consent, including the user, client, scopes, and whether the consent is permanent. This event is essential for compliance logging (GDPR, CCPA) and understanding user permissions.

### Usage Example

```csharp
try
{
    // Simulate a user granting consent
    var consentGrantedEvent = new ConsentGrantedEvent
    {
        UserId = "user123",
        ClientId = "client123",
        GrantedScopes = new[] { "openid", "profile", "email" },
        IsPermanent = true,
        ClientIpAddress = "192.168.1.100"
    };

    // Publish the event
    var eventPublisher = new EventPublisher();
    await eventPublisher.PublishAsync(consentGrantedEvent);

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
// UserId: the user who granted consent
// ClientId: the client application for which consent was granted
// GrantedScopes: the scopes to which the user consented
// IsPermanent: whether the consent is permanent or session-scoped
// ClientIpAddress: the client IP address for audit/security purposes (optional)
```

// ... rest of content ...
