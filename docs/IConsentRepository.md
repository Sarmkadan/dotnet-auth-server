# IConsentRepository

`IConsentRepository` defines asynchronous persistence and lookup operations for `Consent` entities. It extends `IRepository<Consent, string>`, so consent identifiers are strings and the interface includes the inherited CRUD operations as well as consent-specific queries and revocation operations.

Namespace: `DotnetAuthServer.Data.Repositories`

## Inherited CRUD operations

```csharp
Task<Consent?> GetByIdAsync(
    string id,
    CancellationToken cancellationToken = default);

Task<IEnumerable<Consent>> GetAllAsync(
    CancellationToken cancellationToken = default);

Task<Consent> CreateAsync(
    Consent entity,
    CancellationToken cancellationToken = default);

Task<Consent> UpdateAsync(
    Consent entity,
    CancellationToken cancellationToken = default);

Task DeleteAsync(
    Consent entity,
    CancellationToken cancellationToken = default);

Task DeleteByIdAsync(
    string id,
    CancellationToken cancellationToken = default);

Task<bool> ExistsAsync(
    string id,
    CancellationToken cancellationToken = default);
```

These members are inherited from `IRepository<Consent, string>`:

- `GetByIdAsync` returns the consent with the specified `ConsentId`, or `null` when it is not found.
- `GetAllAsync` returns all stored consent records.
- `CreateAsync` stores a consent and returns the stored entity. The in-memory implementation generates a string identifier when `ConsentId` is null, empty, or whitespace.
- `UpdateAsync` replaces an existing consent and returns the updated entity.
- `DeleteAsync` removes the supplied consent entity.
- `DeleteByIdAsync` removes the consent with the specified identifier.
- `ExistsAsync` reports whether the specified identifier is present.

## Consent-specific operations

### GetByUserIdAsync

```csharp
Task<IEnumerable<Consent>> GetByUserIdAsync(
    string userId,
    CancellationToken cancellationToken = default);
```

Returns all consent records belonging to `userId`. An empty sequence indicates that the user has no stored consents.

### GetByUserAndClientAsync

```csharp
Task<Consent?> GetByUserAndClientAsync(
    string userId,
    string clientId,
    CancellationToken cancellationToken = default);
```

Returns the consent associated with the specified user and OAuth client, or `null` when no matching relationship exists.

### GetByClientIdAsync

```csharp
Task<IEnumerable<Consent>> GetByClientIdAsync(
    string clientId,
    CancellationToken cancellationToken = default);
```

Returns all consent records associated with `clientId`, across all users. An empty sequence indicates that the client has no stored consents.

### RevokeUserConsentsAsync

```csharp
Task<int> RevokeUserConsentsAsync(
    string userId,
    CancellationToken cancellationToken = default);
```

Revokes all consent records belonging to `userId` and returns the number successfully revoked. In the in-memory implementation, revocation removes the matching records from the repository.

### RevokeConsentAsync

```csharp
Task<bool> RevokeConsentAsync(
    string userId,
    string clientId,
    CancellationToken cancellationToken = default);
```

Revokes the consent relationship for the specified user and client. Returns `true` when a matching record was revoked and `false` when no match was found. In the in-memory implementation, revocation removes the record from the repository.

## Usage

```csharp
IConsentRepository repository = new ConsentRepository();

var consent = new Consent
{
    UserId = "user-123",
    ClientId = "reporting-app",
    GrantedScopes = "openid profile",
    Status = ConsentStatus.Approved
};

Consent created = await repository.CreateAsync(consent, cancellationToken);

Consent? stored = await repository.GetByUserAndClientAsync(
    created.UserId,
    created.ClientId,
    cancellationToken);

bool revoked = await repository.RevokeConsentAsync(
    created.UserId,
    created.ClientId,
    cancellationToken);
```

The example requires `DotnetAuthServer.Domain.Entities` and `DotnetAuthServer.Domain.Enums` in addition to the repository namespace.

## Implementation notes

- All methods accept an optional `CancellationToken`. Implementations that perform I/O should observe it; the current in-memory implementation completes synchronously and does not inspect the token.
- The interface does not define validation or exception behavior. The current `ConsentRepository` throws `ArgumentNullException` when `CreateAsync` receives `null`, `ArgumentException` when an update has no valid identifier, and `InvalidOperationException` when the consent being updated does not exist.
- The current in-memory implementation uses exact, case-sensitive comparisons for user IDs, client IDs, and consent IDs.
- The current implementation stores and returns entity references. Callers can therefore mutate an entity after it has been stored; persistent implementations may behave differently.
- Revocation in `ConsentRepository` is a physical removal. It does not call `Consent.Revoke` or retain an expired consent as an audit record.
