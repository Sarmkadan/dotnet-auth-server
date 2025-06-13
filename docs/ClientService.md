# ClientService

Provides operations for managing OAuth clients, including registration, updates, secret rotation, and activation state changes.

## API

### `ClientService()`

Initializes a new instance of the `ClientService` class.

### `async Task<Client> RegisterClientAsync(ClientRegistration registration)`

Registers a new client in the system.

- **Parameters**
  - `registration`: A `ClientRegistration` containing the client's metadata (name, redirect URIs, scopes, etc.).
- **Return value**
  - A `Task<Client>` resolving to the newly registered `Client` instance.
- **Exceptions**
  - Throws `ArgumentNullException` if `registration` is `null`.
  - Throws `InvalidOperationException` if the client name or redirect URIs are invalid.

### `async Task<Client> UpdateClientAsync(string clientId, ClientUpdate update)`

Updates an existing client's metadata.

- **Parameters**
  - `clientId`: The unique identifier of the client to update.
  - `update`: A `ClientUpdate` containing the fields to modify.
- **Return value**
  - A `Task<Client>` resolving to the updated `Client` instance.
- **Exceptions**
  - Throws `ArgumentNullException` if `clientId` or `update` is `null`.
  - Throws `KeyNotFoundException` if no client exists with the given `clientId`.
  - Throws `InvalidOperationException` if the update would violate uniqueness constraints (e.g., duplicate redirect URIs).

### `async Task<string> RotateClientSecretAsync(string clientId)`

Generates a new client secret for the specified client.

- **Parameters**
  - `clientId`: The unique identifier of the client whose secret should be rotated.
- **Return value**
  - A `Task<string>` resolving to the new secret (base64-encoded).
- **Exceptions**
  - Throws `ArgumentNullException` if `clientId` is `null`.
  - Throws `KeyNotFoundException` if no client exists with the given `clientId`.
  - Throws `InvalidOperationException` if the client is inactive.

### `async Task DeactivateClientAsync(string clientId)`

Deactivates a client, revoking its ability to authenticate.

- **Parameters**
  - `clientId`: The unique identifier of the client to deactivate.
- **Return value**
  - A `Task` representing the asynchronous operation.
- **Exceptions**
  - Throws `ArgumentNullException` if `clientId` is `null`.
  - Throws `KeyNotFoundException` if no client exists with the given `clientId`.

### `async Task ReactivateClientAsync(string clientId)`

Reactivates a previously deactivated client.

- **Parameters**
  - `clientId`: The unique identifier of the client to reactivate.
- **Return value**
  - A `Task` representing the asynchronous operation.
- **Exceptions**
  - Throws `ArgumentNullException` if `clientId` is `null`.
  - Throws `KeyNotFoundException` if no client exists with the given `clientId`.

### `bool ValidateClientSecret(string clientId, string clientSecret)`

Validates that the provided secret matches the client's current secret.

- **Parameters**
  - `clientId`: The unique identifier of the client.
  - `clientSecret`: The secret to validate (base64-encoded).
- **Return value**
  - `true` if the secret is valid; otherwise, `false`.
- **Exceptions**
  - Throws `ArgumentNullException` if `clientId` or `clientSecret` is `null`.
  - Throws `KeyNotFoundException` if no client exists with the given `clientId`.

## Usage

### Registering a new client
