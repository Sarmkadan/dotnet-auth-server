# WebAuthnCredential

Represents a stored WebAuthn credential associated with a user account. This type holds the cryptographic public key, metadata about the authenticator device, and usage tracking information required for FIDO2/WebAuthn authentication ceremonies. It serves as the persistent record of a registered credential within the authentication server.

## API

### Public Members

**`string Id`**
The unique internal identifier for this credential record within the server's data store. This is distinct from the WebAuthn credential ID and is used for database indexing and relationships.

**`string CredentialId`**
The raw credential identifier as provided by the authenticator during registration, encoded as a Base64Url string. This value is sent to the client during authentication ceremonies to identify which credential should be used.

**`byte[] PublicKey`**
The CBOR-encoded COSE public key bytes stored during credential registration. This key is used to verify the authenticator's signature during authentication assertions. The format corresponds to the algorithm specified in the `Algorithm` property.

**`int Algorithm`**
The COSE algorithm identifier (e.g., -7 for ES256, -257 for RS256) that defines the cryptographic algorithm this credential's public key uses. Must match the algorithm negotiated during the WebAuthn registration ceremony.

**`uint SignatureCounter`**
The current signature counter value as reported by the authenticator. This value is updated after each successful authentication and is used to detect potential authenticator cloning by comparing it against previously stored values.

**`string UserId`**
The user identifier associated with this credential, matching the user handle provided during registration. Links the credential to a specific user account within the system.

**`string FriendlyName`**
A human-readable label assigned by the user or system to distinguish this credential from others (e.g., "My YubiKey 5C" or "Windows Hello"). Useful for credential management interfaces.

**`string AaGuid`**
The Authenticator Attestation GUID identifying the authenticator model. This value is reported during attestation and can be used to determine authenticator capabilities and manufacturer metadata.

**`bool BackupEligible`**
Indicates whether the authenticator that created this credential supports backup (multi-device credential synchronization). Set from the authenticator data flags during registration.

**`bool BackedUp`**
Indicates whether this credential is believed to have been backed up or synchronized to another device. Set from the authenticator data flags during registration and may be updated during subsequent authentications.

**`DateTime CreatedAt`**
The UTC timestamp when this credential was originally registered in the system. Set once at creation time and never modified.

**`DateTime? LastUsedAt`**
The UTC timestamp of the most recent successful authentication using this credential, or `null` if the credential has never been used for authentication since registration.

**`bool IsActive`**
Controls whether this credential can be used for authentication. Inactive credentials are ignored during authentication ceremonies but remain stored for audit purposes. Allows soft-deletion or temporary disabling without data loss.

## Usage

### Registering and Storing a New Credential

```csharp
// After a successful WebAuthn registration ceremony, persist the credential
var credential = new WebAuthnCredential
{
    Id = Guid.NewGuid().ToString("N"),
    CredentialId = Base64Url.Encode(attestationResult.CredentialId),
    PublicKey = attestationResult.PublicKeyCbor,
    Algorithm = attestationResult.CoseAlgorithm,
    SignatureCounter = attestationResult.SignCount,
    UserId = user.Id,
    FriendlyName = "Primary Security Key",
    AaGuid = attestationResult.AaGuid.ToString(),
    BackupEligible = attestationResult.Flags.HasFlag(AuthenticatorFlags.BackupEligible),
    BackedUp = attestationResult.Flags.HasFlag(AuthenticatorFlags.BackedUp),
    CreatedAt = DateTime.UtcNow,
    LastUsedAt = null,
    IsActive = true
};

await credentialRepository.SaveAsync(credential);
```

### Validating Signature Counter During Authentication

```csharp
// During authentication assertion verification, check for counter anomalies
var storedCredential = await credentialRepository.GetByCredentialIdAsync(
    Base64Url.Encode(assertionResult.CredentialId));

if (storedCredential == null || !storedCredential.IsActive)
{
    throw new UnauthorizedAccessException("Credential not found or inactive.");
}

// Zero signature counter indicates the authenticator does not support counters
if (storedCredential.SignatureCounter != 0 &&
    assertionResult.SignCount != 0 &&
    assertionResult.SignCount <= storedCredential.SignatureCounter)
{
    // Possible authenticator cloning detected
    storedCredential.IsActive = false;
    await credentialRepository.UpdateAsync(storedCredential);
    throw new SecurityException("Signature counter anomaly detected.");
}

// Update the stored counter and last-used timestamp
storedCredential.SignatureCounter = assertionResult.SignCount;
storedCredential.LastUsedAt = DateTime.UtcNow;
await credentialRepository.UpdateAsync(storedCredential);
```

## Notes

- The `SignatureCounter` comparison logic must account for authenticators that report a zero counter value, which indicates counter support is absent. Do not flag zero-to-zero transitions as anomalies.
- `PublicKey` is stored as opaque CBOR bytes. Parsing or re-encoding these bytes outside of WebAuthn library routines may break signature verification. Treat them as immutable after storage.
- `BackupEligible` and `BackedUp` reflect the state reported at registration time. A credential's backup status may change over time; relying parties should consider re-evaluating these flags during subsequent authentications if attestation data is available.
- This type is not inherently thread-safe. Concurrent modifications to the same instance (e.g., simultaneous counter updates from parallel authentication requests) must be synchronized externally, typically through database-level concurrency controls or optimistic locking on the persistence layer.
- Setting `IsActive` to `false` does not delete the credential record. Authentication attempts referencing an inactive credential should fail with an appropriate error indicating the credential is disabled, not missing, to avoid leaking existence information.
- The `LastUsedAt` field remains `null` until the first successful authentication. UI surfaces displaying credential activity should handle this null case explicitly.
