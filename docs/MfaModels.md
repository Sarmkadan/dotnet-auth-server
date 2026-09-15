# Mfa Models

This document describes the Data Transfer Objects (DTOs) used for Multi-Factor Authentication (MFA) operations in the DotnetAuthServer.

## MfaSetupResponse

Returned when TOTP enrollment is initiated, containing the information needed for the user to configure an authenticator app.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `SecretKey` | `string` | Base32-encoded TOTP secret that must be entered into the authenticator app. Display this only once; do not store it in the browser. |
| `ProvisioningUri` | `string` | `otpauth://` URI suitable for rendering as a QR code. Most authenticator apps can scan this URI directly. |
| `BackupCodes` | `IList<string>` | Eight single-use backup codes the user can store offline. Each code is usable exactly once if the authenticator device is unavailable. |

### Remarks
- The `SecretKey` and `ProvisioningUri` should be shown to the user only once during setup.
- The `BackupCodes` should be provided to the user for secure offline storage.

## MfaVerifyRequest

Request model for verifying a TOTP or backup code.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Code` | `string` | Six-digit TOTP code from the authenticator app, or an 8-character backup code (alphanumeric). This field is required. |

### Remarks
- The `Code` property is decorated with the `[Required]` attribute, ensuring validation fails if not provided.

## MfaStatusResponse

Read-only MFA status for a user account.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `IsEnabled` | `bool` | Whether TOTP MFA has been enrolled and confirmed. |
| `EnabledAt` | `DateTime?` | Timestamp when MFA was first enabled (UTC), or null if never enabled. |
| `LastUsedAt` | `DateTime?` | Timestamp of the most recent successful TOTP verification (UTC). |
| `BackupCodesRemaining` | `int` | Number of unused backup codes remaining. |

### Remarks
- All properties are read-only and reflect the current state of MFA for the user.
- `EnabledAt` and `LastUsedAt` are nullable DateTime values representing UTC timestamps.