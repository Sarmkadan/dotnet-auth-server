# DynamicClientRegistrationService

`DynamicClientRegistrationService` implements OAuth 2.0 Dynamic Client Registration (RFC 7591). It validates a client metadata request, creates a `Client` record in the client repository, and returns the registered metadata. For confidential clients it also returns a generated `client_secret`, which is shown only once.

The class is `public sealed` and lives in `src/Services/DynamicClientRegistrationService.cs`, namespace `DotnetAuthServer.Services`. It has no interface; `Program.cs` registers it as a scoped concrete type. It is used by `ClientRegistrationController`, which maps `AuthServerException` to the HTTP response. The service adds no NuGet dependencies; it uses only the BCL (`System.Security.Cryptography`, `System.Text`).

## Role in the system

- Validates the registration request against the server's supported grant types, token endpoint authentication methods, and redirect URI syntax.
- Generates a `client_id` and, for confidential clients, a `client_secret`.
- Stores only a SHA-256 hash of the secret on the `Client` entity.
- Intersects requested scopes with the server's supported scopes.
- Applies server policy (PKCE and consent defaults) to the new client.
- Returns the registered metadata in the shape defined by RFC 7591 §3.2.1.

## Constructor

### `public DynamicClientRegistrationService(IClientRepository clientRepository, AuthServerOptions options, ILogger<DynamicClientRegistrationService> logger)`

Creates the service.

**Parameters:**
- `clientRepository` – stores the new `Client` through `CreateAsync`.
- `options` – server options. Read fields: `SupportedScopes`, `RequirePkceForAllClients`, `RequireUserConsent`.
- `logger` – logs one information entry per successful registration.

**Exceptions:** none thrown by the constructor itself. The service does not guard against `null` arguments; the DI container supplies them.

## Supported values

These values are fixed in the source. Changing them requires a code change.

**Grant types** (`SupportedGrantTypes`, compared case-insensitively):

| Value | Constant |
|---|---|
| `authorization_code` | `Constants.GrantTypes.AuthorizationCode` |
| `client_credentials` | `Constants.GrantTypes.ClientCredentials` |
| `implicit` | `Constants.GrantTypes.Implicit` |
| `refresh_token` | `Constants.GrantTypes.RefreshToken` |

**Token endpoint authentication methods** (`SupportedAuthMethods`, compared case-insensitively):

| Value | Meaning |
|---|---|
| `none` | Public client. No secret is issued. |
| `client_secret_basic` | Confidential client. Secret is sent in the `Authorization` header. |
| `client_secret_post` | Confidential client. Secret is sent in the request body. |

## Registration

### `public Task<ClientRegistrationResponse> RegisterAsync(ClientRegistrationRequest request, CancellationToken cancellationToken = default)`

Registers a client. The method runs the steps below in order and stops at the first failure.

1. **Required fields.** `request.IsValid()` must return `true`. See [Request validation](#request-validation).
2. **Grant types.** Every entry in `GrantTypes` must be in the supported list.
3. **Authentication method.** `TokenEndpointAuthMethod` must be in the supported list.
4. **Redirect URIs.** Every entry in `RedirectUris` must parse as an absolute URI (`Uri.TryCreate` with `UriKind.Absolute`).
5. **Identifiers and secret.** A new `client_id` is generated. If the method is not `none`, a new secret is generated and its hash is kept.
6. **Scopes.** Requested scopes are split on spaces and intersected with `SupportedScopes` (case-insensitive).
7. **Persist.** A `Client` is created and passed to `IClientRepository.CreateAsync`.
8. **Respond.** A `ClientRegistrationResponse` is returned.

The method does not wrap the repository call in a transaction and does not roll back on failure.

**Parameters:**
- `request` – the client metadata. Property names follow RFC 7591 §2 through `ClientRegistrationRequest`.
- `cancellationToken` – passed to `IClientRepository.CreateAsync`. The method does not check it itself.

**Return value:** a `ClientRegistrationResponse` describing the new client. See [Response](#response).

**Exceptions:**
- `AuthServerException` with error code `invalid_request` and HTTP status `400` for every validation failure listed in [Validation errors](#validation-errors).
- Exceptions from `IClientRepository.CreateAsync` propagate unchanged.
- `RandomNumberGenerator.Fill` failures propagate unchanged.

## Request validation

`ClientRegistrationRequest.IsValid()` returns `false` when:
- `client_name` is `null`, empty, or whitespace; or
- `grant_types` contains `authorization_code` or `implicit` (compared case-insensitively) and `redirect_uris` is empty.

The service does not check `response_types`, `logo_uri`, `policy_uri`, `tos_uri`, `contacts`, or `client_uri` beyond what the model holds. `client_uri` is accepted by the request model but is not copied to the `Client` entity or the response.

### Validation errors

All of these use error code `invalid_request` and status `400`.

| Check | Message |
|---|---|
| Required fields missing | `client_name is required and redirect_uris must be provided for authorization_code / implicit grant types.` |
| Unsupported grant type | `Grant type '<value>' is not supported by this server.` |
| Unsupported auth method | `token_endpoint_auth_method '<value>' is not supported.` |
| Malformed redirect URI | `redirect_uri '<value>' is not a valid absolute URI.` |

The first unsupported grant type in the request is reported. The same error is used for a missing `client_name` and for a missing `redirect_uris` on a flow that needs them.

## Client record

The service builds a `Client` with these values:

| Field | Value |
|---|---|
| `ClientId` | New GUID, `N` format (32 lowercase hex characters, no hyphens). |
| `ClientName` | `client_name` from the request. |
| `ClientSecretHash` | Base64 SHA-256 of the plain secret, or `null` for public clients. |
| `IsConfidential` | `true` unless `token_endpoint_auth_method` is `none`. |
| `IsActive` | `true`. |
| `RedirectUris` | Copy of `redirect_uris`. |
| `AllowedGrantTypes` | Copy of `grant_types`. |
| `AllowedScopes` | Intersection of requested and supported scopes (see [Scope handling](#scope-handling)). |
| `LogoUri`, `PolicyUri` | From `logo_uri`, `policy_uri`. |
| `TermsOfServiceUri` | From `tos_uri`. |
| `Contacts` | Copy of `contacts`. |
| `RequirePkce` | Confidential: `RequirePkceForAllClients`. Public: always `true`. |
| `RequireConsent` | `RequireUserConsent`. |

### Secrets

- A confidential client gets a 32-byte random secret, encoded as unpadded base64url (43 characters).
- The plain secret is returned once, in the response. Only the hash is stored.
- The hash is unsalted SHA-256 in base64. It is not a password-hashing function; the secret has enough entropy that this is acceptable for a random machine-generated value.
- Public clients get no secret. `ClientSecret` and `ClientSecretExpiresAt` are `null` in the response.

### Scope handling

- If the request has no `scope`, or `scope` contains only whitespace, `AllowedScopes` is the full `SupportedScopes` list.
- Otherwise, `AllowedScopes` is the subset of requested scopes that appear in `SupportedScopes`. Unknown scopes are dropped silently, as RFC 7591 §3.2.2 allows.
- If the intersection is empty, `AllowedScopes` is empty and the response `scope` is omitted.

Scope matching is case-insensitive. The stored scope value keeps the casing of the requested string, not the casing from `SupportedScopes`.

## Response

The method returns a `ClientRegistrationResponse`. Its fields are:

| Field | Value |
|---|---|
| `ClientId` | The generated `client_id`. |
| `ClientSecret` | Plain secret for confidential clients; omitted for public clients. |
| `ClientIdIssuedAt` | Unix time in seconds, taken after the repository call. |
| `ClientSecretExpiresAt` | `0` (no expiry) for confidential clients; omitted for public clients. |
| `ClientName` | Stored client name. |
| `GrantTypes` | Stored grant types. |
| `RedirectUris` | Stored redirect URIs. |
| `ResponseTypes` | `response_types` from the request, echoed without validation. |
| `Scope` | Space-joined `AllowedScopes`; omitted when empty. |
| `TokenEndpointAuthMethod` | The value from the request, echoed with its original casing. |
| `LogoUri`, `PolicyUri`, `TosUri` | Stored values; omitted when `null`. |
| `Contacts` | Stored contacts. An empty list is written as `[]`. |

Because `ResponseTypes` is echoed, a registration can succeed with response types that the server does not use.

## Logging

On success the service logs at `Information` level:

```
Dynamically registered client {ClientId} (confidential={IsConfidential})
```

The plain secret is never logged. Validation failures are not logged here; `ClientRegistrationController` logs them at `Warning` level.

## Notes

- **Storage.** `IClientRepository` is an in-memory `ConcurrentDictionary` in the default build, so registrations do not survive a restart.
- **Null collections.** The service reads `GrantTypes` and `RedirectUris` without a null check. A JSON `null` for either property causes a `NullReferenceException` rather than an `AuthServerException`. The request model defaults both to empty or single-value collections, so this happens only for an explicit `null` in the JSON.
- **Empty grant types.** An empty `grant_types` array passes validation and creates a client with no allowed grant types.
- **Client URI.** `client_uri` is accepted and then discarded.
- **Secret hash.** The hash is unsalted SHA-256. Changing it would invalidate every secret already stored, so treat it as part of the stored data format.

## Example

Request:

```json
{
  "client_name": "Example SPA",
  "grant_types": ["authorization_code"],
  "redirect_uris": ["https://app.example.com/callback"],
  "scope": "openid profile unknown_scope",
  "token_endpoint_auth_method": "none"
}
```

Response (`201 Created`), assuming `openid` and `profile` are supported:

```json
{
  "client_id": "5b1f0c9e2d3a4f6b8c7d9e0f1a2b3c4d",
  "client_id_issued_at": 1792944000,
  "client_name": "Example SPA",
  "grant_types": ["authorization_code"],
  "redirect_uris": ["https://app.example.com/callback"],
  "response_types": ["code"],
  "scope": "openid profile",
  "token_endpoint_auth_method": "none"
}
```

The client ID, timestamp, and other values in this example are illustrative. The controller serializes the response with the same JSON property names as `ClientRegistrationResponse`.

## Related

- `ClientRegistrationRequest` and `ClientRegistrationResponse` in `src/Domain/Models/` – request and response models.
- `ClientRegistrationController` – the `POST /register` endpoint.
- `Client` in `src/Domain/Entities/` – the stored record.
- `IClientRepository` in `src/Data/Repositories/` – persistence seam.
- `AuthServerOptions` in `src/Configuration/` – `SupportedScopes`, `RequirePkceForAllClients`, `RequireUserConsent`.
- `AuthServerException` in `src/Exceptions/` – error type thrown on validation failure.
