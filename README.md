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

## TokenRevocationHandler

The `TokenRevocationHandler` class implements OAuth2 token revocation functionality as specified in RFC 7009. It allows clients to revoke tokens they no longer need by removing them from server-side storage, preventing their use in future requests. This is essential for logout flows, compromised token recovery, and maintaining security compliance.

The handler supports both individual token revocation (via `RevokeTokenAsync`) and bulk revocation for all tokens issued to a specific user (via `RevokeUserTokensAsync`).

### Usage Example

```csharp
try
{
    // Setup dependencies
    var refreshTokenRepository = new RefreshTokenRepository();
    var grantRepository = new AuthorizationGrantRepository();
    var revokedTokenStore = new RevokedTokenStore();
    var options = new AuthServerOptions
    {
        JwtSigningKey = "your-signing-key-here",
        IssuerUrl = "https://auth.example.com"
    };
    var logger = new Logger<TokenRevocationHandler>();
    
    var handler = new TokenRevocationHandler(
        refreshTokenRepository,
        grantRepository,
        revokedTokenStore,
        options,
        logger);

    // Revoke a specific token
    var revocationResult = await handler.RevokeTokenAsync(
        token: "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
        tokenTypeHint: "refresh_token");
    
    if (revocationResult.Success)
    {
        Console.WriteLine(revocationResult.Revoked 
            ? "Token successfully revoked"
            : "Token not found (possibly already revoked)");
    }
    
    // Revoke all tokens for a user (logout operation)
    var userRevocationResult = await handler.RevokeUserTokensAsync("user123");
    
    if (userRevocationResult.Success)
    {
        Console.WriteLine("All user tokens revoked successfully");
    }
    else
    {
        Console.WriteLine($"Error: {userRevocationResult.Error}");
    }
}
catch (Exception ex)
{
    // Handle exceptions
    Console.WriteLine($"Revocation failed: {ex.Message}");
}
```

### Key Members

- `RevokeTokenAsync(string? token, string? tokenTypeHint, CancellationToken cancellationToken)`: Revokes a specific token by removing it from storage
- `RevokeUserTokensAsync(string userId, CancellationToken cancellationToken)`: Revokes all tokens issued to a specific user
- `RevocationResult.Success`: Indicates whether the operation completed without errors
- `RevocationResult.Revoked`: Indicates whether the token was actually found and revoked
- `RevocationResult.Error`: Contains error message if operation failed
