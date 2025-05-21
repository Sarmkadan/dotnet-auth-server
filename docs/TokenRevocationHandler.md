# TokenRevocationHandler

The `TokenRevocationHandler` class provides the core logic for invalidating access and refresh tokens within the `dotnet-auth-server` ecosystem. It encapsulates the asynchronous operations required to revoke individual tokens or bulk-revoke all tokens associated with a specific user identity, returning a structured result that indicates the success of the operation, whether a revocation actually occurred, or if an error prevented completion.

## API

### Constructors

#### `public TokenRevocationHandler()`
Initializes a new instance of the `TokenRevocationHandler` class. This constructor prepares the handler to process revocation requests using the default configuration and underlying token stores defined in the application context.

### Methods

#### `public async Task<RevocationResult> RevokeTokenAsync(string token)`
Attempts to locate and invalidate a specific token identified by its string value.

*   **Parameters**:
    *   `token`: The string representation of the access or refresh token to be revoked.
*   **Return Value**:
    *   Returns a `Task<RevocationResult>` containing the outcome of the operation. The result indicates if the token was found and successfully marked as revoked.
*   **Exceptions**:
    *   Throws an exception if the underlying token store is unavailable or if the provided token string format is invalid according to server policies.

#### `public async Task<RevocationResult> RevokeUserTokensAsync(string userId)`
Invalidates all active tokens issued to a specific user, effectively forcing a global logout for that identity.

*   **Parameters**:
    *   `userId`: The unique identifier of the user whose tokens should be revoked.
*   **Return Value**:
    *   Returns a `Task<RevocationResult>` summarizing the bulk operation. The `Revoked` property in the result typically reflects whether at least one token was invalidated during the process.
*   **Exceptions**:
    *   Throws an exception if the user ID is null or empty, or if a database connectivity issue prevents scanning the user's token set.

### Properties

The following properties are typically inspected on the `RevocationResult` returned by the methods above, or exposed by the handler context depending on implementation specifics:

#### `public bool Success`
Indicates whether the revocation request was processed without encountering a system-level error. A value of `true` means the operation completed logically, regardless of whether a matching token was found.

#### `public bool Revoked`
Specifies whether a token was actually found and invalidated. This may be `false` even if `Success` is `true` (e.g., if the token was already expired or previously revoked).

#### `public string? Error`
Contains a descriptive error message if the operation failed (`Success` is `false`). If the operation succeeded, this property is `null`.

## Usage

### Example 1: Revoking a Single Refresh Token
This example demonstrates handling a logout request where a specific refresh token is presented by the client.

```csharp
using DotNetAuthServer.Handlers;
using DotNetAuthServer.Models;

public async Task<IActionResult> Logout(string refreshToken)
{
    var handler = new TokenRevocationHandler();
    
    // Attempt to revoke the specific token
    var result = await handler.RevokeTokenAsync(refreshToken);

    if (!result.Success)
    {
        // Log system error: result.Error contains details
        return StatusCode(500, "Unable to process revocation request.");
    }

    if (result.Revoked)
    {
        return Ok("Token successfully revoked.");
    }
    
    // Token was not found or already invalid, but no system error occurred
    return Ok("Token is no longer active.");
}
```

### Example 2: Bulk Revocation for Security Events
This example illustrates revoking all tokens for a user following a password change or suspected compromise.

```csharp
using DotNetAuthServer.Handlers;
using DotNetAuthServer.Models;

public async Task ForceLogoutUser(string userId)
{
    var handler = new TokenRevocationHandler();

    // Revoke all sessions for the user
    var result = await handler.RevokeUserTokensAsync(userId);

    if (!result.Success)
    {
        throw new InvalidOperationException(
            $"Failed to revoke tokens for user {userId}: {result.Error}"
        );
    }

    // Audit log: result.Revoked indicates if there were active sessions to kill
    if (result.Revoked)
    {
        Console.WriteLine($"Active sessions terminated for user {userId}");
    }
}
```

## Notes

*   **Idempotency**: The `RevokeTokenAsync` method is idempotent. Calling it multiple times with the same valid token string will return `Success = true`, but `Revoked` will only be `true` on the first invocation where the token was actually active. Subsequent calls will return `Revoked = false`.
*   **Null Safety**: The `Error` property is nullable (`string?`). Consumers must check the `Success` property before reading `Error` to avoid assuming an error state when the operation completed successfully.
*   **Thread Safety**: The `TokenRevocationHandler` instance itself should be treated as stateless regarding request data, but the underlying asynchronous operations rely on external token stores. While the handler methods are `async` and non-blocking, creating a new instance per request is recommended to avoid any potential state leakage between concurrent user operations.
*   **Race Conditions**: In high-concurrency scenarios between `RevokeTokenAsync` and `RevokeUserTokensAsync` for the same user, the `Revoked` flag reflects the state at the exact moment of execution. It is possible for a bulk revocation to report `Revoked = true` while a simultaneous single-token revocation reports `Revoked = false` if the bulk operation completed first.
