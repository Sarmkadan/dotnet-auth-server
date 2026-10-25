# UserManagementController

`UserManagementController` is the administrative REST API for user accounts. It is mounted at `/api/users` and provides list and search, lookup, create, update, delete, role assignment and removal, and account lock and unlock. The controller parses HTTP input, delegates to `UserManagementService`, and wraps results in `ApiResponse` envelopes.

The class is `public sealed`, derives from `ControllerBase`, and lives in `src/Controllers/UserManagementController.cs`, namespace `DotnetAuthServer.Controllers`. It is annotated with `[ApiController]`, `[Route("api/users")]`, and `[Produces("application/json")]`. There are nine public actions, all documented below.

The controller contains no user rules. Validation, uniqueness checks, role logic, and lockout logic live in `UserManagementService` and `UserService`.

## Constructor

### `public UserManagementController(UserManagementService userManagementService, ILogger<UserManagementController> logger)`

Creates the controller. The container injects both parameters. `UserManagementService` is registered as scoped in `Program.cs`.

**Parameters:**
- `userManagementService` - runs the user operations.
- `logger` - records unexpected exceptions (error level). Expected `AuthServerException` failures are not logged here.

## Response shapes

Successful responses wrap their payload in `ApiResponse<T>` (with data) or `ApiResponse` (without data). Both serialize with camelCase keys, since `Program.cs` configures no custom JSON options.

```json
{
  "success": true,
  "data": { "userId": "..." },
  "message": "User created successfully",
  "error": null,
  "code": null,
  "traceId": null,
  "timestamp": "2026-10-25T12:00:00Z"
}
```

Failures that the controller handles itself (`AuthServerException` and unexpected exceptions) use the same envelope with `success: false`, `error` set to the error code, and `message` set to the description.

Two failure paths do not use the envelope. They are described under [Known behaviour](#known-behaviour):
- Automatic model-validation `400` responses, which use `ValidationProblemDetails`.
- Exceptions that escape the controller's `try` blocks, which `ErrorHandlingMiddleware` turns into an OAuth-style `{"error","error_description","error_uri"}` body.

### `UserResponse`

Every user-returning action uses this projection. Password hash and TOTP secret are excluded.

| Field | Type | Notes |
|---|---|---|
| `userId` | `string` | Generated GUID string. |
| `username` | `string` | |
| `email` | `string` | |
| `fullName` | `string?` | |
| `isActive` | `bool` | |
| `emailVerified` | `bool` | |
| `roles` | `string[]` | |
| `attributes` | `object` | ABAC attribute bag. |
| `createdAt` | `DateTime` | UTC. |
| `updatedAt` | `DateTime` | UTC. |
| `lastLoginAt` | `DateTime?` | UTC, `null` if the user never logged in. |
| `isLocked` | `bool` | Result of `User.IsLocked()` at mapping time. |
| `lockedUntil` | `DateTime?` | UTC lock expiry, or `null`. |

## Endpoints

| Method | Route | Action | Success |
|---|---|---|---|
| `GET` | `/api/users?q=` | `GetUsersAsync` | `200` |
| `GET` | `/api/users/{userId}` | `GetUserAsync` | `200` |
| `POST` | `/api/users` | `CreateUserAsync` | `201` |
| `PUT` | `/api/users/{userId}` | `UpdateUserAsync` | `200` |
| `DELETE` | `/api/users/{userId}` | `DeleteUserAsync` | `204` |
| `POST` | `/api/users/{userId}/roles` | `AssignRoleAsync` | `200` |
| `DELETE` | `/api/users/{userId}/roles/{role}` | `RemoveRoleAsync` | `200` |
| `POST` | `/api/users/{userId}/lock?minutes=60` | `LockUserAsync` | `200` |
| `POST` | `/api/users/{userId}/unlock` | `UnlockUserAsync` | `200` |

### `GET /api/users` - list or search

`public async Task<IActionResult> GetUsersAsync([FromQuery] string? q, CancellationToken cancellationToken)`

Returns users matching the optional `q` query parameter. The service matches it as a substring of username, email, or full name.

- **Query:** `q` - search term. See [Known behaviour](#known-behaviour): the parameter is effectively required.
- **`200 OK`:** `ApiResponse<IEnumerable<UserResponse>>`. The data is an array, empty if nothing matches.
- **`500`:** unexpected failure, returned as `ApiResponse` with `error: "server_error"`.

If `q` is whitespace-only (for example `?q=%20`), the service returns all users.

### `GET /api/users/{userId}` - get one user

`public async Task<IActionResult> GetUserAsync(string userId, CancellationToken cancellationToken)`

- **Route:** `userId` - the user's identifier.
- **`200 OK`:** `ApiResponse<UserResponse>`.
- **`404 Not Found`:** `ApiResponse` with `error: "user_not_found"` and `message` `User '<userId>' not found`.
- **`500`:** unexpected failure.

This is the only action that returns the `user_not_found` error code. Other actions return `invalid_request` with status `404` for the same condition.

### `POST /api/users` - create a user

`public async Task<IActionResult> CreateUserAsync([FromBody] CreateUserRequest request, CancellationToken cancellationToken)`

Creates a user account. The request must be `application/json`.

**Request body (`CreateUserRequest`):**

| Field | Required | Rules |
|---|---|---|
| `username` | yes | 3-50 characters. Letters, digits, `.`, `_`, `-`. Must be unique. |
| `email` | yes | Valid email address. Must be unique. |
| `password` | yes | At least 8 characters. Must satisfy the configured password policy. |
| `fullName` | no | Display name. |
| `roles` | no | Initial roles, assigned after creation. Default `[]`. |

The service creates the account with `isActive: true` and then assigns each entry of `roles`.

- **`201 Created`:** `ApiResponse<UserResponse>` with `message: "User created successfully"`. The `Location` header is `/api/users/{userId}`.
- **`400 Bad Request`, model validation:** `ValidationProblemDetails` (see [Known behaviour](#known-behaviour)). Raised when a DataAnnotations attribute fails, such as a too-short username or an invalid email.
- **`400 Bad Request`, service validation:** `ApiResponse` with `error: "invalid_request"`. The `message` names the failed rule. Cases:
  - Username format is invalid.
  - Email format is invalid.
  - The password policy fails. The message joins all failing rules with `; `.
  - Username already exists (`Username already exists`).
  - Email already registered (`Email already registered`).
- **`500`:** unexpected failure. This includes an internal `server_error` raised during creation.

### `PUT /api/users/{userId}` - update a user

`public async Task<IActionResult> UpdateUserAsync(string userId, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)`

Updates mutable fields. Every field in the body is optional, and only the fields that are present are applied:

| Field | Behaviour |
|---|---|
| `fullName` | Applied only when non-empty after trimming. Otherwise the current value is kept. |
| `isActive` | Applied when present, including `false`. |
| `attributes` | Merged into the existing attribute bag. Keys in the request overwrite existing keys; other keys are kept. |

`username`, `email`, and `roles` cannot be changed by this endpoint.

- **`200 OK`:** `ApiResponse<UserResponse>` with `message: "User updated successfully"`.
- **`400`:** model validation failure (`ValidationProblemDetails`).
- **`404`:** `ApiResponse` with `error: "invalid_request"` and `message` `User '<userId>' not found`.
- **`500`:** unexpected failure.

### `DELETE /api/users/{userId}` - delete a user

`public async Task<IActionResult> DeleteUserAsync(string userId, CancellationToken cancellationToken)`

Permanently deletes the account. The service first revokes all of the user's refresh tokens and sessions with the reason `Account deleted`, then deletes the user.

- **`204 No Content`:** no body.
- **`404`:** `ApiResponse` with `error: "invalid_request"`. A second delete of the same user returns this.
- **`500`:** unexpected failure.

### `POST /api/users/{userId}/roles` - assign a role

`public async Task<IActionResult> AssignRoleAsync(string userId, [FromBody] AssignRoleRequest request, CancellationToken cancellationToken)`

**Request body (`AssignRoleRequest`):** `role` (required, 1-100 characters).

Idempotent. If the user already has the role, compared case-insensitively, nothing changes and the call still succeeds.

- **`200 OK`:** `ApiResponse` with `message` `Role '<role>' assigned to user '<userId>'`. No `data`.
- **`400`:** model validation failure (`ValidationProblemDetails`).
- **`404`:** `ApiResponse` with `error: "invalid_request"`.
- **`500`:** unexpected failure, including a storage failure during assignment.

### `DELETE /api/users/{userId}/roles/{role}` - remove a role

`public async Task<IActionResult> RemoveRoleAsync(string userId, string role, CancellationToken cancellationToken)`

Idempotent. Removing a role the user does not have succeeds without error.

- **Route:** `role` - the role name to remove.
- **`200 OK`:** `ApiResponse` with `message` `Role '<role>' removed from user '<userId>'`.
- **`404`:** `ApiResponse` with `error: "invalid_request"`.
- **`500`:** unexpected failure.

### `POST /api/users/{userId}/lock` - lock an account

`public async Task<IActionResult> LockUserAsync(string userId, [FromQuery] int minutes = 60, CancellationToken cancellationToken = default)`

Locks the account until `now + minutes` (UTC). The service also resets the failed-login counter.

- **Query:** `minutes` - lockout length in minutes. Default `60`. A value of `0` or below is replaced with `60`. A non-integer value is rejected by model binding with a `400`.
- **`200 OK`:** `ApiResponse` with `message` `User '<userId>' locked for <minutes> minute(s)`. The message shows the effective value after the `0`-or-below fallback.
- **`404`:** `ApiResponse` with `error: "invalid_request"`.
- **`500`:** unexpected failure.

### `POST /api/users/{userId}/unlock` - unlock an account

`public async Task<IActionResult> UnlockUserAsync(string userId, CancellationToken cancellationToken)`

Clears `lockedUntil` and resets the failed-login counter to zero.

- **`200 OK`:** `ApiResponse` with `message` `User '<userId>' unlocked successfully`.
- **`404`:** `ApiResponse` with `error: "invalid_request"`.
- **`500`:** unexpected failure.

## Known behaviour

These behaviours come from the current source. The controller has not been changed to address them.

- **`GET /api/users` without `q` fails.** The action calls `ArgumentException.ThrowIfNullOrEmpty(q)` before its `try` block. When `q` is missing or empty, that call throws outside the controller's handlers. `ErrorHandlingMiddleware` then returns `500` with `{"error":"server_error","error_description":"An internal server error occurred","error_uri":null}`. The "list all users" branch is reachable only with a whitespace-only `q`.
- **Explicit `ModelState` checks never run.** The controller calls `if (!ModelState.IsValid) return BadRequest(ModelState);`. `[ApiController]` rejects an invalid model before the action body runs, so the response is an automatic `400` with a `ValidationProblemDetails` body. That body has `errors` keyed by property name, not the `ApiResponse` envelope.
- **Two error codes for "user not found".** `GET /api/users/{userId}` returns `user_not_found`. All other actions return `invalid_request` with status `404`. Clients that branch on the code must handle both.
- **Only `AuthServerException` is mapped to its status.** Each action passes `ex.StatusCode` and `ex.ErrorCode` through. `GET /api/users/{userId}` is the exception: it catches only `404` and sends every other status to the generic `500` handler.
- **Account-level errors are not OAuth-shaped.** Errors come back as the `ApiResponse` envelope, not as `{"error","error_description"}`. This differs from the OAuth endpoints, which use `ErrorHandlingMiddleware`'s format.
- **No authorization attribute.** The controller has no `[Authorize]`. Access control for `api/users` depends on the middleware pipeline and any policies configured in `Program.cs`. This document does not assume either.

## Errors summary

| Status | `error` | Source |
|---|---|---|
| `400` | `invalid_request` | Service validation (`AuthServerException`), including password policy and duplicate username or email. |
| `400` | (ProblemDetails) | Automatic model validation by `[ApiController]`. |
| `404` | `user_not_found` | `GET /api/users/{userId}` only. |
| `404` | `invalid_request` | Any other action where the user does not exist. |
| `415` | (none) | Non-JSON `Content-Type` on `[Consumes("application/json")]` actions. |
| `500` | `server_error` | Unexpected exceptions caught by the controller, or escaping it (see [Known behaviour](#known-behaviour)). |

## Related

- `UserManagementService` - business logic for every action (`docs/UserManagementService.md`).
- `CreateUserRequest`, `UpdateUserRequest`, `AssignRoleRequest`, `UserResponse` - request and response models (`docs/UserManagementModels.md`).
- `ApiResponse` - response envelope (`docs/ApiResponse.md`).
- `AuthServerException` - source of the `ErrorCode`, `StatusCode`, and message used in error responses (`docs/AuthServerException.md`).
- `ErrorHandlingMiddleware` - handles exceptions the controller does not catch (`docs/ErrorHandlingMiddleware.md`).
