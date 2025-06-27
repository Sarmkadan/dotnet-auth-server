# AuthServerOptions

Configuration class for the dotnet-auth-server authentication server, defining runtime parameters for token issuance, client security policies, user account management, and database connectivity.

## API

### `IssuerUrl`
Gets or sets the URL that will be used as the `iss` claim in issued JWTs. This value must match the URL used by clients to access the authorization server.

### `JwtSigningKey`
Gets or sets the cryptographic key used to sign issued JWTs. Must be a secure, base64-encoded secret of sufficient length for the selected algorithm (e.g., 256-bit for HS256).

### `JwtAlgorithm`
Gets or sets the algorithm used to sign JWTs. Common values include `HS256`, `HS384`, and `HS512`. Must be compatible with the provided `JwtSigningKey`.

### `AccessTokenLifetimeSeconds`
Gets or sets the lifetime, in seconds, of access tokens issued by the server. After this period elapses, clients must obtain a new token via refresh or re-authentication.

### `RefreshTokenLifetimeSeconds`
Gets or sets the maximum lifetime, in seconds, of refresh tokens. Refresh tokens may be rotated or invalidated earlier based on server policy.

### `AuthorizationCodeLifetimeSeconds`
Gets or sets the lifetime, in seconds, of authorization codes issued during the OAuth 2.0 authorization code flow. Codes must be exchanged for tokens before expiration.

### `RequirePkceForAllClients`
Gets or sets a value indicating whether Proof Key for Code Exchange (PKCE) is required for all clients, including public clients. Enabling this mitigates authorization code interception attacks.

### `AutoRefreshTokenRotation`
Gets or sets a value indicating whether refresh tokens are automatically rotated on each use. When enabled, old refresh tokens are invalidated and new ones are issued, improving security.

### `MaxRefreshTokenGenerations`
Gets or sets the maximum number of times a refresh token can be used before it is permanently invalidated. Prevents indefinite refresh token usage and limits exposure.

### `ClockSkewToleranceSeconds`
Gets or sets the tolerance, in seconds, for clock skew when validating token expiration times. Accounts for minor time differences between server and client clocks.

### `DatabaseConnectionString`
Gets or sets the connection string used to connect to the persistence store. Used for user accounts, refresh tokens, and other server state.

### `UseInMemoryDatabase`
Gets or sets a value indicating whether an in-memory database should be used instead of a persistent store. Primarily for testing; not suitable for production.

### `FailedLoginAttemptThreshold`
Gets or sets the number of consecutive failed login attempts allowed before account lockout is triggered.

### `AccountLockoutDurationMinutes`
Gets or sets the duration, in minutes, for which an account remains locked after exceeding the failed login threshold.

### `RequireUserConsent`
Gets or sets a value indicating whether users must explicitly consent to authorization requests. When disabled, authorization is granted implicitly.

### `SupportedScopes`
Gets or sets the collection of OAuth 2.0 scopes supported by the server. Scopes define the extent of access granted to clients.

### `SupportedGrantTypes`
Gets or sets the collection of OAuth 2.0 grant types supported by the server. Common types include `authorization_code`, `client_credentials`, and `refresh_token`.

## Usage
