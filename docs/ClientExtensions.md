# ClientExtensions

The `ClientExtensions` static class provides a collection of extension methods designed to simplify the retrieval and validation of configuration properties for OAuth 2.0 and OpenID Connect clients. These utilities centralize common logic used throughout the authentication server to determine client capabilities, security requirements, and policy constraints based on the `Client` entity.

## API

### IsPublicClient
Determines whether the specified client is classified as a public client, which does not require a client secret.

*   **Parameters:** `Client client` - The client instance to inspect.
*   **Returns:** `bool` - `true` if the client is a public client; otherwise, `false`.
*   **Throws:** `ArgumentNullException` if the `client` parameter is `null`.

### RequiresPkce
Indicates whether the specified client is required to use Proof Key for Code Exchange (PKCE) during the authorization flow.

*   **Parameters:** `Client client` - The client instance to inspect.
*   **Returns:** `bool` - `true` if the client must use PKCE; otherwise, `false`.
*   **Throws:** `ArgumentNullException` if the `client` parameter is `null`.

### GetTokenLifetimeMinutes
Retrieves the configured token lifetime duration for the specified client, measured in minutes.

*   **Parameters:** `Client client` - The client instance to inspect.
*   **Returns:** `int` - The lifetime of the token in minutes.
*   **Throws:** `ArgumentNullException` if the `client` parameter is `null`.

### HasCorsOrigins
Checks if the specified client has any Cross-Origin Resource Sharing (CORS) origins explicitly configured.

*   **Parameters:** `Client client` - The client instance to inspect.
*   **Returns:** `bool` - `true` if the client has one or more CORS origins defined; otherwise, `false`.
*   **Throws:** `ArgumentNullException` if the `client` parameter is `null`.

## Usage

### Example 1: Validating Client Security Requirements
This example demonstrates how to check if a client requires PKCE before initiating an authorization request.

```csharp
public void ValidateAuthorizationRequest(Client client)
{
    if (client.RequiresPkce())
    {
        // Enforce PKCE requirement
        ValidatePkceParameters();
    }
}
```

### Example 2: Configuring Token Lifetimes
This example retrieves the client-specific token lifetime to set the expiration for a generated access token.

```csharp
public Token GenerateAccessToken(Client client)
{
    int lifetimeMinutes = client.GetTokenLifetimeMinutes();
    return new Token 
    { 
        ExpiresAt = DateTime.UtcNow.AddMinutes(lifetimeMinutes) 
    };
}
```

## Notes

*   **Null Safety:** All methods in this class expect a non-null `Client` instance. Passing a `null` argument will result in an `ArgumentNullException`.
*   **Thread Safety:** The methods within `ClientExtensions` are stateless and operate only on the provided `Client` instance. They are inherently thread-safe, provided the `Client` object itself is not being modified concurrently by another thread.
*   **Performance:** These methods are designed as simple wrappers around properties of the `Client` entity and are intended to have minimal performance overhead.
