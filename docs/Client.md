# Client

Represents an OAuth 2.0 / OpenID Connect client registered with the authorization server. Used to validate client credentials, enforce security policies, and control token issuance parameters for applications interacting with the server.

## API

### `ClientId`
Unique identifier for the client. Required for all client types. Must be non-empty and unique across all registered clients.

### `ClientName`
Human-readable name of the client. Displayed to users during consent and token issuance flows. Optional.

### `ClientSecretHash`
BCrypt hash of the client secret. Used for confidential clients to authenticate during token requests. Optional; if null, the client is considered public.

### `Description`
Human-readable description of the client's purpose or organization. Optional.

### `IsConfidential`
Indicates whether the client is confidential (requires a secret) or public (no secret required). Affects authentication and token issuance behavior.

### `IsActive`
Determines whether the client is enabled and can interact with the authorization server. Disabled clients cannot obtain tokens or use endpoints.

### `RedirectUris`
Collection of registered redirect URIs for authorization code and implicit flows. Must match exactly during authorization requests. Empty collection disables redirect-based flows.

### `PostLogoutRedirectUris`
Collection of URIs to which the user agent may be redirected after logout. Used in OpenID Connect session management. Optional.

### `AllowedGrantTypes`
Collection of grant types the client is permitted to use (e.g., `authorization_code`, `client_credentials`, `refresh_token`). Restricts which OAuth 2.0 flows the client can initiate.

### `AllowedScopes`
Collection of scopes the client can request. Defines the permissions it may obtain in access tokens. Must be non-empty for token issuance to succeed.

### `AllowedCorsOrigins`
Collection of origins permitted for CORS requests during authorization and token endpoints. Used to control cross-origin access to the server. Optional.

### `AccessTokenLifetime`
Lifetime in seconds of access tokens issued to this client. Defaults to server-wide setting if not specified. Must be positive.

### `RefreshTokenLifetime`
Lifetime in seconds of refresh tokens issued to this client. Controls session duration and re-authentication frequency. Must be positive.

### `RefreshTokenRotation`
Indicates whether refresh tokens are rotated (single-use) upon each refresh request. When true, old tokens are invalidated immediately after use.

### `RequirePkce`
Requires Proof Key for Code Exchange (PKCE) for authorization code flows. Enforced for public clients to mitigate authorization code interception attacks.

### `RequireConsent`
Requires explicit user consent before issuing tokens or releasing user data. Enables user control over data sharing.

### `Contacts`
Collection of email addresses for individuals responsible for the client. Used for administrative notifications and security alerts. Optional.

### `LogoUri`
URL to a logo image representing the client. Displayed in consent screens and user interfaces. Must be an absolute HTTPS URI. Optional.

### `PolicyUri`
URL to a page describing the client's privacy policy or data handling practices. Displayed during consent. Must be an absolute HTTPS URI. Optional.

### `TermsOfServiceUri`
URL to the client's terms of service. Displayed during consent. Must be an absolute HTTPS URI. Optional.

## Usage

### Registering a new confidential client
