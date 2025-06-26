# IUserSessionRepository

Centralizes data access operations for user session entities, providing methods to create, read, update, delete, and query sessions with user-specific and expiration-based filtering.

## API

### `Task<UserSession?> GetByIdAsync(Guid id)`
Retrieves a single session by its unique identifier. Returns `null` if no session exists for the given `id`. Throws `ArgumentException` if `id` is empty.

### `Task<IEnumerable<UserSession>> GetAllAsync()`
Returns all sessions currently stored in the repository. The result may be empty but never `null`. No exceptions are thrown under normal operation.

### `Task<UserSession> CreateAsync(UserSession session)`
Persists a new session entity. Returns the created session with its assigned identifier populated. Throws `ArgumentNullException` if `session` is `null` or if required fields are invalid.

### `Task<UserSession> UpdateAsync(UserSession session)`
Overwrites an existing session with the provided entity. Returns the updated session. Throws `ArgumentNullException` if `session` is `null` or if the session identifier is missing or invalid. Throws `KeyNotFoundException` if no session exists for the given identifier.

### `Task DeleteAsync(UserSession session)`
Removes the specified session from storage. Throws `ArgumentNullException` if `session` is `null`. No exception is raised if the session does not exist.

### `Task DeleteByIdAsync(Guid id)`
Removes the session identified by `id`. Throws `ArgumentException` if `id` is empty. No exception is raised if no session exists for the given `id`.

### `Task<bool> ExistsAsync(Guid id)`
Checks whether a session with the specified `id` exists. Returns `true` if found, `false` otherwise. Throws `ArgumentException` if `id` is empty.

### `Task<IEnumerable<UserSession>> GetByUserIdAsync(Guid userId)`
Returns all sessions associated with the given `userId`. The result may be empty but never `null`. Throws `ArgumentException` if `userId` is empty.

### `Task<IEnumerable<UserSession>> GetActiveByUserIdAsync(Guid userId)`
Returns all sessions for the specified `userId` that have not yet expired. The result may be empty but never `null`. Throws `ArgumentException` if `userId` is empty.

### `Task<IEnumerable<UserSession>> GetAllActiveAsync()`
Returns all sessions in the repository that have not yet expired. The result may be empty but never `null`. No exceptions are thrown under normal operation.

### `Task<int> RevokeAllUserSessionsAsync(Guid userId)`
Invalidates all sessions for the given `userId` by marking them as revoked. Returns the count of sessions affected. Throws `ArgumentException` if `userId` is empty.

### `Task<int> DeleteExpiredAsync()`
Removes all sessions that have passed their expiration time. Returns the count of sessions deleted. No exceptions are thrown under normal operation.

## Usage
