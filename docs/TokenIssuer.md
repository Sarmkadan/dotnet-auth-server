# TokenIssuer

`TokenIssuer` is the service that issues OAuth 2.0 tokens at the token endpoint. It dispatches each request on its `grant_type` to one of four grant handlers (authorization code, refresh token, client credentials, password), issues the access token and, where the grant allows it, a refresh token, and returns a `TokenResponse`. It also exposes two helpers for client secrets.

The class implements `ITokenIssuer` and lives in `src/Services/TokenIssuer.cs`, namespace `DotnetAuthServer.Services`. It is `sealed`.

## Role in the system

`TokenIssuer` is the back end of the token endpoint. The controller parses the HTTP form and passes a `TokenRequest` here. The service:

1. Checks that the request is well formed and that `grant_type` is present.
2. Routes the request to the matching grant handler.
3. Generates the JWT access token (`GenerateAccessToken` or `GenerateClientCredentialsAccessToken`) and, for user grants, an opaque refresh token that is stored hashed (SHA-256) in `IRefreshTokenRepository`.
4. Returns a `TokenResponse` with `TokenType` set to `Bearer`.

Callers should depend on `ITokenIssuer`, not on the concrete class.

## Registration

`TokenIssuer` is a scoped service. Callers resolve it through `ITokenIssuer`, so register it as:

```csharp
builder.Services.AddScoped<ITokenIssuer, TokenIssuer>();
```

## Constructor

### `public TokenIssuer(AuthServerOptions options, IUserRepository userRepository, IClientRepository clientRepository, IAuthorizationGrantRepository grantRepository, IRefreshTokenRepository refreshTokenRepository, LoginRateLimiter loginRateLimiter, ILogger<TokenIssuer> logger)`

Creates a token issuer from its collaborators.

- **Parameters**:
  - `options`: Server options. Supplies the default access and refresh token lifetimes (`AccessTokenLifetimeSeconds`, `RefreshTokenLifetimeSeconds`) and the `AutoRefreshTokenRotation` switch.
  - `userRepository`: Looks up users by username (password grant) and by ID (refresh grant).
  - `clientRepository`: Looks up active clients by client ID.
  - `grantRepository`: Looks up and updates authorization codes (authorization code grant).
  - `refreshTokenRepository`: Looks up, stores, and revokes refresh tokens.
  - `loginRateLimiter`: Enforces per-username and per-IP limits on the password grant.
  - `logger`: Logger for grant outcomes and errors.
- **Throws**:
  - `ArgumentNullException`: If any parameter is `null`.

## Methods

### `public async Task<TokenResponse> HandleTokenRequestAsync(TokenRequest request, CancellationToken cancellationToken = default)`

Handles a token request and returns the token response. This is the only grant entry point.

- **Parameters**:
  - `request`: The parsed token request. `GrantType` selects the handler.
  - `cancellationToken`: Cancels repository calls.
- **Return value**: A `Task<TokenResponse>`. For a successful request, `AccessToken`, `TokenType` (`Bearer`), `ExpiresIn` (seconds), `Scope`, and, for user grants, `RefreshToken` are set.
- **Dispatch**: The `grant_type` value selects the handler:

  | `grant_type` | Handler | Refresh token issued |
  |---|---|---|
  | `authorization_code` | Authorization code grant | Yes |
  | `refresh_token` | Refresh token grant | Yes (a new one if rotation is on) |
  | `client_credentials` | Client credentials grant | No |
  | `password` | Password grant | Yes |

- **Throws**:
  - `ArgumentNullException`: If `request` is `null`.
  - `AuthServerException` (code `invalid_request`, HTTP 400): If `IsValid()` fails or `grant_type` is empty.
  - `UnsupportedGrantTypeException` (HTTP 400): If `grant_type` is not one of the four supported values.
  - `InvalidGrantException`: Errors from the authorization code and refresh token grants (see below).
  - `InvalidClientException`: Errors from the client credentials grant (see below).
  - `AuthServerException` (code `server_error`, HTTP 500): Any unexpected exception raised by a grant handler. The original exception is attached as the inner exception and logged. `OperationCanceledException` is not wrapped and propagates unchanged.

  `ErrorHandlingMiddleware` converts these exceptions to `{"error","error_description"}` responses.

- **Grant behaviour**:
  - **Authorization code**: Requires `code` and `redirect_uri` (`IsValidForGrantType`). The code must exist and be valid, and `redirect_uri` must match the stored value (case-insensitive). If the grant has a PKCE challenge, the `code_verifier` must match it. The grant is marked as used and updated before tokens are issued. The access token's scopes are the grant's `GrantedScopes`.
  - **Refresh token**: Looks up the refresh token by its SHA-256 hash. Presenting a revoked token is rejected with `InvalidGrantException`, and a rotation-revoked token is logged as a security event. The old token is revoked before the new one is issued. When `AutoRefreshTokenRotation` is on, the old token is replaced by a rotated token; otherwise the same refresh token stays valid.
  - **Client credentials**: The client must exist and be active, and a confidential client must present a matching secret (`ValidateClientSecret`). The access token's scopes come from `Scope`. No refresh token is issued.
  - **Password**: Requires `username` and `password`. The request is rejected if the rate limiter blocks the username or IP. An unknown or locked user is recorded as a failed attempt and returns `invalid_grant` with the message `Invalid credentials`. A successful login clears the username's failure count, and the access and refresh tokens are issued. See the note on unknown clients below.

- **Lifetimes**: Access and refresh token lifetimes use the client's own `AccessTokenLifetime` / `RefreshTokenLifetime` when that value is greater than zero, and the global `AuthServerOptions` default otherwise.

## Helper methods

### `public bool ValidateClientSecret(Client client, string? providedSecret)`

Checks a client secret against the stored hash.

- **Parameters**:
  - `client`: The client whose secret is checked. Its `IsConfidential` and `ClientSecretHash` properties are used.
  - `providedSecret`: The secret the caller presented. May be `null`.
- **Return value**: `true` if the client is public (`IsConfidential == false`), regardless of `providedSecret`. For a confidential client, `true` only when `providedSecret` is not empty or whitespace and its hash equals `ClientSecretHash` (ordinal comparison). Otherwise `false`.
- **Throws**: None. Errors while hashing are logged and return `false`.

> **Note:** Callers that need to reject public clients presenting a secret must check `IsConfidential` themselves. This method returns `true` for them.

### `public string HashClientSecret(string secret)`

Hashes a client secret with SHA-256 and returns the Base64 string.

- **Parameters**:
  - `secret`: The plain-text secret. Encoded as UTF-8 before hashing.
- **Return value**: The Base64-encoded SHA-256 hash of `secret`. The hash is unsalted and deterministic, so identical secrets always produce identical hashes.
- **Throws**: `ArgumentNullException` if `secret` is `null` (from `Encoding.UTF8.GetBytes`).

## Notes

- **Password grant and unknown clients**: The password grant looks up the client with `GetActiveClientAsync`, but it does not reject a `null` result. It falls back to the default lifetimes and still issues tokens for the requested `client_id`. Validate the client before calling this grant if that matters to your deployment.
- **Hashing**: Both refresh tokens and client secrets are stored as unsalted SHA-256 hashes. The helper methods here are the only place these are computed in this class.
- **Private helpers**: `GenerateAccessToken`, `GenerateClientCredentialsAccessToken`, `GenerateAndStoreRefreshTokenAsync`, and the other private members are implementation details and are not part of the public API.
