# TokenIntrospectionHandler

**Namespace:** `DotnetAuthServer.Handlers`

Implements the token introspection logic of OAuth 2.0 Token Introspection (RFC 7662). Given a token string, it validates the JWT's signature, issuer and lifetime, checks the token's `jti` against the revoked-token store, and returns an `IntrospectionResponse` describing whether the token is active and, if so, its scope, client, subject and timing claims.

The handler never throws for an invalid token. Any failure (missing token, malformed JWT, bad signature, expired token, revoked `jti`, unexpected exception) produces an inactive response, so the response does not reveal why a token was rejected. Details are written to the log instead.

`sealed`. Stateless apart from its constructor-time configuration, so a single instance can be shared across requests. Services and handlers are registered as scoped in DI, so it is normally resolved per request.

---

## Constructor

### `TokenIntrospectionHandler(AuthServerOptions options, RevokedTokenStore revokedTokenStore, ILogger<TokenIntrospectionHandler> logger)`

Creates the handler and builds its validation configuration:

| Setting | Value | Source |
|---|---|---|
| Signing key | `SymmetricSecurityKey` from the UTF-8 bytes of `options.JwtSigningKey` (HS256) | `AuthServerOptions.JwtSigningKey` |
| `ValidateIssuerSigningKey` | `true` | - |
| `ValidateIssuer` | `true`, expected issuer is `options.IssuerUrl` | `AuthServerOptions.IssuerUrl` |
| `ValidateAudience` | `false` (audience varies by client) | - |
| `ValidateLifetime` | `true` | - |
| `ClockSkew` | 5 seconds | - |

| Parameter | Type | Description |
|---|---|---|
| `options` | `AuthServerOptions` | Server options supplying the signing key and issuer URL. |
| `revokedTokenStore` | `RevokedTokenStore` | Store consulted for revoked `jti` values. |
| `logger` | `ILogger<TokenIntrospectionHandler>` | Logger for introspection events. |

**Throws:** `ArgumentNullException` if `options`, `revokedTokenStore` or `logger` is `null`.

---

## Methods

### `IntrospectionResponse IntrospectToken(string? token)`

Introspects a token. Never throws for bad input; see the behavior list below.

| Parameter | Type | Description |
|---|---|---|
| `token` | `string?` | The raw token string (typically the access token from a request). |

**Returns:** An `IntrospectionResponse`. Active responses carry claim data; inactive responses have `Active = false` and every other property `null`.

**Behavior, in order:**

1. Logs the token length at debug level. The token value itself is not logged.
2. If `token` is `null`, empty or whitespace, logs a warning and returns `Active = false`.
3. Validates the token with `JwtSecurityTokenHandler.ValidateToken` using the parameters set in the constructor.
4. If the validated token is a `JwtSecurityToken` with a non-empty `jti` that `RevokedTokenStore.IsRevoked` reports as revoked, logs the `jti` and returns `Active = false`.
5. Otherwise builds an active response from the claims (see mapping below) and returns it.
6. Any exception from steps 3 to 5 is logged at error level and results in `Active = false`. A validated token that is not a `JwtSecurityToken` also results in `Active = false`.

**Claim mapping for active responses:**

| `IntrospectionResponse` property | Source |
|---|---|
| `Active` | `true` |
| `Scope` | Value of the first `scope` claim, or `null`. |
| `ClientId` | Value of the first `aud` claim, or `null`. |
| `Username` | Value of the first `sub` claim, or `null`. |
| `TokenType` | Always `"Bearer"`. |
| `Exp` | `ValidTo` converted to Unix seconds (UTC). |
| `Iat` | `IssuedAt` converted to Unix seconds (UTC). |
| `Sub` | Value of the first `sub` claim, or `null`. |

Because lifetime validation is enabled, an active response always has an `exp` claim. `iat` is not required by validation; if it is absent, `Iat` holds the converted value of the default `DateTime`, not a meaningful timestamp.

**Example:**

```csharp
var handler = scope.ServiceProvider.GetRequiredService<TokenIntrospectionHandler>();
IntrospectionResponse result = handler.IntrospectToken(accessToken);

if (result.Active)
{
    Console.WriteLine($"{result.Sub} via {result.ClientId}: {result.Scope}");
}
```

---

## Related Types

### `IntrospectionResponse`

**Namespace:** `DotnetAuthServer.Handlers` (declared in the same file as the handler)

Response model for the introspection endpoint. Mutable POCO with `get; set;` properties. Every property except `Active` is nullable.

| Property | Type | Description |
|---|---|---|
| `Active` | `bool` | `true` if the token is valid, unexpired and not revoked. |
| `Scope` | `string?` | Space-separated scopes from the `scope` claim. |
| `ClientId` | `string?` | Client identifier, taken from the `aud` claim. |
| `Username` | `string?` | Human-readable resource owner identifier. Currently populated from `sub`. |
| `TokenType` | `string?` | Token type; always `"Bearer"` for active responses. |
| `Exp` | `long?` | Expiration time, seconds since the Unix epoch. |
| `Iat` | `long?` | Issue time, seconds since the Unix epoch. |
| `Sub` | `string?` | Subject identifier from the `sub` claim. |

#### `override string ToString()`

Returns a single-line representation listing every property, for example `IntrospectionResponse { Active = True, Scope = read, ClientId = client-1, ... }`. Intended for logging and debugging; do not rely on its exact format.

---

## Usage Notes

- Call `IntrospectToken` from the introspection endpoint after authenticating the calling client. The handler does not authenticate the caller.
- Callers should check `Active` first. Inactive responses are deliberately uninformative.
- `Username` and `Sub` currently hold the same value.
- `ClientId` comes from `aud`, which is the client identifier only when tokens are issued with the client as their audience.
- Audience is not checked, so a token issued for a different resource server is still reported as active unless it is expired, revoked or badly signed.
- Revocation is checked only by `jti`. Tokens without a `jti` cannot be revoked through this path.
- Logs never contain the token value. They include its length, and the `jti` for revoked tokens.

---

## See Also

- [RevokedTokenStore](RevokedTokenStore.md): source of revocation state checked during introspection.
- [TokenRevocationHandler](TokenRevocationHandler.md): records the revocations this handler enforces.
- [TokenValidator](TokenValidator.md): wraps this handler for internal token validation.
- [TokenController](TokenController.md): exposes the introspection endpoint.
- [docs/ARCHITECTURE.md](ARCHITECTURE.md): design overview and known limitations.
