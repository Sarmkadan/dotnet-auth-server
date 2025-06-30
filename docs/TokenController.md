# TokenController

Handles OAuth 2.0 token lifecycle operations including issuance, inspection, and revocation. Integrates with the authentication server's token service to validate and manage access tokens.

## API

### `TokenController`
Public controller exposing endpoints for token management.

### `async Task<IActionResult> RequestToken`
Issues a new access token based on provided client credentials or refresh token.

- **Parameters**:
  - `HttpContext context`: The current HTTP context.
  - `TokenRequest request`: The token request payload containing grant type, client ID, client secret, and optional refresh token.
- **Return value**:
  - `Task<IActionResult>`: `200 OK` with token response on success; `400 BadRequest` on invalid request; `401 Unauthorized` on authentication failure.
- **Exceptions**:
  - Throws `ArgumentNullException` if `request` is null.
  - Throws `InvalidOperationException` if token service is unavailable.

### `public IActionResult IntrospectToken`
Validates and returns metadata for a given access token.

- **Parameters**:
  - `string token`: The access token to introspect.
- **Return value**:
  - `IActionResult`: `200 OK` with token introspection details (active, scope, client ID, etc.) if valid; `400 BadRequest` if token is malformed; `404 NotFound` if token is unknown.
- **Exceptions**:
  - Throws `ArgumentException` if `token` is null or empty.

### `public IActionResult RevokeToken`
Invalidates a previously issued access token.

- **Parameters**:
  - `string token`: The access token to revoke.
- **Return value**:
  - `IActionResult`: `200 OK` on successful revocation; `400 BadRequest` if token is malformed; `404 NotFound` if token is already revoked or unknown.
- **Exceptions**:
  - Throws `ArgumentException` if `token` is null or empty.

## Usage

### Requesting a Token
