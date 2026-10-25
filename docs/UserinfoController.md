# UserinfoController

`UserinfoController` exposes the OpenID Connect UserInfo endpoint at `oauth/userinfo`. A client presents an access token as a Bearer credential and gets back claims about the token's subject, filtered by the token's scopes. The controller parses the token and runs its checks, then delegates user lookup and scope filtering to `UserinfoHandler`.

The class is `public sealed`, derives from `ControllerBase`, and lives in `src/Controllers/UserinfoController.cs`, namespace `DotnetAuthServer.Controllers`. It is attribute-routed with `[ApiController]`, `[Route("oauth/userinfo")]`, and `[Authorize(AuthenticationSchemes = "Bearer")]`.

## Public surface

| Method | Route | Action | Purpose |
|---|---|---|---|
| `GET` | `/oauth/userinfo` | `GetUserinfo` | Return the user's claims for a Bearer access token. |
| `POST` | `/oauth/userinfo` | `GetUserinfo` | Same as `GET`. OIDC allows both verbs. |

The action is declared with `[Consumes]` for `application/x-www-form-urlencoded` and `application/json`, and with `[Produces("application/json")]`. Both verbs share one action and one response contract. The controller has no other public actions.

## Constructor

### `public UserinfoController(UserinfoHandler userinfoHandler, RevokedTokenStore revokedTokenStore, ITokenValidator tokenValidator, ILogger<UserinfoController> logger)`

Creates the controller. The container injects the parameters.

**Parameters:**
- `userinfoHandler` – required. Looks up the user by `sub` and builds the response from the token's scopes.
- `revokedTokenStore` – required. Checks the token's `jti` against revoked tokens.
- `tokenValidator` – required. Stored but not called by the controller (see [Known limitations](#known-limitations)).
- `logger` – required. Receives warnings, info, and errors for each request.

`ArgumentNullException` is thrown if any parameter is `null`.

## Authentication

The `[Authorize]` attribute names the `Bearer` scheme. The request must carry:

```
Authorization: Bearer <access_token>
```

The `Bearer ` prefix is matched case-insensitively. The action then checks the token itself in the order below. The first failing check returns.

1. The `Authorization` header is present and non-blank.
2. The header starts with `Bearer ` (case-insensitive).
3. The token after the prefix is non-blank.
4. The token reads as a JWT. `JwtSecurityTokenHandler.ReadJwtToken` is used for this.
5. The token's `jti` is not in `RevokedTokenStore`. A token with no `jti` skips this check.
6. The token's `token_use` claim is not `id` or `refresh`.
7. The token has a `scope` claim that contains `openid` (case-insensitive).

Only after these checks does the action call `UserinfoHandler.GetUserinfoAsync`.

## Endpoint

### `GET /oauth/userinfo` and `POST /oauth/userinfo`

`public async Task<IActionResult> GetUserinfo(CancellationToken cancellationToken)`

Returns the claims that the token's scopes allow.

**Request:** no body is required. The `Authorization` header carries the access token. `CancellationToken` comes from the request.

**Success:** `200 OK`, body is the `UserinfoResponse` JSON model.

The handler always sets `sub`. It sets the other fields only when the matching scope is in the token:

| Scope | Fields set |
|---|---|
| `openid` | `sub` |
| `profile` | `name`, `givenName`, `familyName`, `updatedAt` (Unix seconds, UTC) |
| `email` | `email`, `emailVerified` |
| `address` | None. The handler has a placeholder and sets nothing. |
| `phone` | None. The handler has a placeholder and sets nothing. |

Fields that were not set are `null`. The model also declares `address`, `phoneNumber`, and `phoneNumberVerified`, but the handler never sets them. The JSON property names come from the default MVC serializer (camelCase). The model has no `JsonPropertyName` attributes.

**Errors:** every error response has the body `{ "error": "<code>", "error_description": "<text>" }`, except the `404`, which has no body.

| Status | `error` | Condition | `WWW-Authenticate` error |
|---|---|---|---|
| `401` | `invalid_request` | `Authorization` header is missing or blank. | `invalid_request` |
| `401` | `invalid_request` | Header does not start with `Bearer `. | `invalid_request` |
| `401` | `invalid_token` | Bearer token is empty or whitespace. | `invalid_token` |
| `401` | `invalid_token` | Token has been revoked (`jti` is in `RevokedTokenStore`). | `invalid_token` |
| `401` | `invalid_token` | Token has `token_use=id`. | `invalid_token` |
| `401` | `invalid_token` | Token has `token_use=refresh`. | `invalid_token` |
| `403` | `invalid_scope` | Token has no `scope` claim. | `insufficient_scope` |
| `403` | `invalid_scope` | Token's `scope` claim does not contain `openid`. | `insufficient_scope` |
| `404` | (none) | The `sub` is missing, or `UserinfoHandler` returns `null` because the user does not exist. | (none) |
| `500` | `server_error` | Any other exception. Body `error_description` is `An unexpected error occurred`. | (none) |

The `WWW-Authenticate` header is set for the `401` and `403` responses above. It is set to a string such as `error="invalid_token", error_description="Token has been revoked"`.

The `401` and `403` responses use a body whose `error` value is `invalid_token` or `invalid_scope`. The `WWW-Authenticate` value uses `insufficient_scope` for the scope failures. The two fields can differ on the same response.

**Exception paths:**

- `SecurityTokenExpiredException` → `401 invalid_token`, `error_description` `Token expired: <message>`.
- `SecurityTokenException` → `401 invalid_token`, `error_description` `Token validation failed: <message>`.
- `AuthServerException` with status `401` or `403` → the exception's own `ErrorCode` and `Message`, with the same status.
- `AuthServerException` with any other status → falls through to the `500` response.
- Any other exception → `500 server_error`.

## Logging

Every rejected request logs a warning that names the failure. A successful request logs at information level with the `sub` and the scope string. Unhandled exceptions are logged at error level. The request logs can include the raw `Authorization` header value in the malformed-header case. The token itself is not logged.

## Known limitations

These come from reading the code. They were not checked by running the host or the test suite.

- **The token signature and expiry are not checked here.** The controller reads the JWT with `ReadJwtToken`, which does not validate the signature, `exp`, `nbf`, `iss`, or `aud`. `ITokenValidator` is injected and assigned, but never called. The `SecurityTokenExpiredException` and `SecurityTokenException` handlers are therefore only reached if something else throws them. A forged or expired token with a known `sub` and an `openid` scope can get a `200` if `UserinfoHandler` finds that user.
- **The repository does not register a `Bearer` authentication scheme.** No `AddAuthentication` or JWT bearer setup appears in `Program.cs` or under `src/`. Whether the `[Authorize(AuthenticationSchemes = "Bearer")]` attribute then rejects or fails before the action runs has not been verified.
- **The malformed-JWT branch is not reached.** `ReadJwtToken` throws `ArgumentException` for a string it cannot parse. That exception is not caught by the `SecurityTokenException` handler, so the request gets `500 server_error` instead of the `401 invalid_token` in the `jwtToken == null` branch.
- **Duplicate claim types can cause a `500`.** The claims are read into a dictionary with `ToDictionary(c => c.Type, ...)`. A token whose claims repeat a type, such as an array-valued `aud`, throws `ArgumentException` on that line and returns `500 server_error`.
- **`WWW-Authenticate` values are not escaped.** Some values are built with string interpolation, including the `AuthServerException` message. A message containing `"` produces a malformed header.
- **Only one `scope` claim is read.** The `openid` check uses `FirstOrDefault` on the `scope` claim. A token with more than one `scope` claim is checked against the first only. The handler's scope extraction reads `FindFirst("scope")` in the same way.

## Related

- `src/Handlers/UserinfoHandler.cs` – user lookup, scope-based claim filtering, and the `UserinfoResponse` model.
- `src/Configuration/Constants.cs` – `ErrorCodes`, `Scopes`, `Claims`, and `ContentTypes` constants used by the controller.
- `docs/UserinfoHandler.md` – handler reference.
- `docs/ARCHITECTURE.md` – design write-up and known limitations.
