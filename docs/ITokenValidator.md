# ITokenValidator

Namespace: `DotnetAuthServer.Services`

`ITokenValidator` defines token validation, introspection, and revocation operations for OAuth 2.0 and OpenID Connect tokens. The built-in implementation is `TokenValidator`, which delegates introspection to `TokenIntrospectionHandler` and revocation to `TokenRevocationHandler`.

## API

### `Task<IntrospectionResponse> ValidateTokenAsync(string token, string? tokenTypeHint = null)`

Validates a token and returns its active status and available claims as an `IntrospectionResponse`.

The built-in implementation validates JWT signature, issuer, and lifetime and checks whether the token identifier has been revoked. Invalid, expired, malformed, blank, or revoked tokens produce a response whose `Active` property is `false` rather than raising a validation exception. When the token is active, the response can include its scope, client identifier, subject, username, token type, expiration time, and issue time.

#### Parameters

- `token`: The encoded token to validate.
- `tokenTypeHint`: An optional hint describing the token type. The built-in implementation accepts this value for API compatibility and logging but does not use it to select validation behavior.

#### Returns

A task that resolves to an `IntrospectionResponse`. Check `Active` before using any other response properties.

### `IntrospectionResponse IntrospectToken(string token)`

Synchronously introspects a token according to RFC 7662 semantics.

The built-in implementation applies the same JWT validation and revocation checks as `ValidateTokenAsync`. It returns only an inactive response for an invalid token, limiting information disclosure.

#### Parameters

- `token`: The encoded token to introspect.

#### Returns

An `IntrospectionResponse` whose `Active` property indicates whether the token is valid and usable. Claims and token metadata are populated only for an active token.

### `Task<RevocationResult> RevokeTokenAsync(string token, string? tokenTypeHint, CancellationToken cancellationToken)`

Revokes an access token or refresh token using RFC 7009 semantics.

The built-in implementation removes a matching refresh token from storage. For a valid access-token JWT with a token identifier (`jti`), it records that identifier in the revoked-token store until the token expires. To avoid token-enumeration disclosures, an unknown, malformed, blank, already revoked, or otherwise unrevocable token still normally produces a successful result with `Revoked` set to `false`.

#### Parameters

- `token`: The encoded access token or refresh token to revoke.
- `tokenTypeHint`: An optional token-type hint. Callers may use values such as `access_token` or `refresh_token`; the built-in implementation currently attempts refresh-token revocation before access-token revocation regardless of this value.
- `cancellationToken`: A token used to cancel asynchronous repository operations.

#### Returns

A task that resolves to a `RevocationResult`. `Success` indicates whether the request was handled successfully, while `Revoked` indicates whether a token was found and revoked.

Custom implementations may expose failures from their validation or persistence dependencies. The built-in revocation handler catches operational failures and returns a non-disclosing result.

## Usage

Depend on `ITokenValidator` where token operations need to be replaceable or decorated without coupling application code to `TokenValidator`:

```csharp
public sealed class ProtectedResourceService
{
    private readonly ITokenValidator _tokenValidator;

    public ProtectedResourceService(ITokenValidator tokenValidator)
    {
        _tokenValidator = tokenValidator;
    }

    public async Task<bool> IsTokenActiveAsync(string token)
    {
        var response = await _tokenValidator.ValidateTokenAsync(
            token,
            tokenTypeHint: "access_token");

        return response.Active;
    }
}
```

To revoke a token:

```csharp
var result = await tokenValidator.RevokeTokenAsync(
    token,
    tokenTypeHint: "refresh_token",
    cancellationToken);

if (!result.Success)
{
    // Handle an operational failure without revealing token validity.
}
```

Treat token values as credentials: do not write them to logs, exception messages, or persistent storage in plaintext.

## Related Types

- `TokenValidator`: Default implementation of this interface.
- `TokenIntrospectionHandler`: Validates JWTs and creates introspection responses.
- `TokenRevocationHandler`: Revokes refresh tokens and tracks revoked access-token identifiers.
- `IntrospectionResponse`: Reports token activity and the claims exposed by introspection.
- `RevocationResult`: Reports whether a revocation request succeeded and whether a token was actually revoked.
- `RevokedTokenStore`: Tracks revoked access-token identifiers.
