# SessionManagementController

`SessionManagementController` exposes the session management dashboard as a REST API. It lists active sessions globally and per user, revokes single sessions or all sessions of a user, reports aggregate statistics, and removes expired sessions from storage. All routes are under `api/sessions`. The controller parses HTTP, delegates to `UserSessionService`, and wraps results in `ApiResponse`. Session state changes and expiry rules live in `UserSessionService`.

The class is `public sealed`, derives from `ControllerBase`, and lives in `src/Controllers/SessionManagementController.cs`, namespace `DotnetAuthServer.Controllers`. It is attribute-routed with `[ApiController]`, `[Route("api/sessions")]`, and `[Produces("application/json")]`. It is registered by `app.MapControllers()` in `Program.cs`, and `UserSessionService` is registered as a scoped service there.

The same file also declares the public `SessionSummary` DTO, which is the element type of the list responses described below.

## Public surface

| Method | Route | Action | Purpose |
|---|---|---|---|
| `GET` | `/api/sessions` | `GetAllActiveSessionsAsync` | List all active sessions across all users. |
| `GET` | `/api/sessions/stats` | `GetStatsAsync` | Read aggregate session counts. |
| `GET` | `/api/sessions/users/{userId}` | `GetUserSessionsAsync` | List active sessions for one user. |
| `DELETE` | `/api/sessions/{sessionId}` | `RevokeSessionAsync` | Revoke one session. |
| `DELETE` | `/api/sessions/users/{userId}` | `RevokeAllUserSessionsAsync` | Revoke every active session of one user. |
| `POST` | `/api/sessions/cleanup` | `CleanupExpiredAsync` | Delete expired sessions from storage. |

All responses are `application/json`. The controller sets no authorization attribute. Access control for these routes is not defined in this class.

## Constructor

### `public SessionManagementController(UserSessionService sessionService, ILogger<SessionManagementController> logger)`

Creates the controller. The container injects both parameters.

**Parameters:**
- `sessionService` – required. Performs all session reads, revocations, statistics, and cleanup.
- `logger` – required. Logs unhandled exceptions as errors.

## Response envelope

Responses use `ApiResponse<T>` or `ApiResponse` (`src/Domain/Models/ApiResponse.cs`). Fields the controller sets:

| Field | Set on | Value |
|---|---|---|
| `success` | all | `true` on success, `false` on error. |
| `data` | list and stats responses, bulk revoke, cleanup | Payload. Omitted on single-session revoke and on errors. |
| `message` | success responses | Human-readable text. |
| `error` | error responses | An error code (see [Error handling](#error-handling)). |
| `timestamp` | all | UTC time the response was created. |

The controller never sets `code` or `traceId`. The app's default MVC JSON settings produce camelCase property names. This is not configured anywhere in `Program.cs`.

## Session payload

List responses return `SessionSummary` objects in `data`. Each object has these fields:

| Field | Type | Meaning |
|---|---|---|
| `sessionId` | string | Unique session identifier. |
| `userId` | string | Subject that owns the session. |
| `clientId` | string | OAuth2 client that opened the session. |
| `ipAddress` | string or `null` | Client IP at session creation. |
| `userAgent` | string or `null` | `User-Agent` header at session creation. |
| `grantedScopes` | string | Space-separated granted scopes. |
| `createdAt` | UTC datetime | Session creation time. |
| `expiresAt` | UTC datetime | Session expiry time. |
| `lastActivityAt` | UTC datetime or `null` | Last activity time. |
| `isRevoked` | bool | Whether the session has been revoked. |
| `revocationReason` | string or `null` | Reason passed when the session was revoked. |

## Endpoints

### `GET /api/sessions`

`public async Task<IActionResult> GetAllActiveSessionsAsync(CancellationToken cancellationToken)`

Returns the active sessions of all users. Intended for admin dashboards and security monitoring.

- **Success:** `200 OK`, body `ApiResponse<IEnumerable<SessionSummary>>`, with `data` set to the list. `message` is not set.
- **Failure:** `500` with `error: "server_error"`. See [Error handling](#error-handling).

### `GET /api/sessions/stats`

`public async Task<IActionResult> GetStatsAsync(CancellationToken cancellationToken)`

Returns aggregate counts across all stored sessions.

- **Success:** `200 OK`, body `ApiResponse<SessionStats>`. `data` contains:

  | Field | Meaning |
  |---|---|
  | `totalSessions` | Number of stored sessions. |
  | `activeSessions` | Sessions that are active. |
  | `revokedSessions` | Sessions that have been revoked. |
  | `expiredSessions` | Sessions that are not revoked and whose expiry has passed. |
  | `uniqueUsers` | Distinct user IDs (case-insensitive) across all stored sessions. |
  | `computedAt` | UTC time the statistics were computed. |

- **Failure:** `500` with `error: "server_error"`.

### `GET /api/sessions/users/{userId}`

`public async Task<IActionResult> GetUserSessionsAsync(string userId, CancellationToken cancellationToken)`

Returns the active sessions of one user. The user is not checked. An unknown `userId` returns `200` with an empty `data` list.

- **Route value:** `userId` – target user ID.
- **Success:** `200 OK`, body `ApiResponse<IEnumerable<SessionSummary>>`.
- **Failure:** `500` with `error: "server_error"`, `message` set to the exception message.

### `DELETE /api/sessions/{sessionId}`

`public async Task<IActionResult> RevokeSessionAsync(string sessionId, [FromQuery] string? reason, CancellationToken cancellationToken)`

Revokes one session. The session is marked revoked and kept in storage; it is not deleted.

- **Route value:** `sessionId` – the session to revoke.
- **Query:** `reason` – optional, human-readable. Stored as `revocationReason` and written to the log.
- **Request body:** none.
- **Success:** `200 OK`, body `ApiResponse` (non-generic), with `message` set to `"Session '<sessionId>' revoked"`. `data` is not set.
- **Not found:** `404 Not Found`, body:

  ```json
  {
    "success": false,
    "error": "session_not_found",
    "message": "Session '<sessionId>' not found",
    "timestamp": "..."
  }
  ```

  The `404` is raised by `UserSessionService.RevokeSessionAsync` as an `AuthServerException` and mapped by this action. Note that the `error` value here is `session_not_found`, not the `invalid_request` code carried by the exception.
- **Failure:** `500` with `error: "server_error"`.

### `DELETE /api/sessions/users/{userId}`

`public async Task<IActionResult> RevokeAllUserSessionsAsync(string userId, [FromQuery] string? reason, CancellationToken cancellationToken)`

Revokes every active session of one user. Sessions are marked revoked, not deleted.

- **Route value:** `userId` – target user ID.
- **Query:** `reason` – optional. Applied to every revoked session.
- **Request body:** none.
- **Success:** `200 OK`, body `ApiResponse<object>`:

  ```json
  {
    "success": true,
    "data": { "revokedCount": 2 },
    "message": "Revoked 2 session(s) for user 'alice'",
    "timestamp": "..."
  }
  ```

  A user with no active sessions returns `200` with `revokedCount: 0`. The user is not checked.
- **Failure:** `500` with `error: "server_error"`.

### `POST /api/sessions/cleanup`

`public async Task<IActionResult> CleanupExpiredAsync(CancellationToken cancellationToken)`

Deletes expired sessions from storage to free memory. Sessions that are expired and revoked are included.

- **Request body:** none.
- **Success:** `200 OK`, body `ApiResponse<object>`:

  ```json
  {
    "success": true,
    "data": { "removedCount": 5 },
    "message": "Removed 5 expired session(s)",
    "timestamp": "..."
  }
  ```

- **Failure:** `500` with `error: "server_error"`.

## Error handling

The controller catches exceptions in each action and returns the body shape `ApiResponse` with `success: false`. It does not rely on `ErrorHandlingMiddleware` for these routes.

| Status | `error` | When |
|---|---|---|
| `404` | `session_not_found` | `DELETE /{sessionId}` where the session does not exist. |
| `500` | `server_error` | Any other exception in the service or repository. |

For `500`, `message` is set to the exception's `Message`. Service failures raised as `AuthServerException` carry a generic text such as `"Failed to retrieve active sessions"`, and other exceptions are passed through as-is. The controller logs the full exception at error level.

The `[ApiController]` attribute and the route constraints make the `ArgumentException.ThrowIfNullOrEmpty` checks on `userId` and `sessionId` effectively unreachable for routed requests, since ASP.NET Core does not match an empty segment. Those checks run outside the `try` block. If they ever fire, the exception is handled by `ErrorHandlingMiddleware`, which returns `500` with the OAuth-style `server_error` body, not an `ApiResponse`.

## Behavior notes

- **Revocation is soft.** `DELETE` routes set the revoked flag and the revocation reason; the records remain and are counted in `revokedSessions` until `POST /cleanup` removes expired ones.
- **Unknown IDs and users.** `DELETE /{sessionId}` is the only action that returns `404`. Per-user endpoints return `200` for unknown users with an empty list or `revokedCount: 0`.
- **Lists are not paginated.** `GET /api/sessions` and `GET /api/sessions/users/{userId}` return every matching session in one response.
- **Session data is in memory.** Storage is in-memory only, per the repository design described in `docs/ARCHITECTURE.md`. Sessions are lost on restart.

## Related types

- `UserSessionService` (`src/Services/UserSessionService.cs`) – session operations used by every action.
- `SessionStats` (`src/Services/UserSessionService.cs`) – payload of `GET /stats`.
- `SessionSummary` (`src/Controllers/SessionManagementController.cs`) – payload of the list endpoints.
- `ApiResponse`, `ApiResponse<T>` (`src/Domain/Models/ApiResponse.cs`) – response envelope.
- `AuthServerException` (`src/Exceptions/`) – source of the `404` for single-session revocation.
