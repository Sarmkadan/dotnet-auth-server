# AuthorizationService

**Namespace:** `DotnetAuthServer.Services`

Handles the server side of the OAuth 2.0 / OpenID Connect authorization endpoint: validates authorization requests, builds the consent prompt model, creates authorization codes (grants), checks PKCE code verifiers, and deletes expired grants. The class is `public sealed`, has no interface, and holds no per-request state; every method takes the data it needs as arguments. The same file also declares the public type [`ConsentResponse`](#consentresponse), which is returned by `GetConsentPromptAsync`.

## API

| Member | Returns | Purpose |
|---|---|---|
| [`AuthorizationService(...)`](#constructor) | | Constructor |
| [`ValidateAuthorizationRequestAsync`](#validateauthorizationrequestasync) | `Task<AuthorizationRequest>` | Validate an authorization request |
| [`CreateAuthorizationGrantAsync`](#createauthorizationgrantasync) | `Task<AuthorizationGrant>` | Create and store an authorization code |
| [`GetConsentPromptAsync`](#getconsentpromptasync) | `Task<ConsentResponse>` | Build the consent prompt model |
| [`ValidatePkceCodeVerifier`](#validatepkcecodeverifier) | `bool` | Check a PKCE code verifier against a stored challenge |
| [`CleanupExpiredGrantsAsync`](#cleanupexpiredgrantsasync) | `Task` | Delete expired authorization grants |

### Constructor

```csharp
public AuthorizationService(
    AuthServerOptions options,
    IClientRepository clientRepository,
    IAuthorizationGrantRepository grantRepository,
    ILogger<AuthorizationService> logger)
```

Stores the four dependencies. No argument is null-checked in the constructor.

**Parameters**

- `options`: Server options. Read for `SupportedScopes`, `RequirePkceForAllClients`, and `AuthorizationCodeLifetimeSeconds`.
- `clientRepository`: Looks up clients through `GetActiveClientAsync`.
- `grantRepository`: Stores grants (`CreateAsync`) and removes expired ones (`DeleteExpiredAsync`).
- `logger`: Receives warning and debug messages about rejected or accepted requests, and an information message when a grant is created.

### ValidateAuthorizationRequestAsync

```csharp
public async Task<AuthorizationRequest> ValidateAuthorizationRequestAsync(
    AuthorizationRequest request,
    CancellationToken cancellationToken = default)
```

Runs the authorization request checks in the order below. The first failing check throws; nothing is written to the repositories. The method does not modify `request`.

1. `request` is null: `ArgumentNullException`.
2. `request.IsValid()` is false (`ClientId`, `ResponseType`, `RedirectUri`, or `Scope` is missing or whitespace): `AuthServerException` with `invalid_request`, status 400.
3. No active client matches `ClientId`: `InvalidClientException` ("Client not found or inactive").
4. `ResponseType` is not one of `code`, `token`, `id_token`, `code id_token`, `code token`, `id_token token`, `code id_token token` (case-insensitive): `AuthServerException` with `unsupported_response_type`, status 400.
5. `RedirectUri` is not registered for the client (`Client.IsRedirectUriValid`): `invalid_request`, 400.
6. `Scope` longer than 500 characters: `invalid_request`, 400.
7. `State` longer than 500 characters: `invalid_request`, 400.
8. A requested scope is not in `AuthServerOptions.SupportedScopes` (case-insensitive): `InvalidScopeException`.
9. `ResponseType` is not allowed for the client (`Client.IsGrantTypeAllowed`): `UnauthorizedClientException`.
10. `CodeChallengeMethod` is `plain`: `invalid_request`, 400. Only `S256` is accepted.
11. A `CodeChallenge` is present but malformed: `invalid_request`, 400. See [PKCE challenge format](#pkce-challenge-format).
12. The client is public (`IsConfidential == false`) and no `CodeChallenge` is present: `invalid_request`, 400.
13. `RequirePkceForAllClients` is true, the client has `RequirePkce` set, and no `CodeChallenge` is present: `invalid_request`, 400.

**Parameters**

- `request`: The parsed authorization request.
- `cancellationToken`: Passed to the client lookup.

**Returns**

The same `request` instance, unchanged, once all checks pass.

**Exceptions**

All thrown types derive from `AuthServerException`, except `ArgumentNullException` (step 1). Each carries the OAuth error code and HTTP status listed above; `ErrorHandlingMiddleware` turns them into `{"error", "error_description"}` JSON.

### CreateAuthorizationGrantAsync

```csharp
public async Task<AuthorizationGrant> CreateAuthorizationGrantAsync(
    string clientId,
    string userId,
    string grantedScopes,
    string redirectUri,
    AuthorizationRequest request,
    CancellationToken cancellationToken = default)
```

Creates an authorization code for a request that has already been validated and approved, stores it through `grantRepository.CreateAsync`, and returns it. The method does not re-run any validation, does not check the client, and does not check consent. Callers must call `ValidateAuthorizationRequestAsync` first.

The new grant has:

- `GrantId`: a new `Guid` string.
- `Code`: 32 random bytes from `RandomNumberGenerator`, encoded as base64url without padding (43 characters).
- `RequestedScopes`: `request.Scope`, or `""` if it is null.
- `GrantedScopes`: the `grantedScopes` argument.
- `ResponseType`: `request.ResponseType`, or `""` if it is null.
- `State`, `Nonce`, `CodeChallenge`, `CodeChallengeMethod`: copied from `request`.
- `ExpiresAt`: UTC now plus `AuthorizationCodeLifetimeSeconds` (300 by default).

**Parameters**

- `clientId`: The client the code is issued to. Must not be null or empty.
- `userId`: The user who approved the request. Must not be null or empty.
- `grantedScopes`: The scopes actually granted, which may be a subset of the requested scopes. Must not be null or empty.
- `redirectUri`: The redirect URI the code is bound to. Must not be null or empty.
- `request`: The validated request. Must not be null.
- `cancellationToken`: Passed to the repository.

**Returns**

The stored `AuthorizationGrant`, including the generated `Code`.

**Exceptions**

- `ArgumentException` (from `ThrowIfNullOrEmpty`): `clientId`, `userId`, `grantedScopes`, or `redirectUri` is null or empty. Checked in that order.
- `ArgumentNullException`: `request` is null.
- Exceptions from `grantRepository.CreateAsync` propagate unchanged.

### GetConsentPromptAsync

```csharp
public async Task<ConsentResponse> GetConsentPromptAsync(
    AuthorizationRequest request,
    User user,
    CancellationToken cancellationToken = default)
```

Builds the model for the consent screen from the request, the authenticated user, and the client's details. It does not check whether consent was already given; that is the job of [`ConsentService`](ConsentService.md).

**Parameters**

- `request`: The validated authorization request. `request.ClientId` must not be null.
- `user`: The authenticated user.
- `cancellationToken`: Passed to the client lookup.

**Returns**

A [`ConsentResponse`](#consentresponse) with:

- `ClientId`, `ClientName`, `ClientLogoUri`, `ClientDescription`: from the client.
- `UserId`, `UserName`: from `user` (`Username`).
- `RequestedScopes`: the scopes from `request.GetRequestedScopes()`, materialized as a list.
- `RequireConsent`: `client.RequireConsent && RequestedScopes.Any()`.

**Exceptions**

- `ArgumentNullException`: `request`, `user`, or `request.ClientId` is null.
- `InvalidClientException` ("Client not found"): no active client matches `request.ClientId`.

### ValidatePkceCodeVerifier

```csharp
public bool ValidatePkceCodeVerifier(
    string codeChallenge,
    string codeVerifier,
    string? method)
```

Checks whether `codeVerifier` produces `codeChallenge` under `method`. Stateless; it does not use any instance state and can be called from the token endpoint.

- `method` `"S256"`: computes `BASE64URL(SHA256(UTF8(codeVerifier)))` without padding and compares it to `codeChallenge` with `==`.
- `method` `"plain"`: compares `codeVerifier` to `codeChallenge` with `==`.
- `method` `null`: treated as `"plain"`.
- Any other value: returns `false`. Comparison is case-sensitive, so `"s256"` returns `false`.

Comparisons are ordinary string equality, not constant-time.

**Parameters**

- `codeChallenge`: The challenge stored with the grant. Must not be null or empty.
- `codeVerifier`: The verifier the client sent. Must not be null or empty.
- `method`: The challenge method stored with the grant, or `null`.

**Returns**

`true` when the verifier matches the challenge under the method; otherwise `false`.

**Exceptions**

- `ArgumentException` (from `ThrowIfNullOrEmpty`): `codeChallenge` or `codeVerifier` is null or empty. Checked in that order.

### CleanupExpiredGrantsAsync

```csharp
public async Task CleanupExpiredGrantsAsync(
    CancellationToken cancellationToken = default)
```

Calls `grantRepository.DeleteExpiredAsync` to delete every grant whose expiry has passed. It removes expired grants for all clients and users, not only those tied to the current request. `TokenCleanupWorker` runs the same repository call on its own schedule; this method is an alternative entry point for the same job.

**Parameters**

- `cancellationToken`: Passed to the repository.

**Returns**

A task that completes when the repository call finishes.

**Exceptions**

Exceptions from `grantRepository.DeleteExpiredAsync` propagate unchanged.

## Supporting Types

### ConsentResponse

```csharp
public sealed class ConsentResponse
```

Model returned by `GetConsentPromptAsync`. All properties are settable.

| Property | Type | Description |
|---|---|---|
| `ClientId` | `string` | Client identifier. Not null after `GetConsentPromptAsync` returns. |
| `ClientName` | `string` | Client display name. Not null after `GetConsentPromptAsync` returns. |
| `ClientLogoUri` | `string?` | Client logo URI, if registered. |
| `ClientDescription` | `string?` | Client description, if set. |
| `UserId` | `string` | User identifier. Not null after `GetConsentPromptAsync` returns. |
| `UserName` | `string` | User display name. Not null after `GetConsentPromptAsync` returns. |
| `RequestedScopes` | `IEnumerable<string>` | Requested scopes. Defaults to an empty collection. |
| `RequireConsent` | `bool` | Whether the user must explicitly approve. |

## Notes

- **PKCE challenge format**: A challenge must be 43 to 128 characters, and each character must be a letter or digit (`char.IsLetterOrDigit`, which also accepts non-ASCII letters), `-`, `.`, `_`, or `~`.
- **Stateless instance**: The class keeps no per-request state. The instance is safe to share, but the repositories it uses are singletons and handle their own synchronization.
- **Validation before grant**: `CreateAuthorizationGrantAsync` trusts its inputs. Always run `ValidateAuthorizationRequestAsync` first, or the grant may be created for a client, redirect URI, or scope that was never checked.
- **Ordering of PKCE checks**: Steps 11 to 13 in `ValidateAuthorizationRequestAsync` run in that order, so a malformed challenge is reported before the missing-PKCE checks.
- **Logging**: Rejected requests log a warning that names the client but not the secret or code values. Accepted requests log at debug level.
