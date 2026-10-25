# ClientValidationService

**Namespace:** `DotnetAuthServer.Services`

Public service that validates OAuth 2.0 client requests during authorization and token flows: client credentials, redirect URIs, requested scopes, and allowed grant types. The class is `public sealed`. It depends on `IClientRepository` for lookups, `ICacheService` for client caching, and `ILogger<ClientValidationService>` for logging.

Every public method reports failures by throwing an `AuthServerException` subclass. The exceptions are `InvalidClientException` (`invalid_client`, 401), `InvalidScopeException` (`invalid_scope`, 400), and `UnauthorizedClientException` (`unauthorized_client`, 403). `ErrorHandlingMiddleware` turns them into OAuth error responses.

Client lookups go through a private cache-aside helper (see [Client lookup and caching](#client-lookup-and-caching)). Only `ValidateClientCredentialsAsync` checks `clientId` for null or whitespace itself. The other three methods pass it to the lookup, which returns `null` for such IDs, and the caller then reports `Client not found`.

## API

| Member | Returns | Purpose |
|---|---|---|
| [`ClientValidationService(...)`](#constructor) | — | Creates the service |
| [`ValidateClientCredentialsAsync`](#validateclientcredentialsasync) | `Task<Client>` | Authenticates a client by ID and, for confidential clients, secret |
| [`ValidateRedirectUriAsync`](#validateredirecturiasync) | `Task` | Checks that a redirect URI is registered for the client |
| [`ValidateScopesAsync`](#validatescopesasync) | `Task` | Checks that every requested scope is allowed for the client |
| [`ValidateGrantTypeAsync`](#validategranttypeasync) | `Task` | Checks that the client may use a grant type |

### Constructor

```csharp
public ClientValidationService(
    IClientRepository clientRepository,
    ICacheService cacheService,
    ILogger<ClientValidationService> logger)
```

Creates a new instance. The constructor only stores its arguments; it does not validate them, so a `null` argument is not reported here and fails later when it is used.

**Parameters**

- `clientRepository`: Source of client records when the cache has no entry.
- `cacheService`: Cache for client records.
- `logger`: Logger for informational and warning messages. Warnings are logged for each rejected request.

### ValidateClientCredentialsAsync

```csharp
public async Task<Client> ValidateClientCredentialsAsync(
    string? clientId,
    string? clientSecret,
    CancellationToken cancellationToken = default)
```

Authenticates a client. The client must exist and have `IsActive` set to `true`. A confidential client (`IsConfidential == true`) must also present a secret. A public client's secret is not checked, even if one is supplied.

**Parameters**

- `clientId`: Client identifier. Required.
- `clientSecret`: Plaintext secret presented by the caller. Required only for confidential clients.
- `cancellationToken`: Passed to the cache and repository.

**Returns**

`Task<Client>` resolving to the client that was found.

**Exceptions**

- `InvalidClientException` (`invalid_client`, 401):
  - `clientId` is null, empty, or whitespace: `client_id is required`.
  - The client does not exist or `IsActive` is `false`: `Client not found or inactive`.
  - The client is confidential and `clientSecret` is null, empty, or whitespace: `Client secret is required`.
  - The client is confidential and the secret does not match: `Invalid client credentials`.

**Secret comparison**

For confidential clients the presented `clientSecret` is compared with `Client.ClientSecretHash` in constant time. The comparison is done on the strings as given. No hashing is applied here, so the value must already be in the same form as `ClientSecretHash`.

`ClientService` stores the SHA-256 (Base64) hash of the generated secret in `ClientSecretHash`. A plaintext secret therefore does not match what `ClientService` stores, and a confidential client created through `ClientService` cannot pass this check as the code stands. Compare this with the behavior described in [`ClientService`](ClientService.md) before relying on this method for confidential clients.

### ValidateRedirectUriAsync

```csharp
public async Task ValidateRedirectUriAsync(
    string? clientId,
    string? redirectUri,
    CancellationToken cancellationToken = default)
```

Checks that `redirectUri` is one of the client's registered `RedirectUris`. The URI is compared with ordinal (case-sensitive) equality, as OAuth 2.0 requires. The method does not normalize the URI.

**Parameters**

- `clientId`: Client identifier. It is passed to the client lookup without a separate check.
- `redirectUri`: Absolute URI to check. Required.
- `cancellationToken`: Passed to the cache and repository.

**Returns**

`Task` that completes when the URI is registered.

**Exceptions**

- `InvalidClientException` (`invalid_client`, 401):
  - `redirectUri` is null, empty, or whitespace: `redirect_uri is required`. This check runs before the client lookup.
  - `redirectUri` is not a valid absolute URI, as checked by `IsValidAbsoluteUri()`: `redirect_uri is not a valid absolute URI`. This check also runs before the client lookup.
  - The client does not exist: `Client not found`.
  - The URI is not in `RedirectUris`: `redirect_uri is not registered`. A warning is logged with the URI and client ID.

### ValidateScopesAsync

```csharp
public async Task ValidateScopesAsync(
    string? clientId,
    IEnumerable<string> requestedScopes,
    CancellationToken cancellationToken = default)
```

Checks that every requested scope is in the client's `AllowedScopes`. Scopes are compared with the default `HashSet<string>` comparer, which is ordinal (case-sensitive). Duplicate requested scopes are collapsed. An empty request passes.

**Parameters**

- `clientId`: Client identifier.
- `requestedScopes`: Scopes to check. Enumerated once to build a set.
- `cancellationToken`: Passed to the cache and repository.

**Returns**

`Task` that completes when all requested scopes are allowed.

**Exceptions**

- `InvalidClientException` (`invalid_client`, 401): The client does not exist (`Client not found`).
- `InvalidScopeException` (`invalid_scope`, 400): One or more scopes are not allowed. The message is `Invalid scopes requested: ` followed by the disallowed scopes separated by spaces. A warning is logged with the same list.
- `ArgumentNullException`: `requestedScopes` is `null`.

### ValidateGrantTypeAsync

```csharp
public async Task ValidateGrantTypeAsync(
    string? clientId,
    string grantType,
    CancellationToken cancellationToken = default)
```

Checks that the client's `AllowedGrantTypes` contains `grantType`. The comparison uses the collection's default equality, which is case-sensitive.

**Parameters**

- `clientId`: Client identifier.
- `grantType`: Grant type to check, such as `authorization_code` or `client_credentials`.
- `cancellationToken`: Passed to the cache and repository.

**Returns**

`Task` that completes when the grant type is allowed.

**Exceptions**

- `InvalidClientException` (`invalid_client`, 401): The client does not exist (`Client not found`).
- `UnauthorizedClientException` (`unauthorized_client`, 403): The grant type is not allowed. The message is `Client is not authorized to use grant type '{grantType}'`. A warning is logged.

## Client lookup and caching

All four public methods use the private `GetClientAsync` helper. Its behavior:

1. If `clientId` is null or whitespace, it returns `null` without touching the cache or repository.
2. It reads the cache under the key `client:{clientId}`. A cached client is returned as-is.
3. Otherwise it calls `IClientRepository.GetByIdAsync`. If a client is found, it is stored in the cache for one hour.

Because a cached client is returned without re-reading the repository, a change to `IsActive`, redirect URIs, scopes, or grant types can take up to one hour to take effect for a client that is already cached.

## Thread safety

The service holds no mutable state beyond its injected dependencies. Concurrent calls are safe as long as the repository and cache implementations are.
