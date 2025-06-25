# IRefreshTokenRepository

Defines the contract for operations that manage refresh tokens in the authentication system, including creation, retrieval, updating, and deletion of tokens associated with users and clients.

## API

### `Task<RefreshToken?> GetByIdAsync(Guid id)`
Retrieves a refresh token by its unique identifier. Returns `null` if no token with the given ID exists.

- **Parameters**: `id` – The unique identifier of the refresh token.
- **Return value**: A `RefreshToken` instance if found; otherwise, `null`.
- **Exceptions**: Throws if the underlying storage operation fails.

### `Task<IEnumerable<RefreshToken>> GetAllAsync()`
Retrieves all refresh tokens stored in the system.

- **Return value**: An enumerable collection of all `RefreshToken` instances.
- **Exceptions**: Throws if the underlying storage operation fails.

### `Task<RefreshToken> CreateAsync(RefreshToken token)`
Creates a new refresh token in the system.

- **Parameters**: `token` – The refresh token to create.
- **Return value**: The created `RefreshToken` instance, typically with updated metadata (e.g., ID, timestamps).
- **Exceptions**: Throws if the token already exists or if the storage operation fails.

### `Task<RefreshToken> UpdateAsync(RefreshToken token)`
Updates an existing refresh token in the system.

- **Parameters**: `token` – The refresh token with updated values.
- **Return value**: The updated `RefreshToken` instance.
- **Exceptions**: Throws if the token does not exist or if the storage operation fails.

### `Task DeleteAsync(RefreshToken token)`
Deletes the specified refresh token from the system.

- **Parameters**: `token` – The refresh token to delete.
- **Exceptions**: Throws if the token does not exist or if the storage operation fails.

### `Task DeleteByIdAsync(Guid id)`
Deletes a refresh token by its unique identifier.

- **Parameters**: `id` – The unique identifier of the refresh token to delete.
- **Exceptions**: Throws if the token does not exist or if the storage operation fails.

### `Task<bool> ExistsAsync(Guid id)`
Checks whether a refresh token with the given identifier exists.

- **Parameters**: `id` – The unique identifier of the refresh token.
- **Return value**: `true` if the token exists; otherwise, `false`.
- **Exceptions**: Throws if the underlying storage operation fails.

### `Task<RefreshToken?> GetByTokenHashAsync(string tokenHash)`
Retrieves a refresh token by its cryptographic hash.

- **Parameters**: `tokenHash` – The hash of the refresh token to retrieve.
- **Return value**: A `RefreshToken` instance if found; otherwise, `null`.
- **Exceptions**: Throws if the hash is invalid or if the storage operation fails.

### `Task<IEnumerable<RefreshToken>> GetByUserIdAsync(Guid userId)`
Retrieves all refresh tokens associated with a specific user.

- **Parameters**: `userId` – The unique identifier of the user.
- **Return value**: An enumerable collection of `RefreshToken` instances associated with the user.
- **Exceptions**: Throws if the underlying storage operation fails.

### `Task<IEnumerable<RefreshToken>> GetByClientIdAsync(Guid clientId)`
Retrieves all refresh tokens associated with a specific client.

- **Parameters**: `clientId` – The unique identifier of the client.
- **Return value**: An enumerable collection of `RefreshToken` instances associated with the client.
- **Exceptions**: Throws if the underlying storage operation fails.

### `Task<IEnumerable<RefreshToken>> GetValidTokensByUserAsync(Guid userId)`
Retrieves all non-expired refresh tokens associated with a specific user.

- **Parameters**: `userId` – The unique identifier of the user.
- **Return value**: An enumerable collection of valid `RefreshToken` instances associated with the user.
- **Exceptions**: Throws if the underlying storage operation fails.

### `Task RevokeAllUserTokensAsync(Guid userId)`
Revokes all refresh tokens associated with a specific user by marking them as inactive or removing them.

- **Parameters**: `userId` – The unique identifier of the user.
- **Exceptions**: Throws if the underlying storage operation fails.

### `Task DeleteExpiredAsync()`
Deletes all refresh tokens that have expired based on their expiration timestamp.

- **Exceptions**: Throws if the underlying storage operation fails.

## Usage

### Creating and validating a refresh token
