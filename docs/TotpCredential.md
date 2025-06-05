# TotpCredential

Represents a user’s TOTP (Time‑Based One‑Time Password) credential, storing the shared secret, activation state, and usage timestamps. It is intended to be persisted by the authentication server and manipulated through its public members to enable the credential and record successful verifications.

## API

### Id
- **Purpose:** Unique identifier for the credential record.
- **Type:** `string`
- **Parameters:** None.
- **Return value:** The identifier value.
- **Throws:** None.

### UserId
- **Purpose:** Identifier of the user to whom the credential belongs.
- **Type:** `string`
- **Parameters:** None.
- **Return value:** The user identifier.
- **Throws:** None.

### SecretKey
- **Purpose:** Base‑32 encoded secret used to generate and verify TOTP codes.
- **Type:** `string`
- **Parameters:** None.
- **Return value:** The secret key.
- **Throws:** None.

### IsEnabled
- **Purpose:** Indicates whether the credential is currently active for verification.
- **Type:** `bool`
- **Parameters:** None.
- **Return value:** `true` if enabled; otherwise `false`.
- **Throws:** None.

### CreatedAt
- **Purpose:** Timestamp when the credential record was first created.
- **Type:** `DateTime`
- **Parameters:** None.
- **Return value:** Creation time (UTC).
- **Throws:** None.

### EnabledAt
- **Purpose:** Timestamp when the credential was last enabled; `null` if never enabled.
- **Type:** `DateTime?`
- **Parameters:** None.
- **Return value:** Enable time (UTC) or `null`.
- **Throws:** None.

### LastUsedAt
- **Purpose:** Timestamp of the most recent successful verification; `null` if never used.
- **Type:** `DateTime?`
- **Parameters:** None.
- **Return value:** Last use time (UTC) or `null`.
- **Throws:** None.

### BackupCodes
- **Purpose:** List of one‑time backup codes that can be used in place of a TOTP code.
- **Type:** `IList<string>`
- **Parameters:** None.
- **Return value:** The list backing the credential’s backup codes.
- **Throws:** None.

### Enable()
- **Purpose:** Activates the credential for use. Sets `IsEnabled` to `true` and records the current UTC time in `EnabledAt`.
- **Parameters:** None.
- **Return value:** `void`.
- **Throws:** 
  - `InvalidOperationException` if the credential is already enabled.
  - `InvalidOperationException` if `SecretKey` is `null`, empty, or not a valid Base‑32 string.

### RecordVerification()
- **Purpose:** Records that a TOTP verification succeeded, updating `LastUsedAt` to the current UTC time.
- **Parameters:** None.
- **Return value:** `void`.
- **Throws:** 
  - `InvalidOperationException` if the credential is not enabled (`IsEnabled` is `false`).

## Usage

```csharp
// Creating a new TOTP credential for a user
var credential = new TotpCredential
{
    Id = Guid.NewGuid().ToString(),
    UserId = user.Id,
    SecretKey = GenerateBase32Secret(), // helper that returns a valid Base‑32 string
    CreatedAt = DateTime.UtcNow,
    BackupCodes = new List<string> { GenerateBackupCode(), GenerateBackupCode() }
};

// Enable the credential after the user has scanned the QR code
credential.Enable(); // IsEnabled becomes true, EnabledAt set to now
```

```csharp
// Recording a successful verification
if (VerifyTotpCode(credential.SecretKey, code))
{
    credential.RecordVerification(); // LastUsedAt updated to now
    // Optionally consume a backup code if one was used
    if (usedBackupCode)
    {
        credential.BackupCodes.Remove(usedBackupCode);
    }
}
```

## Notes

- The `BackupCodes` list is mutable; external code can add or remove items directly, which will affect the credential’s state. Consumers should treat the list as the source of truth for backup codes.
- Neither `Enable` nor `RecordVerification` provide any synchronization; concurrent calls from multiple threads may result in race conditions (e.g., `EnabledAt` being overwritten). External locking is required if the instance is accessed concurrently.
- `EnabledAt` and `LastUsedAt` are nullable to explicitly represent “never enabled” and “never used” states. After enabling, `EnabledAt` will never revert to `null`.
- The methods throw `InvalidOperationException` only when preconditions are not met (already enabled, not enabled, or invalid secret). They do not throw for other reasons such as I/O failures, as the type is purely in‑memory.
- The `Id` and `UserId` fields are intended to be immutable after construction; changing them may break associations in the data store. The type itself does not enforce immutability, but callers should treat them as such.
