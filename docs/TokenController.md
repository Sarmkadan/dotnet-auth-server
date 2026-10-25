# TokenController

`TokenController` (`src/Controllers/TokenController.cs`, namespace `DotnetAuthServer.Controllers`) exposes the server's form-encoded token endpoints. The class is `public sealed`, derives from `ControllerBase`, and is marked `[ApiController]` with `[Route("oauth/token")]`.

It has three public actions:

| Method | Path | Action | Purpose |
|---|---|---|---|
| `POST` | `/oauth/token` | `RequestToken` | Issues tokens for a supported grant type (RFC 6749). |
| `POST` | `/oauth/token/introspect`, `/oauth/introspect` | `IntrospectToken` | Reports whether a token is active and its claims (RFC 7662). |
| `POST` | `/oauth/token/revoke`, `/oauth/revoke` | `RevokeToken` | Revokes a refresh token or access token (RFC 7009). |

The controller does not contain protocol logic. Token issuance is delegated to `ITokenIssuer`, introspection to `TokenIntrospectionHandler`, and revocation to `TokenRevocationHandler`.

All three actions accept only `application/x-www-form-urlencoded` (`[Consumes]`). Other content types are rejected by ASP.NET Core with `415 Unsupported Media Type`. The responses are `application/json`, except the successful revocation, which has no body.

## Constructor

### `public TokenController(ITokenIssuer tokenIssuer, TokenIntrospectionHandler introspectionHandler, TokenRevocationHandler revocationHandler, ILogger<TokenController> logger)`

Created by the DI container.

- `tokenIssuer` – used by `RequestToken`.
- `introspectionHandler` – used by `IntrospectToken`.
- `revocationHandler` – used by `RevokeToken`.
- `logger` – logs every caught exception at `Error` level.

## Endpoints

### `POST /oauth/token`

`public async Task<IActionResult> RequestToken(...)`

Issues a token. Each parameter is bound with `[FromForm]` and is nullable. The action also takes a `CancellationToken` supplied by ASP.NET Core.

#### Form parameters

| Parameter | Type | Use |
|---|---|---|
| `grant_type` | `string?` | Selects the grant. See [Supported grant types](#supported-grant-types). |
| `client_id` | `string?` | Client identifier. |
| `client_secret` | `string?` | Client secret for confidential clients. |
| `code` | `string?` | Authorization code (`authorization_code` grant). |
| `redirect_uri` | `string?` | Must match the redirect URI used in the authorization request. |
| `refresh_token` | `string?` | Refresh token (`refresh_token` grant). |
| `username` | `string?` | Resource owner name (`password` grant). |
| `password` | `string?` | Resource owner password (`password` grant). |
| `scope` | `string?` | Space-separated requested scopes. |
| `code_verifier` | `string?` | PKCE code verifier (`authorization_code` grant). |

The action copies these fields into a `TokenRequest` and sets `IpAddress` from `HttpContext.Connection.RemoteIpAddress`. It then calls `ITokenIssuer.HandleTokenRequestAsync`. The parameters are not checked for presence in the controller. That is the issuer's job.

#### Supported grant types

`TokenIssuer.HandleTokenRequestAsync` dispatches on `grant_type`:

- `authorization_code` (`Constants.GrantTypes.AuthorizationCode`)
- `refresh_token` (`Constants.GrantTypes.RefreshToken`)
- `client_credentials` (`Constants.GrantTypes.ClientCredentials`)
- `password` (`Constants.GrantTypes.Password`)

Any other value, or a missing `grant_type`, is rejected by the issuer (see [Errors](#errors-on-post-oauthtoken)).

#### Success response

`200 OK` with a `TokenResponse` serialized as JSON. Property names are set by `[JsonPropertyName]`:

```json
{
  "access_token": "...",
  "token_type": "Bearer",
  "expires_in": 3600,
  "refresh_token": "...",
  "scope": "openid profile",
  "id_token": "..."
}
```

`refresh_token`, `scope`, and `id_token` are `null` when the grant does not produce them. Whether they are omitted or sent as `null` depends on the JSON serializer settings and is not set in this controller.

#### Errors on `POST /oauth/token`

Every exception thrown inside `RequestToken` is caught by the `catch (Exception)` block. This includes the exceptions that `TokenIssuer` throws, such as `InvalidGrantException`, `InvalidClientException`, and `AuthServerException` with `unsupported_grant_type`. Because of this, the controller does not let the error reach `ErrorHandlingMiddleware`. Every failure returns:

- **Status:** `500 Internal Server Error`
- **Body:**

```json
{
  "error": "server_error",
  "error_description": "An unexpected error occurred"
}
```

The original error code and message are logged at `Error` level with the message `Unhandled error in token endpoint`, and they are not returned to the client. A client therefore cannot tell `invalid_grant` from `invalid_client`. Only the log shows which one occurred.

Requests that fail model binding or content-type checks before the action runs get the ASP.NET Core response for that case (for example `415`), not this body.

### `POST /oauth/token/introspect` and `POST /oauth/introspect`

`public IActionResult IntrospectToken(...)`

Both routes map to the same action. Introspection is synchronous.

#### Form parameters

| Parameter | Type | Use |
|---|---|---|
| `token` | `string?` | Required. The token to inspect. |
| `token_type_hint` | `string?` | Bound but not used by the action. |
| `client_id` | `string?` | Bound but not used by the action. The caller is not authenticated. |
| `client_secret` | `string?` | Bound but not used by the action. |

#### Responses

- **Missing or whitespace `token`:** `400 Bad Request` with `{"error":"invalid_request","error_description":"token parameter is required"}`.
- **Inactive token** (unknown, expired, or revoked): `200 OK` with `{"active": false}`.
- **Active token:** `200 OK` with:

```json
{
  "active": true,
  "scope": "...",
  "client_id": "...",
  "username": "...",
  "token_type": "...",
  "exp": 1700000000,
  "iat": 1699996400,
  "sub": "..."
}
```

  `exp`, `iat`, and `sub` come from the token's JWT claims. Fields that are not set are serialized as `null`.
- **Unexpected exception:** `500` with `{"error":"server_error","error_description":"An unexpected error occurred"}`.

### `POST /oauth/token/revoke` and `POST /oauth/revoke`

`public async Task<IActionResult> RevokeToken(...)`

Both routes map to the same action.

#### Form parameters

| Parameter | Type | Use |
|---|---|---|
| `token` | `string?` | Required. The refresh token or access token to revoke. |
| `token_type_hint` | `string?` | Passed to the handler. |
| `client_id` | `string?` | Bound but not used by the action. |
| `client_secret` | `string?` | Bound but not used by the action. |

#### Responses

- **Missing or whitespace `token`:** `400 Bad Request` with `{"error":"invalid_request","error_description":"token parameter is required"}`. The action returns this before calling the handler. This differs from RFC 7009, which asks for `200` on invalid tokens. The `200` behavior below applies only to non-empty tokens.
- **Any non-empty token:** `200 OK` with an empty body. The handler returns success whether or not the token exists, to prevent token enumeration.
  - A refresh token is deleted from `IRefreshTokenRepository`.
  - An access token is added to `RevokedTokenStore` by its `jti`, with its expiry. Only a JWT whose `jti` can be read is recorded.
  - Unknown tokens, and tokens whose revocation fails inside the handler, also return `200`.
- **Unexpected exception:** `500` with `{"error":"server_error","error_description":"An unexpected error occurred"}`.

## Notes

- `IntrospectToken` and `RevokeToken` use the same catch-all pattern. Any exception they throw becomes the generic `500` body, and the middleware's `AuthServerException` mapping is not used for these actions either.
- The `client_id` and `client_secret` parameters on the introspection and revocation endpoints are accepted but not checked. Any caller who knows a token can introspect or revoke it.
- These endpoints are listed in `RateLimitingOptions.SensitiveEndpoints` (`/oauth/token`, `/oauth/introspect`, `/oauth/revoke`). The rate limiter runs before the controller, so limits apply to them.
