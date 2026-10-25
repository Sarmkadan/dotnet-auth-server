# WebAuthnServiceExtensions

The `WebAuthnServiceExtensions` class is a public static class of extension methods on `Microsoft.Extensions.DependencyInjection.IServiceCollection`. Its one method, `AddWebAuthn`, registers the WebAuthn/FIDO2 services. The file also defines `InMemoryWebAuthnCredentialStore`, the default credential store that `AddWebAuthn` registers.

The class lives in the `DotnetAuthServer.Extensions` namespace. Two related types are defined in other files and are not documented here:

- `IWebAuthnCredentialStore` and `IWebAuthnService` are in `src/Services/WebAuthnService.cs` (namespace `DotnetAuthServer.Services`). Their members are described below only where they matter to the in-memory store.
- `WebAuthnService` is the `IWebAuthnService` implementation that `AddWebAuthn` registers.

## API

| Member | Kind | Returns / Type | Lifetime |
|---|---|---|---|
| [`AddWebAuthn`](#addwebauthn) | Extension method on `IServiceCollection` | `IServiceCollection` | n/a |
| [`InMemoryWebAuthnCredentialStore`](#inmemorywebauthncredentialstore) | `public sealed class` implementing `IWebAuthnCredentialStore` | n/a | Singleton, when registered by `AddWebAuthn` |

## AddWebAuthn

```csharp
public static IServiceCollection AddWebAuthn(this IServiceCollection services)
```

Registers the WebAuthn/FIDO2 services in the container:

| Service type | Implementation | Lifetime |
|---|---|---|
| `IWebAuthnCredentialStore` | `InMemoryWebAuthnCredentialStore` | Singleton |
| `IWebAuthnService` | `WebAuthnService` | Scoped |

The method has no options parameter. Every call adds both registrations. It does not check for an existing registration first.

**Parameters**

- `services`: The service collection to configure.

**Returns**

The same `services` instance, so calls can be chained.

**Exceptions**

- `ArgumentNullException`: `services` is `null`.

**Prerequisites**

`AddWebAuthn` does not register the dependencies of `WebAuthnService`. The host must register them before the service is resolved:

- `ICacheService`
- `AuthServerOptions` (`WebAuthnService` reads `IssuerUrl` to derive the relying-party ID, origin, and name)
- `ILogger<WebAuthnService>`

**Replacing the store**

The default store is process-local and is lost on restart. To use persistent storage, register your own `IWebAuthnCredentialStore` after `AddWebAuthn`. The last registration for a service type wins when it is resolved:

```csharp
builder.Services
    .AddWebAuthn()
    .AddSingleton<IWebAuthnCredentialStore, MyDbCredentialStore>(); // your implementation
```

## InMemoryWebAuthnCredentialStore

```csharp
public sealed class InMemoryWebAuthnCredentialStore : IWebAuthnCredentialStore
```

A thread-safe, in-process implementation of `IWebAuthnCredentialStore`. It keeps credentials in a `ConcurrentDictionary<string, WebAuthnCredential>` keyed by `CredentialId` with ordinal comparison. It is suitable for development, tests, and single-node deployments that do not need persistence.

The class has no public members beyond the four interface methods below. It has no delete operation, so a credential is removed from lookups only by setting `IsActive` to `false`.

### Members

| Method | Returns | Throws |
|---|---|---|
| [`FindByCredentialIdAsync`](#findbycredentialidasync) | `Task<WebAuthnCredential?>` | `ArgumentNullException` |
| [`GetByUserIdAsync`](#getbyuseridasync) | `Task<IReadOnlyList<WebAuthnCredential>>` | `ArgumentNullException` |
| [`AddAsync`](#addasync) | `Task` | `ArgumentNullException`, `InvalidOperationException` |
| [`UpdateAsync`](#updateasync) | `Task` | `ArgumentNullException`, `KeyNotFoundException` |

All four methods are non-`async`. They check `cancellationToken` and their arguments before doing any work. Those checks throw synchronously from the call itself, not through the returned `Task`. Callers that use `try`/`await` around the call catch them the same way, but code that stores the task and awaits it later will see the exception at the call site.

### FindByCredentialIdAsync

```csharp
public Task<WebAuthnCredential?> FindByCredentialIdAsync(
    string credentialId,
    CancellationToken cancellationToken = default)
```

Looks up a credential by its authenticator-issued ID.

**Parameters**

- `credentialId`: The Base64URL-encoded credential ID, as stored in `WebAuthnCredential.CredentialId`.
- `cancellationToken`: Checked once before the lookup.

**Returns**

The credential if one exists with that ID and `IsActive` is `true`. Otherwise `null`. An unknown ID is not an error.

**Exceptions**

- `ArgumentNullException`: `credentialId` is `null`.
- `OperationCanceledException`: `cancellationToken` was canceled.

### GetByUserIdAsync

```csharp
public Task<IReadOnlyList<WebAuthnCredential>> GetByUserIdAsync(
    string userId,
    CancellationToken cancellationToken = default)
```

Returns the active credentials registered to a user.

**Parameters**

- `userId`: The owner's user ID, matched with `==` against `WebAuthnCredential.UserId`.
- `cancellationToken`: Checked once before the query.

**Returns**

A new list, built on each call, of the user's credentials where `IsActive` is `true`. The list is ordered by `CreatedAt` ascending. It is empty if the user has no active credentials. It is never `null`. Changes to the returned list do not affect the store.

**Exceptions**

- `ArgumentNullException`: `userId` is `null`.
- `OperationCanceledException`: `cancellationToken` was canceled.

### AddAsync

```csharp
public Task AddAsync(WebAuthnCredential credential, CancellationToken cancellationToken = default)
```

Stores a newly registered credential.

**Parameters**

- `credential`: The credential to store. Its `CredentialId` is the key.
- `cancellationToken`: Checked once before the insert.

**Returns**

A completed `Task`.

**Exceptions**

- `ArgumentNullException`: `credential` is `null`.
- `InvalidOperationException`: A credential with the same `CredentialId` is already in the store. This check covers every user and includes inactive credentials.
- `OperationCanceledException`: `cancellationToken` was canceled.

The store keeps a reference to the passed object, not a copy. Changes made to the object after `AddAsync` are visible to later reads, even without calling `UpdateAsync`.

### UpdateAsync

```csharp
public Task UpdateAsync(WebAuthnCredential credential, CancellationToken cancellationToken = default)
```

Replaces an existing credential, typically to store a new `SignatureCounter` or `LastUsedAt` after an authentication.

**Parameters**

- `credential`: The updated credential. The stored entry is found by `CredentialId`.
- `cancellationToken`: Checked once before the update.

**Returns**

A completed `Task`.

**Exceptions**

- `ArgumentNullException`: `credential` is `null`.
- `KeyNotFoundException`: No credential with that `CredentialId` is in the store.
- `OperationCanceledException`: `cancellationToken` was canceled.

The update does not check `IsActive`. It can update an inactive credential, and it can set `IsActive` back to `true`. The whole stored object is replaced with the passed instance.

## Usage

### Example 1: Registration at startup

```csharp
var builder = WebApplication.CreateBuilder(args);

// Prerequisites that WebAuthnService resolves: ICacheService, AuthServerOptions, ILogger<T>.
// Register them as the host already does, then:
builder.Services.AddWebAuthn();

var app = builder.Build();
```

### Example 2: Disabling a credential

```csharp
public async Task DisableCredentialAsync(
    IWebAuthnCredentialStore store,
    string credentialId,
    CancellationToken ct)
{
    var credential = await store.FindByCredentialIdAsync(credentialId, ct);
    if (credential is null)
        return;

    credential.IsActive = false;
    await store.UpdateAsync(credential, ct);
}
```

Because `FindByCredentialIdAsync` only returns active credentials, this example never reaches `UpdateAsync` for an already-disabled credential. It is a no-op in that case.

## Notes

- **Key format:** The store keys on the Base64URL string `CredentialId`, not on the `Id` field or on raw bytes. Comparison is ordinal and case-sensitive.
- **Soft delete:** There is no remove operation. Setting `IsActive` to `false` and calling `UpdateAsync` hides the credential from `FindByCredentialIdAsync` and `GetByUserIdAsync`. The record stays in memory until the process exits.
- **Shared references:** `AddAsync` and `UpdateAsync` store the instance passed in, and reads return the same instance. Mutating a credential that was returned from the store changes the stored data directly.
- **Persistence:** All data is lost when the process restarts. Replace the store with a database-backed implementation before production use.
- **Thread safety:** The in-memory store is safe for concurrent calls from multiple threads. The interface itself makes no such guarantee. A custom store must document its own concurrency behavior.
- **Lifetimes:** The store is a singleton, so one instance is shared by all scopes. `WebAuthnService` is scoped and gets the same store instance each time.
- **Repeated registration:** Calling `AddWebAuthn` more than once adds duplicate registrations. The last registration for each service type is the one resolved.
