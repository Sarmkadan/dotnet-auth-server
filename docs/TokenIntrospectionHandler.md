# TokenIntrospectionHandler

Handles OAuth 2.0 token introspection requests as defined in RFC 7662. It receives an incoming token, validates it against the authorization server's token store, and produces an `IntrospectionResponse` indicating whether the token is currently active along with its associated metadata such as scope, client identifier, and subject claims.

## API

### TokenIntrospectionHandler

```csharp
public TokenIntrospectionHandler
```

Default constructor. Creates a new handler instance. The handler does not maintain internal state between invocations; each call to `IntrospectToken` operates independently.

### IntrospectToken

```csharp
public IntrospectionResponse IntrospectToken(string token)
```

Processes a token introspection request.

**Parameters:**
- `token` — The token string to introspect. Must not be null.

**Returns:**
An `IntrospectionResponse` whose `Active` property indicates validity. When active, the response carries the associated claims; when inactive, metadata fields are absent or null.

**Throws:**
- `ArgumentNullException` — if `token` is null.
- `FormatException` — if the token is malformed and cannot be parsed.
- `CryptographicException` — if signature validation fails for a signed token.

### Active

```csharp
public bool Active { get; }
```

Indicates whether the introspected token is currently valid. `true` if the token exists in the store, has not expired, and has not been revoked; `false` otherwise.

### Scope

```csharp
public string? Scope { get; }
```

The space-delimited scope values associated with the token. Null when the token is inactive or no scopes were originally granted.

### ClientId

```csharp
public string? ClientId { get; }
```

The OAuth 2.0 client identifier for which the token was issued. Null when the token is inactive.

### Username

```csharp
public string? Username { get; }
```

The resource owner username tied to the token, typically present for password grant tokens. Null when the token is inactive or the grant type did not involve a user.

### TokenType

```csharp
public string? TokenType { get; }
```

The type of the token, e.g. `"access_token"` or `"refresh_token"`. Null when the token is inactive.

### Exp

```csharp
public long? Exp { get; }
```

The expiration time of the token as a Unix timestamp (seconds since epoch). Null when the token is inactive or has no explicit expiration.

### Iat

```csharp
public long? Iat { get; }
```

The issued-at time of the token as a Unix timestamp. Null when the token is inactive.

### Sub

```csharp
public string? Sub { get; }
```

The subject identifier — typically the user or principal the token represents. Null when the token is inactive.

## Usage

### Example 1: Basic introspection of an access token

```csharp
var handler = new TokenIntrospectionHandler();

try
{
    IntrospectionResponse response = handler.IntrospectToken(accessToken);

    if (response.Active)
    {
        Console.WriteLine($"Token is active for client '{response.ClientId}'");
        Console.WriteLine($"Scopes: {response.Scope}");
        Console.WriteLine($"Expires at Unix timestamp: {response.Exp}");

        // Authorize the request based on scopes
        if (response.Scope?.Contains("read") == true)
        {
            GrantReadAccess(response.Sub);
        }
    }
    else
    {
        Console.WriteLine("Token is inactive — reject the request.");
    }
}
catch (FormatException)
{
    Console.WriteLine("Token is malformed.");
}
catch (CryptographicException)
{
    Console.WriteLine("Token signature is invalid.");
}
```

### Example 2: Resource server middleware guard

```csharp
public bool AuthenticateRequest(string authorizationHeader, string requiredScope)
{
    if (string.IsNullOrEmpty(authorizationHeader) ||
        !authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
    {
        return false;
    }

    string token = authorizationHeader["Bearer ".Length..];
    var handler = new TokenIntrospectionHandler();
    IntrospectionResponse response = handler.IntrospectToken(token);

    if (!response.Active)
    {
        return false;
    }

    // Verify the token type is an access token
    if (response.TokenType != "access_token")
    {
        return false;
    }

    // Check required scope
    var grantedScopes = response.Scope?.Split(' ') ?? Array.Empty<string>();
    return grantedScopes.Contains(requiredScope);
}
```

## Notes

- **Stateless handler:** `TokenIntrospectionHandler` holds no mutable fields. A single instance can be reused concurrently across multiple threads without synchronization.
- **Null metadata on inactive tokens:** When `Active` is `false`, all metadata properties (`Scope`, `ClientId`, `Username`, `TokenType`, `Exp`, `Iat`, `Sub`) return `null`. Always check `Active` before consuming these values.
- **Expired tokens:** A token past its `Exp` timestamp is considered inactive. The `Exp` value itself may still be populated in the response for diagnostic purposes, but `Active` will be `false`.
- **Revoked tokens:** If the token has been explicitly revoked in the store, `Active` returns `false` regardless of the expiration timestamp.
- **Token format detection:** The handler internally distinguishes between reference tokens (opaque identifiers looked up in storage) and self-contained JWT tokens (validated via signature and claims). Malformed tokens that match neither pattern throw `FormatException`.
- **Unix timestamps:** `Exp` and `Iat` are expressed in seconds since the Unix epoch. Convert with `DateTimeOffset.FromUnixTimeSeconds` when a CLR `DateTime` representation is needed.
