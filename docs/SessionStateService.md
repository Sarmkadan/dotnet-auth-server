# SessionStateService

`SessionStateService` holds short-lived OAuth 2.0 / OpenID Connect authorization state between the authorization request and its completion: the state id, client, redirect URI, requested and granted scopes, nonce, and user id. Entries are kept in an in-memory `ConcurrentDictionary<string, SessionState>` and expire 10 minutes after creation.

The class is `public sealed` and lives in `src/Services/SessionStateService.cs`, namespace `DotnetAuthServer.Services`. It has no interface. The same file defines the public `SessionState` class, which `GetSession` returns.

## Registration

`Program.cs` registers the service with `AddScoped<SessionStateService>()`.

> **Caveat:** the session dictionary is an instance field. With a scoped registration, each request gets a new instance and therefore a new, empty store. A session created in one request is not visible to later requests. Cross-request use needs a singleton registration or an external store. This document describes the class as it is written.

## Role in the system

- Creates state when an authorization request starts.
- Reads state at the callback or consent step.
- Records the authenticated user and the granted scopes.
- Removes state on completion, to prevent replay.
- Removes expired entries on demand.

Logging goes through `ILogger<SessionStateService>`. Informational events are logged for creation, completion, and cleanup. Warnings are logged for unknown and expired state ids. The state id is logged in full, so treat these logs as sensitive.

## Constructor

### `public SessionStateService(ILogger<SessionStateService> logger)`

Creates the service.

**Parameters:**
- `logger` – logger for session events.

**Exceptions:** `ArgumentNullException` if `logger` is `null`.

## Session lifetime

The lifetime is fixed at 10 minutes from creation. It is not configurable. The three places that check expiry use different thresholds:

| Check | Treated as expired when | Used by |
|---|---|---|
| `GetSession` | `ExpiresAt` is within 5 seconds of now or past (`DateTime.IsExpired()`) | `GetSession` |
| `SessionState.IsValid()` | `ExpiresAt <= UtcNow` | Callers that hold a `SessionState` |
| Cleanup | `ExpiresAt <= UtcNow` | `CleanupExpiredSessions` |

In the last 5 seconds of its life, a session still passes `IsValid()` but `GetSession` returns `null`.

## Methods

### `public string CreateSession(string clientId, string redirectUri, string scopes, string? nonce = null)`

Creates a session and returns its state id. The id is `Guid.NewGuid().ToString("N")`: 32 lowercase hex characters with no dashes. The session stores `CreatedAt` as the current UTC time and `ExpiresAt` as 10 minutes later. `scopes` is stored as `RequestedScopes` without parsing or validation. The OAuth convention is a space-separated list. `redirectUri` is not checked for URI format.

**Parameters:**
- `clientId` – ID of the client that started the flow. Must not be `null` or empty.
- `redirectUri` – redirect URI for the response. Must not be `null` or empty.
- `scopes` – requested scopes. Must not be `null` or empty.
- `nonce` – OpenID Connect nonce, stored as given. Optional.

**Return value:** the state id to send with the authorization request.

**Exceptions:**
- `ArgumentNullException` if `clientId`, `redirectUri`, or `scopes` is `null`.
- `ArgumentException` if one of them is an empty string. Whitespace-only values are accepted.

### `public SessionState? GetSession(string stateId)`

Returns the live session for `stateId`, or `null`. The returned object is the stored instance, not a copy, so changes to its properties are visible to later calls.

- `null` or whitespace `stateId`: returns `null` with no log entry.
- Unknown id: logs a warning and returns `null`.
- Expired (see [Session lifetime](#session-lifetime)): logs a warning, removes the entry, and returns `null`.
- Otherwise: returns the session.

**Parameters:**
- `stateId` – the state id from `CreateSession`.

**Exceptions:** none. Invalid input returns `null`.

### `public bool CompleteSession(string stateId)`

Removes the session from the store, so its state id cannot be used again. Call it after the token exchange.

Expiry is not checked. An expired session that has not yet been cleaned up is removed, and the method returns `true`. The session's contents are not returned.

**Parameters:**
- `stateId` – the state id to remove.

**Return value:**
- `true` if an entry was removed. An info entry is logged.
- `false` if `stateId` is `null`, whitespace, or unknown.

**Exceptions:** none.

### `public bool UpdateSession(string stateId, string? userId = null, string? grantedScopes = null)`

Records the user and granted scopes on an existing session. It does not change `ExpiresAt`.

- A `userId` or `grantedScopes` that is `null` or whitespace leaves the current value unchanged. This method cannot clear a value.
- `LastUpdatedAt` is set to the current UTC time on every successful call.
- Expiry is not checked. An expired session that has not yet been removed can still be updated.
- No locking is done. Concurrent calls on the same session are last-write-wins for each field.
- Nothing is logged.

**Parameters:**
- `stateId` – the state id to update. Must not be `null` or empty.
- `userId` – ID of the authenticated user. Optional.
- `grantedScopes` – scopes the user granted. Optional.

**Return value:**
- `true` if the session was found and updated.
- `false` if no session has that id.

**Exceptions:**
- `ArgumentNullException` if `stateId` is `null`.
- `ArgumentException` if `stateId` is an empty string. Whitespace-only values are not rejected; they simply return `false`.

### `public int CleanupExpiredSessions()`

Removes every entry whose `ExpiresAt` is at or before the current UTC time. The method takes a snapshot of the expired ids and removes each one, so the count reflects only entries that this call removed. Logs an info entry when at least one entry is removed.

The class does not run cleanup itself, and nothing in `src/` calls this method. Entries that are never looked up stay in memory until something calls it.

**Parameters:** none.

**Return value:** the number of entries removed.

**Exceptions:** none.

### `public int GetActiveSessionCount()`

Returns the number of entries in the store, which is the raw dictionary count.

Despite the name, the result includes expired entries that have not yet been removed by `GetSession`, `CompleteSession`, or `CleanupExpiredSessions`. It is an upper bound on the number of valid sessions. Call `CleanupExpiredSessions` first if an exact count of unexpired sessions is needed.

**Parameters:** none.

**Return value:** the current entry count.

**Exceptions:** none.

## SessionState

`public sealed class SessionState` is the value stored and returned by this service. All properties have public setters. Setting a property on an object returned by `GetSession` changes the stored session directly. That bypasses `UpdateSession` and does not set `LastUpdatedAt`.

| Property | Type | Default | Description |
|---|---|---|---|
| `StateId` | `string` | `string.Empty` | Unique state id |
| `ClientId` | `string` | `string.Empty` | Client that started the flow |
| `RedirectUri` | `string` | `string.Empty` | Redirect URI for the response |
| `RequestedScopes` | `string` | `string.Empty` | Scopes requested by the client |
| `GrantedScopes` | `string?` | `null` | Scopes granted to the client |
| `Nonce` | `string?` | `null` | OpenID Connect nonce, if provided |
| `UserId` | `string?` | `null` | ID of the authenticated user |
| `CreatedAt` | `DateTime` | `default` | Creation time (UTC) |
| `ExpiresAt` | `DateTime` | `default` | Expiry time (UTC) |
| `LastUpdatedAt` | `DateTime?` | `null` | Time of the last `UpdateSession` call |

### `public bool IsValid()`

Returns `true` when `ExpiresAt` is later than the current UTC time. It does not check whether the session is still in the store. See [Session lifetime](#session-lifetime) for how this differs from `GetSession`.

## Example

```csharp
var stateId = sessionState.CreateSession(clientId, redirectUri, "openid profile", nonce);

// The user signs in and consents. The callback then uses the state id.
sessionState.UpdateSession(stateId, userId: user.Id, grantedScopes: "openid profile");

var session = sessionState.GetSession(stateId);
if (session is null)
{
    // Unknown or expired: reject the callback.
    return;
}

// ... exchange the code, issue tokens ...

sessionState.CompleteSession(stateId);
```

## Thread safety

Adding, looking up, and removing entries is safe to call concurrently, because the store is a `ConcurrentDictionary`. `SessionState` objects are not synchronized. Concurrent updates to one session's fields are last-write-wins.
