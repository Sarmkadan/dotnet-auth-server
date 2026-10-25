# ConsentService

**Namespace:** `DotnetAuthServer.Services`

Public service for user consent decisions: recording approvals and denials, checking whether a user has already consented to a set of scopes, computing the scopes actually granted, revoking consent, and reporting when a consent needs renewal. The class is `public sealed`. It depends on `IConsentRepository` for persistence and `AuthServerOptions` for the expiration settings. It has no logger.

The constructor parameter `IConsentRepository` resolves to the interface declared in `DotnetAuthServer.Services`, in the same file as the service. It is not the `DotnetAuthServer.Data.Repositories.IConsentRepository` interface. The two have different members, so check which one your DI registration provides.

Consent records are stored through the repository. `RecordConsentAsync` creates a new record on every call and does not look for an existing record for the same user and client. Revocation goes through `IConsentRepository.RevokeAllUserConsentsAsync` (for all of a user's consents) or `UpdateAsync` (for one client).

Extension methods for this class are not covered here.

## API

| Member | Returns | Purpose |
|---|---|---|
| [`ConsentService(...)`](#constructor) | — | Creates the service |
| [`HasConsentAsync`](#hasconsentasync) | `Task<bool>` | Checks that a valid approval covers all requested scopes |
| [`RecordConsentAsync`](#recordconsentasync) | `Task<Consent>` | Stores an approve or deny decision |
| [`GetEffectiveScopesAsync`](#geteffectivescopesasync) | `Task<IEnumerable<string>>` | Returns the requested scopes that the user has granted |
| [`RevokeAllUserConsentsAsync`](#revokealluserconsentsasync) | `Task` | Revokes every consent a user has granted |
| [`RevokeConsentAsync`](#revokeconsentasync) | `Task` | Revokes the consent for one user and client pair |
| [`RequiresReconsent`](#requiresreconsent) | `bool` | Tells whether a consent is unapproved, expired, or near expiry |
| [`GetConsentRemainingValidity`](#getconsentremainingvalidity) | `TimeSpan?` | Returns the time left before a consent expires |
| [`GetConsentValidityPercentage`](#getconsentvaliditypercentage) | `double?` | Returns the share of the validity period that remains |

### Constructor

```csharp
public ConsentService(IConsentRepository consentRepository, AuthServerOptions authServerOptions)
```

Creates a new instance.

**Parameters**

- `consentRepository`: Repository used to read and write consent records.
- `authServerOptions`: Supplies `SessionConsentExpirationHours` and `ConsentExpirationDays`, which set the expiry of new approvals.

**Exceptions**

- `ArgumentNullException`: `consentRepository` or `authServerOptions` is `null`.

### HasConsentAsync

```csharp
public async Task<bool> HasConsentAsync(
    string userId,
    string clientId,
    IEnumerable<string> requestedScopes,
    CancellationToken cancellationToken = default)
```

Returns `true` only if the user has a consent record for the client that is approved and not expired, and every requested scope is in its granted scopes. Scope matching ignores case. If `requestedScopes` is empty, the result is `true` whenever a valid approval exists.

**Parameters**

- `userId`: The user who gave consent.
- `clientId`: The client the consent is for.
- `requestedScopes`: The scopes the client is asking for.

**Exceptions**

- `ArgumentNullException`: Any argument is `null`.

### RecordConsentAsync

```csharp
public async Task<Consent> RecordConsentAsync(
    ConsentRequest request,
    CancellationToken cancellationToken = default)
```

Creates a consent record from the user's decision and stores it. The record gets a new GUID as its `ConsentId`. The behavior depends on `request.Approved`:

- **Approved.** `GrantedScopes` is set from the request's scopes, and the record is granted with the request's IP address and user agent. If `RememberConsent` is `false`, `ExpiresAt` is set to now plus `SessionConsentExpirationHours`. If it is `true`, `ExpiresAt` is set to now plus `ConsentExpirationDays` and `IsOfflineConsent` is set to `true`.
- **Denied.** `GrantedScopes` is empty, the record is marked rejected with `request.DenialReason`, and `ExpiresAt` is `null`, so a denial does not expire.

**Parameters**

- `request`: The decision. `UserId` and `ClientId` must be non-blank. An approval must also list at least one scope.

**Returns**

The stored `Consent`.

**Exceptions**

- `ArgumentNullException`: `request` is `null`.
- `AuthServerException`: The request is invalid. The error code is `Constants.ErrorCodes.InvalidRequest`, with status 400 and the description `Consent request is invalid`.

### GetEffectiveScopesAsync

```csharp
public async Task<IEnumerable<string>> GetEffectiveScopesAsync(
    string userId,
    string clientId,
    IEnumerable<string> requestedScopes,
    CancellationToken cancellationToken = default)
```

Returns the requested scopes that the user has granted, in the order they were requested. If there is no valid approval, the result is empty. Scope matching ignores case.

The result is produced by a lazy `Where` over `requestedScopes`, so the caller's sequence is read when the result is enumerated. Materialize the result if the source may change in between.

**Parameters**

- `userId`: The user who gave consent.
- `clientId`: The client the consent is for.
- `requestedScopes`: The scopes to filter.

**Exceptions**

- `ArgumentNullException`: Any argument is `null`.

### RevokeAllUserConsentsAsync

```csharp
public async Task RevokeAllUserConsentsAsync(string userId, CancellationToken cancellationToken = default)
```

Revokes every consent granted by the user by delegating to `IConsentRepository.RevokeAllUserConsentsAsync`. Intended for events such as account closure. The repository marks each record revoked and does not delete it.

**Parameters**

- `userId`: The user whose consents are revoked.

**Exceptions**

- `ArgumentNullException`: `userId` is `null`.

### RevokeConsentAsync

```csharp
public async Task RevokeConsentAsync(
    string userId,
    string clientId,
    CancellationToken cancellationToken = default)
```

Revokes the consent for one user and client pair. The record is marked revoked with the reason `User revoked consent` and written back with `UpdateAsync`. If no record exists, the method does nothing and does not throw.

**Parameters**

- `userId`: The user who gave consent.
- `clientId`: The client whose consent is revoked.

**Exceptions**

- `ArgumentNullException`: `userId` or `clientId` is `null`.

### RequiresReconsent

```csharp
public bool RequiresReconsent(Consent consent)
```

Returns `true` if the consent is not in the approved state, has already expired, or has at most 20% of its validity period left (measured from `CreatedAt` to `ExpiresAt`). A consent with no `ExpiresAt` never requires reconsent unless it is not approved. This method is synchronous and does not read the repository.

**Parameters**

- `consent`: The consent to check.

**Exceptions**

- `ArgumentNullException`: `consent` is `null`.

### GetConsentRemainingValidity

```csharp
public TimeSpan? GetConsentRemainingValidity(Consent consent)
```

Returns the time from now until `ExpiresAt`, or `TimeSpan.Zero` if that time has passed. Returns `null` if the consent does not expire.

**Parameters**

- `consent`: The consent to measure.

**Exceptions**

- `ArgumentNullException`: `consent` is `null`.

### GetConsentValidityPercentage

```csharp
public double? GetConsentValidityPercentage(Consent consent)
```

Returns the share of the validity period that remains, clamped to the range 0 to 100. The period runs from `CreatedAt` to `ExpiresAt`. Returns `null` if the consent does not expire, and `0` if the total period is zero or negative.

**Parameters**

- `consent`: The consent to measure.

**Exceptions**

- `ArgumentNullException`: `consent` is `null`.
