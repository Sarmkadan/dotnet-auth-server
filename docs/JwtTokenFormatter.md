# JwtTokenFormatter

**Namespace:** `DotnetAuthServer.Formatters`

Reads a JWT and returns its header and claims as a `TokenInspection` for debugging and logging. It does not validate anything: signature, expiry, issuer and audience are all taken as written in the token.

> **Warning:** `InspectToken` does not check the signature. Never make an authorization decision from a `TokenInspection`. Use `ITokenValidator` for that.

`sealed`. Registered as scoped in DI (`Program.cs`). The only dependency is the logger.

---

## Constructor

### `JwtTokenFormatter(ILogger<JwtTokenFormatter> logger)`

Stores the logger. The constructor performs no null checks.

| Parameter | Type | Description |
|---|---|---|
| `logger` | `ILogger<JwtTokenFormatter>` | Receives all log output from the formatter. |

---

## Methods

### `TokenInspection? InspectToken(string token)`

Parses `token` with `JwtSecurityTokenHandler.ReadJwtToken` and maps the result to a `TokenInspection`. The method does not check the signature or expiry.

| Parameter | Type | Description |
|---|---|---|
| `token` | `string` | The compact-serialized JWT (`header.payload.signature`). |

**Returns:** A populated `TokenInspection`, or `null` if any of the following holds:

- `token` is `null`, empty or whitespace.
- `JwtSecurityTokenHandler.CanReadToken` returns `false`, meaning the string is not a well-formed JWT.
- Parsing throws. The exception is logged and not rethrown.

**Mapping:**

| `TokenInspection` member | Source |
|---|---|
| `Header.Alg` | Header `alg` |
| `Header.Typ` | Header `typ` |
| `Header.Kid` | Header `kid` |
| `Payload.Subject` | `sub` claim |
| `Payload.Issuer` | `iss` claim |
| `Payload.Audience` | First value of `aud`; `null` if there is none |
| `Payload.IssuedAt` | `iat`, as returned by `JwtSecurityToken.IssuedAt` |
| `Payload.ExpiresAt` | `exp`, as returned by `JwtSecurityToken.ValidTo` |
| `Payload.NotBefore` | `nbf`, as returned by `JwtSecurityToken.ValidFrom` |
| `Payload.Claims` | Every claim, grouped by type |
| `Raw` | The `token` argument, unchanged |

Missing-claim behaviour for the time fields comes from `System.IdentityModel.Tokens.Jwt`. The formatter passes those values through without changing them.

**Logging:**

- Information: `InspectToken called with {Token}`. This logs the **full raw token**.
- Warning: `Token cannot be read as JWT: {Token}`. Also logs the full token.
- Information on success: subject and issuer.
- Error on exception: the exception and the full token.

> **Security:** Tokens are bearer credentials. The Information, Warning and Error entries above all contain the token. Raising the log level does not stop the Warning and Error entries. Before using this class on live tokens, exclude `JwtTokenFormatter` from logs or change the logging.

---

### `string FormatForLogging(TokenInspection inspection)`

Returns a one-line summary of the inspection. The summary includes only `iss`, `sub`, `aud` and `exp`. It omits the header, the other claims and `Raw`.

| Parameter | Type | Description |
|---|---|---|
| `inspection` | `TokenInspection` | The inspection to format. The parameter is non-nullable, but a `null` value is handled at runtime. |

**Returns:**

- `null` input: `"(null token inspection)"`. A warning is logged.
- Otherwise: `JWT{ iss=<Issuer>, sub=<Subject>, aud=<Audience>, exp=<ExpiresAt in ISO 8601 "O" format> }`. A `null` field renders as an empty string.

Example output:

```
JWT{ iss=https://auth.example.com, sub=user-42, aud=api, exp=2026-10-25T12:00:00.0000000Z }
```

Like `InspectToken`, this method logs one Information line on each call.

---

### `override string ToString()`

Returns a fixed placeholder string. It does not reflect the instance or any inspection:

```
JwtTokenFormatter { Header = N/A, Payload = N/A, Raw = N/A, Alg = N/A, Typ = N/A, Kid = N/A }
```

Use `FormatForLogging` to describe a token.

---

## Types

All three types are `sealed` POCOs in `DotnetAuthServer.Formatters`. Every property has a public setter.

### `TokenInspection`

| Property | Type | Default | Description |
|---|---|---|---|
| `Header` | `TokenHeader` | `new()` | JWT header of the token. |
| `Payload` | `TokenPayload` | `new()` | JWT payload of the token. |
| `Raw` | `string` | `string.Empty` | The original token string. |

### `TokenHeader`

| Property | Type | Description |
|---|---|---|
| `Alg` | `string?` | Signing algorithm (`alg`). |
| `Typ` | `string?` | Token type (`typ`). |
| `Kid` | `string?` | Key identifier (`kid`). |

### `TokenPayload`

| Property | Type | Description |
|---|---|---|
| `Subject` | `string?` | `sub` claim. |
| `Issuer` | `string?` | `iss` claim. |
| `Audience` | `string?` | First `aud` value. |
| `IssuedAt` | `DateTime?` | `iat` claim. |
| `ExpiresAt` | `DateTime?` | `exp` claim. |
| `NotBefore` | `DateTime?` | `nbf` claim. |
| `Claims` | `Dictionary<string, List<string>>` | Claim type to all of its values. Default `new()`. |

---

## Usage

### Inspecting a token

```csharp
public sealed class TokenDebugService
{
    private readonly JwtTokenFormatter _formatter;

    public TokenDebugService(JwtTokenFormatter formatter)
    {
        _formatter = formatter;
    }

    public void Describe(string token)
    {
        var inspection = _formatter.InspectToken(token);
        if (inspection is null)
        {
            return;
        }

        var summary = _formatter.FormatForLogging(inspection);
        // summary is e.g. "JWT{ iss=..., sub=..., aud=..., exp=... }"
    }
}
```

### Reading claims

```csharp
if (inspection.Payload.Claims.TryGetValue("roles", out var roles))
{
    foreach (var role in roles)
    {
        // Values come straight from the token and are not validated.
    }
}
```

---

## Notes

- **Nullability:** `InspectToken` returns `null` for unreadable input, so check the result before using it. `Header` and `Payload` are never `null` on a result, but their fields may be.
- **Untrusted data:** Every value in the result comes from the token as sent. Validate it before use.
- **Thread safety:** Each call creates its own `JwtSecurityTokenHandler`, and the class holds no mutable state. An instance can be shared, provided the injected `ILogger` is thread-safe, which the standard implementations are. Do not mutate a `TokenInspection` that is shared between threads.
