# TokenRequest

The `TokenRequest` class represents the payload for OAuth 2.0 and OpenID Connect token requests, encapsulating all standard and extension parameters required to obtain tokens from an authorization server. It is used to deserialize incoming token request messages and validate their contents before processing.

## API

### `public string? GrantType`
The OAuth 2.0 grant type identifier (e.g., `authorization_code`, `password`, `refresh_token`). This field determines which other parameters are required and how the token request is processed. Must not be `null` or empty for a valid request.

### `public string? ClientId`
The client identifier issued during registration. Used to authenticate the client and associate the request with a specific application. Required for all grant types except those explicitly exempted by the authorization server policy.

### `public string? ClientSecret`
The client secret, if applicable, used to authenticate confidential clients. Must be provided when the client is of type `confidential` and the authorization server requires client authentication. Never transmitted in plaintext over insecure channels.

### `public string? Code`
The authorization code received from the authorization server. Used in the `authorization_code` grant type to exchange an authorization code for an access token. Must be provided when `GrantType` is `authorization_code`.

### `public string? RedirectUri`
The redirection URI used in the initial authorization request. Must match one of the pre-registered URIs for the client. Required when `GrantType` is `authorization_code` or `implicit`.

### `public string? RefreshToken`
The refresh token issued by the authorization server. Used to obtain a new access token without user interaction. Must be provided when `GrantType` is `refresh_token`.

### `public string? Username`
The resource owner username. Used in the `password` grant type to authenticate the resource owner. Must be provided when `GrantType` is `password`.

### `public string? Password`
The resource owner password. Used in the `password` grant type to authenticate the resource owner. Must be provided when `GrantType` is `password`.

### `public string? Scope`
A space-delimited list of scopes being requested. Used to limit the scope of the issued token. If omitted, defaults to the scopes associated with the client or the resource owner’s default scopes.

### `public string? CodeVerifier`
The PKCE code verifier. Used to bind the token request to the initial authorization request when Proof Key for Code Exchange (PKCE) is enabled. Must be provided when the authorization request included a `code_challenge`.

### `public string? RequestedTokenType`
The type of token being requested (e.g., `urn:ietf:params:oauth:token-type:access_token`). Used to request a specific token format from the authorization server. Optional; if omitted, the server returns the default token type.

### `public string? SubjectToken`
The token representing the identity of the party on behalf of whom the token is being requested (e.g., a JWT or SAML assertion). Used in token exchange flows to delegate identity or authorization.

### `public string? SubjectTokenType`
The type of the `SubjectToken` (e.g., `urn:ietf:params:oauth:token-type:id_token`, `urn:ietf:params:oauth:token-type:saml2`). Required when `SubjectToken` is provided.

### `public string? ActorToken`
The token representing the identity of the acting party in a token exchange. Used to delegate authority or act on behalf of another party.

### `public string? ActorTokenType`
The type of the `ActorToken` (e.g., `urn:ietf:params:oauth:token-type:id_token`). Required when `ActorToken` is provided.

### `public string? IpAddress`
The IP address of the client making the token request. Used for risk analysis, rate limiting, and audit logging. Automatically populated by the server from the transport layer.

### `public Dictionary<string, string> CustomParameters`
A collection of additional, non-standard parameters sent in the request. Used to support extension grants or custom server-specific functionality. All keys and values are treated as opaque strings.

### `public bool IsValid`
Indicates whether the request has passed server-side validation rules (e.g., required fields, format checks, client authentication). Read-only; computed during validation.

### `public bool IsValidForGrantType`
Indicates whether the request is valid for the specified `GrantType` (e.g., all required fields are present and correctly formatted). Read-only; computed during validation.

## Usage
