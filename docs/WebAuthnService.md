# WebAuthnService

`WebAuthnService` implements the server side of WebAuthn (FIDO2 Level 2) passwordless login. It runs the two browser ceremonies:

- **Registration** (`navigator.credentials.create()`): the server issues a challenge, verifies the authenticator's attestation response, and stores the new public-key credential.
- **Authentication** (`navigator.credentials.get()`): the server issues a challenge, verifies the signed assertion against the stored public key, and returns the authenticated user's subject.

The class is in `src/Services/WebAuthnService.cs`, namespace `DotnetAuthServer.Services`. It implements `IWebAuthnService`. The file also defines the option records it returns and the `IWebAuthnCredentialStore` persistence contract it depends on.

## Supported algorithms

| COSE alg | Name | Key format stored in `WebAuthnCredential.PublicKey` | Signature check |
|---|---|---|---|
| `-7` | ES256 (ECDSA P-256 / SHA-256) | `x` and `y` coordinates, length-prefixed | DER-encoded `(r, s)`, `Rfc3279DerSequence` |
| `-257` | RS256 (RSASSA-PKCS1-v1_5 / SHA-256) | modulus `n` and exponent `e`, length-prefixed | PKCS#1 v1.5 |

Any other algorithm in the attested credential data is rejected with `InvalidGrantException`.

## Role in the system

`WebAuthnService` answers three questions:

1. **Which options should the browser use to register a passkey or security key?** `GenerateRegistrationOptionsAsync` returns them and stores the challenge.
2. **Is this registration genuine?** `CompleteRegistrationAsync` checks the client data, RP ID, user-presence flag, and public key, then persists a `WebAuthnCredential`.
3. **Is this login genuine?** `CompleteAuthenticationAsync` checks the challenge, the credential, the RP ID, the user-presence flag, the signature counter, and the signature, then returns the subject.

Every failed check throws `InvalidGrantException` (from `DotnetAuthServer.Exceptions`), so the global `ErrorHandlingMiddleware` turns it into an OAuth-style error response. Attestation statements are not verified: the `fmt` field is read and required to be present, but its value is not checked.

## Registration

`IWebAuthnService` and `WebAuthnService` are registered by `AddWebAuthn` in `src/Extensions/WebAuthnServiceExtensions.cs`:

```csharp
services.AddSingleton<IWebAuthnCredentialStore, InMemoryWebAuthnCredentialStore>();
services.AddScoped<IWebAuthnService, WebAuthnService>();
```

The default store is in-memory. Replace it with your own `IWebAuthnCredentialStore` registration to persist credentials. Registering the store after calling `AddWebAuthn` overrides the default.

Callers should depend on `IWebAuthnService`, not on the concrete class.

## Configuration values derived from `AuthServerOptions`

The constructor reads `AuthServerOptions.IssuerUrl` once and derives the Relying Party values from it:

| Value | Derived from | Example for `https://auth.example.com:8443` |
|---|---|---|
| RP ID (`RpId`) | `IssuerUrl` host | `auth.example.com` |
| Origin (checked against `clientDataJSON.origin`) | `IssuerUrl` scheme and authority | `https://auth.example.com:8443` |
| RP name (`RpName`) | `IssuerUrl` host | `auth.example.com` |

The origin comparison is case-insensitive. The RP ID hash in `authenticatorData` must equal `SHA-256(RpId)`.

## Lifetimes and constants

| Constant | Value | Meaning |
|---|---|---|
| Challenge length | 32 bytes | Generated with `RandomNumberGenerator`, encoded as Base64URL |
| Registration challenge TTL | 300 s | Also reported as `TimeoutMs` (300000) |
| Authentication challenge TTL | 180 s | Also reported as `TimeoutMs` (180000) |
| `UserVerification` | `"preferred"` | Returned for both ceremonies |

Challenges are kept in `ICacheService`:

- Registration challenges are keyed by user: `webauthn:reg:{userId}`. Generating new options for the same user replaces the previous challenge.
- Authentication challenges are keyed by the challenge value: `webauthn:auth:{challenge}`. Each call creates a new entry.

A challenge is removed from the cache as soon as its completion method reads it, before any verification. A failed attempt therefore uses up its challenge, and the client must request new options.

## Types

### `WebAuthnPublicKeyParam`

```csharp
public sealed record WebAuthnPublicKeyParam(string Type, int Alg);
```

One entry of `pubKeyCredParams`.

- **`Type`**: Always `"public-key"`.
- **`Alg`**: COSE algorithm identifier, `-7` (ES256) or `-257` (RS256).

### `WebAuthnRegistrationOptions`

```csharp
public sealed record WebAuthnRegistrationOptions(
    string Challenge, string RpId, string RpName,
    string UserId, string Username, string DisplayName,
    IReadOnlyList<WebAuthnPublicKeyParam> PubKeyCredParams,
    int TimeoutMs, string UserVerification);
```

Options for `navigator.credentials.create()`.

- **`Challenge`**: Base64URL challenge, 32 random bytes.
- **`RpId`**, **`RpName`**: Relying Party values (see above).
- **`UserId`**: Base64URL encoding of the UTF-8 bytes of the `userId` argument. This is the opaque user handle.
- **`Username`**, **`DisplayName`**: Shown by the browser UI.
- **`PubKeyCredParams`**: Always ES256 first, then RS256.
- **`TimeoutMs`**: 300000.
- **`UserVerification`**: `"preferred"`.

### `WebAuthnAuthenticationOptions`

```csharp
public sealed record WebAuthnAuthenticationOptions(
    string Challenge, string RpId,
    IReadOnlyList<string> AllowCredentials,
    int TimeoutMs, string UserVerification);
```

Options for `navigator.credentials.get()`.

- **`Challenge`**: Base64URL challenge, 32 random bytes.
- **`RpId`**: Relying Party ID.
- **`AllowCredentials`**: Base64URL credential IDs for the user, as returned by the store. Empty when no `userId` was given (discoverable-credential flow).
- **`TimeoutMs`**: 180000.
- **`UserVerification`**: `"preferred"`.

### `IWebAuthnCredentialStore`

```csharp
public interface IWebAuthnCredentialStore
{
    Task<WebAuthnCredential?> FindByCredentialIdAsync(string credentialId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WebAuthnCredential>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task AddAsync(WebAuthnCredential credential, CancellationToken cancellationToken = default);
    Task UpdateAsync(WebAuthnCredential credential, CancellationToken cancellationToken = default);
}
```

The persistence seam for `WebAuthnCredential` (`src/Domain/Entities/WebAuthnCredential.cs`). The service relies on these contracts:

- `FindByCredentialIdAsync` returns `null` when no credential matches. Credential IDs are the Base64URL strings the service produces.
- `GetByUserIdAsync` returns the user's active credentials.
- `AddAsync` persists a new credential. The service does not check for an existing credential with the same ID first, so the store must handle duplicates.
- `UpdateAsync` persists changes to an existing credential, such as the signature counter and `LastUsedAt`.

### `IWebAuthnService`

Public contract for the two ceremonies. Each method is described below.

## Methods

### `GenerateRegistrationOptionsAsync`

```csharp
Task<WebAuthnRegistrationOptions> GenerateRegistrationOptionsAsync(
    string userId, string username, string displayName,
    CancellationToken cancellationToken = default);
```

Starts a registration ceremony.

- **Parameters**:
  - `userId`: Unique identifier of the user. Used as the challenge key and encoded as the user handle.
  - `username`: Login name, for display only.
  - `displayName`: Full name, for display only.
  - `cancellationToken`: Propagates cancellation to the cache call.
- **Return value**: `WebAuthnRegistrationOptions` to pass to `navigator.credentials.create()`.
- **Side effects**: Stores the challenge under `webauthn:reg:{userId}` for 300 seconds. Replaces any earlier registration challenge for the same user.
- **Throws**:
  - `ArgumentNullException`: If `userId`, `username`, or `displayName` is `null`.
  - `ArgumentException`: If one of them is an empty string.

### `CompleteRegistrationAsync`

```csharp
Task<WebAuthnCredential> CompleteRegistrationAsync(
    string userId, string clientDataJsonB64, string attestationObjectB64,
    string? friendlyName = null, CancellationToken cancellationToken = default);
```

Verifies the attestation response from `navigator.credentials.create()` and stores the new credential.

- **Parameters**:
  - `userId`: The user who started the registration. Must match the `userId` used in `GenerateRegistrationOptionsAsync`.
  - `clientDataJsonB64`: Base64URL `clientDataJSON` from the authenticator.
  - `attestationObjectB64`: Base64URL CBOR `attestationObject` from the authenticator.
  - `friendlyName`: Optional label. Defaults to `"Security Key"`.
  - `cancellationToken`: Propagates cancellation.
- **Return value**: The stored `WebAuthnCredential`. Its fields are set as follows:
  - `CredentialId`: Base64URL of the raw credential ID.
  - `PublicKey`: Packed key bytes (see Supported algorithms).
  - `Algorithm`: `-7` or `-257`.
  - `SignatureCounter`: Counter from `authenticatorData`.
  - `UserId`: The `userId` argument.
  - `FriendlyName`: `friendlyName`, or `"Security Key"`.
  - `AaGuid`: The authenticator AAGUID as a GUID string, or an empty string if it is not 16 bytes.
  - `BackupEligible`: Flag bit `0x08` (BE).
  - `BackedUp`: Flag bit `0x10` (BS).
- **Side effects**: Removes the registration challenge from the cache, then calls `IWebAuthnCredentialStore.AddAsync`.
- **Checks, in order**:
  1. The challenge for `userId` exists and has not expired. It is removed here, even if a later check fails.
  2. `clientDataJSON`: `type` is `"webauthn.create"`, `challenge` matches the stored challenge, and `origin` matches the server origin.
  3. `attestationObject` is a CBOR map with `fmt` and `authData`.
  4. `authenticatorData` is at least 37 bytes, has the `AT` flag set, and includes attested credential data.
  5. The RP ID hash matches `SHA-256(RpId)`.
  6. The user-presence flag (`0x01`) is set.
  7. The COSE key is ES256 or RS256 and has the required fields.
- **Throws**:
  - `ArgumentNullException` / `ArgumentException`: If an argument is `null` or empty.
  - `InvalidGrantException`: If the challenge is missing or expired, if any check above fails, or if the COSE algorithm is unsupported.
  - `FormatException`: If a Base64URL argument cannot be decoded.
  - `JsonException`: If `clientDataJSON` is not valid JSON.
  - `InvalidDataException`: If the CBOR is malformed.

### `GenerateAuthenticationOptionsAsync`

```csharp
Task<WebAuthnAuthenticationOptions> GenerateAuthenticationOptionsAsync(
    string? userId = null, CancellationToken cancellationToken = default);
```

Starts an authentication ceremony.

- **Parameters**:
  - `userId`: Optional. When given, `AllowCredentials` lists that user's credentials. When `null`, the browser may choose any discoverable credential.
  - `cancellationToken`: Propagates cancellation.
- **Return value**: `WebAuthnAuthenticationOptions` to pass to `navigator.credentials.get()`.
- **Side effects**: Stores a new challenge under `webauthn:auth:{challenge}` for 180 seconds. The stored value is `userId`, or an empty string when `userId` is `null`. The store is read for `AllowCredentials` only when `userId` is given.
- **Throws**: None of its own. Exceptions from `IWebAuthnCredentialStore.GetByUserIdAsync` and the cache propagate.

### `CompleteAuthenticationAsync`

```csharp
Task<string> CompleteAuthenticationAsync(
    string credentialId, string clientDataJsonB64, string authenticatorDataB64,
    string signatureB64, string? userHandle = null,
    CancellationToken cancellationToken = default);
```

Verifies an assertion from `navigator.credentials.get()`.

- **Parameters**:
  - `credentialId`: Base64URL credential ID the authenticator selected.
  - `clientDataJsonB64`: Base64URL `clientDataJSON`.
  - `authenticatorDataB64`: Base64URL `authenticatorData`.
  - `signatureB64`: Base64URL assertion signature.
  - `userHandle`: Base64URL user handle from the authenticator, if it returned one.
  - `cancellationToken`: Propagates cancellation.
- **Return value**: The subject. If `userHandle` is given, it is the UTF-8 decoding of the handle. Otherwise it is the `UserId` of the stored credential.
- **Side effects**: On success, sets the credential's `SignatureCounter` and `LastUsedAt` (UTC now), and calls `IWebAuthnCredentialStore.UpdateAsync`. The challenge is removed from the cache before verification, as described above.
- **Checks, in order**:
  1. `clientDataJSON` is parsed and its `challenge` is read. The challenge is looked up under `webauthn:auth:{challenge}` and removed.
  2. `clientDataJSON`: `type` is `"webauthn.get"`, `challenge` matches, and `origin` matches the server origin.
  3. The credential exists (`FindByCredentialIdAsync`) and `IsActive` is `true`.
  4. `authenticatorData` is at least 37 bytes. The RP ID hash matches, and the user-presence flag (`0x01`) is set.
  5. Signature counter: if the received counter is non-zero and is less than or equal to the stored counter, the request fails. A warning is logged. This indicates a possibly cloned authenticator. A counter of zero means the authenticator does not implement a counter, and the check is skipped.
  6. The signature verifies over `authenticatorData || SHA-256(clientDataJSON)` with the stored public key and algorithm.
- **Throws**:
  - `ArgumentNullException` / `ArgumentException`: If a required argument is `null` or empty.
  - `InvalidGrantException`: If the challenge is missing or expired, the credential is unknown or revoked, or any check above fails.
  - `FormatException`: If a Base64URL argument cannot be decoded.
  - `JsonException`: If `clientDataJSON` is not valid JSON.
  - `KeyNotFoundException`: If `clientDataJSON` lacks the `type`, `challenge`, or `origin` property. These errors are not converted to `InvalidGrantException`.
  - `InvalidOperationException`: If one of those properties is not a JSON string.

## Known limitations

These behaviors are visible in the code and are relevant to callers:

- **`userHandle` is not cross-checked.** `CompleteAuthenticationAsync` returns the decoded `userHandle` as the subject without comparing it to the stored credential's `UserId`. The `userId` saved with the authentication challenge is also not checked. Callers that need to bind a login to one user should compare the returned subject with the expected user.
- **No duplicate check on registration.** The service does not look up an existing credential ID before `AddAsync`.
- **Attestation is not verified.** The `fmt` and `attStmt` values are not validated. Any well-formed attestation object with a supported key is accepted.
- **Challenge consumed on failure.** A failed completion uses up its challenge. The client must request new options.
- **Malformed client data surfaces as framework exceptions.** Some malformed-JSON and missing-property cases in `clientDataJSON` throw `JsonException`, `KeyNotFoundException`, or `InvalidOperationException` rather than `InvalidGrantException`. Such errors may not map to an OAuth error response.

## Related types

- `WebAuthnCredential` (`src/Domain/Entities/WebAuthnCredential.cs`): The persisted credential record.
- `InvalidGrantException` (`src/Exceptions/`): Thrown for all protocol failures.
- `ICacheService` (`src/Caching/`): Holds the challenges.
- `AuthServerOptions.IssuerUrl`: Source of the RP ID and origin.
