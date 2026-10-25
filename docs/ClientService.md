# ClientService

**Namespace:** `DotnetAuthServer.Services`

Public service for managing OAuth 2.0 clients: registration, configuration updates, client secret rotation, activation and deactivation, and client secret validation. The class is `public sealed`. It depends on `IClientRepository` for persistence and `ILogger<ClientService>` for logging.

Client persistence is delegated to `IClientRepository.CreateAsync` and `IClientRepository.UpdateAsync`. In the methods that write to the repository, errors other than `OperationCanceledException` are wrapped in an `AuthServerException` with `server_error` and HTTP status 500. `ValidateClientSecret` is the exception: it logs the error and returns `false`.

Extension methods for this class live in `ClientServiceExtensions.cs` and are not covered here.

## API

| Member | Returns | Purpose |
|---|---|---|
| [`ClientService(...)`](#constructor) | — | Creates the service |
| [`RegisterClientAsync`](#registerclientasync) | `Task<Client>` | Creates and stores a new client |
| [`UpdateClientAsync`](#updateclientasync) | `Task<Client>` | Changes name, redirect URIs, scopes, or CORS origins |
| [`RotateClientSecretAsync`](#rotateclientsecretasync) | `Task<string>` | Issues a new secret for a confidential client |
| [`DeactivateClientAsync`](#deactivateclientasync) | `Task` | Sets `IsActive` to `false` |
| [`ReactivateClientAsync`](#reactivateclientasync) | `Task` | Sets `IsActive` to `true` |
| [`ValidateClientSecret`](#validateclientsecret) | `bool` | Checks a presented secret against the stored hash |

### Constructor

```csharp
public ClientService(IClientRepository clientRepository, ILogger<ClientService> logger)
```

Creates a new instance.

**Parameters**

- `clientRepository`: Repository used to create and update clients.
- `logger`: Logger for informational and error messages.

**Exceptions**

- `ArgumentNullException`: `clientRepository` or `logger` is `null`.

### RegisterClientAsync

```csharp
public async Task<Client> RegisterClientAsync(
    string clientName,
    bool isConfidential,
    IEnumerable<string> redirectUris,
    IEnumerable<string> allowedGrantTypes,
    IEnumerable<string> allowedScopes,
    CancellationToken cancellationToken = default)
```

Creates a client with a generated ID (`client_` followed by 16 hex characters), `IsActive = true`, and the given configuration. The collections are copied into new lists. For a confidential client, a secret is generated and only its SHA-256 hash (Base64) is stored in `ClientSecretHash`. The plaintext secret is not returned, and no other method returns it. The stored secret is therefore not recoverable; use [`RotateClientSecretAsync`](#rotateclientsecretasync) to obtain a new one.

**Parameters**

- `clientName`: Display name. Must not be null, empty, or whitespace.
- `isConfidential`: `true` for a client that holds a secret; `false` for a public client.
- `redirectUris`: At least one URI. Each URI must pass the rules in [Redirect URI rules](#redirect-uri-rules).
- `allowedGrantTypes`: At least one grant type. Values are not checked against a fixed list.
- `allowedScopes`: Scopes the client may request. Each scope must not be null or whitespace. An empty collection is allowed.
- `cancellationToken`: Passed to the repository.

**Returns**

`Task<Client>` resolving to the instance returned by `CreateAsync`.

**Exceptions**

- `ArgumentException`: `clientName` is null, empty, or whitespace.
- `ArgumentNullException`: `redirectUris`, `allowedGrantTypes`, or `allowedScopes` is `null`.
- `AuthServerException` (`invalid_request`, 400):
  - `redirectUris` is empty.
  - `allowedGrantTypes` is empty.
  - A redirect URI fails validation (`Invalid redirect URI: {uri}`).
  - A scope is null or whitespace.
- `AuthServerException` (`server_error`, 500):
  - The built client fails `Client.IsValid()`.
  - The repository throws an exception other than `OperationCanceledException`.

The argument-null and `ArgumentException` checks run before the method's internal `try` block, so those exceptions are not wrapped. The `redirectUris`, `allowedGrantTypes`, and `allowedScopes` sequences are enumerated several times. Pass materialized collections if the source is expensive or single-use.

### UpdateClientAsync

```csharp
public async Task<Client> UpdateClientAsync(
    Client client,
    string? clientName = null,
    IEnumerable<string>? redirectUris = null,
    IEnumerable<string>? allowedScopes = null,
    IEnumerable<string>? corsOrigins = null,
    CancellationToken cancellationToken = default)
```

Applies the non-null arguments to `client`, sets `UpdatedAt` to the current UTC time, and calls `UpdateAsync`. A `null` argument leaves the matching property unchanged.

- `clientName` is applied only if it is not null, empty, or whitespace.
- `redirectUris` is validated in full first. If it is non-null, it replaces `RedirectUris`. An empty collection clears the list; it is not rejected.
- `allowedScopes` replaces `AllowedScopes` without validation. An empty collection clears it.
- `corsOrigins` replaces `AllowedCorsOrigins`.

Because `clientName` is assigned before `redirectUris` is validated, an invalid redirect URI can leave `client.ClientName` changed in memory even though the update fails and is not persisted. Callers that reuse the `Client` instance after an exception should reload it.

**Parameters**

- `client`: The client to update. The instance is modified in place.
- `clientName`: New display name, or `null` to keep the current one.
- `redirectUris`: New redirect URIs, or `null` to keep the current ones.
- `allowedScopes`: New scopes, or `null` to keep the current ones.
- `corsOrigins`: New CORS origins, or `null` to keep the current ones.
- `cancellationToken`: Passed to the repository.

**Returns**

`Task<Client>` resolving to the instance returned by `UpdateAsync`.

**Exceptions**

- `ArgumentNullException`: `client` is `null`.
- `AuthServerException` (`invalid_request`, 400): A redirect URI fails validation (`Invalid redirect URI: {uri}`).
- `AuthServerException` (`server_error`, 500): The repository throws an exception other than `OperationCanceledException`.

### RotateClientSecretAsync

```csharp
public async Task<string> RotateClientSecretAsync(
    Client client,
    CancellationToken cancellationToken = default)
```

Generates a new secret, stores its SHA-256 hash in `client.ClientSecretHash`, sets `UpdatedAt`, and calls `UpdateAsync`. The previous secret stops validating immediately.

The secret is 32 random bytes from `RandomNumberGenerator`, encoded as Base64URL without padding (43 characters). This is the only point at which the plaintext secret is available, so the caller must store or deliver it.

**Parameters**

- `client`: A confidential client. The instance is modified in place.
- `cancellationToken`: Passed to the repository.

**Returns**

`Task<string>` resolving to the new plaintext secret.

**Exceptions**

- `ArgumentNullException`: `client` is `null`.
- `AuthServerException` (`invalid_request`, 400): `client.IsConfidential` is `false` (`Only confidential clients have secrets`).
- `AuthServerException` (`server_error`, 500): The repository throws an exception other than `OperationCanceledException`. In that case the in-memory `ClientSecretHash` has already been changed, and the stored hash is unchanged.

The return value of `UpdateAsync` is not used. The method returns the secret it generated, not the instance the repository returned.

### DeactivateClientAsync

```csharp
public async Task DeactivateClientAsync(
    Client client,
    CancellationToken cancellationToken = default)
```

Sets `client.IsActive` to `false`, sets `UpdatedAt`, and calls `UpdateAsync`. Calling it on an inactive client is allowed and performs another update. This method does not check whether the client is already inactive.

**Parameters**

- `client`: The client to deactivate. The instance is modified in place.
- `cancellationToken`: Passed to the repository.

**Returns**

A `Task` that completes when the update finishes.

**Exceptions**

- `ArgumentNullException`: `client` is `null`.
- `AuthServerException` (`server_error`, 500): The repository throws an exception other than `OperationCanceledException`.

### ReactivateClientAsync

```csharp
public async Task ReactivateClientAsync(
    Client client,
    CancellationToken cancellationToken = default)
```

Sets `client.IsActive` to `true`, sets `UpdatedAt`, and calls `UpdateAsync`. It behaves like `DeactivateClientAsync` and does not check the current state.

**Parameters**

- `client`: The client to reactivate. The instance is modified in place.
- `cancellationToken`: Passed to the repository.

**Returns**

A `Task` that completes when the update finishes.

**Exceptions**

- `ArgumentNullException`: `client` is `null`.
- `AuthServerException` (`server_error`, 500): The repository throws an exception other than `OperationCanceledException`.

### ValidateClientSecret

```csharp
public bool ValidateClientSecret(Client client, string? providedSecret)
```

Checks a presented secret against the client's stored hash. The method is synchronous and does not access the repository.

The result is determined in this order:

1. A public client (`IsConfidential` is `false`) returns `true` regardless of `providedSecret`.
2. A null, empty, or whitespace `providedSecret` returns `false`.
3. Otherwise the method computes the SHA-256 hash of `providedSecret` (UTF-8, Base64) and compares it to `ClientSecretHash` with an ordinal string comparison.

This method does not check `IsActive`. Callers that need to reject inactive clients must check that property separately. The string comparison is not constant-time.

Any exception during hashing or comparison is logged at error level and the method returns `false`.

**Parameters**

- `client`: The client to check.
- `providedSecret`: The secret presented by the caller, or `null`.

**Returns**

`true` if the client is public or the secret matches; otherwise `false`.

**Exceptions**

- `ArgumentNullException`: `client` is `null`.

## Redirect URI rules

`RegisterClientAsync` and `UpdateClientAsync` validate each redirect URI with a private helper. A URI is accepted if it matches any of these rules:

- It starts with `http://localhost` or `http://127.0.0.1`, case-insensitively. The check is a prefix match, so `http://localhost.example.com/callback` is also accepted.
- It starts with `https://`, case-insensitively.
- It contains `://` and does not start with `http://`. This accepts custom schemes such as `myapp://callback`.

Empty or whitespace URIs are rejected. Any other `http://` URI is rejected.

## Usage

```csharp
// Registration
var client = await clientService.RegisterClientAsync(
    clientName: "Billing Portal",
    isConfidential: true,
    redirectUris: new[] { "https://billing.example.com/signin-oidc" },
    allowedGrantTypes: new[] { "authorization_code", "refresh_token" },
    allowedScopes: new[] { "openid", "profile" });

// Rotation: the returned secret is the only copy of the plaintext
var newSecret = await clientService.RotateClientSecretAsync(client);

// Secret check on an incoming request
var isValid = clientService.ValidateClientSecret(client, presentedSecret) && client.IsActive;

// Deactivation
await clientService.DeactivateClientAsync(client);
```
