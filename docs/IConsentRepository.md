# IConsentRepository

Defines the contract for a repository that manages user consent records in the authentication server, enabling storage, retrieval, updating, and revocation of consent grants between users and OAuth clients.

## API

### `Task<Consent?> GetByIdAsync(Guid id)`
Retrieves a single consent record by its unique identifier.
- **Parameters**: `id` – The unique identifier of the consent record.
- **Returns**: The `Consent` instance if found; otherwise, `null`.
- **Throws**: `ArgumentNullException` if `id` is `null`.

### `Task<IEnumerable<Consent>> GetAllAsync()`
Retrieves all consent records stored in the repository.
- **Returns**: An enumerable of all `Consent` records.
- **Throws**: No documented exceptions.

### `Task<Consent> CreateAsync(Consent consent)`
Creates a new consent record in the repository.
- **Parameters**: `consent` – The consent record to be created.
- **Returns**: The created `Consent` instance, including any assigned identifiers.
- **Throws**: `ArgumentNullException` if `consent` is `null`.

### `Task<Consent> UpdateAsync(Consent consent)`
Updates an existing consent record in the repository.
- **Parameters**: `consent` – The consent record containing updated values.
- **Returns**: The updated `Consent` instance.
- **Throws**: `ArgumentNullException` if `consent` is `null`.

### `Task DeleteAsync(Consent consent)`
Deletes an existing consent record from the repository.
- **Parameters**: `consent` – The consent record to be deleted.
- **Throws**: `ArgumentNullException` if `consent` is `null`.

### `Task DeleteByIdAsync(Guid id)`
Deletes a consent record by its unique identifier.
- **Parameters**: `id` – The unique identifier of the consent record.
- **Throws**: `ArgumentNullException` if `id` is `null`.

### `Task<bool> ExistsAsync(Guid id)`
Checks whether a consent record with the specified identifier exists.
- **Parameters**: `id` – The unique identifier of the consent record.
- **Returns**: `true` if the record exists; otherwise, `false`.
- **Throws**: `ArgumentNullException` if `id` is `null`.

### `Task<Consent?> GetByUserAndClientAsync(string userId, string clientId)`
Retrieves a consent record for a specific user and client.
- **Parameters**:
  - `userId` – The identifier of the user.
  - `clientId` – The identifier of the OAuth client.
- **Returns**: The `Consent` instance if found; otherwise, `null`.
- **Throws**: `ArgumentNullException` if `userId` or `clientId` is `null`.

### `Task<IEnumerable<Consent>> GetByUserIdAsync(string userId)`
Retrieves all consent records associated with a specific user.
- **Parameters**: `userId` – The identifier of the user.
- **Returns**: An enumerable of `Consent` records for the user.
- **Throws**: `ArgumentNullException` if `userId` is `null`.

### `Task<IEnumerable<Consent>> GetByClientIdAsync(string clientId)`
Retrieves all consent records associated with a specific OAuth client.
- **Parameters**: `clientId` – The identifier of the OAuth client.
- **Returns**: An enumerable of `Consent` records for the client.
- **Throws**: `ArgumentNullException` if `clientId` is `null`.

### `Task RevokeAllUserConsentsAsync(string userId)`
Revokes all consent records associated with a specific user.
- **Parameters**: `userId` – The identifier of the user whose consents are to be revoked.
- **Throws**: `ArgumentNullException` if `userId` is `null`.

### `ConsentService`
A service component responsible for managing consent lifecycle operations. This member is likely a factory or dependency injection registration point rather than a runtime method.

### `Task<bool> HasConsentAsync(string userId, string clientId)`
Checks whether a consent record exists for a specific user and client.
- **Parameters**:
  - `userId` – The identifier of the user.
  - `clientId` – The identifier of the OAuth client.
- **Returns**: `true` if a consent record exists; otherwise, `false`.
- **Throws**: `ArgumentNullException` if `userId` or `clientId` is `null`.

### `Task<Consent> RecordConsentAsync(string userId, string clientId, IEnumerable<string> scopes)`
Records a new consent grant for a user and client with the specified scopes.
- **Parameters**:
  - `userId` – The identifier of the user.
  - `clientId` – The identifier of the OAuth client.
  - `scopes` – The set of scopes being consented to.
- **Returns**: The newly created `Consent` record.
- **Throws**: `ArgumentNullException` if `userId`, `clientId`, or `scopes` is `null`.

### `Task<IEnumerable<string>> GetEffectiveScopesAsync(string userId, string clientId)`
Retrieves the effective set of scopes consented to by a user for a specific client.
- **Parameters**:
  - `userId` – The identifier of the user.
  - `clientId` – The identifier of the OAuth client.
- **Returns**: An enumerable of scope strings representing the effective consent.
- **Throws**: `ArgumentNullException` if `userId` or `clientId` is `null`.

### `Task RevokeConsentAsync(string userId, string clientId)`
Revokes the consent record for a specific user and client.
- **Parameters**:
  - `userId` – The identifier of the user.
  - `clientId` – The identifier of the OAuth client.
- **Throws**: `ArgumentNullException` if `userId` or `clientId` is `null`.

## Usage
