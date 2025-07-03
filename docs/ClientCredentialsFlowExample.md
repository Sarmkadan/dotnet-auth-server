# ClientCredentialsFlowExample

The `ClientCredentialsFlowExample` class provides a demonstration implementation of the OAuth 2.0 client credentials flow within the `dotnet-auth-server` project, facilitating secure service-to-service authentication and communication. It encapsulates the lifecycle of acquiring, validating, and revoking access tokens, alongside integrated examples for executing background tasks and multi-service authentication scenarios.

## API

### Constructors

*   **`ClientCredentialsFlowExample()`**: Initializes a new instance of the `ClientCredentialsFlowExample` class.

### Methods

*   **`Task<TokenResponse?> GetServiceAccessTokenAsync()`**: Requests an access token from the authorization server using the client credentials flow. Returns a `TokenResponse` object if successful, otherwise returns `null`. Throws `HttpRequestException` if the network request fails.
*   **`Task CallDownstreamApiAsync()`**: Invokes a protected downstream API using the currently managed access token. Throws `InvalidOperationException` if an access token is not available.
*   **`Task<bool> ValidateTokenAsync()`**: Validates the current access token against the authorization server. Returns `true` if the token is active, otherwise `false`.
*   **`Task RevokeTokenAsync()`**: Revokes the current access token.
*   **`Task ExecuteJobAsync()`**: Performs a background data processing job.
*   **`Task ShutdownAsync()`**: Gracefully shuts down the background processor service.
*   **`Task DemonstrateMultiServiceAsync()`**: Demonstrates a multi-service authentication flow.

### Properties

*   **`BackgroundDataProcessorService`**: Gets or sets the background data processor service instance.
*   **`MultiServiceAuthenticationExample`**: Gets or sets the multi-service authentication example instance.
*   **`AccessToken`**: Gets the current access token string.
*   **`TokenType`**: Gets the type of the current access token.
*   **`ExpiresIn`**: Gets the remaining lifetime of the access token in seconds.
*   **`Scope`**: Gets the scopes associated with the access token.
*   **`Active`**: Gets a value indicating whether the current access token is active.
*   **`ClientId`**: Gets the client identifier, if available.
*   **`Subject`**: Gets the subject of the access token, if available.
*   **`ExpiresAt`**: Gets the expiration timestamp of the access token, if available.

## Usage

### Basic Token Acquisition and API Call

```csharp
var authExample = new ClientCredentialsFlowExample();
var tokenResponse = await authExample.GetServiceAccessTokenAsync();

if (tokenResponse != null)
{
    // Access token acquired, proceed to call downstream API
    await authExample.CallDownstreamApiAsync();
}
```

### Demonstrating Multi-Service Authentication

```csharp
var authExample = new ClientCredentialsFlowExample();

// Execute a multi-service demonstration flow
await authExample.DemonstrateMultiServiceAsync();
```

## Notes

*   **Asynchronous Operations**: All methods performing I/O operations (token requests, API calls) are asynchronous and should be awaited to avoid blocking the calling thread.
*   **Error Handling**: Methods interacting with the authorization server may throw `HttpRequestException` in case of network connectivity issues or server-side errors. Consumers should implement appropriate retry policies or exception handling.
*   **Thread Safety**: The `ClientCredentialsFlowExample` class is designed for use in asynchronous contexts, but it is not inherently thread-safe for concurrent modifications of its internal state (e.g., properties like `AccessToken`). If accessed from multiple threads simultaneously, external synchronization mechanisms are required.
*   **Token Expiration**: The `ExpiresIn` and `ExpiresAt` properties should be used to proactively manage token lifetime and trigger token refresh or acquisition before the current token expires.
