# UserSession

`UserSession` represents an authenticated session for a user within the authentication server. It stores identifiers, metadata, and state information that determine whether a session is valid, active, or has been revoked.

## API

### SessionId  
**Type:** `string`  
**Purpose:** Unique identifier for the session. Set when the session is created and never changes during the session lifetime.  
**Remarks:** Should be treated as opaque; do not rely on any specific format.

### UserId  
**Type:** `string`  
**Purpose:** Identifier of the user to whom the session belongs.  
**Remarks:** Correlates with the user store; may be empty only for malformed sessions.

### ClientId  
**Type:** `string`  
**Purpose:** Identifier of the client application that requested the session.  
**Remarks:** Used for auditing and scope validation.

### IpAddress  
**Type:** `string?`  
**Purpose:** IP address from which the session was initiated. May be `null` if not available (e.g., behind a proxy that strips the header).  
**Remarks:** Read‑only after initialization; changes are not expected.

### UserAgent  
**Type:** `string?`  
**Purpose:** User‑agent string of the client that created the session. May be `null` if the header was absent.  
**Remarks:** Informational only; not used for security decisions.

### GrantedScopes  
**Type:** `string`  
**Purpose:** Space‑separated list of OAuth scopes granted to this session.  
**Remarks:** Empty string indicates no scopes; parsing should split on whitespace.

### CreatedAt  
**Type:** `DateTime`  
**Purpose:** Timestamp (UTC) when the session was first created.  
**Remarks:** Never modified after session creation.

### ExpiresAt  
**Type:** `DateTime`  
**Purpose:** Timestamp (UTC) after which the session is considered expired.  
**Remarks:** Should be greater than `CreatedAt`; expiration is enforced by the server logic.

### LastActivityAt  
**Type:** `DateTime?`  
**Purpose:** Timestamp (UTC) of the most recent request that used the session. `null` indicates no activity since creation.  
**Remarks:** Updated by the `Touch` method.

### IsRevoked  
**Type:** `bool`  
**Purpose:** Indicates whether the session has been explicitly revoked.  
**Remarks:** When `true`, the session is considered invalid regardless of expiration.

### RevocationReason  
**Type:** `string?`  
**Purpose:** Optional human‑readable explanation for why the session was revoked. Populated only when `IsRevoked` is `true`.  
**Remarks:** May be `null` if no reason was supplied.

### IsActive  
**Type:** `bool`  
**Purpose:** Computed property indicating whether the session is currently usable.  
**Remarks:** Returns `true` when `!IsRevoked` and `DateTime.UtcNow < ExpiresAt`. Does not consider `LastActivityAt`.

### Revoke  
**Signature:** `public void Revoke(string? reason = null)`  
**Purpose:** Marks the session as revoked, preventing further use.  
**Parameters:**  
- `reason`: Optional explanation for revocation; stored in `RevocationReason`.  
**Return Value:** None.  
**Exceptions:**  
- `InvalidOperationException` if the session is already revoked (calling `Revoke` again with a different reason is not allowed).  

### Touch  
**Signature:** `public void Touch()`  
**Purpose:** Updates `LastActivityAt` to the current UTC time, signalling recent use of the session.  
**Parameters:** None.  
**Return Value:** None.  
**Exceptions:**  
- `InvalidOperationException` if the session is revoked (`IsRevoked` is `true`).  

## Usage

```csharp
// Creating a new session after successful authentication
var session = new UserSession
{
    SessionId   = Guid.NewGuid().ToString(),
    UserId      = user.Id,
    ClientId    = client.Id,
    IpAddress   = context.Connection.RemoteIpAddress?.ToString(),
    UserAgent   = context.Request.Headers.UserAgent.ToString(),
    GrantedScopes = string.Join(" ", grantedScopes),
    CreatedAt   = DateTime.UtcNow,
    ExpiresAt   = DateTime.UtcNow.AddMinutes(20),
    IsRevoked   = false
};

// Later, when a request arrives with the session token
if (session.IsActive && !session.IsRevoked)
{
    // Refresh activity timestamp
    session.Touch();

    // Proceed with authorized operation
    ProcessRequest(session);
}
else
{
    // Session is expired or revoked; reject request
    return Results.Unauthorized();
}
```

```csharp
// Revoking a session explicitly (e.g., user logs out or admin action)
session.Revoke("User initiated logout");

// After revocation, any further checks should fail
if (session.IsRevoked)
{
    // Optionally log the reason
    Logger.LogWarning("Session {SessionId} revoked: {Reason}",
                      session.SessionId, session.RevocationReason);
}
```

## Notes

- All fields are mutable; the type does not provide immutability guarantees. Concurrent reads and writes to the same instance without external synchronization can lead to race conditions, particularly for `LastActivityAt`, `IsRevoked`, and `RevocationReason`.
- The `Revoke` and `Touch` methods are not thread‑safe; invoking them from multiple threads simultaneously may result in lost updates or inconsistent state. Callers should employ locking or rely on a single‑threaded context (e.g., per‑request processing) when accessing a shared `UserSession` instance.
- `GrantedScopes` is stored as a plain string; consumers should treat it as an unordered set and normalize (trim, lower‑case) before comparison if case‑sensitivity is not intended.
- `IpAddress` and `UserAgent` may be `null`; code that depends on these values must handle the null case.
- The `IsActive` property does **not** incorporate a sliding expiration policy; it solely checks against the absolute `ExpiresAt`. Applications requiring idle timeout must evaluate `LastActivityAt` separately.
