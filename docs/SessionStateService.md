# SessionStateService

The `SessionStateService` class manages the lifecycle and persistence of OAuth 2.0 and OpenID Connect authorization session states within the `dotnet-auth-server` project. It serves as both a data model representing a specific session instance and a service interface for creating, retrieving, updating, and validating sessions. This component ensures that transient authorization data, such as nonces, requested scopes, and user identifiers, is maintained securely between the initial authorization request and the final callback, while providing mechanisms for expiration management and cleanup.

## API

### Constructor

**`public SessionStateService()`**
Initializes a new instance of the `SessionStateService`. This constructor typically prepares the internal storage mechanisms required to track session states.

### Methods

**`public string CreateSession(string clientId, string redirectUri, string requestedScopes, string? nonce, string? userId, TimeSpan expiration)`**
Creates a new session state record and persists it.
*   **Parameters**:
    *   `clientId`: The identifier of the client application initiating the request.
    *   `redirectUri`: The URI to which the response will be sent.
    *   `requestedScopes`: A space-separated string of scopes requested by the client.
    *   `nonce`: An optional string value used to mitigate replay attacks.
    *   `userId`: An optional identifier for the authenticated user, if available at creation time.
    *   `expiration`: The duration for which the session remains valid.
*   **Returns**: A unique string identifier (`StateId`) for the newly created session.
*   **Throws**: May throw an exception if the underlying storage mechanism fails or if input parameters violate validation rules (e.g., invalid URI format).

**`public SessionState? GetSession(string stateId)`**
Retrieves an existing session state by its unique identifier.
*   **Parameters**:
    *   `stateId`: The unique identifier of the session to retrieve.
*   **Returns**: A `SessionState` object if the session exists and has not expired; otherwise, `null`.
*   **Throws**: Generally does not throw for missing sessions (returns `null`), but may throw if the data store is inaccessible.

**`public bool CompleteSession(string stateId, string? grantedScopes)`**
Finalizes a session, typically called after successful user authentication and consent.
*   **Parameters**:
    *   `stateId`: The unique identifier of the session to complete.
    *   `grantedScopes`: The final list of scopes granted by the user, which may differ from the requested scopes.
*   **Returns**: `true` if the session was successfully found, validated, and marked as completed; `false` if the session does not exist, has already expired, or was already completed.
*   **Throws**: Unlikely to throw exceptions; failures are indicated by the boolean return value.

**`public bool UpdateSession(string stateId, Action<SessionState> updateAction)`**
Applies specific updates to an existing session state.
*   **Parameters**:
    *   `stateId`: The unique identifier of the session to update.
    *   `updateAction`: A delegate defining the modifications to apply to the session object.
*   **Returns**: `true` if the update was applied successfully; `false` if the session could not be found or is invalid.
*   **Throws**: May throw if the `updateAction` delegate causes an internal error or if concurrency checks fail.

**`public int CleanupExpiredSessions()`**
Removes all session records that have passed their `ExpiresAt` timestamp.
*   **Parameters**: None.
*   **Returns**: The number of sessions removed from the storage.
*   **Throws**: May throw if the storage backend encounters an error during deletion.

**`public int GetActiveSessionCount()`**
Counts the number of currently valid (non-expired) sessions.
*   **Parameters**: None.
*   **Returns**: An integer representing the count of active sessions.
*   **Throws**: May throw if the storage backend is unavailable.

### Properties

The following properties represent the state of a specific session instance when retrieved via `GetSession` or within the context of the service:

*   **`public string StateId`**: The unique identifier for this session.
*   **`public string ClientId`**: The ID of the client associated with this session.
*   **`public string RedirectUri`**: The registered redirect URI for this flow.
*   **`public string RequestedScopes`**: The original scopes requested by the client.
*   **`public string? GrantedScopes`**: The scopes actually granted by the resource owner; null until the session is completed.
*   **`public string? Nonce`**: The cryptographic nonce associated with the request, if provided.
*   **`public string? UserId`**: The identifier of the user associated with the session, if authenticated.
*   **`public DateTime CreatedAt`**: The UTC timestamp when the session was created.
*   **`public DateTime ExpiresAt`**: The UTC timestamp when the session becomes invalid.
*   **`public DateTime? LastUpdatedAt`**: The UTC timestamp of the last modification, if any.
*   **`public bool IsValid`**: A computed property indicating whether the session is currently within its valid time window and has not been revoked.

## Usage

### Example 1: Creating and Retrieving a Session
This example demonstrates initiating an authorization flow by creating a session and subsequently retrieving it to validate the state parameter.

```csharp
using System;
using DotNetAuthServer.Services;

public class AuthorizationFlow
{
    private readonly SessionStateService _sessionService;

    public AuthorizationFlow(SessionStateService sessionService)
    {
        _sessionService = sessionService;
    }

    public string InitiateLogin(string clientId, string redirectUri)
    {
        // Create a new session with a 15-minute expiration
        string stateId = _sessionService.CreateSession(
            clientId: clientId,
            redirectUri: redirectUri,
            requestedScopes: "openid profile email",
            nonce: Guid.NewGuid().ToString(),
            userId: null,
            expiration: TimeSpan.FromMinutes(15)
        );

        // Retrieve to verify creation
        var session = _sessionService.GetSession(stateId);
        
        if (session != null && session.IsValid)
        {
            return stateId; // Return stateId to be included in the authorize URL
        }

        throw new InvalidOperationException("Failed to initialize session state.");
    }
}
```

### Example 2: Completing a Session and Cleanup
This example shows how to finalize a session after user consent and perform routine maintenance to remove expired entries.

```csharp
using System;
using DotNetAuthServer.Services;

public class CallbackHandler
{
    private readonly SessionStateService _sessionService;

    public CallbackHandler(SessionStateService sessionService)
    {
        _sessionService = sessionService;
    }

    public bool HandleCallback(string stateId, string[] userGrantedScopes)
    {
        // Attempt to complete the session with the granted scopes
        bool success = _sessionService.CompleteSession(
            stateId: stateId,
            grantedScopes: string.Join(" ", userGrantedScopes)
        );

        if (!success)
        {
            // Session might be expired, already completed, or invalid
            return false;
        }

        // Perform periodic cleanup of expired sessions
        int removedCount = _sessionService.CleanupExpiredSessions();
        Console.WriteLine($"Cleanup removed {removedCount} expired sessions.");

        return true;
    }
}
```

## Notes

*   **Expiration Logic**: The `IsValid` property is strictly time-bound based on `CreatedAt` and `ExpiresAt`. Once `DateTime.UtcNow` exceeds `ExpiresAt`, `IsValid` returns `false`, and operations like `CompleteSession` will fail for that ID.
*   **Thread Safety**: While individual method calls are designed to be atomic, the `SessionStateService` does not guarantee thread safety for complex multi-step operations performed externally (e.g., checking `IsValid` then calling `UpdateSession`). Consumers should implement appropriate locking if race conditions on specific session IDs are a concern in high-concurrency environments.
*   **Null Handling**: Properties such as `GrantedScopes`, `Nonce`, and `UserId` are nullable. Consumers must check for null values before accessing members or performing string operations on these fields, particularly before a session is completed.
*   **Cleanup Strategy**: The `CleanupExpiredSessions` method is a manual trigger. It is the responsibility of the hosting application to call this method periodically (e.g., via a background hosted service) to prevent storage bloat, as sessions are not automatically deleted upon expiration.
*   **Immutability of Keys**: Core identifiers like `StateId`, `ClientId`, and `RedirectUri` are set at creation time and should not be modified via `UpdateSession`. Changing these values would invalidate the cryptographic binding of the authorization request.
