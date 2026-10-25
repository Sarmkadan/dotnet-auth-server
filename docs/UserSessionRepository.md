# UserSessionRepository

In-memory implementation of `IUserSessionRepository` (which extends `IRepository<UserSession, string>`). Sessions are stored in a `ConcurrentDictionary<string, UserSession>` keyed by `SessionId` with `StringComparer.OrdinalIgnoreCase`, so session IDs are matched case-insensitively. Data lives for the lifetime of the process; replace it with a persistent store for production use.

The class is `public sealed` and is registered as a singleton (see `CLAUDE.md` DI conventions). Methods return the stored `UserSession` instances themselves, not copies, so mutating a returned session (for example via `Revoke`) changes what the repository holds.

## Behavior notes

- **Argument validation** is inconsistent across methods. Methods that take a session ID or user ID as a key (`GetByIdAsync`, `DeleteByIdAsync`, `ExistsAsync`, `GetByUserIdAsync`) throw `ArgumentException` for a null or empty value. `GetActiveByUserIdAsync`, `RevokeAllUserSessionsAsync`, and `RevokeAllOtherUserSessionsAsync` do not validate their user ID, and a null or empty value simply matches nothing.
- **`CancellationToken`** parameters are accepted for interface compatibility but are not observed. Operations complete synchronously and return already-completed tasks.
- **Consistency**: the dictionary is thread-safe per operation, but multi-step operations (revoke-all, delete-expired) read a snapshot (`ToList()`) and then act on it. Concurrent changes between the snapshot and the action are not coordinated.
- **Revoked vs. expired**: a session is "active" when `IsActive()` returns true, i.e. it is not revoked and `ExpiresAt` is in the future. `GetByUserIdAsync` and `GetAllAsync` return revoked and expired sessions as well. `DeleteExpiredAsync` removes only sessions whose `ExpiresAt` is at or before the current UTC time; revoked sessions that have not yet expired are kept.

## API

### `Task<UserSession?> GetByIdAsync(string id, CancellationToken cancellationToken = default)`
Returns the session with the given `SessionId`, or `null` if none exists. Throws `ArgumentException` if `id` is null or empty.

### `Task<IEnumerable<UserSession>> GetAllAsync(CancellationToken cancellationToken = default)`
Returns a snapshot list of every stored session, including revoked and expired ones. The result is never `null` and may be empty.

### `Task<UserSession> CreateAsync(UserSession entity, CancellationToken cancellationToken = default)`
Adds `entity` keyed by its `SessionId` and returns the same instance. Throws `ArgumentNullException` if `entity` is null. Throws `InvalidOperationException` if a session with the same `SessionId` (case-insensitive) already exists.

### `Task<UserSession> UpdateAsync(UserSession entity, CancellationToken cancellationToken = default)`
Replaces the stored session that has the same `SessionId` with `entity` and returns `entity`. Throws `InvalidOperationException` if no such session exists. Passing a null `entity` is not checked and fails with a `NullReferenceException`.

### `Task DeleteAsync(UserSession entity, CancellationToken cancellationToken = default)`
Removes the stored session whose `SessionId` matches `entity.SessionId`. Throws `ArgumentNullException` if `entity` is null. Throws `InvalidOperationException` if no such session exists.

### `Task DeleteByIdAsync(string id, CancellationToken cancellationToken = default)`
Removes the session with the given `SessionId`. Throws `ArgumentException` if `id` is null or empty. Does nothing if the session does not exist.

### `Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)`
Returns `true` if a session with the given `SessionId` is stored, regardless of its revocation or expiry state. Throws `ArgumentException` if `id` is null or empty.

### `Task<IEnumerable<UserSession>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)`
Returns every session whose `UserId` matches `userId` (case-insensitive), including revoked and expired sessions. Throws `ArgumentException` if `userId` is null or empty.

### `Task<IEnumerable<UserSession>> GetActiveByUserIdAsync(string userId, CancellationToken cancellationToken = default)`
Returns the sessions for `userId` (case-insensitive) for which `IsActive()` is true. No argument validation is performed; a null or empty `userId` yields an empty result.

### `Task<IEnumerable<UserSession>> GetAllActiveAsync(CancellationToken cancellationToken = default)`
Returns a snapshot of all sessions for which `IsActive()` is true, across all users. The result may be empty.

### `Task<int> RevokeAllUserSessionsAsync(string userId, string? reason = null, CancellationToken cancellationToken = default)`
Calls `Revoke(reason)` on every active session belonging to `userId` (case-insensitive) and returns how many sessions were revoked. Sessions that are already revoked or expired are not counted or changed. Does not throw for a null or empty `userId`; it returns `0` because no session matches.

### `Task<int> RevokeAllOtherUserSessionsAsync(string userId, string keepSessionId, string? reason = null, CancellationToken cancellationToken = default)`
Same as `RevokeAllUserSessionsAsync`, but skips the session whose `SessionId` equals `keepSessionId` (case-insensitive). Typically used to sign out every other device while keeping the current session. No validation is performed on `keepSessionId`; a null value excludes nothing.

### `Task<int> DeleteExpiredAsync(CancellationToken cancellationToken = default)`
Removes every session whose `ExpiresAt` is less than or equal to `DateTime.UtcNow` and returns the number removed. Revoked sessions that have not yet expired are retained.
