# TotpService

`TotpService` implements TOTP multi-factor authentication as defined in RFC 6238 (HMAC-SHA1, RFC 4226 dynamic truncation). It covers enrollment (secret and backup code generation), confirmation, verification of TOTP codes and backup codes, MFA status, and disabling MFA.

The class is `public sealed` and lives in `src/Services/TotpService.cs`, namespace `DotnetAuthServer.Services`. It has no interface; register and inject the concrete type. HMAC and random bytes come from the BCL, so the service adds no NuGet dependencies.

## Role in the system

- Credential state is stored through `ITotpCredentialRepository` (one `TotpCredential` per user).
- The user's `MfaEnabled` flag is kept in step with the credential through `IUserRepository`. A missing user is ignored, so credential operations for unknown IDs still succeed.
- Parameters (digits, time step, drift, backup code count) come from `MfaOptions`. The clock skew tolerance comes from `AuthServerOptions.ClockSkewToleranceSeconds`.
- Codes and secrets are never logged. Log entries carry the user ID only.

## Configuration

| Option | Default | Used for |
|---|---|---|
| `MfaOptions.Issuer` | `"DotnetAuthServer"` | Issuer label in the provisioning URI |
| `MfaOptions.Digits` | `6` | Length of a TOTP code |
| `MfaOptions.TimeStepSeconds` | `30` | Time-step (period) length |
| `MfaOptions.AllowedDriftSteps` | `1` | Default verification window, in steps, each side of the current step |
| `MfaOptions.RecoveryCodeCount` | `8` | Backup codes generated per enrollment |
| `MfaOptions.RecoveryCodeLength` | `10` | Characters per backup code |
| `AuthServerOptions.ClockSkewToleranceSeconds` | `300` | Minimum clock skew window, converted to steps (see [Verification window](#verification-window)) |

## Constructor

### `public TotpService(ITotpCredentialRepository credentialRepository, IUserRepository userRepository, ILogger<TotpService> logger, AuthServerOptions options, MfaOptions mfaOptions)`

Creates the service from its collaborators.

**Parameters:**
- `credentialRepository` – store for `TotpCredential` records.
- `userRepository` – store for `User` records, used to maintain `MfaEnabled`.
- `logger` – logger for enrollment, verification, and replay events.
- `options` – general server options; supplies `ClockSkewToleranceSeconds`.
- `mfaOptions` – MFA parameters (digits, time step, drift, backup codes, issuer).

**Exceptions:** `ArgumentNullException` if any parameter is `null`.

## Enrollment

Enrollment is two-phase. `InitiateSetupAsync` creates a pending credential. `ConfirmSetupAsync` enables it once the user proves possession of the secret.

### `public async Task<MfaSetupResponse> InitiateSetupAsync(string userId, string username, CancellationToken cancellationToken = default)`

Starts enrollment. Generates a 160-bit secret (20 random bytes, Base32-encoded), `RecoveryCodeCount` backup codes, and an `otpauth://` provisioning URI. Persists the credential as pending (`IsEnabled = false`).

**Side effects:**
1. Deletes any existing credential for the user, **including an enabled one**. Starting enrollment on an MFA-protected account turns MFA off until the new setup is confirmed.
2. Sets `User.MfaEnabled` to `false`, if the user exists.
3. Creates the pending `TotpCredential`.

**Parameters:**
- `userId` – the user's ID.
- `username` – account name shown in the authenticator app label.
- `cancellationToken` – cancellation token.

**Return value:** an `MfaSetupResponse` with:
- `SecretKey` – Base32 secret for manual entry. Show it once.
- `ProvisioningUri` – `otpauth://` URI for a QR code.
- `BackupCodes` – plain-text backup codes. Show them once.

**Exceptions:** `ArgumentNullException` / `ArgumentException` if `userId` or `username` is null (or empty).

### `public async Task ConfirmSetupAsync(string userId, string code, CancellationToken cancellationToken = default)`

Confirms enrollment. Checks `code` against the pending credential's secret using the legacy synchronous check (`VerifyTotpCode`, see [Replay protection](#replay-protection)). On success, enables the credential, records the verification time, and sets `User.MfaEnabled` to `true`.

**Parameters:**
- `userId` – the user's ID.
- `code` – the TOTP code shown by the authenticator app.
- `cancellationToken` – cancellation token.

**Exceptions:**
- `ArgumentNullException` / `ArgumentException` if `userId` or `code` is null (or empty).
- `AuthServerException` with `invalid_request` and status `404` if there is no credential for the user. The message tells the caller to start with `/mfa/setup`.
- `AuthServerException` with `invalid_grant` and status `400` if the code is wrong.

## Verification

### `public async Task<bool> VerifyAsync(string userId, string code, CancellationToken cancellationToken = default)`

Verifies a second factor for an enabled credential. The code can be either a TOTP code or a backup code.

**Order of checks:**
1. If there is no credential, or it is not enabled, returns `false`.
2. Normalizes the input: trims whitespace, converts to upper case (invariant), and removes `-`. Compares the result against each unused backup code with a constant-time comparison. On a match, removes that backup code, records the verification, and returns `true`.
3. Otherwise calls `VerifyTotpCodeAsync` (below) with the original input.

**Parameters:**
- `userId` – the user's ID.
- `code` – a TOTP code or a backup code.
- `cancellationToken` – cancellation token.

**Return value:** `true` if the code is valid and MFA passes; otherwise `false`. An unknown user or a disabled credential returns `false` and does not throw.

**Exceptions:** `ArgumentNullException` / `ArgumentException` if `userId` or `code` is null (or empty).

**Notes:**
- A backup code is consumed on use. Once removed, it cannot be used again.
- The backup-code loop stops at the first match, so the position of a match is observable through timing. Each individual comparison is constant-time.

### `public async Task<bool> VerifyTotpCodeAsync(string userId, string base32Secret, string code, int? clockSkewSteps = null)`

Verifies a TOTP code against an explicit Base32 secret. Applies the replay check and the verification window described below. On success, stores the matched time step in the user's credential when one exists.

**Verification steps:**
1. Returns `false` if `code.Length` is not `Digits`, or the code does not parse as an integer. The parse is `int.TryParse`, so it also accepts a leading sign or surrounding whitespace.
2. Computes the current counter: `UnixTimeSeconds / TimeStepSeconds`.
3. Replay check (see [Replay protection](#replay-protection)). If the credential has a `LastAcceptedTimeStep` and the current counter is more than `allowedSteps` before it, returns `false`.
4. Compares the code against the HMAC for each step in the window `[current - w, current + w]`, where `w` is the effective window (see [Verification window](#verification-window)).
5. On a match, if a credential exists, calls `RecordVerification(matchedStep)` and saves it. Returns `true`.

**Parameters:**
- `userId` – the user's ID. Used to find the credential for replay state.
- `base32Secret` – Base32-encoded secret. Not required to belong to a stored credential.
- `code` – the code to check.
- `clockSkewSteps` – steps to check each side of the current step. Defaults to `MfaOptions.AllowedDriftSteps`. It cannot narrow the window below the clock-skew value (see [Verification window](#verification-window)).

**Return value:** `true` if the code is valid and passes the replay check; otherwise `false`.

**Exceptions:** `ArgumentNullException` / `ArgumentException` if `userId`, `base32Secret`, or `code` is null (or empty). Repository exceptions propagate.

### `public bool VerifyTotpCode(string base32Secret, string code, int? windowSteps = null)`

> **Obsolete.** Use `VerifyTotpCodeAsync`, which applies replay prevention and a constant-time comparison. The attribute message says the same thing.

Synchronous check of a TOTP code against a secret. Does not read or write the credential store, so it does not record the matched step. `ConfirmSetupAsync` still calls it, with a `#pragma` suppression.

**Parameters:**
- `base32Secret` – Base32-encoded secret.
- `code` – the code to check.
- `windowSteps` – steps each side of the current step. Defaults to `MfaOptions.AllowedDriftSteps`. Unlike the async method, this value is used exactly, with no clock-skew floor.

**Return value:** `true` if a step in the window matches; otherwise `false`. Returns `false` (does not throw) if either argument is null, empty, or whitespace.

## Status and disabling

### `public async Task<MfaStatusResponse> GetStatusAsync(string userId, CancellationToken cancellationToken = default)`

Returns the MFA state for a user.

**Return value:** an `MfaStatusResponse` with:
- `IsEnabled` – `true` only for a confirmed credential.
- `EnabledAt` – time of confirmation, or `null`.
- `LastUsedAt` – time of the most recent successful verification, or `null`.
- `BackupCodesRemaining` – unused backup codes. Also counts codes on a pending credential.

For an unknown user, returns `IsEnabled = false`, `BackupCodesRemaining = 0`, and does not throw.

**Exceptions:** `ArgumentNullException` / `ArgumentException` if `userId` is null (or empty).

### `public async Task DisableMfaAsync(string userId, CancellationToken cancellationToken = default)`

Deletes the user's TOTP credential, pending or enabled, and sets `User.MfaEnabled` to `false`. Succeeds when there is no credential.

**Exceptions:** `ArgumentNullException` / `ArgumentException` if `userId` is null (or empty).

## Provisioning URI

### `public string BuildProvisioningUri(string secretKey, string username)`

Builds the `otpauth://totp/` URI for QR code generation. This is an instance method. The issuer comes from `MfaOptions.Issuer`, not from a parameter.

Format:

```
otpauth://totp/{issuer}:{username}?secret={secretKey}&issuer={issuer}&algorithm=SHA1&digits={Digits}&period={TimeStepSeconds}
```

The label and the `issuer` value are escaped with `Uri.EscapeDataString`. The Base32 secret is not escaped, since its alphabet is URL-safe.

**Exceptions:** `ArgumentNullException` / `ArgumentException` if `secretKey` or `username` is null (or empty).

## Base32 encoding

Both methods are `public static`. Base32 uses the RFC 4648 alphabet (`A–Z`, `2–7`).

### `public static string EncodeBase32(byte[] data)`

Encodes bytes to upper-case Base32 with no padding. An empty array returns `""`.

**Exceptions:** `ArgumentNullException` if `data` is null.

### `public static byte[] DecodeBase32(string base32)`

Decodes a Base32 string to bytes.

- Strips trailing `=`, converts to upper case (invariant), and ignores any character outside the alphabet. This includes whitespace and hyphens, which are skipped without an error.
- Discards trailing bits that do not form a full byte.
- Does not throw `FormatException`. Malformed input produces different bytes, not an error.

**Exceptions:** `ArgumentNullException` if `base32` is null; `ArgumentException` if it is empty. Input that is only `=` passes the check and returns an empty array.

## Verification window

The window is `w = max(allowedSteps, ClockSkewToleranceSeconds / TimeStepSeconds)`, where `allowedSteps` is the `clockSkewSteps` argument or `AllowedDriftSteps`.

With default settings this is `max(1, 300 / 30) = 10`. The effective default window is **±10 steps (±5 minutes)**, not the ±1 step that `AllowedDriftSteps` suggests. `AllowedDriftSteps` only takes effect when `ClockSkewToleranceSeconds` is lowered (below `TimeStepSeconds × AllowedDriftSteps`). Operators who want a tighter window must lower `ClockSkewToleranceSeconds`.

## Replay protection

Replay protection is **partial**. Read the following before relying on it.

- After a successful `VerifyTotpCodeAsync`, the matched step is stored in `TotpCredential.LastAcceptedTimeStep`.
- The replay check rejects a code only when the current counter is more than `allowedSteps` before the last accepted step. That is, it only fires when the clock moves backwards. It does not reject a code that was already accepted.
- While a step is still inside the window, the same code is accepted again. Accepting an earlier step also lowers `LastAcceptedTimeStep`, which widens what is accepted next.
- `ConfirmSetupAsync` uses the legacy synchronous check. It does not store the step, so the setup code can be reused in the first `VerifyAsync` call within the window.
- `VerifyAsync` calls `RecordVerification()` (no argument) after the step has been stored. That only updates `LastUsedAt`.

Compare with the code comment in `VerifyTotpCodeAsync`, which describes rejecting codes "at or before" the last accepted step. The implementation does not do that.

If the credential repository returns copies rather than shared instances, the step written by `VerifyTotpCodeAsync` can be overwritten by the later `UpdateAsync` in `VerifyAsync`. The in-memory `ConcurrentDictionary` repositories store references, so this does not happen there.

## Errors

| Situation | Exception |
|---|---|
| Null argument | `ArgumentNullException` |
| Empty string argument | `ArgumentException` |
| No credential when confirming setup | `AuthServerException` (`invalid_request`, 404) |
| Wrong code when confirming setup | `AuthServerException` (`invalid_grant`, 400) |

Other methods return `false` or an empty result for missing data and do not throw. See the individual methods above.

## Usage

### Example 1: Enrolling a user

```csharp
// Start enrollment; show the URI as a QR code and the backup codes once
var setup = await totpService.InitiateSetupAsync(userId: "user-42", username: "alice@example.com");
Console.WriteLine(setup.ProvisioningUri);

// Confirm with the 6-digit code from the authenticator app
await totpService.ConfirmSetupAsync(userId: "user-42", code: "123456");
```

### Example 2: Verifying a second factor

```csharp
// Accepts a TOTP code or an unused backup code
bool passed = await totpService.VerifyAsync(userId: "user-42", code: submittedCode);
if (!passed)
{
    // Reject the login
}
```

## See also

- [`TotpCredential`](TotpCredential.md)
- [`MfaSetupResponse`](MfaSetupResponse.md)
- [`ITotpCredentialRepository`](ITotpCredentialRepository.md)
- [`MfaController`](MfaController.md)
