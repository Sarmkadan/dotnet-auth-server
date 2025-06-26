# IAuthorizationGrantRepository

Defines the contract for a repository that manages OAuth 2.0 authorization grants in a persistent store. Implementations are responsible for storing, retrieving, updating, and deleting authorization grants while ensuring data integrity and consistency.

## API

### `Task<AuthorizationGrant?> GetByIdAsync(Guid id)`

Retrieves an authorization grant by its unique identifier.

- **Parameters**
  - `id` – The unique identifier of the authorization grant to retrieve.
- **Returns**
  - A `Task` resolving to the `AuthorizationGrant` instance if found; otherwise, `null`.
- **Exceptions**
  - Throws if the underlying store is unavailable or if the identifier is malformed.

### `Task<IEnumerable<AuthorizationGrant>> GetAllAsync()`

Retrieves all authorization grants stored in the repository.

- **Returns**
  - A `Task` resolving to an `IEnumerable<AuthorizationGrant>` containing all stored grants.
- **Exceptions**
  - Throws if the underlying store is unavailable.

### `Task<AuthorizationGrant> CreateAsync(AuthorizationGrant grant)`

Creates a new authorization grant in the repository.

- **Parameters**
  - `grant` – The authorization grant to persist.
- **Returns**
  - A `Task` resolving to the newly created `AuthorizationGrant` instance.
- **Exceptions**
  - Throws if the grant already exists, if the grant is invalid, or if the underlying store fails.

### `Task<AuthorizationGrant> UpdateAsync(AuthorizationGrant grant)`

Updates an existing authorization grant in the repository.

- **Parameters**
  - `grant` – The authorization grant containing updated values.
- **Returns**
  - A `Task` resolving to the updated `AuthorizationGrant` instance.
- **Exceptions**
  - Throws if the grant does not exist, if the update violates constraints, or if the underlying store fails.

### `Task DeleteAsync(AuthorizationGrant grant)`

Deletes an authorization grant from the repository.

- **Parameters**
  - `grant` – The authorization grant to delete.
- **Exceptions**
  - Throws if the grant does not exist or if the underlying store fails.

### `Task DeleteByIdAsync(Guid id)`

Deletes an authorization grant by its unique identifier.

- **Parameters**
  - `id` – The unique identifier of the authorization grant to delete.
- **Exceptions**
  - Throws if the identifier is malformed or if the underlying store fails.

### `Task<bool> ExistsAsync(Guid id)`

Determines whether an authorization grant with the specified identifier exists.

- **Parameters**
  - `id` – The unique identifier to check.
- **Returns**
  - A `Task<bool>` resolving to `true` if the grant exists; otherwise, `false`.
- **Exceptions**
  - Throws if the identifier is malformed or if the underlying store fails.

### `Task<AuthorizationGrant?> GetByCodeAsync(string code)`

Retrieves an authorization grant by its authorization code.

- **Parameters**
  - `code` – The authorization code to search for.
- **Returns**
  - A `Task` resolving to the `AuthorizationGrant` instance if found; otherwise, `null`.
- **Exceptions**
  - Throws if the code is malformed or if the underlying store fails.

### `Task<IEnumerable<AuthorizationGrant>> GetByUserIdAsync(string userId)`

Retrieves all authorization grants associated with a specific user identifier.

- **Parameters**
  - `userId` – The user identifier to filter grants by.
- **Returns**
  - A `Task` resolving to an `IEnumerable<AuthorizationGrant>` containing matching grants.
- **Exceptions**
  - Throws if the user identifier is malformed or if the underlying store fails.

### `Task<IEnumerable<AuthorizationGrant>> GetByClientIdAsync(string clientId)`

Retrieves all authorization grants associated with a specific client identifier.

- **Parameters**
  - `clientId` – The client identifier to filter grants by.
- **Returns**
  - A `Task` resolving to an `IEnumerable<AuthorizationGrant>` containing matching grants.
- **Exceptions**
  - Throws if the client identifier is malformed or if the underlying store fails.

### `Task DeleteExpiredAsync()`

Deletes all authorization grants that have expired based on their expiration time.

- **Returns**
  - A `Task` that completes when the operation finishes.
- **Exceptions**
  - Throws if the underlying store fails.

## Usage

### Example 1: Creating and retrieving an authorization grant
