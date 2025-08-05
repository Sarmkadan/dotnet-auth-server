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

## ScopeMetadataHandler

The `ScopeMetadataHandler` class provides metadata about OAuth2 scopes including descriptions and consent requirements. It enables clients to display informative consent screens and servers to validate scope requests. The handler manages both standard OIDC scopes and custom application-specific scopes.

### Usage Example

```csharp
try
{
    // Setup dependencies
    var cacheService = new MemoryCacheService();
    var logger = new Logger<ScopeMetadataHandler>();
    
    var handler = new ScopeMetadataHandler(cacheService, logger);
    
    // Get metadata for a specific scope
    var openIdMetadata = await handler.GetScopeMetadataAsync("openid");
    Console.WriteLine($"Scope: {openIdMetadata?.DisplayName}");
    Console.WriteLine($"Description: {openIdMetadata?.Description}");
    Console.WriteLine($"Requires consent: {openIdMetadata?.RequiresConsent}");
    
    // Get metadata for multiple scopes
    var scopes = new[] { "profile", "email", "phone" };
    var multipleMetadata = await handler.GetScopesMetadataAsync(scopes);
    
    foreach (var metadata in multipleMetadata)
    {
        Console.WriteLine($"- {metadata.DisplayName}: {metadata.Description}");
    }
    
    // Get all available scopes
    var allScopes = await handler.GetAllScopesAsync();
    Console.WriteLine($"Total scopes available: {allScopes.Count()}");
    
    // Check which scopes require consent
    var consentScopes = handler.GetScopesRequiringConsent(scopes);
    Console.WriteLine($"Scopes requiring consent: {string.Join(", ", consentScopes.Select(s => s.DisplayName))}");
    
    // Register a custom scope
    var customScope = new ScopeMetadataHandler.ScopeMetadata
    {
        Name = "custom_api",
        DisplayName = "Custom API Access",
        Description = "Access to custom API endpoints",
        RequiresConsent = true,
        Icon = "🔧",
        RelatedScopes = new List<string> { "profile", "email" }
    };
    
    handler.RegisterCustomScope(customScope);
    
    // Verify custom scope was registered
    var customMetadata = await handler.GetScopeMetadataAsync("custom_api");
    Console.WriteLine($"Custom scope registered: {customMetadata?.DisplayName}");
}
catch (Exception ex)
{
    // Handle exceptions
    Console.WriteLine($"Error: {ex.Message}");
}
```

### Key Members

- `GetScopeMetadataAsync(string scopeName, CancellationToken cancellationToken)`: Gets metadata for a specific scope
- `GetScopesMetadataAsync(IEnumerable<string> scopeNames, CancellationToken cancellationToken)`: Gets metadata for multiple scopes
- `GetAllScopesAsync(CancellationToken cancellationToken)`: Gets all available scopes
- `GetScopesRequiringConsent(IEnumerable<string> scopeNames)`: Gets scopes that require user consent
- `RegisterCustomScope(ScopeMetadata metadata)`: Registers a custom scope metadata
- `ScopeMetadata.Name`: The scope identifier
- `ScopeMetadata.DisplayName`: Human-readable name for UI display
- `ScopeMetadata.Description`: Description of what the scope grants access to
- `ScopeMetadata.RequiresConsent`: Whether user consent is required
- `ScopeMetadata.Icon`: Optional icon for UI display
- `ScopeMetadata.RelatedScopes`: List of related scope names
