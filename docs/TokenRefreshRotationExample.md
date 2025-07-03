# TokenRefreshRotationExample

The `TokenRefreshRotationExample` class provides a demonstration of secure token rotation patterns within the `dotnet-auth-server` ecosystem. It illustrates how clients should interact with the authentication server to refresh access tokens, manage token lifecycles in mobile application contexts, and implement resilient refresh strategies to handle network instability or token expiration effectively.

## API

### TokenRefreshRotationExample
*   **`public async Task<TokenResponse?> RefreshTokenAsync()`**: Requests a new set of tokens from the authorization server using a current valid refresh token. Returns a `TokenResponse` if successful, otherwise `null`. Throws `HttpRequestException` if the network request fails.
*   **`public async Task DemonstrateTokenRotationAsync()`**: Orchestrates a complete demonstration of the token rotation flow, covering initial authentication, subsequent rotation, and cleanup of the old token.
*   **`public async Task<TokenResponse?> RefreshWithExpirationAsync()`**: Attempts to refresh a token while checking against known expiration constraints. Returns `TokenResponse` if the refresh succeeds within the allowed timeframe, otherwise `null`.

### MobileAppTokenManager
*   **`public void SetToken(string accessToken, string refreshToken, int expiresIn, string tokenType)`**: Updates the local storage with new token material.
*   **`public async Task<string?> GetValidAccessTokenAsync()`**: Checks the current access token's validity; if expired or nearing expiration, it triggers the rotation logic. Returns the valid access token string or `null` if authorization is lost.
*   **`public async Task<bool> CallApiAsync(string url)`**: Executes an authenticated API call. Automatically manages token validation and rotation prior to the request. Returns `true` on a successful response (2xx), `false` otherwise.

### ResilientTokenRefreshExample
*   **`public async Task<TokenResponse?> RefreshWithRetryAsync()`**: Performs a token refresh request, automatically applying retries upon transient network failures. Returns `TokenResponse` or `null` after exhausting retries.
*   **`public async Task<TokenResponse?> RefreshWithFallbackAsync()`**: Attempts a token refresh; if the primary endpoint fails, it attempts a secondary fallback mechanism. Returns `TokenResponse` or `null`.
*   **`public string AccessToken`**: The current active access token.
*   **`public string RefreshToken`**: The refresh token utilized to obtain new access tokens.
*   **`public int ExpiresIn`**: The remaining lifetime of the access token in seconds.
*   **`public string TokenType`**: The type of the token (e.g., "Bearer").

## Usage

### Example 1: Basic Token Rotation
```csharp
var rotationExample = new TokenRefreshRotationExample();
// Demonstrate the full rotation flow
await rotationExample.DemonstrateTokenRotationAsync();
```

### Example 2: Managing Tokens in a Mobile Context
```csharp
var manager = new MobileAppTokenManager();
// Set initial tokens obtained from login
manager.SetToken("initial_access", "initial_refresh", 3600, "Bearer");

// Automatically manage token validity during API interaction
bool success = await manager.CallApiAsync("https://api.example.com/data");
if (!success)
{
    // Handle unauthorized state
}
```

## Notes

*   **Thread Safety**: The `MobileAppTokenManager` implementation is designed to be thread-safe for reading the access token, but `SetToken` should be invoked in a serialized manner if concurrent updates to token storage are expected.
*   **Network Resilience**: The `ResilientTokenRefreshExample` should be utilized in environments with intermittent connectivity. It does not replace proper server-side token revocation handling.
*   **Edge Cases**:
    *   If `RefreshTokenAsync` returns `null`, the client must assume the refresh token is invalid or revoked and prompt the user for re-authentication.
    *   `GetValidAccessTokenAsync` must be called immediately prior to any sensitive API request to ensure that the token used in the `Authorization` header is not stale.
