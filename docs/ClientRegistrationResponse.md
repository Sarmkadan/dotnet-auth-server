# ClientRegistrationResponse

The `ClientRegistrationResponse` type represents the response returned by the authorization server after a client registration request. It contains metadata about the registered client, including identifiers, credentials, grant types, redirect URIs, and other configuration details required for OAuth 2.0 and OpenID Connect client interactions.

## API

### `public string ClientId`
The unique identifier assigned to the client by the authorization server. This value is required and cannot be `null` or empty.
- **Purpose**: Used by the authorization server to identify the client in subsequent requests.
- **Throws**: N/A (guaranteed to be populated by the server).

### `public string? ClientSecret`
The client secret issued by the authorization server, if applicable. This value may be `null` for public clients or clients using alternative authentication methods.
- **Purpose**: Used for client authentication at the token endpoint.
- **Throws**: N/A.

### `public long ClientIdIssuedAt`
The timestamp (in Unix epoch seconds) indicating when the `ClientId` was issued.
- **Purpose**: Used to track the age of the client registration.
- **Throws**: N/A.

### `public long? ClientSecretExpiresAt`
The timestamp (in Unix epoch seconds) indicating when the `ClientSecret` expires, if applicable. This value is `null` if the secret does not expire.
- **Purpose**: Used to enforce secret rotation policies.
- **Throws**: N/A.

### `public string ClientName`
A human-readable name for the client.
- **Purpose**: Displayed in user interfaces or consent screens.
- **Throws**: N/A.

### `public ICollection<string> GrantTypes`
A collection of grant types the client is permitted to use (e.g., `authorization_code`, `client_credentials`).
- **Purpose**: Determines which OAuth 2.0 flows the client can initiate.
- **Throws**: N/A.

### `public ICollection<string> RedirectUris`
A collection of URIs to which the authorization server may redirect the user after authorization.
- **Purpose**: Validates redirect URIs during authorization requests.
- **Throws**: N/A.

### `public ICollection<string> ResponseTypes`
A collection of response types the client is permitted to use (e.g., `code`, `token`).
- **Purpose**: Determines the format of the authorization response.
- **Throws**: N/A.

### `public string? Scope`
A space-separated list of scopes the client is authorized to request.
- **Purpose**: Defines the permissions the client may request during authorization.
- **Throws**: N/A.

### `public string TokenEndpointAuthMethod`
The authentication method the client uses at the token endpoint (e.g., `client_secret_basic`, `none`).
- **Purpose**: Specifies how the client authenticates during token requests.
- **Throws**: N/A.

### `public string? LogoUri`
A URI pointing to a logo for the client.
- **Purpose**: Displayed in user interfaces or consent screens.
- **Throws**: N/A.

### `public string? PolicyUri`
A URI pointing to the client's privacy policy.
- **Purpose**: Provided to users during consent flows.
- **Throws**: N/A.

### `public string? TosUri`
A URI pointing to the client's terms of service.
- **Purpose**: Provided to users during consent flows.
- **Throws**: N/A.

### `public ICollection<string> Contacts`
A collection of email addresses or other contact methods for the client's administrators.
- **Purpose**: Used for administrative communication.
- **Throws**: N/A.

## Usage

### Example 1: Registering a Client and Processing the Response
