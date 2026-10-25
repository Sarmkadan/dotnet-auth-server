# TokenValidator

`TokenValidator` is the service that validates OAuth 2.0 / OpenID Connect tokens on behalf of the rest of the application. It is a thin facade over two protocol handlers:

- **Introspection** (`TokenIntrospectionHandler`, RFC 7662) decides whether a signed access token (JWT) is currently active and returns its claims.
- **Revocation** (`TokenRevocationHandler`, RFC 7009) invalidates a refresh token or an access token.

The class implements `ITokenValidator` and lives in `src/Services/TokenValidator.cs`, namespace `DotnetAuthServer.Services`. It adds logging around the handler calls and gives callers a single seam to depend on.

## Role in the system

`TokenValidator` answers two questions:

1. **Is this access token valid right now?** A token is active only if its JWT signature verifies against `JwtSigningKey` (HS256 by default), its issuer matches `IssuerUrl`, its lifetime is current (5 seconds of clock skew allowed), and its `jti` has not been revoked.
2. **Make this token unusable.** Revocation removes a refresh token from storage, or records an access token's `jti` in `RevokedTokenStore` until the token's original expiry.

Refresh tokens are not validated by the introspection path. The introspection handler only reports a token as active when it is a valid signed JWT. A refresh token that is not itself a current, signed JWT therefore comes back as `Active = false` from `IntrospectToken` or `ValidateTokenAsync`. Refresh tokens are handled by revocation, which looks them up by a SHA-256 hash of the token in `IRefreshTokenRepository` before trying the access-token path.

Validation never throws for a bad token. An invalid, expired, malformed, or revoked token yields an `IntrospectionResponse` with `Active = false`.

## Registration

`TokenValidator` is registered as a scoped service in `Program.cs`:

```csharp
builder.Services.AddScoped<ITokenValidator, TokenValidator>();
```

Callers should depend on `ITokenValidator`, not on the concrete class.

## Constructor

### `public TokenValidator(TokenRevocationHandler revocationHandler, TokenIntrospectionHandler introspectionHandler, ILogger<TokenValidator> logger)`

Creates a validator from its collaborators.

- **Parameters**:
  - `revocationHandler`: Handles RFC 7009 revocation.
  - `introspectionHandler`: Handles RFC 7662 introspection and JWT validation.
  - `logger`: Logger for validation, introspection, and revocation activity.
- **Throws**:
  - `ArgumentNullException`: If any parameter is `null`.

## Methods

### `public async Task<IntrospectionResponse> ValidateTokenAsync(string token, string? tokenTypeHint = null)`

Validates an access token and returns its introspection result.

- **Parameters**:
  - `token`: The token to validate.
  - `tokenTypeHint`: Optional hint such as `access_token` or `refresh_token`, as defined by RFC 7009/7662. It is written to the log only. It does not change how the token is validated.
- **Return value**: A `Task<IntrospectionResponse>`. `Active` is `true` only for a valid, unexpired, non-revoked JWT. Otherwise `Active` is `false` and the other fields are `null`.
- **Throws**: None for invalid input. A `null` or whitespace token returns `Active = false`.

This method is `async`, but it does not await anything. It delegates to the synchronous `IntrospectToken` path.

### `public IntrospectionResponse IntrospectToken(string token)`

Introspects a token and returns its claims and validity (RFC 7662).

- **Parameters**:
  - `token`: The token to introspect.
- **Return value**: An `IntrospectionResponse`. See [IntrospectionResponse](#introspectionresponse) for the fields populated when `Active` is `true`.
- **Throws**: None. Errors are logged and reported as `Active = false`.

Validation rules applied by the handler:

- Signature must verify against the `JwtSigningKey` symmetric key (HMAC-SHA256).
- Issuer must equal `AuthServerOptions.IssuerUrl`.
- Lifetime must be current, with a 5-second clock skew.
- Audience is **not** checked, because it varies per client.
- A token whose `jti` is in `RevokedTokenStore` is inactive.

### `public async Task<RevocationResult> RevokeTokenAsync(string token, string? tokenTypeHint, CancellationToken cancellationToken)`

Revokes a token per RFC 7009.

- **Parameters**:
  - `token`: The token to revoke.
  - `tokenTypeHint`: Optional hint, written to the log. The handler tries the refresh-token lookup first regardless of the hint.
  - `cancellationToken`: Passed through to the refresh-token repository.
- **Return value**: A `Task<RevocationResult>`.
  - If the token is a stored refresh token, it is deleted. Result: `Success = true, Revoked = true`.
  - Otherwise, if the token is a JWT with a `jti`, that `jti` is added to `RevokedTokenStore` with the token's expiry. Result: `Success = true, Revoked = true`.
  - If neither applies, the result is `Success = true, Revoked = false`. This is intentional: RFC 7009 requires a 200 response for unknown tokens so callers cannot probe for valid tokens.
- **Throws**: Any exception that escapes the handler is logged and rethrown. In practice the handler catches its own exceptions and reports success, so the rethrow is rarely reached. See [Known limitations](#known-limitations).

## Types

### IntrospectionResponse

Defined in `src/Handlers/TokenIntrospectionHandler.cs`. Mirrors the RFC 7662 response shape.

| Property | Type | Description |
|---|---|---|
| `Active` | `bool` | `true` when the token is valid, unexpired, and not revoked. |
| `Scope` | `string?` | Space-separated `scope` claim. |
| `ClientId` | `string?` | Value of the `aud` claim. The token's audience is the client it was issued to. |
| `Username` | `string?` | Value of the `sub` claim. |
| `TokenType` | `string?` | Always `Bearer` for active tokens. |
| `Exp` | `long?` | Expiry, in seconds since the Unix epoch. |
| `Iat` | `long?` | Issue time, in seconds since the Unix epoch. |
| `Sub` | `string?` | Value of the `sub` claim. |

### RevocationResult

Defined in `src/Handlers/TokenRevocationHandler.cs`.

| Property | Type | Description |
|---|---|---|
| `Success` | `bool` | `true` when the operation completed without an authentication error. |
| `Revoked` | `bool` | `true` when a token was actually found and revoked. |
| `Error` | `string?` | Error message when the operation failed. |

## Usage

### Checking an access token

```csharp
public sealed class ProfileService
{
    private readonly ITokenValidator _tokenValidator;

    public ProfileService(ITokenValidator tokenValidator)
    {
        _tokenValidator = tokenValidator;
    }

    public async Task<string?> GetSubjectAsync(string accessToken)
    {
        var introspection = await _tokenValidator.ValidateTokenAsync(accessToken, "access_token");
        return introspection.Active ? introspection.Sub : null;
    }
}
```

### Revoking a token on logout

```csharp
var result = await _tokenValidator.RevokeTokenAsync(
    refreshToken,
    "refresh_token",
    cancellationToken);

// result.Success is true for every well-formed request, per RFC 7009.
// result.Revoked tells you whether anything was actually revoked.
```

## Logging

`TokenValidator` writes Information-level entries at the start and end of each operation. It logs the `tokenTypeHint` value, never the token itself. Errors during revocation are logged at Error level with the exception and rethrown.

The handlers log additional detail. Introspection failures for malformed or invalid tokens are logged at Error level. Revocation of unknown tokens is logged at Debug level.

## Known limitations

- **`tokenTypeHint` is ignored.** It is logged but does not select a code path. Revocation always tries the refresh-token store first, then the access-token path.
- **`ValidateTokenAsync` does not await.** It is declared `async` but calls the synchronous introspection path.
- **Cancellation is swallowed during revocation.** `TokenRevocationHandler` catches all exceptions, including `OperationCanceledException`, and returns `Success = true, Revoked = false`. A cancelled revocation looks like an unknown token to the caller.
- **The interface summary is out of date.** `ITokenValidator.ValidateTokenAsync` says it "returns the principal if valid", but it returns an `IntrospectionResponse`.
- **Refresh tokens are not introspectable.** Only access tokens (signed JWTs) can be reported active.
- **`ITokenValidator` has no call sites in `src/` yet.** It is registered in `Program.cs` and injected into `UserinfoController`, but no method is invoked there. `TokenController` calls `TokenIntrospectionHandler` and `TokenRevocationHandler` directly.
- **Revoked `jti` entries are held in memory.** `RevokedTokenStore` is process-local, so revocations do not survive a restart and are not shared across instances. This matches the in-memory storage described in `docs/ARCHITECTURE.md`.

## See also

- [ARCHITECTURE.md](ARCHITECTURE.md): overall design and known limitations.
- `src/Handlers/TokenIntrospectionHandler.cs`: JWT validation rules.
- `src/Handlers/TokenRevocationHandler.cs`: revocation flow.
