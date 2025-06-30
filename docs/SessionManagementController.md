# SessionManagementController

The `SessionManagementController` provides HTTP endpoints for managing user sessions in the authentication server. It enables querying active sessions, retrieving statistics, revoking individual or bulk sessions, and cleaning up expired records.

## API

### GetAllActiveSessionsAsync
**Purpose:** Returns a collection of all currently active sessions.  
**Parameters:** none.  
**Return Value:** `Task<IActionResult>` yielding a JSON array of session objects (containing `SessionId`, `UserId`, `ClientId`, `IpAddress`, `UserAgent`, `GrantedScopes`, `CreatedAt`, `ExpiresAt`, `LastActivityAt`, `IsRevoked`, `RevocationReason`) with a 200 OK status, or an appropriate error status.  
**When it throws:** May propagate exceptions from the underlying session store (e.g., `InvalidOperationException` if the store is unavailable) which are caught by the ASP.NET Core pipeline and transformed into a 500 response.

### GetStatsAsync
**Purpose:** Provides aggregate statistics about sessions (e.g., total active, expired, revoked counts).  
**Parameters:** none.  
**Return Value:** `Task<IActionResult>` containing a JSON object with statistical data and a 200 OK status, or an error status on failure.  
**When it throws:** Any exception from the statistics query is bubbled up and results in a 500 response.

### GetUserSessionsAsync
**Purpose:** Retrieves all sessions associated with a specific user.  
**Parameters:** none.  
**Return Value:** `Task<IActionResult>` with a JSON array of the user’s sessions and a 200 OK status, or an error status if the user cannot be found.  
**When it throws:** Exceptions from the data access layer are propagated and become 500 responses.

### RevokeSessionAsync
**Purpose:** Marks a single session as revoked, preventing further use of its tokens.  
**Parameters:** none.  
**Return Value:** `Task<IActionResult>` returning 200 OK on successful revocation, 404 if the session does not exist, or 500 on unexpected errors.  
**When it throws:** Errors accessing the store are propagated as 500 responses.

### RevokeAllUserSessionsAsync
**Purpose:** Revokes every session belonging to a given user.  
**Parameters:** none.  
**Return Value:** `Task<IActionResult>` yielding 200 OK when all sessions are revoked, or an error status on failure.  
**When it throws:** Store‑related exceptions become 500 responses.

### CleanupExpiredAsync
**Purpose:** Deletes sessions whose `ExpiresAt` timestamp is in the past.  
**Parameters:** none.  
**Return Value:** `Task<IActionResult>` returning 200 OK with a count of removed sessions, or an error status.  
**When it throws:** Exceptions from the cleanup operation are propagated as 500 responses.

### SessionId
**Type:** `string`  
**Purpose:** Unique identifier for the session.  

### UserId
**Type:** `string`  
**Purpose:** Identifier of the user to whom the session belongs.  

### ClientId
**Type:** `string`  
**Purpose:** Identifier of the client application that requested the session.  

### IpAddress
**Type:** `string?`  
**Purpose:** IP address from which the session was initiated; may be null if not recorded.  

### UserAgent
**Type:** `string?`  
**Purpose:** User‑agent string of the client device; may be null.  

### GrantedScopes
**Type:** `string`  
**Purpose:** Space‑separated list of OAuth scopes granted to the session.  

### CreatedAt
**Type:** `DateTime`  
**Purpose:** Timestamp when the session was created.  

### ExpiresAt
**Type:** `DateTime`  
**Purpose:** Timestamp when the session will expire if not renewed or revoked.  

### LastActivityAt
**Type:** `DateTime?`  
**Purpose:** Timestamp of the last request made using the session; null if no activity has been recorded.  

### IsRevoked
**Type:** `bool`  
**Purpose:** Indicates whether the session has been revoked (`true`) or is still valid (`false`).  

### RevocationReason
**Type:** `string?`  
**Purpose:** Optional description explaining why the session was revoked; null if not revoked or reason not supplied.  

## Usage

```csharp
// Example 1: Retrieving all active sessions via dependency injection
[ApiController]
[Route("api/[controller]")]
public class SessionManagementController : ControllerBase
{
    private readonly ISessionService _sessionService;

    public SessionManagementController(ISessionService sessionService)
    {
        _sessionService = sessionService;
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetAllActiveSessionsAsync()
    {
        var sessions = await _sessionService.GetAllActiveSessionsAsync();
        return Ok(sessions);
    }
}

// Example 2: Revoking a specific session
[HttpPost("revoke/{sessionId}")]
public async Task<IActionResult> RevokeSessionAsync(string sessionId)
{
    var result = await _sessionService.RevokeSessionAsync(sessionId);
    if (!result.Success)
        return NotFound(new { Error = result.ErrorMessage });

    return Ok();
}
```

## Notes
- The controller itself is stateless and thread‑safe; concurrent requests are safe as long as the injected `ISessionService` implementation handles concurrency correctly.
- `IpAddress` and `UserAgent` may be null; consumers should guard against null reference when logging or displaying these values.
- `LastActivityAt` being null indicates that no activity has been recorded since session creation; treat it as equivalent to `CreatedAt` for idle‑timeout calculations.
- Revoking a session that is already revoked is idempotent – the operation will succeed without error.
- The `CleanupExpiredAsync` endpoint should be called periodically (e.g., via a hosted background service) to prevent unbounded growth of the session store.
- Exception handling is deliberately minimal in the controller; all unexpected errors are allowed to bubble up to the ASP.NET Core exception middleware, which returns a 500 Internal Server Error response. This keeps the controller focused on request/response translation.
