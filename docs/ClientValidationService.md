# ClientValidationService

The `ClientValidationService` is responsible for validating OAuth 2.0 client requests, including credentials, redirect URIs, scopes, and grant types. It ensures that client requests conform to the server's security policies before processing.

## API

### `public ClientValidationService`

Initializes a new instance of the `ClientValidationService` with dependencies required for client validation.

### `public async Task<Client> ValidateClientCredentialsAsync`

Validates the provided client credentials (e.g., client ID and secret) against the server's stored client records.

- **Parameters**:
  - `clientId`: The unique identifier of the client.
  - `clientSecret`: The secret associated with the client.
- **Return value**: A `Task<Client>` resolving to the validated `Client` instance if credentials are valid.
- **Throws**:
  - `ArgumentNullException`: If `clientId` or `clientSecret` is `null`.
  - `InvalidOperationException`: If the client is not found or the credentials are invalid.

### `public async Task ValidateRedirectUriAsync`

Validates that the provided redirect URI matches one of the registered URIs for the client.

- **Parameters**:
  - `clientId`: The unique identifier of the client.
  - `redirectUri`: The URI to validate.
- **Return value**: A `Task` that completes when validation succeeds.
- **Throws**:
  - `ArgumentNullException`: If `clientId` or `redirectUri` is `null`.
  - `InvalidOperationException`: If the client does not exist or the URI is not registered.

### `public async Task ValidateScopesAsync`

Validates that the requested scopes are permitted for the client.

- **Parameters**:
  - `clientId`: The unique identifier of the client.
  - `scopes`: The collection of scopes to validate.
- **Return value**: A `Task` that completes when validation succeeds.
- **Throws**:
  - `ArgumentNullException`: If `clientId` or `scopes` is `null`.
  - `InvalidOperationException`: If the client does not exist or any scope is not permitted.

### `public async Task ValidateGrantTypeAsync`

Validates that the requested grant type is supported by the client.

- **Parameters**:
  - `clientId`: The unique identifier of the client.
  - `grantType`: The grant type to validate (e.g., `authorization_code`, `client_credentials`).
- **Return value**: A `Task` that completes when validation succeeds.
- **Throws**:
  - `ArgumentNullException`: If `clientId` or `grantType` is `null`.
  - `InvalidOperationException`: If the client does not exist or the grant type is not supported.

## Usage

### Example 1: Validating Client Credentials
