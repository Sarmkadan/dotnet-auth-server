# WebAuthnServiceExtensions

`WebAuthnServiceExtensions` provides extension methods for registering WebAuthn (FIDO2) credential persistence services into the .NET dependency injection container and defines a contract for credential storage operations. It exposes a registration method for the DI pipeline and an asynchronous interface for querying, adding, and updating WebAuthn credentials in a backing store.

## API

### `AddWebAuthn`

```csharp
public static IServiceCollection AddWebAuthn(this IServiceCollection services, Action<WebAuthnOptions> configure)
```

Registers WebAuthn-related services into the service collection. It configures the credential store implementation and any required dependencies based on the provided options delegate.

- **Parameters:**
  - `services` — The `IServiceCollection` to add the services to.
  - `configure` — A delegate that configures `WebAuthnOptions`, specifying the credential store implementation and other WebAuthn settings.
- **Returns:** The same `IServiceCollection` for chaining.
- **Throws:** `ArgumentNullException` if `services` or `configure` is `null`.

### `FindByCredentialIdAsync`

```csharp
public Task<WebAuthnCredential?> FindByCredentialIdAsync(byte[] credentialId, CancellationToken cancellationToken = default)
```

Retrieves a single WebAuthn credential by its raw credential identifier.

- **Parameters:**
  - `credentialId` — The raw byte array representing the credential ID to look up.
  - `cancellationToken` — Optional cancellation token.
- **Returns:** A `Task` that resolves to the matching `WebAuthnCredential`, or `null` if no credential with the given ID exists.
- **Throws:** `ArgumentNullException` if `credentialId` is `null`. `OperationCanceledException` if the token is canceled.

### `GetByUserIdAsync`

```csharp
public Task<IReadOnlyList<WebAuthnCredential>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
```

Retrieves all WebAuthn credentials associated with a specific user.

- **Parameters:**
  - `userId` — The user identifier whose credentials should be returned.
  - `cancellationToken` — Optional cancellation token.
- **Returns:** A `Task` that resolves to a read-only list of `WebAuthnCredential` instances. The list is empty if the user has no registered credentials.
- **Throws:** `ArgumentNullException` if `userId` is `null`. `OperationCanceledException` if the token is canceled.

### `AddAsync`

```csharp
public Task AddAsync(WebAuthnCredential credential, CancellationToken cancellationToken = default)
```

Persists a new WebAuthn credential to the store.

- **Parameters:**
  - `credential` — The `WebAuthnCredential` to store.
  - `cancellationToken` — Optional cancellation token.
- **Returns:** A `Task` that completes when the credential has been stored.
- **Throws:** `ArgumentNullException` if `credential` is `null`. `OperationCanceledException` if the token is canceled. Implementations may throw additional exceptions for duplicate credential IDs or storage failures.

### `UpdateAsync`

```csharp
public Task UpdateAsync(WebAuthnCredential credential, CancellationToken cancellationToken = default)
```

Updates an existing WebAuthn credential in the store, typically to refresh the signature counter or other mutable metadata after an authentication ceremony.

- **Parameters:**
  - `credential` — The `WebAuthnCredential` with updated values.
  - `cancellationToken` — Optional cancellation token.
- **Returns:** A `Task` that completes when the update has been applied.
- **Throws:** `ArgumentNullException` if `credential` is `null`. `OperationCanceledException` if the token is canceled. Implementations may throw if the credential does not already exist in the store.

## Usage

### Example 1: Registration at Startup

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWebAuthn(options =>
{
    options.CredentialStore = new InMemoryWebAuthnStore();
    options.RelyingPartyId = "example.com";
    options.RelyingPartyName = "Example App";
});

var app = builder.Build();
```

### Example 2: Credential Lookup and Update During Authentication

```csharp
public async Task<WebAuthnCredential?> HandleAuthentication(
    IWebAuthnStore store,
    byte[] credentialId,
    uint newSignCount,
    CancellationToken ct)
{
    var credential = await store.FindByCredentialIdAsync(credentialId, ct);
    if (credential is null)
        return null;

    credential.SignCount = newSignCount;
    await store.UpdateAsync(credential, ct);

    return credential;
}
```

## Notes

- **Null handling:** All methods throw `ArgumentNullException` for required reference-type arguments. Callers must guard against null inputs before invocation.
- **Empty results:** `GetByUserIdAsync` returns an empty list, not `null`, when no credentials exist for the given user.
- **Duplicate detection:** `AddAsync` implementations are expected to reject duplicate credential IDs. The exact exception type is implementation-defined.
- **Thread safety:** The interface itself does not guarantee thread safety. Concrete implementations registered via `AddWebAuthn` must document their own concurrency semantics. Callers performing concurrent registrations for the same user should serialize access externally if the backing store is not safe for concurrent writes.
- **Cancellation:** All async methods accept an optional `CancellationToken`. When canceled, they throw `OperationCanceledException`; partial side effects (e.g., a half-written credential) depend on the store implementation’s transactional guarantees.
- **Store lifetime:** The credential store instance configured in `AddWebAuthn` is registered with the DI container and shares the lifetime scope specified by the options or the default registration.
