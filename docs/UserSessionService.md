# UserSessionService

`UserSessionService` manages the lifecycle of authenticated user sessions. A session is created when tokens are issued, can be touched on activity, and can be revoked one at a time or in bulk, for example on a password change or account deletion. It also regenerates sessions after privilege changes and reports session statistics.

The class is `public sealed` and lives in `src/Services/UserSessionService.cs`, namespace `DotnetAuthServer.Services`. It has no interface. Register and inject the concrete type, as with the other services. The file also defines the public types `UserSessionService.UserSessionInfo` (nested) and `SessionStats` (top-level).

## Role in the system

- Persistence goes through `IUserSessionRepository`.
- New sessions expire `RefreshTokenLifetimeSeconds` after creation (from `AuthServerOptions`). The session's `LastActivityAt` starts at creation time.
- Unexpected failures are logged and rethrown as `AuthServerException` with status `500` and code `server_error`. Argument validation errors are thrown as `ArgumentException` before the `try` block and are not wrapped. `OperationCanceledException` is never wrapped.
- Revocation logs use `none` when no reason is given.

## Constructor

### `public UserSessionService(IUserSessionRepository sessionRepository, ILogger<UserSessionService> logger, AuthServerOptions options)`

Creates the service from its collaborators.

- **Parameters**:
  - `sessionRepository`: Persistence for `UserSession` entities.
  - `logger`: Logger for session events.
  - `options`: Server options. `options.RefreshTokenLifetimeSeconds` sets the lifetime of new sessions.
- **Throws**: `ArgumentNullException` if any argument is `null`.

## Methods

### `public async Task<UserSession> CreateSessionAsync(string userId, string clientId, string grantedScopes, string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default)`

Creates and persists a new session after a successful token grant.

- **Parameters**:
  - `userId`: The authenticated user's ID.
  - `clientId`: The OAuth2 client that obtained the tokens.
  - `grantedScopes`: Space-delimited scopes granted in this session.
  - `ipAddress`: Client IP address. Optional.
  - `userAgent`: Client user-agent string. Optional.
  - `cancellationToken`: Cancels the call.
- **Return value**: The new `UserSession`.
- **Behavior**: Always creates a new session. Existing sessions are not touched. Use `RegenerateSessionAsync` to replace them.
- **Throws**:
  - `ArgumentException` if `userId`, `clientId`, or `grantedScopes` is `null`, empty, or whitespace.
  - `AuthServerException` with status `500` if the repository fails.

### `public async Task<IEnumerable<UserSession>> GetActiveSessionsAsync(string userId, CancellationToken cancellationToken = default)`

Returns the active sessions for a user.

- **Parameters**:
  - `userId`: The user's ID.
  - `cancellationToken`: Cancels the call.
- **Return value**: The sessions returned by `GetActiveByUserIdAsync`, unchanged.
- **Throws**:
  - `ArgumentException` if `userId` is `null`, empty, or whitespace.
  - `AuthServerException` with status `500` if the repository fails.

### `public async Task<IEnumerable<UserSession>> GetAllSessionsAsync(string userId, CancellationToken cancellationToken = default)`

Returns every session for a user, including revoked and expired ones.

- **Parameters**:
  - `userId`: The user's ID.
  - `cancellationToken`: Cancels the call.
- **Return value**: The sessions returned by `GetByUserIdAsync`, unchanged.
- **Throws**: Same as `GetActiveSessionsAsync`.

### `public async Task<IEnumerable<UserSession>> GetAllActiveSessionsAsync(CancellationToken cancellationToken = default)`

Returns the currently active sessions across all users. This is meant for admin dashboards. Restrict access to it at the caller.

- **Parameters**:
  - `cancellationToken`: Cancels the call.
- **Return value**: The sessions returned by `GetAllActiveAsync`, unchanged.
- **Throws**: `AuthServerException` with status `500` if the repository fails. No argument validation.

### `public async Task<IEnumerable<UserSessionInfo>> GetActiveSessionsWithDetailsAsync(string userId, CancellationToken cancellationToken = default)`

Returns the active sessions for a user as `UserSessionInfo` projections, for display in a session list.

- **Parameters**:
  - `userId`: The user's ID.
  - `cancellationToken`: Cancels the call.
- **Return value**: One `UserSessionInfo` per active session. The projection uses `Select`, so mapping runs when the sequence is enumerated. Exceptions during enumeration are not wrapped by this method.
- **Throws**: `ArgumentException` if `userId` is `null`, empty, or whitespace. `AuthServerException` with status `500` if the repository call fails.

### `public async Task<int> RevokeAllOtherUserSessionsAsync(string userId, string keepSessionId, string? reason = null, CancellationToken cancellationToken = default)`

Revokes every session for a user except one.

- **Parameters**:
  - `userId`: The user whose sessions to revoke.
  - `keepSessionId`: The session to keep active.
  - `reason`: Why the sessions are revoked. Optional.
  - `cancellationToken`: Cancels the call.
- **Return value**: The number of sessions revoked. This is `0` if there were no other sessions.
- **Throws**:
  - `ArgumentException` if `userId` or `keepSessionId` is `null`, empty, or whitespace.
  - `AuthServerException` with status `500` if the repository fails.

### `public async Task RevokeSessionAsync(string sessionId, string? reason = null, CancellationToken cancellationToken = default)`

Revokes a single session.

- **Parameters**:
  - `sessionId`: The session to revoke.
  - `reason`: Why the session is revoked. Optional.
  - `cancellationToken`: Cancels the call.
- **Behavior**: Loads the session, calls `Revoke(reason)` on it, and saves it. An already revoked session is revoked again. Its reason is overwritten.
- **Throws**:
  - `ArgumentException` if `sessionId` is `null`, empty, or whitespace.
  - `AuthServerException` with status `404` (`invalid_request`) if no session has that ID.
  - `AuthServerException` with status `500` if the repository fails.

### `public async Task<int> RevokeAllUserSessionsAsync(string userId, string? reason = null, CancellationToken cancellationToken = default)`

Revokes all active sessions for a user.

- **Parameters**:
  - `userId`: The user whose sessions to revoke.
  - `reason`: Why the sessions are revoked. Optional.
  - `cancellationToken`: Cancels the call.
- **Return value**: The number of sessions revoked.
- **Throws**: `ArgumentException` if `userId` is `null`, empty, or whitespace. `AuthServerException` with status `500` if the repository fails.

### `public async Task<UserSession> RegenerateSessionAsync(string userId, string clientId, string grantedScopes, string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default)`

Replaces all of a user's sessions with a new one. Call it after a privilege change, such as a login or a passed MFA challenge, so that the old session ID stops being valid.

- **Parameters**: Same as `CreateSessionAsync`.
- **Return value**: The new `UserSession`, with a fresh session ID.
- **Behavior**:
  1. Revokes all of the user's sessions with the reason `Session regenerated due to privilege change`.
  2. Creates the new session.

  The two steps are not wrapped in a transaction. If step 2 fails, the old sessions stay revoked and no new session exists.
- **Throws**:
  - `ArgumentException` if `userId`, `clientId`, or `grantedScopes` is `null`, empty, or whitespace.
  - `AuthServerException` with status `500` if either repository call fails.

### `public async Task TouchSessionAsync(string sessionId, CancellationToken cancellationToken = default)`

Records activity on a session by updating its last-activity time.

- **Parameters**:
  - `sessionId`: The session to touch.
  - `cancellationToken`: Cancels the call.
- **Behavior**: If the session does not exist or is not active (revoked or expired), the method returns without error and without saving anything. Otherwise it calls `Touch()` and saves the session.
- **Throws**:
  - `ArgumentException` if `sessionId` is `null`, empty, or whitespace.
  - `AuthServerException` with status `500` if the repository fails.

### `public async Task<SessionStats> GetStatsAsync(CancellationToken cancellationToken = default)`

Computes session counts across all users.

- **Parameters**:
  - `cancellationToken`: Cancels the call.
- **Return value**: A `SessionStats` value. `ActiveSessions` uses `IsActive()`. `ExpiredSessions` counts sessions that are not revoked and whose `ExpiresAt` is in the past. `UniqueUsers` compares user IDs case-insensitively.
- **Behavior**: Loads every session into memory with `GetAllAsync` and counts them there. Expect this to be expensive with a large store.
- **Throws**: `AuthServerException` with status `500` if the repository fails.

### `public async Task<int> CleanupExpiredSessionsAsync(CancellationToken cancellationToken = default)`

Deletes sessions that have passed their expiry time.

- **Parameters**:
  - `cancellationToken`: Cancels the call.
- **Return value**: The number of sessions removed, as reported by `DeleteExpiredAsync`.
- **Throws**: `AuthServerException` with status `500` if the repository fails.

### `public override string ToString()`

Returns a fixed text representation of the service. It does not include any runtime state.

## Nested and related types

### `UserSessionService.UserSessionInfo`

A `public sealed` DTO returned by `GetActiveSessionsWithDetailsAsync`. All properties have settable public accessors.

| Property | Type | Notes |
|---|---|---|
| `SessionId` | `string` | Defaults to `string.Empty`. |
| `UserId` | `string` | Defaults to `string.Empty`. |
| `ClientId` | `string` | Defaults to `string.Empty`. |
| `IpAddress` | `string?` | Optional. |
| `UserAgent` | `string?` | Optional. |
| `CreatedAt` | `DateTime` | UTC. |
| `LastActivityAt` | `DateTime?` | UTC. |
| `ExpiresAt` | `DateTime` | UTC. |
| `IsRevoked` | `bool` | Whether the session was explicitly revoked. |

`ToString()` returns a text summary of the first six fields, without `ExpiresAt`, `LastActivityAt`, or `IsRevoked`.

### `SessionStats`

A `public sealed` DTO returned by `GetStatsAsync`.

| Property | Type | Meaning |
|---|---|---|
| `TotalSessions` | `int` | All session records, in any state. |
| `ActiveSessions` | `int` | Neither revoked nor expired. |
| `RevokedSessions` | `int` | Explicitly revoked. |
| `ExpiredSessions` | `int` | Expired without being revoked. |
| `UniqueUsers` | `int` | Distinct user IDs with any session. |
| `ComputedAt` | `DateTime` | UTC time when the statistics were computed. Set by the initializer. |

## Errors

| Situation | Exception | Status | Code |
|---|---|---|---|
| Required string argument is `null`, empty, or whitespace | `ArgumentException` | n/a | n/a |
| `RevokeSessionAsync` target not found | `AuthServerException` | `404` | `invalid_request` |
| Repository or unexpected failure | `AuthServerException` | `500` | `server_error` |
| Cancellation | `OperationCanceledException` | n/a | n/a |

## Related

- `IUserSessionRepository`: the persistence contract.
- `UserSession`: the session entity, with `Revoke`, `Touch`, and `IsActive`.
- `UserManagementService`: revokes sessions when a user is deleted.
- `AuthServerException`: the error type used for all wrapped failures.
