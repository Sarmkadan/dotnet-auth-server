# TokenRevocationHandler

**Namespace:** `DotnetAuthServer.Handlers`

Implements token revocation for OAuth 2.0 Token Revocation (RFC 7009). Given a token string, it revokes a refresh token by deleting it from the refresh-token repository, or, if the string is not a known refresh token, revokes an access token (JWT) by recording its `jti` in the `RevokedTokenStore` until the token's original expiry. It also offers a bulk operation that revokes all refresh tokens of a user.

Per RFC 7009, `RevokeTokenAsync` never reports a failure for an unknown, expired or malformed token. Such requests return `Success = true, Revoked = false`, so the response does not reveal whether a token existed. Errors are logged, not returned to the caller.

`sealed`. Stateless apart from its constructor-time dependencies. It is registered as scoped in DI (`Program.cs`), so it is normally resolved per request.

---

## Constructor

### `TokenRevocationHandler(IRefreshTokenRepository refreshTokenRepository, IAuthorizationGrantRepository grantRepository, RevokedTokenStore revokedTokenStore, AuthServerOptions options, ILogger<TokenRevocationHandler> logger)`

Stores the dependencies. The constructor performs no null checks.

| Parameter | Type | Description |
|---|---|---|
| `refreshTokenRepository` | `IRefreshTokenRepository` | Finds and deletes refresh tokens, and revokes all refresh tokens of a user. |
| `grantRepository` | `IAuthorizationGrantRepository` | Stored in a field but not used by any method in this class. |
| `revokedTokenStore` | `RevokedTokenStore` | Records revoked access-token `jti` values. |
| `options` | `AuthServerOptions` | Supplies `JwtSigningKey` and `IssuerUrl` for access-token validation. |
| `logger` | `ILogger<TokenRevocationHandler>` | Logger for revocation events. |

---

## Methods

### `Task<RevocationResult> RevokeTokenAsync(string? token, string? tokenTypeHint, CancellationToken cancellationToken = default)`

Revokes a single token. Never throws; any exception is caught and logged.

| Parameter | Type | Description |
|---|---|---|
| `token` | `string?` | The token presented for revocation. |
| `tokenTypeHint` | `string?` | RFC 7009 `token_type_hint`. Accepted but not read: the handler always tries the refresh-token path first, then the access-token path. |
| `cancellationToken` | `CancellationToken` | Passed to the refresh-token repository. |

**Returns:** A `RevocationResult`. On success or on any non-error outcome, `Success` is `true`.

**Behavior, in order:**

1. If `token` is `null`, empty or whitespace, logs a warning and returns `Success = true, Revoked = false`.
2. Computes the SHA-256 hash of the UTF-8 bytes of `token` and encodes it as Base64. This is the same scheme used when refresh tokens are stored.
3. Looks up a refresh token by that hash. If one is found, deletes it with `DeleteAsync` (a hard delete; the record is removed, not marked as revoked) and returns `Success = true, Revoked = true`.
4. Otherwise tries to read the token as a JWT access token (see [Access-token validation](#access-token-validation)). If it yields a non-empty `jti`, calls `RevokedTokenStore.Revoke(jti, expiresAt)` and returns `Success = true, Revoked = true`.
5. Otherwise logs at debug level and returns `Success = true, Revoked = false`.
6. Any exception from steps 2 to 4 is logged at error level and results in `Success = true, Revoked = false`. This includes `OperationCanceledException`: a cancelled request is reported as a successful no-op, not as an error.

**Access-token validation:**

The access-token path validates the JWT with `JwtSecurityTokenHandler.ValidateToken` using these parameters:

| Setting | Value |
|---|---|
| Signing key | HS256 `SymmetricSecurityKey` from the UTF-8 bytes of `options.JwtSigningKey` |
| `ValidateIssuerSigningKey` | `true` |
| `ValidateIssuer` | `true`, expected issuer is `options.IssuerUrl` |
| `ValidateAudience` | `false` |
| `ValidateLifetime` | `false`, so already-expired tokens can still be revoked |

Only tokens signed by this server and issued by `IssuerUrl` are recorded. Malformed or untrusted input is caught and skipped; it does not produce an error.

**Example:**

```csharp
var handler = scope.ServiceProvider.GetRequiredService<TokenRevocationHandler>();
RevocationResult result = await handler.RevokeTokenAsync(token, "refresh_token", cancellationToken);

// RFC 7009: respond 200 OK whatever the result. Use Revoked only for internal metrics or logging.
```

---

### `Task<RevocationResult> RevokeUserTokensAsync(string userId, CancellationToken cancellationToken = default)`

Revokes all refresh tokens of one user. Never throws; repository exceptions are caught and logged.

| Parameter | Type | Description |
|---|---|---|
| `userId` | `string` | Identifier of the user whose refresh tokens should be revoked. |
| `cancellationToken` | `CancellationToken` | Accepted for the signature, but not passed to the repository call. |

**Returns:** A `RevocationResult`.

**Behavior:**

- If `userId` is `null`, empty or whitespace, returns `Success = false, Error = "User ID required"`.
- Otherwise calls `IRefreshTokenRepository.RevokeAllUserTokensAsync(userId, "All tokens revoked for user", ...)`. The repository marks each of the user's refresh tokens as revoked (the records stay in storage). On success, returns `Success = true, Revoked = true`.
- `Revoked` is `true` after a successful call even if the user had no refresh tokens. It does not mean that any token was affected.
- If the repository throws, logs at error level and returns `Success = false, Error = "Internal server error"`.
- Access tokens are not affected. Only refresh tokens are revoked by this method; no `jti` is recorded in `RevokedTokenStore`, so an issued access token stays valid until it expires unless it is revoked individually.

At the time of writing, nothing in `src/` calls this method.

**Example:**

```csharp
var handler = scope.ServiceProvider.GetRequiredService<TokenRevocationHandler>();
RevocationResult result = await handler.RevokeUserTokensAsync(userId);

if (!result.Success)
{
    logger.LogError("Bulk revocation failed for {UserId}: {Error}", userId, result.Error);
}
```

---

## Related Types

### `RevocationResult`

**Namespace:** `DotnetAuthServer.Handlers` (declared in the same file as the handler)

Result model returned by both methods. Mutable POCO with `get; set;` properties.

| Property | Type | Description |
|---|---|---|
| `Success` | `bool` | `true` if the request was processed without a system error. `false` only for `RevokeUserTokensAsync` failures. A token that was not found still gives `true`. |
| `Revoked` | `bool` | `true` if a refresh token was deleted, an access token `jti` was recorded, or (for `RevokeUserTokensAsync`) the bulk revocation completed. |
| `Error` | `string?` | Error message when `Success` is `false`; otherwise `null`. |

The parameterless constructor sets `Success = false`, `Revoked = false` and `Error = null`. The handler always sets `Success` explicitly.

#### `override string ToString()`

Returns a single-line representation, for example `TokenRevocationHandler { Success = True, Revoked = False, Error =  }`. The prefix names the handler, not `RevocationResult`. Intended for logging and debugging; do not rely on its exact format.

---

## Usage Notes

- The handler does not authenticate the caller. RFC 7009 requires the revocation endpoint to authenticate the client. Do that before calling `RevokeTokenAsync`. `TokenController` calls the handler directly (around line 175); `TokenValidator.RevokeTokenAsync` wraps it.
- Revoking a refresh token deletes it. A second call with the same refresh token finds nothing and returns `Revoked = false`.
- Revoking an access token records its `jti` until the token's expiry. The `RevokedTokenStore` lookup is what makes [TokenIntrospectionHandler](TokenIntrospectionHandler.md) report the token as inactive. The handler does not check whether a `jti` is already recorded, so a second call with the same access token returns `Revoked = true` again.
- `tokenTypeHint` is ignored. A refresh token is tried first even when the hint says `access_token`, and vice versa.
- Tokens that are neither a known refresh token nor a valid JWT from this server are reported as `Success = true, Revoked = false`. Clients cannot distinguish these from successful revocations, which is the intended RFC 7009 behavior.
- Logs never contain the token value. Access-token revocations log the `jti`; bulk revocations log the `userId`.

---

## See Also

- [TokenIntrospectionHandler](TokenIntrospectionHandler.md): reads the revocation state this handler writes.
- [RevokedTokenStore](RevokedTokenStore.md): stores revoked access-token `jti` values.
- [IRefreshTokenRepository](IRefreshTokenRepository.md): repository used for refresh-token lookup, deletion and bulk revocation.
- [TokenController](TokenController.md): exposes the revocation endpoint.
- [TokenValidator](TokenValidator.md): wraps this handler for internal use.
- [docs/ARCHITECTURE.md](ARCHITECTURE.md): design overview and known limitations.
