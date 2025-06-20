# ITotpCredentialRepository

Provides asynchronous operations for managing TOTP (Time-based One-Time Password) credentials in a persistent store. This interface abstracts the persistence layer for TOTP secrets, allowing integration with various storage backends such as databases, caches, or distributed stores.

## API

### `Task<TotpCredential?> GetByIdAsync(Guid id)`

Retrieves a TOTP credential by its unique identifier.

- **Parameters**:
  - `id` – The unique identifier of the TOTP credential to retrieve.
- **Returns**:
  - A `Task` resolving to the `TotpCredential` if found, or `null` if not found.
- **Exceptions**:
  - Throws if the underlying storage operation fails (e.g., network error, database down).

---

### `Task<IEnumerable<TotpCredential>> GetAllAsync()`

Retrieves all TOTP credentials stored in the repository.

- **Returns**:
  - A `Task` resolving to an `IEnumerable<TotpCredential>` containing all stored credentials.
- **Exceptions**:
  - Throws if the operation cannot be completed (e.g., access denied, storage failure).

---

### `Task<TotpCredential> CreateAsync(TotpCredential credential)`

Stores a new TOTP credential.

- **Parameters**:
  - `credential` – The `TotpCredential` to be created. Must not be `null`.
- **Returns**:
  - A `Task` resolving to the created `TotpCredential`, typically with an assigned `Id`.
- **Exceptions**:
  - Throws `ArgumentNullException` if `credential` is `null`.
  - Throws if the credential cannot be persisted (e.g., duplicate key, constraint violation).

---

### `Task<TotpCredential> UpdateAsync(TotpCredential credential)`

Updates an existing TOTP credential.

- **Parameters**:
  - `credential` – The `TotpCredential` to update. Must not be `null` and must have a non-default `Id`.
- **Returns**:
  - A `Task` resolving to the updated `TotpCredential`.
- **Exceptions**:
  - Throws `ArgumentNullException` if `credential` is `null`.
  - Throws if the credential does not exist or cannot be updated (e.g., optimistic concurrency failure).

---
### `Task DeleteAsync(TotpCredential credential)`

Deletes a TOTP credential.

- **Parameters**:
  - `credential` – The `TotpCredential` to delete. Must not be `null`.
- **Returns**:
  - A `Task` representing the asynchronous operation.
- **Exceptions**:
  - Throws `ArgumentNullException` if `credential` is `null`.
  - Throws if the credential does not exist or cannot be deleted.

---
### `Task DeleteByIdAsync(Guid id)`

Deletes a TOTP credential by its unique identifier.

- **Parameters**:
  - `id` – The unique identifier of the TOTP credential to delete.
- **Returns**:
  - A `Task` representing the asynchronous operation.
- **Exceptions**:
  - Throws if the credential does not exist or cannot be deleted.

---
### `Task<bool> ExistsAsync(Guid id)`

Checks whether a TOTP credential with the given identifier exists.

- **Parameters**:
  - `id` – The unique identifier to check.
- **Returns**:
  - A `Task<bool>` resolving to `true` if the credential exists, otherwise `false`.
- **Exceptions**:
  - Throws if the operation cannot be completed (e.g., storage error).

---
### `Task<TotpCredential?> GetByUserIdAsync(string userId)`

Retrieves a TOTP credential associated with a specific user.

- **Parameters**:
  - `userId` – The user identifier to search by. Must not be `null` or empty.
- **Returns**:
  - A `Task` resolving to the `TotpCredential` if found, or `null` if not found.
- **Exceptions**:
  - Throws `ArgumentException` if `userId` is `null` or empty.
  - Throws if the underlying storage operation fails.

---
### `Task DeleteByUserIdAsync(string userId)`

Deletes a TOTP credential associated with a specific user.

- **Parameters**:
  - `userId` – The user identifier whose credential should be deleted. Must not be `null` or empty.
- **Returns**:
  - A `Task` representing the asynchronous operation.
- **Exceptions**:
  - Throws `ArgumentException` if `userId` is `null` or empty.
  - Throws if the credential does not exist or cannot be deleted.

## Usage

### Example 1: Creating and retrieving a TOTP credential
