# MfaController

`MfaController` exposes TOTP multi-factor authentication management as a REST API. It covers enrollment, confirmation, verification, status, and disabling, all nested under `api/users/{userId}/mfa`. The controller parses HTTP, delegates to `TotpService`, and wraps results in `ApiResponse`. Enrollment rules, code checks, and backup-code handling live in `TotpService`. Verification attempts are throttled by `TotpRateLimiter`.

The class is `public sealed`, derives from `ControllerBase`, and lives in `src/Controllers/MfaController.cs`, namespace `DotnetAuthServer.Controllers`. It is attribute-routed with `[ApiController]`, `[Route("api/users/{userId}/mfa")]`, and `[Produces("application/json")]`. The `userId` route value is bound to the `string userId` parameter of every action.

## Public surface

| Method | Route | Action | Purpose |
|---|---|---|---|
| `GET` | `/api/users/{userId}/mfa` | `GetStatusAsync` | Read the MFA state. |
| `POST` | `/api/users/{userId}/mfa/setup` | `SetupAsync` | Start (or restart) TOTP enrollment. |
| `POST` | `/api/users/{userId}/mfa/confirm` | `ConfirmSetupAsync` | Confirm enrollment with a code from the authenticator app. |
| `POST` | `/api/users/{userId}/mfa/verify` | `VerifyAsync` | Check a TOTP or backup code for an enrolled user. |
| `DELETE` | `/api/users/{userId}/mfa` | `DisableMfaAsync` | Remove the TOTP credential. |

All request bodies are `application/json`, and all responses are `application/json`.

## Constructor

### `public MfaController(TotpService totpService, TotpRateLimiter totpRateLimiter, ILogger<MfaController> logger, Data.Repositories.IUserRepository? userRepository = null)`

Creates the controller. The container injects the parameters.

**Parameters:**
- `totpService` – required. Performs all enrollment, verification, status, and disable work.
- `totpRateLimiter` – required. Throttles verification attempts per user.
- `logger` – required. Logs unhandled exceptions as errors.
- `userRepository` – optional. When it is resolved, `POST /setup` checks that the user exists. When it is `null`, the check is skipped and setup proceeds for any `userId`. `Program.cs` registers `IUserRepository`, so the check is active in the running host.

`ArgumentNullException` is thrown if `totpService`, `totpRateLimiter`, or `logger` is `null`.

## Response envelope

Responses use `ApiResponse` (`src/Domain/Models/ApiResponse.cs`) or `ApiResponse<T>`. Fields the controller sets:

| Field | Set on | Value |
|---|---|---|
| `success` | all | `true` on success, `false` on error. |
| `data` | `GET /mfa`, `POST /setup` | Payload (`MfaStatusResponse` or `MfaSetupResponse`). Omitted on the other responses. |
| `message` | success responses | Human-readable text. |
| `error` | error responses | An OAuth-style error code. |
| `timestamp` | all | UTC time the response was created. |

The controller never sets `code` or `traceId`. The app's default MVC JSON settings produce camelCase property names. This is not configured anywhere in `Program.cs`.

## Endpoints

### `GET /api/users/{userId}/mfa`

`public async Task<IActionResult> GetStatusAsync(string userId, CancellationToken cancellationToken)`

Returns the MFA state of a user.

- **Success:** `200 OK`, body `ApiResponse<MfaStatusResponse>`.

  | Field | Meaning |
  |---|---|
  | `isEnabled` | `true` only after confirmation. |
  | `enabledAt` | When MFA was first enabled (UTC), or `null`. |
  | `lastUsedAt` | Last successful verification (UTC), or `null`. |
  | `backupCodesRemaining` | Unused backup codes. `0` when no credential exists. |

- The user is not checked. An unknown `userId` returns `200` with `isEnabled: false`.
- **Failure:** `500` with `error: "server_error"`, `message` set to the exception message. See [Error handling](#error-handling).

### `POST /api/users/{userId}/mfa/setup`

`public async Task<IActionResult> SetupAsync(string userId, CancellationToken cancellationToken)`

Starts TOTP enrollment. It creates a new secret and eight backup codes, stores them as an unconfirmed credential, and returns what the user needs to add the account to an authenticator app. MFA is not active until `POST /confirm` succeeds.

- **Request body:** none.
- **Success:** `200 OK`, body `ApiResponse<MfaSetupResponse>`, with `message` set to `"Scan the QR code or enter the secret key in your authenticator app, then confirm with POST /confirm"`.

  | Field | Meaning |
  |---|---|
  | `secretKey` | Base32 TOTP secret. Shown once; the client should not store it. |
  | `provisioningUri` | `otpauth://` URI for a QR code. |
  | `backupCodes` | Eight single-use recovery codes. The count comes from `MfaOptions.RecoveryCodeCount`. |

- **User check:** when `userRepository` is resolved and the user does not exist, the action returns `404` with `error: "user_not_found"` and `message: "User '<userId>' not found"`.
- The account label in the provisioning URI is the user's `Username`. If the user is missing and no repository is injected, the raw `userId` is used.

> **Warning.** Setup deletes the existing credential before it creates the new one. If the user already has confirmed MFA, calling `/setup` turns MFA off immediately, even if `/confirm` is never called. The user loses the active authenticator until enrollment is confirmed. `TotpService.InitiateSetupAsync` also clears the user's `MfaEnabled` flag.

- **Failure:** `500` with `error: "server_error"`.

### `POST /api/users/{userId}/mfa/confirm`

`public async Task<IActionResult> ConfirmSetupAsync(string userId, [FromBody] MfaVerifyRequest request, CancellationToken cancellationToken)`

Confirms enrollment with the first code from the authenticator app. On success it enables the credential and sets the user's MFA flag.

- **Request body:** `MfaVerifyRequest` – `{ "code": "123456" }`. `code` is required. The TOTP length is `MfaOptions.Digits` (6 or 8). The controller does not check the length itself.
- **Success:** `200 OK`, body `ApiResponse` with `message: "MFA has been enabled for your account"`. The `data` field is absent.
- **Failures:**

  | Status | `error` | When |
  |---|---|---|
  | `400` | framework `ValidationProblemDetails` | `code` is missing. `[ApiController]` runs model validation before the action body. |
  | `400` | `invalid_grant` | The code does not match the pending secret. `message` is `"Invalid TOTP code; please try again"`. |
  | `404` | `invalid_request` | No pending credential exists for the user. `message` is `"No pending TOTP setup found for this user. Call /mfa/setup first."` |
  | `500` | `server_error` | Any other exception. |

Errors thrown as `AuthServerException` are returned with the status code the exception carries and its `ErrorCode` as `error`. This is done explicitly in this action.

Confirming does not check whether MFA is already enabled. A valid code on an active credential rewrites it.

### `POST /api/users/{userId}/mfa/verify`

`public async Task<IActionResult> VerifyAsync(string userId, [FromBody] MfaVerifyRequest request, CancellationToken cancellationToken)`

Checks a TOTP code or a backup code for an enrolled user. This is the endpoint a login flow calls after a password check.

- **Request body:** `MfaVerifyRequest` – `{ "code": "123456" }`. The value may be a TOTP code or a recovery code. Backup codes are checked first. A matched backup code is removed on use.
- **Order of checks:**
  1. The attempt limit is checked before the code is looked at. `TotpRateLimiter.ThrowIfBlocked` is called with `userId`. Its limits come from `TotpAttemptsPerWindow` (default 5) and `TotpRateLimitWindowSeconds` (default 30).
  2. The code is verified with `TotpService.VerifyAsync`.
  3. On a failed code, a failure is recorded against the limiter.
  4. On success, the limiter's counter for the user is cleared.

- **Success:** `200 OK`, body `ApiResponse` with `message: "MFA verification successful"`.
- **Invalid code, or no enabled credential:** `401 Unauthorized`, body with `error: "invalid_grant"` and `message: "Invalid or expired MFA code"`. An unknown user gets the same response.
- **Failure:** see [Known issues](#known-issues) for the rate-limit case.

### `DELETE /api/users/{userId}/mfa`

`public async Task<IActionResult> DisableMfaAsync(string userId, CancellationToken cancellationToken)`

Removes the TOTP credential and backup codes, and clears the user's `MfaEnabled` flag.

- **Request body:** none.
- **Success:** `200 OK`, body `ApiResponse` with `message: "MFA has been disabled for user '<userId>'"`.
- The operation is idempotent. It returns `200` even when no credential existed or the user is unknown.
- **Failure:** `500` with `error: "server_error"`.

## Error handling

The controller does not use `ErrorHandlingMiddleware` for these responses. Each action builds its own error body with `ApiResponse.ErrorResponse`. Only `ConfirmSetupAsync` maps `AuthServerException` to its own status code. The other actions return `500` for every exception.

Those `500` responses include `ex.Message` in `message`. Internal exception text reaches the client.

## Known issues

These follow from the current source. This document describes them and does not fix them.

1. **Rate-limit responses come back as `500`.** `TotpRateLimiter.ThrowIfBlocked` throws `AuthServerException` with status `429` and `error: "too_many_requests"`. It is called inside the `try` block of `VerifyAsync`, and that action catches only `Exception`. A blocked user therefore receives `500 server_error` with `message` `"Too many TOTP attempts. Please try again in N seconds."`, not `429`. Clients that check for `429` or `Retry-After` will not see it. The fix is to add an `AuthServerException` catch that returns `ex.StatusCode`, as `ConfirmSetupAsync` does.

2. **Setup can disable active MFA.** See the warning under `POST /setup`.

3. **No authentication or authorization on the controller.** The class has no `[Authorize]` attribute, and `Program.cs` configures no fallback policy. Anyone who can reach the host can enroll, confirm, verify, or disable MFA for any `userId`. Deployments must restrict these routes at the gateway or add an authorization policy. The `verify` endpoint is designed to be called by trusted server-side code, not by end users directly.

4. **`GET /mfa` and `DELETE /mfa` do not check that the user exists.** They return success for unknown IDs.

5. **Internal error messages are returned to clients.** The `500` responses include `ex.Message`.

## Related types

- `MfaVerifyRequest` (`src/Domain/Models/MfaModels.cs`) – `Code` is `[Required]`.
- `MfaSetupResponse`, `MfaStatusResponse` – see the endpoint sections for fields.
- `TotpService` (`src/Services/TotpService.cs`) – enrollment, verification, and status logic.
- `TotpRateLimiter` (`src/Security/TotpRateLimiter.cs`) – per-user sliding-window attempt limit.
- `MfaOptions` – `Digits`, `TimeStepSeconds`, `AllowedDriftSteps`, `RecoveryCodeCount`, `RecoveryCodeLength`. Registered by `AddMfaOptions` in `Program.cs`.
- `MfaControllerValidation` (`src/Controllers/MfaControllerValidation.cs`) – checks the controller's injected dependencies by reflection. It does not run during requests.
