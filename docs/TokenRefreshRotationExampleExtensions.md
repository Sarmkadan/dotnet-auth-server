# TokenRefreshRotationExampleExtensions

Extension methods for `TokenRefreshRotationExample` that provide token validation, refresh batching, statistics calculation, and safe token extraction utilities for token refresh and rotation scenarios.


## API

### ValidateTokenResponse

Validates that a token response contains all required fields for a successful OAuth2 token exchange.

- **Parameters:**
  - `example` – The `TokenRefreshRotationExample` instance (extension method receiver)
  - `tokenResponse` – The token response to validate
- **Returns:** `true` if the token response has a non-empty access token, refresh token, positive `expires_in`, and token type; otherwise `false`
- **Exceptions:** Throws `ArgumentNullException` if `example` or `tokenResponse` is `null`

### GetTokenExpirationTime

Computes the absolute UTC expiration time for a token based on the current time and the `expires_in` value from the response.

- **Parameters:**
  - `example` – The `TokenRefreshRotationExample` instance (extension method receiver)
  - `tokenResponse` – The token response containing `ExpiresIn`
- **Returns:** A `DateTime` representing when the token will expire in UTC
- **Exceptions:** Throws `ArgumentNullException` if `example` or `tokenResponse` is `null`

### GetTokenTimeRemaining

Calculates the remaining lifetime of a token in seconds, returning zero if the token is already expired.

- **Parameters:**
  - `example` – The `TokenRefreshRotationExample` instance (extension method receiver)
  - `tokenResponse` – The token response containing `ExpiresIn`
- **Returns:** The remaining lifetime in seconds; zero if the token has expired or will expire within the next tick
- **Exceptions:** Throws `ArgumentNullException` if `example` or `tokenResponse` is `null`

### SafeGetAccessToken

Safely extracts the access token from a response, returning `null` if the token is `null`, empty, or whitespace.

- **Parameters:**
  - `example` – The `TokenRefreshRotationExample` instance (extension method receiver)
  - `tokenResponse` – The token response to extract from
- **Returns:** The access token if it is non-empty; otherwise `null`
- **Exceptions:** Throws `ArgumentNullException` if `example` is `null`

### SafeGetRefreshToken

Safely extracts the refresh token from a response, returning `null` if the token is `null`, empty, or whitespace.

- **Parameters:**
  - `example` – The `TokenRefreshRotationExample` instance (extension method receiver)
  - `tokenResponse` – The token response to extract from
- **Returns:** The refresh token if it is non-empty; otherwise `null`
- **Exceptions:** Throws `ArgumentNullException` if `example` is `null`

### FormatTokenInfo

Creates a multi-line string representation of a token response including access and refresh token prefixes, expiration details, and remaining lifetime.

- **Parameters:**
  - `example` – The `TokenRefreshRotationExample` instance (extension method receiver)
  - `tokenResponse` – The token response to format
- **Returns:** A formatted string with token metadata and truncated token values for safe logging
- **Exceptions:** Throws `ArgumentNullException` if `example` or `tokenResponse` is `null`

### BatchRefreshTokensAsync

Enumerates over a collection of refresh tokens, asynchronously refreshing each one and yielding a result per token.

- **Parameters:**
  - `example` – The `TokenRefreshRotationExample` instance (extension method receiver)
  - `refreshTokens` – An enumerable of refresh token strings to refresh
- **Returns:** An `IAsyncEnumerable<TokenRefreshResult>` that streams results as each refresh completes
- **Exceptions:** Throws `ArgumentNullException` if `example` or `refreshTokens` is `null`
- **Remarks:** Each yielded `TokenRefreshResult` contains the original refresh token, success flag, new token (if successful), and an optional error message. Invalid or empty tokens are reported with `Success = false` and an appropriate error.

### GetTokenStatistics

Aggregates statistics from a collection of token responses, including counts, average expiration, min/max values, and total valid duration.

- **Parameters:**
  - `example` – The `TokenRefreshRotationExample` instance (extension method receiver)
  - `tokenResponses` – An enumerable of token responses to analyze
- **Returns:** A `TokenStatistics` instance populated with counts and computed metrics
- **Exceptions:** Throws `ArgumentNullException` if `example` or `tokenResponses` is `null`
- **Remarks:** Only responses with `ExpiresIn > 0` are considered valid. If no valid tokens exist, all numeric properties are zeroed.

### TokenRefreshResult

Represents the outcome of a single token refresh operation within a batch.

- **Properties:**
  - `RefreshToken` (`string`, required) – The original refresh token used for this operation
  - `Success` (`bool`, required) – Indicates whether the refresh operation succeeded
  - `NewToken` (`TokenResponse?`) – The new token response if the refresh succeeded; otherwise `null`
  - `Error` (`string?`) – An error message if the refresh failed; otherwise `null`

### TokenStatistics

Aggregated statistics for a collection of token responses.

- **Properties:**
  - `TotalTokens` (`int`, required) – Total number of tokens in the collection
  - `ValidTokens` (`int`, required) – Number of tokens with `ExpiresIn > 0`
  - `AverageExpiresIn` (`int`, required) – Average `ExpiresIn` value in seconds for valid tokens
  - `MinExpiresIn` (`int`, required) – Minimum `ExpiresIn` value in seconds among valid tokens
  - `MaxExpiresIn` (`int`, required) – Maximum `ExpiresIn` value in seconds among valid tokens
  - `TotalValidDuration` (`int`, required) – Sum of `ExpiresIn` values for all valid tokens in seconds

## Usage

### Example 1: Validating and formatting token responses

```csharp
using DotnetAuthServer.Domain.Models;
using DotnetAuthServer.Examples;

var example = new TokenRefreshRotationExample("https://auth.example.com");

var token = new TokenResponse
{
    AccessToken = "abc123xyz",
    RefreshToken = "def456uvw",
    ExpiresIn = 3600,
    TokenType = "Bearer"
};

// Validate the token response
bool isValid = example.ValidateTokenResponse(token);
Console.WriteLine($"Token valid: {isValid}");

// Get expiration time and remaining duration
double secondsLeft = example.GetTokenTimeRemaining(token);
DateTime expiresAt = example.GetTokenExpirationTime(token);

// Safely extract tokens
string? accessToken = example.SafeGetAccessToken(token);
string? refreshToken = example.SafeGetRefreshToken(token);

// Format token information for logging
string info = example.FormatTokenInfo(token);
Console.WriteLine(info);
```

### Example 2: Batch refreshing tokens and analyzing results

```csharp
using DotnetAuthServer.Domain.Models;
using DotnetAuthServer.Examples;

var example = new TokenRefreshRotationExample("https://auth.example.com");
var refreshTokens = new[] { "token1", "token2", "token3" };

// Batch refresh tokens asynchronously
var results = example.BatchRefreshTokensAsync(refreshTokens);

var stats = new TokenStatistics
{
    TotalTokens = 0,
    ValidTokens = 0,
    AverageExpiresIn = 0,
    MinExpiresIn = 0,
    MaxExpiresIn = 0,
    TotalValidDuration = 0
};

await foreach (var result in results)
{
    if (result.Success)
    {
        Console.WriteLine($"✓ Refreshed: {result.RefreshToken[..10]}... -> New token: {result.NewToken?.AccessToken[..10]}...");
        stats.TotalTokens++;
        stats.ValidTokens++;
        stats.TotalValidDuration += result.NewToken?.ExpiresIn ?? 0;
    }
    else
    {
        Console.WriteLine($"✗ Failed: {result.RefreshToken} - {result.Error}");
    }
}

// Calculate final statistics
stats = example.GetTokenStatistics(new[] { /* your refreshed tokens here */ });
Console.WriteLine($"Total: {stats.TotalTokens}, Valid: {stats.ValidTokens}, Avg lifetime: {stats.AverageExpiresIn}s");
```

## Notes

- **Thread Safety:** All extension methods are safe to call concurrently from multiple threads. They do not mutate shared state beyond the provided arguments.
- **Null Safety:** Methods validate arguments with `ArgumentNullException.ThrowIfNull` and safely handle `null` or empty tokens via helper methods like `SafeGetAccessToken`.
- **Time Handling:** `GetTokenExpirationTime` and `GetTokenTimeRemaining` use `DateTime.UtcNow` to avoid local time zone issues; results are suitable for UTC-based comparisons and logging.
- **Token Truncation:** `FormatTokenInfo` truncates token values to 20 characters to avoid logging sensitive data in full.
- **Batch Behavior:** `BatchRefreshTokensAsync` streams results as they complete, enabling early processing of successful refreshes while failures are still being retried.
- **Statistics Edge Cases:** When no valid tokens exist, `GetTokenStatistics` returns a zeroed `TokenStatistics` instance rather than `null`, simplifying consumer code.
- **Validation Rules:** `ValidateTokenResponse` enforces presence of `AccessToken`, `RefreshToken`, positive `ExpiresIn`, and non-empty `TokenType` to match typical OAuth2 server behavior.