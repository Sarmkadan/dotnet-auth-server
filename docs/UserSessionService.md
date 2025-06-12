# UserSessionService

The `UserSessionService` is a service responsible for managing user sessions within the `dotnet-auth-server` project. It provides functionality to create, retrieve, revoke, and monitor sessions, including tracking active, revoked, and expired sessions. The service also offers statistical insights into session activity and performs cleanup of expired sessions.

## API

### `UserSessionService`
The primary service class for session management.

---

### `Task<UserSession> CreateSessionAsync`
Creates a new user session.

**Returns:**
- A `Task<UserSession>` representing the newly created session.

**Throws:**
- `InvalidOperationException`: If session creation fails due to underlying storage or validation issues.

---

### `Task<IEnumerable<UserSession>> GetActiveSessionsAsync`
Retrieves all active sessions (sessions that are neither revoked nor expired).

**Returns:**
- A `Task<IEnumerable<UserSession>>` containing the active sessions.

---

### `Task<IEnumerable<UserSession>> GetAllSessionsAsync`
Retrieves all sessions, including active, revoked, and expired sessions.

**Returns:**
- A `Task<IEnumerable<UserSession>>` containing all sessions.

---

### `Task<IEnumerable<UserSession>> GetAllActiveSessionsAsync`
Alias for `GetActiveSessionsAsync`. Retrieves all active sessions.

**Returns:**
- A `Task<IEnumerable<UserSession>>` containing the active sessions.

---

### `Task RevokeSessionAsync`
Revokes a specific session, marking it as inactive.

**Parameters:**
- `sessionId` (`string`): The unique identifier of the session to revoke.

**Throws:**
- `KeyNotFoundException`: If the session does not exist.
- `InvalidOperationException`: If the session cannot be revoked (e.g., already revoked or expired).

---

### `Task<int> RevokeAllUserSessionsAsync`
Revokes all sessions associated with a specific user.

**Parameters:**
- `userId` (`string`): The unique identifier of the user whose sessions should be revoked.

**Returns:**
- A `Task<int>` representing the number of sessions revoked.

**Throws:**
- `InvalidOperationException`: If no sessions exist for the user or revocation fails.

---

### `Task TouchSessionAsync`
Updates the last activity timestamp of a session to mark it as recently active.

**Parameters:**
- `sessionId` (`string`): The unique identifier of the session to update.

**Throws:**
- `KeyNotFoundException`: If the session does not exist.
- `InvalidOperationException`: If the session is revoked or expired.

---

### `Task<SessionStats> GetStatsAsync`
Retrieves statistical data about sessions, including counts of total, active, revoked, and expired sessions, as well as unique users.

**Returns:**
- A `Task<SessionStats>` containing the computed statistics.

---

### `Task<int> CleanupExpiredSessionsAsync`
Removes all expired sessions from the system.

**Returns:**
- A `Task<int>` representing the number of sessions cleaned up.

---

### `int TotalSessions`
Gets the total number of sessions (active, revoked, and expired) currently tracked by the service.

**Returns:**
- An `int` representing the total session count.

---

### `int ActiveSessions`
Gets the number of active sessions (not revoked or expired).

**Returns:**
- An `int` representing the count of active sessions.

---

### `int RevokedSessions`
Gets the number of revoked sessions.

**Returns:**
- An `int` representing the count of revoked sessions.

---

### `int ExpiredSessions`
Gets the number of expired sessions.

**Returns:**
- An `int` representing the count of expired sessions.

---

### `int UniqueUsers`
Gets the number of unique users with at least one session.

**Returns:**
- An `int` representing the count of unique users.

---

### `DateTime ComputedAt`
Gets the timestamp when the statistical properties (`TotalSessions`, `ActiveSessions`, etc.) were last computed.

**Returns:**
- A `DateTime` representing the computation timestamp.

## Usage

### Example 1: Creating and Managing a User Session
