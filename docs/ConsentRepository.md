# ConsentRepository

Centralizes storage and retrieval of user consent records for OAuth clients. Handles creation, updates, revocations, and queries scoped by user, client, or consent identifier.

## API

### `Task<Consent?> GetByIdAsync(Guid id)`
Fetches a single consent record by its unique identifier.
- **Parameters**: `id` – the consent record identifier.
- **Returns**: The matching `Consent` instance or `null` if not found.
- **Exceptions**: Throws if the underlying store fails.

### `Task<IEnumerable<Consent>> GetAllAsync()`
Retrieves all consent records stored in the repository.
- **Returns**: An enumerable of all `Consent` instances.
- **Exceptions**: Throws if the underlying store fails.

### `Task<Consent> CreateAsync(Consent consent)`
Persists a new consent record.
- **Parameters**: `consent` – the consent to create; must not be `null`.
- **Returns**: The persisted `Consent` instance, including any generated identifiers.
- **Exceptions**: Throws if the record already exists or the store fails.

### `Task<Consent> UpdateAsync(Consent consent)`
Updates an existing consent record.
- **Parameters**: `consent` – the updated consent; must not be `null` and must exist.
- **Returns**: The updated `Consent` instance.
- **Exceptions**: Throws if the record does not exist or the store fails.

### `Task DeleteAsync(Consent consent)`
Removes a consent record from the store.
- **Parameters**: `consent` – the consent to delete; must not be `null`.
- **Exceptions**: Throws if the record does not exist or the store fails.

### `Task DeleteByIdAsync(Guid id)`
Removes a consent record by its unique identifier.
- **Parameters**: `id` – the consent record identifier.
- **Exceptions**: Throws if the record does not exist or the store fails.

### `Task<bool> ExistsAsync(Guid id)`
Checks whether a consent record with the given identifier exists.
- **Parameters**: `id` – the consent record identifier.
- **Returns**: `true` if the record exists; otherwise `false`.
- **Exceptions**: Throws if the store fails.

### `Task<IEnumerable<Consent>> GetByUserIdAsync(string userId)`
Retrieves all consent records associated with a specific user.
- **Parameters**: `userId` – the user identifier; must not be `null`.
- **Returns**: An enumerable of matching `Consent` instances.
- **Exceptions**: Throws if the store fails.

### `Task<Consent?> GetByUserAndClientAsync(string userId, string clientId)`
Fetches the consent record for a specific user and client combination.
- **Parameters**:
  - `userId` – the user identifier; must not be `null`.
  - `clientId` – the OAuth client identifier; must not be `null`.
- **Returns**: The matching `Consent` instance or `null` if not found.
- **Exceptions**: Throws if the store fails.

### `Task<IEnumerable<Consent>> GetByClientIdAsync(string clientId)`
Retrieves all consent records associated with a specific OAuth client.
- **Parameters**: `clientId` – the OAuth client identifier; must not be `null`.
- **Returns**: An enumerable of matching `Consent` instances.
- **Exceptions**: Throws if the store fails.

### `Task<int> RevokeUserConsentsAsync(string userId)`
Revokes all consents for a given user by marking them as inactive.
- **Parameters**: `userId` – the user identifier; must not be `null`.
- **Returns**: The count of consents revoked.
- **Exceptions**: Throws if the store fails.

### `Task<bool> RevokeConsentAsync(Guid id)`
Revokes a single consent record by marking it as inactive.
- **Parameters**: `id` – the consent record identifier.
- **Returns**: `true` if the record existed and was revoked; otherwise `false`.
- **Exceptions**: Throws if the store fails.

## Usage
